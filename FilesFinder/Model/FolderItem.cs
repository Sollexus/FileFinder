using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FilesFinder.Model
{
    //basicly Composite pattern to store information about folder's content
	[XmlInclude(typeof(FileFolderItem))]
	[XmlRoot("Folder")]
	public class FolderItem
	{
		[XmlArrayItem(Type = typeof(FolderItem), ElementName = "Folder"),
		XmlArrayItem(Type = typeof(FileFolderItem), ElementName = "File")]
		public List<FolderItem> Children { get; set; }
		[XmlAttribute]
		public string Name { get; set; }
		[XmlIgnore]
		public string FilePath { get; set; }
		[XmlIgnore]
		public FolderItem Parent { get; set; }
	}
	
	public class FileFolderItem : FolderItem
	{
		[XmlAttribute]
		public string CreationTime { get; set; }
		[XmlAttribute]
		public string LastAccessTime { get; set; }
		[XmlAttribute]
		public string Length { get; set; }
		[XmlAttribute]
		public string Owner { get; set; }
	}
}
