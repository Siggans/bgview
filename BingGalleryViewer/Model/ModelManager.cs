using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BingGalleryViewer.Utility;
using BingGalleryViewer.Utility.Bing;
using BingGalleryViewer.Utility.Bing.WebAPI;
using BingGalleryViewer.Utility.SQLite;

namespace BingGalleryViewer.Model
{
	/// <summary>
	/// Logic layer for interacting with database ans end user
	/// </summary>
	internal partial class ModelManager
	{
		/// <summary>
		/// Delegate function for handing returned path for data saved associated with date
		/// </summary>
		/// <param name="date">the date value the data is associatd with</param>
		/// <param name="isSuccess">true when data save is successful</param>
		/// <param name="path">location to data that is stored on harddrive</param>
		public delegate void RangedQueryIndivisualResultCallback(DateTime date, bool isSuccess, string path);

		// number of concurrent connection allowed to pull image with in Calander view
		private const int MaxDaysConnection = 8;
		// number of concurrent connection allowed to pull image with in Gallery view
		private const int MaxGalleryConnection = 1;

		
		// extra connection for possible download from save.
		public const int RequiredConcurrentWebConnection = MaxDaysConnection + MaxGalleryConnection + 1;

		// bing host url
		private static readonly Uri BingBaseUri = new Uri("http://www.bing.com/");

		
		/// <summary>
		/// True when ModelManager is initialized and ready to be used
		/// </summary>
		public static bool IsInitialized { get { return _isInitialized; } }
		private static bool _isInitialized = false;

		// Readonly database
		private static Datastore _readonlyStore = null;

		/// <summary>
		/// Starts ModelManager initialization.  Needs to be called before ModelManager may be used.
		/// </summary>
		/// <returns>async task</returns>
		public static async Task InitializeAsync()
		{
			// call once
			if (!IsInitialized)
			{
				var today = DateTime.Now.Date;
				using (var store = new Datastore(true))
				{
					bool isMinDateSet = false;

					// get the minimum dates to query to 31 days before today.
					DateTime queryMinDate = today.AddDays(-31).Date;  

					// retrieve latest date recorded on database
					var lastRecordedStartDate = await store.ReadImageInfoLastStartdate();

					if (lastRecordedStartDate.HasValue)
					{
						// convert the retrieve int value to date.
						DateTime newMinDate;
						if (BingDataHelper.TryConvertStartdate(lastRecordedStartDate.Value, out newMinDate) && newMinDate >= queryMinDate)
						{
							// set the minum query date to the day past the latest of the valid record.
							queryMinDate = newMinDate.Date;
						}

						// get earliest date of the recorded date and save it to program setting if available.
						var tmp = await store.ReadImageInfoFirstStartdate();
						if (tmp.HasValue && BingDataHelper.TryConvertStartdate(tmp.Value, out newMinDate))
						{
							Setting.GetCurrentSetting().SetBingDateMin(newMinDate);
							isMinDateSet = true;
						}
					}

					if (queryMinDate <= today)
					{
						// try to get the date range from Bing and store them on database.
						var infos = InitializeAsync_AcquireMissingInfos(ref today, ref queryMinDate);
						await store.SaveDatesAsync(infos);
					}

					if (!isMinDateSet)
					{
						// if mindate is not retrieved from datastore.  set it using the retrieved value from Bing's webservice
						Setting.GetCurrentSetting().SetBingDateMin(queryMinDate);
						isMinDateSet = true;
					}
					Setting.GetCurrentSetting().SetBingDateMax(today);
				}

				// configuration complete.  Open a readonly datastore interface.
				_readonlyStore = new Datastore();
				_isInitialized = true;
			};
		}

