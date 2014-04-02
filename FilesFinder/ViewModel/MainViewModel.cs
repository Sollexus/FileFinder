using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using FilesFinder.Model;
using FilesFinder.Service;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Threading;

namespace FilesFinder.ViewModel
{
	public class MainViewModel : ViewModelBase
	{
	    private readonly IErrorLoggingService _errorLoggingService;
	    private readonly IFileReadingService _fileService;
		private List<FileModel> _fileData;
	    private bool _isBusy;

	    private string _folderPath;
	    private FileModel _rootFileModel;
	    private ObservableCollection<string> _errors = new ObservableCollection<string>();

	    public ObservableCollection<string> Errors {
            get { return _errors; }
	        set {
	            _errors = value;
                RaisePropertyChanged("Errors");
	        }
	    }

		public IEnumerable<FileModel> FileData {
			get { return _fileData ?? (_fileData = new List<FileModel> {RootFileModel}); }
		} 

	    public FileModel RootFileModel
		{
			get { return _rootFileModel; }
			set {
				_rootFileModel = value; 
				RaisePropertyChanged("RootFileModel"); 
			}
		}

	    public string FolderPath
		{
			get { return _folderPath; }
			set
			{
				_folderPath = value;
				RaisePropertyChanged("FolderPath");
				Search.RaiseCanExecuteChanged();
			}
		}

		private string _xmlFilePath;
		public string XmlFilePath
		{
			get { return _xmlFilePath; }
			set
			{
				_xmlFilePath = value;
				RaisePropertyChanged("XmlFilePath");
			}
		}

		public MainViewModel(IErrorLoggingService errorLoggingService, IFileReadingService fileReadingService)
		{
			Search = new RelayCommand(ExecuteSearch, CanExecuteSearch);

		    _errorLoggingService = errorLoggingService;
		    _fileService = fileReadingService;
		    //error logging thread
		    Task.Factory.StartNew(() => {
		        while (true) {
		            string error;
		            while ((error = _errorLoggingService.GetNextError()) != null) {
			            var unclosuredError = error;
			            DispatcherHelper.RunAsync(() => Errors.Add(unclosuredError));
		            }

			        Thread.Sleep(1000);    
		        }
		    });
		}

		#region "Search command"

		public RelayCommand Search { get; private set; }

		private void ExecuteSearch()
		{
			_fileService.Start(_folderPath);

			//this thread reads info from filesystem thread to build a tree
			_isBusy = true;
			var treeTask = Task.Factory.StartNew(() =>
			{
				try
				{
					BuildFileTree();
				}
				catch (InvalidOperationException ex)
				{
					_errorLoggingService.LogError(ex.Message);
				}
				finally
				{
					RaisePropertyChanged("RootFileModel");
					RaisePropertyChanged("FileData");
				}
			});

			//thread that writes folder's content info to the xml file
			var xmlTask = Task.Factory.StartNew(() =>
			{
				try
				{
					WriteToXmlFile();
				}
				catch (Exception ex)
				{
					_errorLoggingService.LogError(ex.Message);
				}
			});

			Task.Factory.StartNew(() => {
				Task.WaitAll(treeTask, xmlTask);
				_isBusy = false;
				Search.RaiseCanExecuteChanged();
			});
		}

		private void WriteToXmlFile() {
			var filePath = String.IsNullOrEmpty(XmlFilePath) ? Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\FolderContents.xml" : XmlFilePath;

			if (!File.Exists(filePath)) File.Create(filePath).Close();
			var folderLine = "<Folder Name=\"{0}\">\n";
			var folderClosingTag = "</Folder>\n";
			var fileLine = "<File Name=\"{0}\" CreationDate=\"{1}\" LastAccessDate=\"{2}\" Size=\"{3}\" Owner=\"{4}\"/>\n ";

			Func<FileModel, int> getFileIndent = (FileModel fM) => {
				var n = 0;
				while (fM.Parent != null) {
					n++;
					fM = fM.Parent;
				}
				return n;
			};

			File.WriteAllText(filePath, "");

			FileModel curParent = null;
			var lastIndent = 0;
			
			while (true)
			{
				var curFileModel = _fileService.XmlFiles.Take();
				var curFileIndent = getFileIndent(curFileModel);

				if (curParent == null) 
				{
					curParent = curFileModel;
				}

				if (curFileIndent < lastIndent) {
					File.AppendAllText(filePath, new String('\t', getFileIndent(curParent)) + folderClosingTag);
					curParent = curFileModel;
				}

				lastIndent = curFileIndent;

				if (!curFileModel.IsFile) {
					File.AppendAllText(filePath, new String('\t', getFileIndent(curFileModel)) + string.Format(folderLine, curFileModel.Name));
				}
				else {
					var info = new FileInfo(curFileModel.FilePath);

					File.AppendAllText(filePath, new String('\t', getFileIndent(curFileModel)) + string.Format(fileLine, 
						curFileModel.Name, info.CreationTime, info.LastAccessTime, info.Length, 
						File.GetAccessControl(curFileModel.FilePath).GetOwner(typeof(System.Security.Principal.NTAccount))));
				}
			}
		}

		//builds a tree to show at ui
		private void BuildFileTree()
		{
			while (true)
			{
				var curFileModel = _fileService.Files.Take();

				if (curFileModel.Parent != null) {
					if (curFileModel.Parent.Children == null)
						curFileModel.Parent.Children = new List<FileModel>();
					curFileModel.Parent.Children.Add(curFileModel);
				}
				else {
					_fileData = new List<FileModel> { (_rootFileModel = curFileModel) };
				}

				RaisePropertyChanged("RootFileModel");
				RaisePropertyChanged("FileData");
			}
		}

		private bool CanExecuteSearch()
		{
			return Directory.Exists(FolderPath) && (!_isBusy);
		}

		#endregion
	}
}