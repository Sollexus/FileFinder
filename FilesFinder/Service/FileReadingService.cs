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
            Files = new BlockingCollection<FileModel>();
            XmlFiles = new BlockingCollection<FileModel>();
			_task = Task.Factory.StartNew(() =>
			{
				_ct.ThrowIfCancellationRequested();

				if (!Directory.Exists(dirPath)) throw new FileReadingException("Directory doesn't exists");
				
				var parentDir = new FileModel {Name = Path.GetFileName(dirPath), FilePath = dirPath};
				Files.Add(parentDir, _ct);
				XmlFiles.Add(parentDir, _ct);
				FileSearchInternal(parentDir, dirPath);
				Files.CompleteAdding();
				XmlFiles.CompleteAdding();
			}, _ct);
		}

		private void FileSearchInternal(FileModel parentDir, string dirPath)
		{
			if (_ct.IsCancellationRequested)
			{
				_ct.ThrowIfCancellationRequested();
			}

			try
			{
				foreach (var dir in Directory.GetDirectories(dirPath))
				{
					var fileModel = new FileModel {Name = Path.GetFileName(dir), Parent = parentDir, FilePath = dir};
					Files.Add(fileModel, _ct);
					XmlFiles.Add(fileModel, _ct);
					FileSearchInternal(fileModel, dir);
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
					var fileModel = new FileModel { Name = Path.GetFileName(file), Parent = parentDir, IsFile = true, FilePath = file};
					Files.Add(fileModel, _ct);
					XmlFiles.Add(fileModel, _ct);
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

		public BlockingCollection<FileModel> Files { get; private set; }
		public BlockingCollection<FileModel> XmlFiles { get; private set; }
	}
}