		#region InitializeAsync Helper
		// Contacts Bing's image web services and try to retrieve the date range as provided by
		// the inputs.
		// Returns a list of image models for the dates queried.
		private static BingImageInfo[] InitializeAsync_AcquireMissingInfos(ref DateTime anchorDate, ref DateTime minDate, int retry = 0)
		{
			if (retry > 3) return new BingImageInfo[0];

			var set = new SortedSet<BingImageInfo>(new BingImageInfo.Comparer());
			int startOffset = 0, countMax = 8;
			int startOffsetMax = (anchorDate - minDate).Days;
			bool isDateShifted = false;
			while (startOffset <= startOffsetMax)
			{
				var count = (startOffsetMax - startOffset) + 1;
				if (count > countMax) count = countMax;
				var infos = BingDailyImage.RequestImages(startOffset, count);

				// bing image request has 3 behaviors
				// 1. request fail (infos.count=0)
				// 2. request success and count(infos)=count => valid startOffset and count for RequestImages
				// 3. request success and count(infos)<>count => when count > count(infos), bing returns all valid date.  
				//   otherwise,  count(infos) is the max number of days Bing is returing per service call.

				if (infos.Length == 0) break; // condition 1.  assum query fails

				if (infos.Length != count)
				{
					// condition 3
					if (infos.Length > count)
					{
						// bing returned all valid.
						foreach (var info in infos) if (!set.Contains(info)) set.Add(info);
						break; // task complete
					}

					countMax = count = infos.Length;
					// this essentially makes count(infos)<count into condition 2.
				}

				// condition 2. 
				foreach (var info in infos)
				{
					if (!set.Contains(info)) set.Add(info);
					else isDateShifted = true; // repeat date.  should not occure unless the firstDate is shifted.
				}
				startOffset += count;
			}

			// check if date change occured while performing api calls.
			if (isDateShifted && !InitializeAsync_AcquireMissingInfos_TryShiftDate(ref anchorDate, set))
			{
				// something weird happened. let's requery bing image and hope for best.
				return InitializeAsync_AcquireMissingInfos(ref anchorDate, ref minDate, retry + 1);
			}

			if (set.Count() != 0)
			{
				// set the min and max date of this call for caller.
				BingDataHelper.TryConvertStartdate(set.Max.StartDate, out anchorDate);
				BingDataHelper.TryConvertStartdate(set.Min.StartDate, out minDate);
			}
			else
			{
				// no data retrieved.. let's requery 
				return InitializeAsync_AcquireMissingInfos(ref anchorDate, ref minDate, retry + 1);
			}

			return set.ToArray();
		}

		// Attempt to account for possible missing days from result returned by Bing
		private static bool InitializeAsync_AcquireMissingInfos_TryShiftDate(ref DateTime anchorDate, SortedSet<BingImageInfo> set)
		{
			// let's see if we need to reanchor
			var infos = BingDailyImage.RequestImages(0, 1);
			if (infos.Length == 1) // sanity check, anything not in one day while calling for this operation will fail
			{
				DateTime newAnchor;
				if (BingDataHelper.TryConvertStartdate(infos[0].StartDate, out newAnchor))
				{
					if (newAnchor != anchorDate)
					{
						anchorDate = newAnchor;
						// date shift should only occure for one day.  Any other case, we will fail by default.
						if ((newAnchor - anchorDate).Days == 1)
						{
							// add in case the new date wasn't set.
							if (!set.Contains(infos[0])) set.Add(infos[0]);
							return true;
						}
					}
				}
			}
			return false; // failed to shift
		}
		#endregion InitializeAsync Helper

		/// <summary>
		/// Request image model from datastore
		/// </summary>
		/// <param name="date">date of the image to request</param>
		/// <returns>image info model</returns>
		public static async Task<BingImageInfo> RequestImageInfoAsync(DateTime date)
		{
			if (!IsInitialized) throw new InvalidOperationException("Not initialized");

			return await _readonlyStore.ReadImageInfoAsync(date);
		}

		/// <summary>
		/// Request image model from datastore with a range of days
		/// </summary>
		/// <param name="date1">beginning date, may swap with date2</param>
		/// <param name="date2">ending date, may swap with date1</param>
		/// <returns></returns>
		public static async Task<BingImageInfo[]> RequestImageInfosAsync(DateTime date1, DateTime date2)
		{

			if (!IsInitialized) throw new InvalidOperationException("Not initialized");
			return await _readonlyStore.ReadImageInfosAsync(date1, date2);
		}

