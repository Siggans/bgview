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
			if (handler == null) throw new ArgumentNullException("handler");
			this.handler = handler;
		}
		
		private List<DateTime> _currentDates = new List<DateTime>();
		private HashSet<DateTime> _hash;
		public async void SetCurrentDates(List<CalendarDayView> dayViews)
		{
			_currentDates.Clear();
			_hash = new HashSet<DateTime>();
			var dates = from view in dayViews select view.Date;
			foreach (var date in dates)
			{
				_hash.Add(date);
				_currentDates.Add(date);
			}
			await App.WaitForCurrentAppInitializationAsync();
			await ModelManager.StartRequestImagePathForCalendarAsync(_currentDates, RequestSuccessHandler);
		}

		private void RequestSuccessHandler(DateTime date, bool isSuccess, string path)
		{
			var hash = _hash; // just to make sure we dont corrupt _hash due to async/await. a few uncatched return is fine.

			if (hash.Contains(date)) handler(date, isSuccess, path);
			
		}

	}
}
