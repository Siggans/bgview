using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BingGalleryViewer.Utility;
using BingGalleryViewer.ViewModel;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace BingGalleryViewer.View
{
	/// <summary>
	/// Interaction logic for Setting.xaml
	/// </summary>
	public partial class SettingView : UserControl
	{
		public SettingView()
		{
			InitializeComponent();
			this.DataContext = new SettingVM();
		}

		private void Browse_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new CommonOpenFileDialog()
			{
				IsFolderPicker = true,
				InitialDirectory = this.CachePathDisplay.Text,
				DefaultFileName = this.CachePathDisplay.Text,
				
				EnsureValidNames = true,
				EnsurePathExists=true,
				Multiselect=false,
				Title = "Select New Cache Path...",
			};
			var result = dialog.ShowDialog(App.Current.MainWindow);
			if (result == CommonFileDialogResult.Ok)
			{
				try
				{
					if (!Directory.Exists(dialog.FileName))
						Directory.CreateDirectory(dialog.FileName);
					this.CachePathDisplay.Text = dialog.FileName;
				}
				catch { } // silent fail.  can do with some dialog later on.
				
			}


		}
	}
}
