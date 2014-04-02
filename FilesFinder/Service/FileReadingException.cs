using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FilesFinder.Service
{
	public class FileReadingException: Exception
	{
		public FileReadingException(string message): base(message){ }
	}
}
