using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FilesFinder.Model;

namespace FilesFinder.Service
{
	public interface IFileReadingService
	{
		void Start(string dirPath);
		void Stop();
		BlockingCollection<FolderItem> Files { get; }
		BlockingCollection<FolderItem> XmlFiles { get; }
	}
}
