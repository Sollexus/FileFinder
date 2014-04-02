using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FilesFinder.Model;

namespace FilesFinder.Service
{
    //this thread iterates folder's content recursively
	public class FileReadingService : IFileReadingService
	{
	    private readonly IErrorLoggingService _errorLoggingService;
	    private Task _task;
		private readonly CancellationTokenSource _tokenSource;
		private CancellationToken _ct;

		public FileReadingService(IErrorLoggingService errorLoggingService)
		{
		    _errorLoggingService = errorLoggingService;
		    _tokenSource = new CancellationTokenSource();
			 _ct = _tokenSource.Token;
		}

		public void Start(string dirPath)
		{
            Files = new BlockingCollection<FolderItem>();
            XmlFiles = new BlockingCollection<FolderItem>();
			_task = Task.Factory.StartNew(() =>
			{
				_ct.ThrowIfCancellationRequested();

				if (!Directory.Exists(dirPath)) throw new FileReadingException("Directory doesn't exists");
				
				var parentDir = new FolderItem {Name = Path.GetFileName(dirPath), FilePath = dirPath};
				var xmlParentDir = new FolderItem { Name = Path.GetFileName(dirPath), FilePath = dirPath };
				Files.Add(parentDir, _ct);
				XmlFiles.Add(xmlParentDir, _ct);
				FileSearchInternal(parentDir, xmlParentDir, dirPath);
				Files.CompleteAdding();
				XmlFiles.CompleteAdding();
			}, _ct);
		}

		private void FileSearchInternal(FolderItem parentDir, FolderItem xmlParentDir, string dirPath)
		{
			if (_ct.IsCancellationRequested)
			{
				_ct.ThrowIfCancellationRequested();
			}

			try
			{
				foreach (var dir in Directory.GetDirectories(dirPath))
				{
					var fileModel = new FolderItem {Name = Path.GetFileName(dir), Parent = parentDir, FilePath = dir};
					var xmlFileModel = new FolderItem {Name = Path.GetFileName(dir), Parent = xmlParentDir, FilePath = dir};
					Files.Add(fileModel, _ct);
					XmlFiles.Add(xmlFileModel, _ct);
					FileSearchInternal(fileModel, xmlFileModel, dir);
				}
			}
			catch (Exception ex)
			{
				_errorLoggingService.LogError(ex.Message);
			}

			try
			{
				foreach (var file in Directory.GetFiles(dirPath))
				{
					var fileModel = new FileFolderItem { Name = Path.GetFileName(file), Parent = parentDir, FilePath = file};
					var xmlFileModel = new FileFolderItem {Name = Path.GetFileName(file), Parent = xmlParentDir, FilePath = file};
					Files.Add(fileModel, _ct);
					XmlFiles.Add(xmlFileModel, _ct);
				}
			}
			catch (Exception ex)
			{
				_errorLoggingService.LogError(ex.Message);
			}
		}

		public void Stop()
		{
			_tokenSource.Cancel();
		}

		public BlockingCollection<FolderItem> Files { get; private set; }
		public BlockingCollection<FolderItem> XmlFiles { get; private set; }
	}
}