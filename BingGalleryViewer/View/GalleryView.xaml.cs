using System;
using System.Collections.Generic;
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
using MahApps.Metro.Controls;

namespace BingGalleryViewer.View
{
	/// <summary>
	/// Interaction logic for GalleryView.xaml
	/// </summary>
	public partial class GalleryView : UserControl
	{
		public static DateTime MaxTime { get { return Setting.GetCurrentSetting().BingDateMax; } }
		public static DateTime MinTime { get { return Setting.GetCurrentSetting().BingDateMin; } }


		public static DependencyProperty CurrentCalenderTimeProperty = DependencyProperty.Register(
			"CurrentCalendarTime",
			typeof(DateTime),
			typeof(GalleryView), new PropertyMetadata(DateTime.MinValue));


		public DateTime CurrentCalendarTime
		{
			get { return (DateTime)this.GetValue(CurrentCalenderTimeProperty); }
			set { SetCurrentCalendarTime(value, true); }
		}

		private void SetCurrentCalendarTime(DateTime date, bool modifyUI = false)
		{
			if (modifyUI)
			{
				var span = date - CurrentCalendarTime;
				AddDayToCurrentTime(span.Days, true);
			}
			else
			{
				this.SetValue(CurrentCalenderTimeProperty, date.Date);
			}
		}

		public static DependencyProperty ErrorMessageProperty = DependencyProperty.Register(
			"ErrorMessage",
			typeof(string),
			typeof(GalleryView), new PropertyMetadata(null, new PropertyChangedCallback(ErrorMessagePropertyChangedCallback)));

		private static void ErrorMessagePropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var view = d as GalleryView;
			if (view != null && view.PictureFrameControl!=null)
			{
				var pv = view.PictureFrameControl.Content as PictureView;
				if (pv != null && !pv.IsImageLoaded && e.NewValue is string)
				{
					pv.SetCaption(e.NewValue as string);
				}
			}
		}

		public string ErrorMessage
		{
			get { return this.GetValue(ErrorMessageProperty) as string; }
			set { this.SetValue(ErrorMessageProperty, value); }
		}

		public static DependencyProperty ImagePathProperty = DependencyProperty.Register(
			"ImagePath",
			typeof(string),
			typeof(GalleryView), new PropertyMetadata(null, new PropertyChangedCallback(ImagePathPropertyChangedCallback)));

		private static void ImagePathPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var view = d as GalleryView;
			if (view != null && view.PictureFrameControl != null)
			{
				var pv = view.PictureFrameControl.Content as PictureView;
				if (pv != null && !pv.IsImageLoaded && e.NewValue is string)
				{
					pv.ImageSourceUri = new Uri(e.NewValue as string);
				}
			}
		}

		public string ImagePath
		{
			get { return this.GetValue(ImagePathProperty) as string; }
			set { this.SetValue(ImagePathProperty, value); }
		}

		public GalleryView()
		{
			this.DataContext = new GalleryVM();
			InitializeComponent();
			this.Loaded += GalleryView_Loaded;
			this.CurrentCalendarTime = DateTime.Now.Date;
		}

		private void GalleryView_Loaded(object sender, RoutedEventArgs e)
		{
			if(this.PictureFrameControl.Content is PictureView)
			{
				var view = this.PictureFrameControl.Content as PictureView;
				if (view.ImageSourceUri != null && view.ImageSourceUri.OriginalString == this.ImagePath) return;
			}
			this.AddDayToCurrentTime(0, true);
		}

		private void AddDayToCurrentTime(int days, bool force = false)
		{
			this.SetTitleBarButtonsEnableStatus(true);
			this.PictureFrameControl.Transition = days >= 0 ? TransitionType.Down : TransitionType.Up;
			var newDate = this.CurrentCalendarTime.AddDays(days).Date;
			if (newDate >= GalleryView.MaxTime) newDate = MaxTime.Date;
			if (newDate <= GalleryView.MinTime) newDate = MinTime.Date;
			bool shouldTransition = newDate != CurrentCalendarTime || force;
			if (shouldTransition)
			{
				this.EnablePauseFrame(true);
				this.PictureFrameControl.Content = new PictureView();
				this.SetCurrentCalendarTime(newDate);
				this.EnablePauseFrame(false);
			}
			this.SetTitleBarButtonsEnableStatus();

		}

		private void EnablePauseFrame(bool isEnable)
		{
			this.PauseFrameCaption.Visibility = isEnable ? Visibility.Visible : Visibility.Collapsed;
			this.PauseFrameCover.Visibility = isEnable ? Visibility.Visible : Visibility.Collapsed;
		}

		private void SetTitleBarButtonsEnableStatus(bool isDisable = false)
		{
			if (isDisable)
			{
				this.BtnIncrement.IsEnabled = false;
				this.BtnDecrement.IsEnabled = false;
				return;
			}

			this.BtnIncrement.IsEnabled = this.CurrentCalendarTime < MaxTime;
			this.BtnDecrement.IsEnabled = this.CurrentCalendarTime > MinTime;
		}

		private void BtnDecrement_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			AddDayToCurrentTime(-1);
		}

		private void BtnIncrement_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			AddDayToCurrentTime(1);
		}

	}
}
