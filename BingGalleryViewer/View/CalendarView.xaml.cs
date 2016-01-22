using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using BingGalleryViewer.Utility;
using BingGalleryViewer.ViewModel;
using MahApps.Metro.Controls;

namespace BingGalleryViewer.View
{
	/// <summary>
	/// Interaction logic for CalenderView.xaml
	/// </summary>
	public partial class CalendarView : UserControl
	{

		public static DateTime MaxTime { get { return Setting.GetCurrentSetting().BingDateMax; } }
		public static DateTime MinTime { get { return Setting.GetCurrentSetting().BingDateMin; } }
		private DateTime _currentCalendarTime = CalendarView.MaxTime;
		public DateTime CurrentCalendarTime { get { return _currentCalendarTime; } }

		private DateTime _currentMinTime = MinTime;
		private DateTime _currentMaxTime = MaxTime;

		private List<CalendarDayView> days;
		public CalendarView()
		{
			this.DataContext = new CalendarVM(this.OnIndivisualResultCompleted);
			InitializeComponent();
			this.AddMonthToCalendarTime(0, true);
			if (!VsDesignHelper.IsDesignerHosted)
			{
				this.Loaded += CalendarView_Loaded;
			}
		}

		private void CalendarView_Loaded(object sender, RoutedEventArgs e)
		{
			if (MinTime != _currentMinTime || MaxTime != _currentMaxTime)
			{
				_currentMinTime = MinTime;
				_currentMaxTime = MaxTime;
				this.AddMonthToCalendarTime(0, true);
			}

		}

		private void days_AttachEvents()
		{
			if (days != null)
				foreach (var day in days)
				{
					day.DateSelected += day_DateSelected;
				}
		}

		private void days_DetachEvents()
		{
			if (days != null)
				foreach (var day in days)
				{
					day.DateSelected -= day_DateSelected;
				}
		}

		private void ChangeTransitionDirection(bool isLeft)
		{
			var direction = isLeft ? TransitionType.Left : TransitionType.Right;
			this.YearTransition.Transition = direction;
			this.MonthTransition.Transition = direction;
			this.CalendarTransition.Transition = direction;
		}

		private void OnIndivisualResultCompleted(DateTime date, bool isSuccess, string path)
		{
			if (App.Current != null)
			{
				if (App.Current.Dispatcher.Thread == System.Threading.Thread.CurrentThread)
				{
					var list = days;
					if(list !=null)
					{
						foreach(var day in list)
						{
							if(day.Date==date)
							{
								if (isSuccess) day.SetImageSource(new Uri(path));
								else day.IsInUse = false;
								break;
							}

						}
					}

				}
				else App.Current.Dispatcher.Invoke(() => OnIndivisualResultCompleted(date, isSuccess, path));
			}
		}

		private void AddMonthToCalendarTime(int offsetMonth, bool forceTransition = false)
		{
			this.ChangeTransitionDirection(offsetMonth <= 0);
			var newDate = this.CurrentCalendarTime.AddMonths(offsetMonth).Date;
			if (newDate >= CalendarView.MaxTime) newDate = new DateTime(CalendarView.MaxTime.Year, CalendarView.MaxTime.Month, 1);
			if (newDate <= CalendarView.MinTime) newDate = new DateTime(CalendarView.MinTime.Year, CalendarView.MinTime.Month, 1);
			bool transitionYear = newDate.Year != this.CurrentCalendarTime.Year;
			bool transitionMonth = newDate.Month != this.CurrentCalendarTime.Month;
			bool transitionCalendar = false;
			if (transitionYear || forceTransition)
			{
				transitionCalendar = true;
				this.YearTransition.Content = this.CreateYearDisplay(newDate.Year);
			}
			if (transitionMonth || forceTransition)
			{
				transitionCalendar = true;
				this.MonthTransition.Content = this.CreateMonthDisplay(newDate.Month);
			}
			if (transitionCalendar || forceTransition)
			{
				days_DetachEvents();
				var ctrl = this.CreateCalendarDisplay(newDate, out days);
				if(!VsDesignHelper.IsDesignerHosted)
					((CalendarVM)this.DataContext).SetCurrentDates(days);
				this.CalendarTransition.Content = ctrl;
				days_AttachEvents();
				
			}

			this._currentCalendarTime = newDate;
			this.SetTitleBarButtonsEnableStatus();
		}

		private void day_DateSelected(DependencyObject obj, CalendarDayView.DateSelectedEventArgs args)
		{
			((MainWindow)App.Current.MainWindow).SwitchToView(args.Date);
		}

		private void SetTitleBarButtonsEnableStatus()
		{
			bool canIncrementYear = this.CurrentCalendarTime.Year < CalendarView.MaxTime.Year;
			this.BtnYearIncrement.IsEnabled = canIncrementYear;
			this.BtnMonthIncrement.IsEnabled = canIncrementYear || this.CurrentCalendarTime.Month < CalendarView.MaxTime.Month;

			bool canDecrementYear = this.CurrentCalendarTime.Year > CalendarView.MinTime.Year;
			this.BtnYearDecrement.IsEnabled = canDecrementYear;
			this.BtnMonthDecrement.IsEnabled = canDecrementYear || this.CurrentCalendarTime.Month > CalendarView.MinTime.Month;
		}