		/// <summary>
		/// Query and save the image with the model provided in info, and return the saved path
		/// </summary>
		/// <param name="info">image info model</param>
		/// <returns>a tuple of the date of the image, and path to image data on local harddrive</returns>
		public static async Task<Tuple<DateTime, string>> RequestImagePathForGalleryAsync(BingImageInfo info)
		{
			if (!IsInitialized) throw new InvalidOperationException("Not initialized");
			if (info == null) throw new ArgumentNullException("info");
			DateTime date;
			BingDataHelper.TryConvertStartdate(info.StartDate, out date);
			var s = await RequestImagePathForGalleryStartRequestAsync(info);
			return Tuple.Create(date, s);
		}

		// current information request used by calendar
		private static RangeRequestInfo _currentRangeRequestInfo = null;

		/// <summary>
		///	Query and save the image with the dates provided in the list and return the saved path via handler
		/// </summary>
		/// <param name="list">list of dates to get info with</param>
		/// <param name="handler">callback for all the dates between the earliest and latest dates from list</param>
		/// <returns>async task</returns>
		public static async Task StartRequestImagePathForCalendarAsync(IEnumerable<DateTime> list, RangedQueryIndivisualResultCallback handler)
		{

			if (!IsInitialized) throw new InvalidOperationException("Not initialized");

			if (list == null) throw new ArgumentNullException("list");
			if (handler == null) throw new ArgumentNullException("handler");

			// stop previous task if one is stil running
			var currentRangeInfo = _currentRangeRequestInfo;
			if (currentRangeInfo != null) currentRangeInfo.StopTasks();

			// create a new info for the current request
			_currentRangeRequestInfo = currentRangeInfo = new RangeRequestInfo(handler);

			DateTime maxTime = DateTime.MinValue, minTime = DateTime.MaxValue;
			foreach (var item in list)
			{
				if (maxTime < item.Date) maxTime = item.Date;
				if (minTime > item.Date) minTime = item.Date;
			}

			// retrieve dates from datastore
			var infos = await _readonlyStore.ReadImageInfosAsync(minTime, maxTime);

			if (infos != null)
			{
				// run each date as a separate task
				var llist = new LinkedList<Task>();
				for (int i = 0; i < infos.Length; i++)
				{
					var info = infos[i];
					llist.AddLast(Task.Run(async () => await RequestImagePathForCalendarStartRequestAsync(info, currentRangeInfo)));
					if (llist.Count == MaxDaysConnection)
					{
						// limit to MaxDaysConnections running at the same time.  Clear out tasks that are complete 
						await Task.WhenAny(llist);
						var walker = llist.First;
						while (walker != null)
						{
							if (walker.Value.IsCompleted)
							{
								var marked = walker;
								walker = walker.Next;
								llist.Remove(marked);
							}
							else walker = walker.Next;
						}
					}
				}
				// wait till all tasks are complete
				await Task.WhenAll(llist);
			}
		}

		#region request helpers

		#region gallery request starter
		private static object galleryRequestLock = new object();
		private static int galleryRequestCount = 0;
		private static WebRequest galleryRequest;
		private static string galleryRequestToken = null;

		// Gallery image request helper.  Returns the path to saved date.
		private static async Task<string> RequestImagePathForGalleryStartRequestAsync(BingImageInfo info)
		{
			string path = null;
			DateTime date;
			Trace.WriteLine("Started for " + info.StartDate, "ModelManager().RequestImagePathForGalleryStartRequestAsync");
			
			if (!BingDataHelper.TryConvertStartdate(info.StartDate, out date)) return null;

			// request access token to  ensure that there are no conflict IO access for the same date.
			await IOAccessControl.WaitForAccessAsync(date);
			try
			{
				// check if a valid mage exist before attempting to retrieve image online
				if (RequestImagePath_TryFindValidImage(info.StartDate, out path))
					return path;

				galleryRequestToken = info.StartDate; // sets the token for latest date

				// spinlock for the previous gallery call to terminate
				while (true)
				{
					lock (galleryRequestLock)
					{
						if (galleryRequestCount == 0) { galleryRequestCount++; break; }
					}
					if (galleryRequestToken != info.StartDate) return null; // only spin when we are the latest request
					var request = galleryRequest;
					if (request != null) request.Abort();
					await Task.Yield();
				}

				try
				{
					var fileName = info.StartDate + ".jpg";
					var filePath = Path.Combine(Setting.GetCurrentSetting().TempPath.LocalPath, fileName);
					if (File.Exists(filePath)) File.Delete(filePath); // delete the invalid image

					// save image to temp path and to local cache location.
					// see if we can grab the 1080 source.  if failed, try to get the 1366 source
					if (await RequestImagePath_TryBingApiAsync(new Uri(BingBaseUri, info.Url), filePath))
					{
						if (RequestImagePath_TryFindValidImage(info.StartDate, out path))
							return path;
					}
					else if (!info.Url.ToLower().EndsWith("_1366x768.jpg"))
					{
						if (await RequestImagePath_TryBingApiAsync(new Uri(BingBaseUri, info.UrlBase + "_1366x768.jpg"), filePath))
						{
							if (RequestImagePath_TryFindValidImage(info.StartDate, out path))
								return path;
						}
					}

				}
				finally
				{
					galleryRequest = null;

					Debug.Assert(galleryRequestCount == 1);

					// release lock
					galleryRequestCount--;

				}
				return null;
			}
			finally
			{
				IOAccessControl.ReleaseAccess(date);
			}
		}

