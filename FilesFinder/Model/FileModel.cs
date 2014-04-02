using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FilesFinder.Model
{
    //basicly Composite pattern to store information about folder's content
	public class FileModel
	{
		public List<FileModel> Children { get; set; }
		public string Name { get; set; }
		public string FilePath { get; set; }
		public FileModel Parent { get; set; }
		public bool IsFile { get; set; }
	}
}
