using System;
using System.Collections.Generic;
using System.Globalization;
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

namespace BingGalleryViewer.View
{
	/// <summary>
	/// Interaction logic for CalendarDayView.xaml
	/// </summary>
	public partial class CalendarDayView : UserControl
	{
		public class DateSelectedEventArgs : EventArgs
		{
			public readonly DateTime Date;
			public DateSelectedEventArgs(DateTime date)
			{
				this.Date = date.Date;
			}
		}

		private static readonly Brush WeekendBrush = new SolidColorBrush(Colors.LightBlue);
		private static readonly Brush WeekdayBrush = new SolidColorBrush(Color.FromRgb(0xDA, 0xF0, 0xF7));
		private static readonly Brush InUseBrush = new SolidColorBrush(Colors.White);
		private static readonly Brush NotInUseBrush = new SolidColorBrush(Color.FromRgb(0xB0, 0xB0, 0xB0));
		private static readonly Brush DisabledBrush = new SolidColorBrush(Colors.Black);

		private static string GetShortMonthName(int month)
		{
			return DateTimeFormatInfo.InvariantInfo.GetAbbreviatedMonthName(month);
		}

		public delegate void DateSelectedEventHandler(DependencyObject obj, DateSelectedEventArgs args);
		public event DateSelectedEventHandler DateSelected;


		private bool _isInUse = true;
		public bool IsInUse
		{
			get { return _isInUse; }
			set
			{
				if (!value && IsInUse)
				{
					_isInUse = false;
					this.Cover.Fill = CalendarDayView.NotInUseBrush;
				}
			}
		}

		public readonly DateTime Date;

		public bool HasImage { get { return (this.PictureFrame.Children.Count != 0); } }

		public CalendarDayView()
			: this(DateTime.Now, true, true) { }

		public CalendarDayView(DateTime date, bool? isInUse = true, bool shouldDisplayMonth = false)
		{
			InitializeComponent();

			this.Date = date.Date;
			this._isInUse = isInUse.HasValue ? isInUse.Value : false;
			if (this.Date.Day == 1) shouldDisplayMonth = true;

			this.Cover.Fill = isInUse.HasValue ?
				(isInUse.Value ? CalendarDayView.InUseBrush : CalendarDayView.NotInUseBrush)
				: CalendarDayView.DisabledBrush;
			this.Background = this.Date.DayOfWeek == DayOfWeek.Sunday || this.Date.DayOfWeek == DayOfWeek.Saturday
				? CalendarDayView.WeekendBrush : CalendarDayView.WeekdayBrush;
			this.DateDisplay.Text = shouldDisplayMonth ? this.Date.ToString("MMM dd") : this.Date.ToString("dd");
			if (!isInUse.HasValue) this.DateDisplay.Foreground = new SolidColorBrush(Color.FromRgb(0x50, 0x50, 0x50));
			else if(IsInUse)
			{
				this.PictureFrame.Width = 80;
				this.PictureFrame.Height = 45;
				this.Cover.Opacity = .3;
			}
		}

		public void SetImageSource(Uri uri)
		{
			if (uri != null && uri.IsFile && !this.HasImage)
			{
				var ctrl = new PictureView()
				{
					ImageHeight = 57,
					ImageWidth = 100,
					ImageSourceUri = uri,
				};
				this.PictureFrame.Children.Add(ctrl);
				this.PictureFrameBorder.BorderBrush = DisabledBrush;
			}
		}

		#region events
		private void CalendarDayView_MouseEnter(object sender, MouseEventArgs e)
		{
			if (this.IsInUse)
			{
				this.PictureFrame.Width = 100;
				this.PictureFrame.Height = 57;
				this.Cover.Opacity = 0;
			}
		}

		private void CalendarDayView_MouseLeave(object sender, MouseEventArgs e)
		{
			if (this.IsInUse)
			{
				this._clickStarted = false;
				this.PictureFrame.Width = 80;
				this.PictureFrame.Height = 45;
				this.Cover.Opacity = .3;
			}
		}

		private bool _clickStarted = false;
		private void CalendarDayView_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (this.IsInUse)
			{
				this._clickStarted = true;
			}
		}

		private void CalendarDayView_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (this.IsInUse && this._clickStarted)
			{
				this._clickStarted = false;
				// let control know that date is clicked
				if (this.DateSelected != null)
				{
					this.DateSelected(this, new DateSelectedEventArgs(this.Date));
				}
			}

		}
		#endregion events
	}
}
