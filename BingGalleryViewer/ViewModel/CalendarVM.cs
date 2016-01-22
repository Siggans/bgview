using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BingGalleryViewer.Model;
using BingGalleryViewer.View;
using VMBase.ViewModel;

namespace BingGalleryViewer.ViewModel
{
	internal class CalendarVM : ViewModelBase
	{

		private ModelManager.RangedQueryIndivisualResultCallback handler;

		public CalendarVM(ModelManager.RangedQueryIndivisualResultCallback handler)
		{
			this.handler = handler;
		}
		
		private List<DateTime> _currentDates = new List<DateTime>();
		public void SetCurrentDates(List<CalendarDayView> dayViews)
		{
			_currentDates.Clear();
			var dates = from view in dayViews select view.Date;
			foreach (var date in dates)
			{
				_currentDates.Add(date);
			}
			ModelManager.StartRequestImagePathForCalendar(_currentDates, handler);
		}

	}
}
