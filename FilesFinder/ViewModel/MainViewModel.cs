using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Serialization;
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
		private List<FolderItem> _fileData;
	    private bool _isBusy;

	    private string _folderPath;
	    private FolderItem _rootFolderItem;
	    private ObservableCollection<string> _errors = new ObservableCollection<string>();

	    public ObservableCollection<string> Errors {
            get { return _errors; }
	        set {
	            _errors = value;
                RaisePropertyChanged("Errors");
	        }
	    }

		public IEnumerable<FolderItem> FileData {
			get { return _fileData ?? (_fileData = new List<FolderItem> {RootFolderItem}); }
		} 

	    public FolderItem RootFolderItem
		{
			get { return _rootFolderItem; }
			set {
				_rootFolderItem = value; 
				RaisePropertyChanged("RootFolderItem"); 
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
					RaisePropertyChanged("RootFolderItem");
					RaisePropertyChanged("FileData");
				}
			});

			//thread that writes folder's content info to the xml file
			var xmlTask = Task.Factory.StartNew(WriteXmlDocument);

			Task.Factory.StartNew(() => {
				Task.WaitAll(treeTask, xmlTask);
				_isBusy = false;
				Search.RaiseCanExecuteChanged();
			});
		}

		private void WriteXmlDocument()
		{
			FolderItem rootModel = null;

			//building a tree to serialize
			try
			{
				while (true)
				{
					var curFileModel = _fileService.XmlFiles.Take();

					var curFile = (curFileModel as FileFolderItem);
					if (curFile != null) {
						var info = new FileInfo(curFile.FilePath);

						curFile.CreationTime = info.CreationTime.Date.ToString();
						curFile.LastAccessTime = info.LastAccessTime.Date.ToString();
						curFile.Length = info.Length.ToString();
						curFile.Owner = File.GetAccessControl(curFileModel.FilePath)
							.GetOwner(typeof (System.Security.Principal.NTAccount)).ToString();
					}

					if (curFileModel.Parent != null)
					{
						if (curFileModel.Parent.Children == null)
							curFileModel.Parent.Children = new List<FolderItem>();
						curFileModel.Parent.Children.Add(curFileModel);
					}
					else
					{
						rootModel = curFileModel;
					}
				}
			}
			catch (Exception ex)
			{
				_errorLoggingService.LogError(ex.Message);
			}

			var filePath = String.IsNullOrEmpty(XmlFilePath) ? Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\FolderContents.xml" : XmlFilePath;

			if (!File.Exists(filePath)) File.Create(filePath).Close();

			var ser = new XmlSerializer(typeof(FolderItem));

			using (var myWriter = new StreamWriter(filePath)) {
				try {
					ser.Serialize(myWriter, rootModel);
				}
				catch (Exception ex) 
				{
					_errorLoggingService.LogError(ex.Message);
				}
				finally {
					myWriter.Close();
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
						curFileModel.Parent.Children = new List<FolderItem>();
					curFileModel.Parent.Children.Add(curFileModel);
				}
				else {
					_fileData = new List<FolderItem> { (_rootFolderItem = curFileModel) };
				}

				RaisePropertyChanged("RootFolderItem");
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