		#endregion gallery request starter

		#region calendar request starter
		private static SemaphoreSlim calendarRequestSemaphore = new SemaphoreSlim(MaxDaysConnection);

		// Request Image path for calander
		private static async Task RequestImagePathForCalendarStartRequestAsync(BingImageInfo info, RangeRequestInfo rangeInfo)
		{
			DateTime date;
			string path = null;
			Trace.WriteLine("Started for " + info.StartDate, "ModelManager().RequestImagePathForCalendarStartRequestAsync");
			if (!BingDataHelper.TryConvertStartdate(info.StartDate, out date))
			{
				rangeInfo.Callback(date, false, null);
				return;
			}

			// request access token to  ensure that there are no conflict IO access for the same date.
			await IOAccessControl.WaitForAccessAsync(date);

			try
			{

				// check if a valid mage exist before attempting to retrieve image online
				if (RequestImagePath_TryFindValidImage(info.StartDate, out path))
				{
					rangeInfo.Callback(date, true, path);
					return;
				}

				// wait for a free connection to open up.
				await calendarRequestSemaphore.WaitAsync();

				try
				{
					if (rangeInfo.IsCancelled) return;
					var fileName = info.StartDate + ".jpg";
					var filePath = Path.Combine(Setting.GetCurrentSetting().TempPath.LocalPath, fileName);
					if (File.Exists(filePath)) File.Delete(filePath); // delete the invalid image

					// save image to temp path and to local cache location.
					// see if we can grab the 1080 source.  if failed, try to get the 1366 source
					if (await RequestImagePath_TryBingApiAsync(new Uri(BingBaseUri, info.Url), date, rangeInfo, filePath))
					{
						if (rangeInfo.IsCancelled) return;
						if (RequestImagePath_TryFindValidImage(info.StartDate, out path))
						{
							rangeInfo.Callback(date, true, path);
							return;
						}
					}
					else if (!info.Url.ToLower().EndsWith("_1366x768.jpg"))
					{
						if (await RequestImagePath_TryBingApiAsync(new Uri(BingBaseUri, info.UrlBase + "_1366x768.jpg"), date, rangeInfo, filePath))
						{
							if (rangeInfo.IsCancelled) return;
							if (RequestImagePath_TryFindValidImage(info.StartDate, out path))
							{
								rangeInfo.Callback(date, true, path);
								return;
							}
						}
					}
				}
				catch (Exception e)
				{
					// any exception cause the thread to fail
					Trace.WriteLine(e.Message, "ModelManager().RequestImagePathForCalendarStartRequestAsync Exception");
				}
				finally
				{
					rangeInfo.RemoveRequest(date);
					calendarRequestSemaphore.Release();
				}
				if (!rangeInfo.IsCancelled) rangeInfo.Callback(date, false, null);
			}
			finally
			{
				IOAccessControl.ReleaseAccess(date);
			}
		}

		#endregion calendar request starter

