using GalaSoft.MvvmLight.Threading;

namespace FilesFinder
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App
	{
        static App() { DispatcherHelper.Initialize();}
	}
}
