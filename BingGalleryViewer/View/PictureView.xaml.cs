using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using MahApps.Metro.Controls;

namespace BingGalleryViewer.View
{

	/// <summary>
	/// Interaction logic for PictureView.xaml
	/// </summary>
	public partial class PictureView : UserControl
	{
		public static readonly DependencyProperty ImageSourceUriProperty = DependencyProperty.Register(
			"ImageSourceUri",
			typeof(Uri),
			typeof(PictureView),
			new PropertyMetadata(null, new PropertyChangedCallback(ImageSourceUriChangedCallback)));

		private static void ImageSourceUriChangedCallback(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			var ctrl = obj as PictureView;
			if (ctrl != null)
			{
				ctrl.LoadImage();
			}
		}

		private static FrameworkElement GenerateDisplay(string text)
		{
			var stackPanel = new StackPanel()
			{
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
			};
			stackPanel.Children.Add(new TextBlock()
			{
				Text = text,
				TextAlignment = TextAlignment.Center,
				FontSize = 20,
			});
			return stackPanel;
		}

		public Uri ImageSourceUri
		{
			get { return this.GetValue(PictureView.ImageSourceUriProperty) as Uri; }
			set
			{
				if (this.ImageSourceUri == null)
				{
					if (value != null && value.IsFile)
					{
						this.SetValue(PictureView.ImageSourceUriProperty, value);
					}
				}
			}
		}

		private int _imageHeight = 0;
		public int ImageHeight
		{
			get { return _imageHeight == 0 ? (int)this.ActualHeight : this._imageHeight; }
			set { this._imageHeight = value; }
		}
		private int _imageWidth = 0;
		public int ImageWidth
		{
			get { return _imageWidth == 0 ? (int)this.ActualWidth : this._imageWidth; }
			set { this._imageWidth = value; }
		}
		public bool IsImageLoaded { get { return this.Content is Image; } }

		private object _defaultXamlCtrl = null;

		public PictureView()
		{
			InitializeComponent();
			this._defaultXamlCtrl = this.Content;
			this.Loaded += PictureView.PictureView_Loaded;
			this.Unloaded += PictureView.PictureView_Unloaded;
		}
		public void SetCaption(string s) { this.Content = GenerateDisplay(s); }

		private void LoadImage()
		{
			if ((!IsImageLoaded) && this.ImageSourceUri != null)
			{
				BitmapImage tmpImg = null;
				try
				{
					using (var stream = new FileStream(this.ImageSourceUri.LocalPath, FileMode.Open, FileAccess.Read, FileShare.Read))
					{

						tmpImg = new BitmapImage();
						tmpImg.DecodeFailed += this.BitmapImage_DecodeFailed;
						tmpImg.BeginInit();
						tmpImg.StreamSource = stream;
						tmpImg.DecodePixelHeight = this.ImageHeight;
						tmpImg.DecodePixelWidth = this.ImageWidth;
						tmpImg.CreateOptions = BitmapCreateOptions.None;
						tmpImg.CacheOption = BitmapCacheOption.OnLoad;
						tmpImg.EndInit();
					}
				}
				catch (Exception e)
				{
					Debug.WriteLine(this.GetType().FullName + ".LoadImageAsync => Exception Failed to load image: " + e.Message);
					tmpImg.DecodeFailed -= this.BitmapImage_DecodeFailed;
					tmpImg = null;
				}


				if (tmpImg != null)
				{
					this.Content = new Image() { Source = tmpImg };
					tmpImg.DecodeFailed -= this.BitmapImage_DecodeFailed;
				}
				else this.SetCaption("Error opening image...");
			}
		}

		private void BitmapImage_DecodeFailed(object sender, ExceptionEventArgs e)
		{
			if (Application.Current != null && Application.Current.Dispatcher.Thread != System.Threading.Thread.CurrentThread)
			{
				Application.Current.Dispatcher.Invoke(() => { BitmapImage_DecodeFailed(sender, e); });
			}
			else
			{
				Debug.WriteLine(this.GetType().FullName + ".BitmapImage_DecodeFailed => Decode failed on " + this.ImageSourceUri.LocalPath);
				if (this.Content is Image) this.SetCaption("Error opening image...");

			}
		}

		private static void PictureView_Loaded(object sender, RoutedEventArgs e)
		{
			var dp = sender as PictureView;
			if (dp != null && dp.ImageSourceUri != null && !dp.IsImageLoaded)
			{
				dp.LoadImage();
			}
		}

		private static void PictureView_Unloaded(object sender, RoutedEventArgs e)
		{
			var dp = sender as PictureView;
			if (dp != null)
			{
				if (dp.IsImageLoaded)
				{
					((Image)dp.Content).Source = null;

					dp.Content = dp._defaultXamlCtrl;
				}
			}
		}


	}
}