		// Attempt to pull image data from Bing for gallery
		private static async Task<bool> RequestImagePath_TryBingApiAsync(Uri uri, string path)
		{

			// save request so that it may be cancelled from another thread
			galleryRequest = WebRequest.CreateHttp(uri);
			try
			{
				using (var response = await galleryRequest.GetResponseAsync())
				using (var stream = response.GetResponseStream())
				{
					try
					{
						using (var image = Image.FromStream(stream, true, true))
						{
							// save the image as source.
							image.Save(path, ImageFormat.Jpeg);
						}
						return true;
					}
					catch (Exception) { }
				}
			}
			catch (WebException e)
			{
				// only consume exception where response isn't canceled
				if (e.Status == WebExceptionStatus.RequestCanceled) throw e;
				galleryRequest = null;
			}

			return false;
		}

		// Attempt to pull image data from Bing for calendar
		private static async Task<bool> RequestImagePath_TryBingApiAsync(Uri uri, DateTime date, RangeRequestInfo rangeInfo, string path)
		{
			// save request so that it may be cancelled from another thread
			var request = WebRequest.CreateHttp(uri);
			rangeInfo.SaveRequest(date, request);
			try
			{
				using (var response = await request.GetResponseAsync())
				using (var stream = response.GetResponseStream())
				{
					try
					{
						using (var image = Image.FromStream(stream, true, true))
						{
							image.Save(path, ImageFormat.Jpeg);
						}
						return true;
					}
					catch (Exception) { }
				}
			}
			catch (WebException e)
			{
				// only consume case where response isn't canceled
				if (e.Status == WebExceptionStatus.RequestCanceled) throw e;
			}

			return false;
		}

		// Look through cache and temp path and attempt to locate a valid image to load
		private static bool RequestImagePath_TryFindValidImage(string p, out string path)
		{
			p += ".jpg";
			string cachePath = Path.Combine(Setting.GetCurrentSetting().CachePath.LocalPath, p);
			string tempPath = Path.Combine(Setting.GetCurrentSetting().TempPath.LocalPath, p);
			// check cache path.
			if (RequestImagePath_TryFindValidImage_CheckCache(cachePath)) { path = cachePath; return true; }

			// check tmp path.
			if (File.Exists(tempPath))
			{
				try
				{
					using (var image = Bitmap.FromFile(tempPath) as Image)
					{
						if (!Setting.GetCurrentSetting().IsUsingCache) { path = tempPath; return true; }

						// need to save to local cache;
						if (!Setting.GetCurrentSetting().IsUsingCacheHd)
						{
							// we will downscale image to 800x450 for low rez save
							RequestImagePath_TryFindValidImage_DownscaleImageAndSave(image, cachePath);
						}
						else
						{
							image.Save(cachePath, ImageFormat.Jpeg);
						}
						path = cachePath;
						return true;

					}
				}
				catch { }

			}
			path = null;
			return false;
		}

		// perform quality adjustment for downscaling image downloaded from web.
		private static void RequestImagePath_TryFindValidImage_DownscaleImageAndSave(Image image, string path)
		{
			var rect = new Rectangle(0, 0, 800, 450);
			using (var bitmap = new Bitmap(800, 450))
			{
				bitmap.SetResolution(image.HorizontalResolution, image.VerticalResolution);
				using (var graphics = Graphics.FromImage(bitmap))
				using (var attribute = new ImageAttributes())
				{
					graphics.CompositingMode = CompositingMode.SourceCopy;
					graphics.CompositingQuality = CompositingQuality.HighQuality;
					graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
					graphics.SmoothingMode = SmoothingMode.HighQuality;
					graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
					attribute.SetWrapMode(WrapMode.TileFlipXY);
					graphics.DrawImage(image, rect, 0, 0, 800, 450, GraphicsUnit.Pixel, attribute);
				}
				bitmap.Save(path, ImageFormat.Jpeg);
			}


		}

		//  Helper method to check image in cache.  return false if image is missing or image is not HQ
		private static bool RequestImagePath_TryFindValidImage_CheckCache(string path)
		{
			if (Setting.GetCurrentSetting().IsUsingCache && File.Exists(path))
			{
				if (!Setting.GetCurrentSetting().IsUsingCacheHd) return true;  // won't try to downscale cache picture.

				// check picture quality before returning
				try
				{
					using (var image = Image.FromFile(path))
					{
						if (image.Width >= 1366) return true;
					}
				}
				catch { }

			}
			return false;
		}

		#endregion download helpers