		#region events
		private void BtnYearDecrement_Click(object sender, MouseButtonEventArgs e)
		{
			this.AddMonthToCalendarTime(-12);
		}
		private void BtnYearIncrement_Click(object sender, MouseButtonEventArgs e)
		{
			this.AddMonthToCalendarTime(12);
		}
		private void BtnMonthDecrement_Click(object sender, MouseButtonEventArgs e)
		{
			this.AddMonthToCalendarTime(-1);
		}
		private void BtnMonthIncrement_Click(object sender, MouseButtonEventArgs e)
		{
			this.AddMonthToCalendarTime(1);
		}
		#endregion events
	}

	public partial class CalendarView
	{
		private class CalendarDayViewComparer : IComparer<CalendarDayView>
		{
			public int Compare(CalendarDayView x, CalendarDayView y)
			{
				if (x == null || y == null) throw new ArgumentNullException();
				return x.Date.CompareTo(y.Date);
			}
		}
	}
	public static class CalendarViewExtensions
	{
		private static int GetWeekdayStartOffset(DateTime date)
		{
			switch (date.DayOfWeek)
			{
				case DayOfWeek.Sunday: return 0;
				case DayOfWeek.Monday: return 1;
				case DayOfWeek.Tuesday: return 2;
				case DayOfWeek.Wednesday: return 3;
				case DayOfWeek.Thursday: return 4;
				case DayOfWeek.Friday: return 5;
				default: return 6;
			}
		}
		private static int GetWeekdayEndOffset(DateTime date)
		{
			switch (date.DayOfWeek)
			{
				case DayOfWeek.Sunday: return 6;
				case DayOfWeek.Monday: return 5;
				case DayOfWeek.Tuesday: return 4;
				case DayOfWeek.Wednesday: return 3;
				case DayOfWeek.Thursday: return 2;
				case DayOfWeek.Friday: return 1;
				default: return 0;
			}
		}

		private static List<CalendarDayView> CreateWeeks(DateTime date, DateTime minDate, DateTime maxDate, out List<CalendarDayView> days)
		{
			var beginDate = new DateTime(date.Year, date.Month, 1);
			var daysInMonth = DateTime.DaysInMonth(date.Year, date.Month);
			var startOffset = GetWeekdayStartOffset(beginDate);
			var endOffset = GetWeekdayEndOffset(beginDate.AddDays(daysInMonth - 1).Date) + daysInMonth;
			var list = new List<CalendarDayView>();
			days = new List<CalendarDayView>();

			// process begin dates
			for (var x = -startOffset; x < 0; x++)
				list.Add(new CalendarDayView(beginDate.AddDays(x), null, x == -startOffset));

			for (var x = 0; x < daysInMonth; x++)
			{
				var newDay = beginDate.AddDays(x).Date;
				var enable = minDate <= newDay && newDay <= maxDate;
				var ctrl = new CalendarDayView(newDay, enable);
				list.Add(ctrl);
				if (enable) days.Add(ctrl);
			}
			for (var x = daysInMonth; x < endOffset; x++)
				list.Add(new CalendarDayView(beginDate.AddDays(x), null));

			Debug.Assert(list.Count % 7 == 0, "list is not multiple of 7");
			return list;
		}

		public static StackPanel CreateCalendarDisplay(this CalendarView cv, DateTime date, out List<CalendarDayView> days)
		{
			var displayDays = CreateWeeks(date, CalendarView.MinTime, CalendarView.MaxTime, out days);
			var stackPanel = new StackPanel() { Orientation = Orientation.Vertical };
			for (int i = 0; i < displayDays.Count; i += 7)
			{
				var weekPanel = new StackPanel()
				{
					Height = 70,
					Width = 770,
					Orientation = Orientation.Horizontal
				};
				for (int j = 0; j < 7; j++)
					weekPanel.Children.Add(displayDays[i + j]);
				stackPanel.Children.Add(weekPanel);
			}
			return stackPanel;
		}

		private static TextBlock CreateTitleTextBlock(CalendarView cv, string text)
		{
			return new TextBlock()
			{
				VerticalAlignment = VerticalAlignment.Center,
				TextAlignment = TextAlignment.Center,
				FontWeight = FontWeights.Bold,
				Foreground = (Brush)cv.Resources["DateBarTitleBrush"],
				FontSize = 20,
				Text = text,
			};
		}

		public static TextBlock CreateYearDisplay(this CalendarView cv, int year)
		{
			return CreateTitleTextBlock(cv, year.ToString());
		}
		public static TextBlock CreateMonthDisplay(this CalendarView cv, int month)
		{
			return CreateTitleTextBlock(cv, DateTimeFormatInfo.CurrentInfo.GetMonthName(month));
		}
	}

}
