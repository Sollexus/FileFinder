using System.Collections.Generic;
using System.Windows;
using System.Windows.Forms;
using FilesFinder.Model;
using FilesFinder.ViewModel;
using Microsoft.Win32;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace FilesFinder
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow
	{
		public MainWindow()
		{
			InitializeComponent();
		}

	    private MainViewModel ViewModel { get { return (MainViewModel) DataContext; }}

		private void LoadFile_Click(object sender, RoutedEventArgs e)
		{
			using (var dialog = new FolderBrowserDialog()) {
				if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				{
					ViewModel.FolderPath = dialog.SelectedPath;
				}
			}
		}

		private void ChooseXmlFile_Click(object sender, RoutedEventArgs e) {
			var dialog = new SaveFileDialog {DefaultExt = ".xml", Filter = "Xml file|*.xml", FileName = ""};

			if (dialog.ShowDialog().HasValue) {
				ViewModel.XmlFilePath = dialog.FileName;
			}
		}
	}
}