		/// <summary>
		/// perform cleanup tasks before program exits.
		/// </summary>
		public static void Cleanup()
		{
			if (_readonlyStore != null)
			{
				_readonlyStore.Dispose();
				_readonlyStore = null;
				_isInitialized = false;
			}
		}

		/// <summary>
		/// Save the bing image from date to a local location.
		/// </summary>
		/// <param name="info">image info model</param>
		/// <param name="saveLocation">file path to save to</param>
		/// <returns>true if successful</returns>
		public static async Task<bool> SaveFileAsync(BingImageInfo info, string saveLocation)
		{

			if (!IsInitialized) throw new InvalidOperationException("Not initialized");

			string fileName = info.StartDate + ".jpg";
			string cachePath = Path.Combine(Setting.GetCurrentSetting().CachePath.LocalPath, fileName);
			string tempPath = Path.Combine(Setting.GetCurrentSetting().TempPath.LocalPath, fileName);

			try
			{
				if (File.Exists(tempPath)) File.Copy(tempPath, saveLocation);
				return true;
			}
			catch (Exception e) { Trace.WriteLine(e.Message); }

			// file error, test next

			try
			{
				if (File.Exists(cachePath) && Setting.GetCurrentSetting().IsUsingCacheHd) File.Copy(cachePath, saveLocation);
				return true;
			}
			catch (Exception e) { Trace.WriteLine(e.Message); }

			// still error, last try: redownload the file
			var request = WebRequest.CreateHttp(new Uri(BingBaseUri, info.Url));
			try
			{
				using (var response = await request.GetResponseAsync())
				using (var stream = response.GetResponseStream())
				using (var fin = new FileStream(saveLocation, FileMode.OpenOrCreate, FileAccess.Write))
				{
					await stream.CopyToAsync(fin);
					return true;
				}
			}
			catch (Exception e) { Trace.WriteLine(e.Message); }
			return false;

		}

		/// <summary>
		/// Helper class for keeping track of the callback handler, list of days to retrieve, and 
		/// for cancelling tasks that are running.
		/// </summary>
		private class RangeRequestInfo
		{
			/// <summary>
			/// Callback handler for the request
			/// </summary>
			public RangedQueryIndivisualResultCallback handler { get; private set; }
			/// <summary>
			/// Check if the current request is being cancelled
			/// </summary>
			public bool IsCancelled { get; private set; }

			// mutex for RequestHash
			private object myLock = new object();
			// request tracker
			private Dictionary<DateTime, WebRequest> RequestHash = new Dictionary<DateTime, WebRequest>();

			/// <summary>
			/// Constructor
			/// </summary>
			/// <param name="handler">callback handler for the current thread</param>
			public RangeRequestInfo(RangedQueryIndivisualResultCallback handler)
			{
				this.handler = handler;
				this.IsCancelled = false;
			}

			/// <summary>
			/// perfrom callback to inform the the request process
			/// </summary>
			/// <param name="date">date of the request</param>
			/// <param name="isSuccess">request status</param>
			/// <param name="path">file path to the image</param>
			public void Callback(DateTime date, bool isSuccess, string path)
			{
				var internalhandler = handler;
				RequestHash.Remove(date);
				if (!IsCancelled && internalhandler != null)
				{
					internalhandler(date, isSuccess, path);
				}
			}

			/// <summary>
			/// Cancel all tasks handled by this instance
			/// </summary>
			public void StopTasks()
			{
				this.handler = null;
				this.IsCancelled = true;
				lock (myLock)
				{
					foreach (var request in RequestHash.Values) if (request != null) request.Abort();
					RequestHash.Clear();
				}
			}

			/// <summary>
			/// Once request is made, save and track the request.  Needs to be performed if the
			/// request is to be allowed to cancel.
			/// </summary>
			/// <param name="date">desired date of the request</param>
			/// <param name="request"> the web request for image data</param>
			public void SaveRequest(DateTime date, WebRequest request)
			{
				lock (myLock) RequestHash[date] = request;
			}

			/// <summary>
			/// Inform the instance to stop tracking the request for the input date
			/// </summary>
			/// <param name="date">date to stop tracking</param>
			public void RemoveRequest(DateTime date)
			{
				lock (myLock) RequestHash.Remove(date);
			}
		}
	}
}
