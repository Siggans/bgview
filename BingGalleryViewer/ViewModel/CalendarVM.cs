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
	/// <summary>
	/// View model for calender view
	/// </summary>
	internal class CalendarVM : ViewModelBase
	{

		// handler for image path callback
		private ModelManager.RangedQueryIndivisualResultCallback handler;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="handler">handler for image path result callback</param>
		public CalendarVM(ModelManager.RangedQueryIndivisualResultCallback handler)
		{
			if (handler == null) throw new ArgumentNullException("handler");
			this.handler = handler;
		}
		
		// list of dates currently displayd by view
		private List<DateTime> _currentDates = new List<DateTime>();
		// hashed set for values of the dates
		private HashSet<DateTime> _hash;

		/// <summary>
		/// Sets the dates that this view model currently represents
		/// </summary>
		/// <param name="dayViews"></param>
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
			// ensure that app is properly initialized before calling model manager
			await App.WaitForCurrentAppInitializationAsync();
			await ModelManager.StartRequestImagePathForCalendarAsync(_currentDates, RequestSuccessHandler);
		}

		// helper to filter result.  if the returned result are not dates that are currently
		// represented by the view model,  ignore the information
		private void RequestSuccessHandler(DateTime date, bool isSuccess, string path)
		{
			var hash = _hash; // just to make sure we dont corrupt _hash due to async/await. a few uncatched return is fine.

			if (hash.Contains(date)) handler(date, isSuccess, path);
			
		}

	}
}
