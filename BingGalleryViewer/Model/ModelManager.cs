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
	internal partial class ModelManager
	{
		public delegate void RangedQueryIndivisualResultCallback(DateTime date, bool isSuccess, string path);

		private const int MaxDaysConnection = 8;
		private const int MaxGalleryConnection = 1;

		// extra one for possible download from save.
		public const int RequiredConcurrentWebConnection = MaxDaysConnection + MaxGalleryConnection + 1;

		private static readonly Uri BingBaseUri = new Uri("http://www.bing.com/");

		private static bool _isInitialized = false;
		public static bool IsInitialized { get { return _isInitialized; } }

		private static Datastore _readonlyStore = null;
		public static async Task InitializeAsync()
		{
			if (!IsInitialized)
			{
				var today = DateTime.Now.Date;
				using (var store = new Datastore(true))
				{
					bool minDateSet = false;
					DateTime minDate = today.AddDays(-31).Date;
					var lastRecordedStartDate = await store.ReadImageInfoLastStartdate();

					if (lastRecordedStartDate.HasValue)
					{
						DateTime newMinDate;
						if (BingDataHelper.TryConvertStartdate(lastRecordedStartDate.Value, out newMinDate) && newMinDate >= minDate)
						{
							minDate = newMinDate;
						}
						var tmp = await store.ReadImageInfoFirstStartdate();
						if (tmp.HasValue && BingDataHelper.TryConvertStartdate(tmp.Value, out newMinDate))
						{
							Setting.GetCurrentSetting().SetBingDateMin(newMinDate);
							minDateSet = true;
						}
					}

					if (minDate < today)
					{
						var infos = InitializeAsync_AcquireMissingInfos(ref today, ref minDate);
						await store.SaveDatesAsync(infos);
					}

					if (!minDateSet)
					{
						Setting.GetCurrentSetting().SetBingDateMin(minDate);
						minDateSet = true;
					}
					Setting.GetCurrentSetting().SetBingDateMax(today);
				}
				_readonlyStore = new Datastore();
				_isInitialized = true;
			};
		}

		#region InitializeAsync Helper
		private static BingImageInfo[] InitializeAsync_AcquireMissingInfos(ref DateTime anchorDate, ref DateTime minDate, int retry = 0)
		{
			if (retry > 3) return new BingImageInfo[0];

			var set = new SortedSet<BingImageInfo>(new BingImageInfo.Comparer());
			int startOffset = 0, countOffset = 8;
			int startOffsetMax = (anchorDate - minDate).Days;
			bool isDateShifted = false;
			while (startOffset <= startOffsetMax)
			{
				var count = (startOffsetMax - startOffset) + 1;
				if (count > countOffset) count = countOffset;
				var infos = BingDailyImage.RequestImages(startOffset, count);

				// bing image request has 3 behaviors
				// 1. request fail (infos.count=0)
				// 2. request success and count(infos)=count => valid i and c
				// 3. request success and count(infos)<>count => when count > count(infos), bing returns all valid date.  
				//   otherwise,  count(infos) is the max bing is happy to offer.

				if (infos.Length == 0) break;

				if (infos.Length != count)
				{
					// condition 3
					if (infos.Length > count)
					{
						// bing returned all valid.
						foreach (var info in infos) if (!set.Contains(info)) set.Add(info);
						break; // nothing more to do.
					}

					countOffset = count = infos.Length;
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

			if (isDateShifted && !InitializeAsync_AcquireMissingInfos_TryShiftDate(ref anchorDate, set))
			{
				// something weird happened. let's requery bing image and hope for best.
				return InitializeAsync_AcquireMissingInfos(ref anchorDate, ref minDate, retry + 1);
			}

			if (set.Count() != 0)
			{
				BingDataHelper.TryConvertStartdate(set.Max.StartDate, out anchorDate);
				BingDataHelper.TryConvertStartdate(set.Min.StartDate, out minDate);
			}

			return set.ToArray();
		}


		private static bool InitializeAsync_AcquireMissingInfos_TryShiftDate(ref DateTime anchorDate, SortedSet<BingImageInfo> set)
		{
			// let's see if we need to reanchor
			var infos = BingDailyImage.RequestImages(0, 1);
			if (infos.Length == 1) // only process if bing isn't throwing us in loops.
			{
				DateTime newAnchor;
				if (BingDataHelper.TryConvertStartdate(infos[0].StartDate, out newAnchor) && newAnchor != anchorDate)
				{
					anchorDate = newAnchor;
					// date shift should only occure for one day
					if ((newAnchor - anchorDate).Days == 1)
					{
						// add in case the new date wasn't set.
						if (!set.Contains(infos[0])) set.Add(infos[0]);
					}
					else return false; // shift unsuccessful
				}
				return true;
			}
			return false;
		}
		#endregion InitializeAsync Helper

		public static async Task<BingImageInfo> RequestImageInfoAsync(DateTime date)
		{
			if (!IsInitialized) throw new InvalidOperationException("Not initialized");

			return await _readonlyStore.ReadImageInfoAsync(date);
		}

		public static async Task<BingImageInfo[]> RequestImageInfosAsync(DateTime date1, DateTime date2)
		{

			if (!IsInitialized) throw new InvalidOperationException("Not initialized");
			return await _readonlyStore.ReadImageInfosAsync(date1, date2);
		}

		public static async Task<Tuple<DateTime, string>> RequestImagePathForGalleryAsync(BingImageInfo info)
		{
			if (!IsInitialized) throw new InvalidOperationException("Not initialized");
			if (info == null) throw new ArgumentNullException("info");
			DateTime date;
			BingDataHelper.TryConvertStartdate(info.StartDate, out date);
			var s = await RequestImagePathForGalleryStartRequestAsync(info);
			return Tuple.Create(date, s);
		}

		private static RangeRequestInfo _currentRangeRequestInfo = null;
		public static async Task StartRequestImagePathForCalendarAsync(IEnumerable<DateTime> list, RangedQueryIndivisualResultCallback handler)
		{

			if (!IsInitialized) throw new InvalidOperationException("Not initialized");

			if (list == null) throw new ArgumentNullException("list");
			if (handler == null) throw new ArgumentNullException("handler");
			var currentRangeInfo = _currentRangeRequestInfo;
			if (currentRangeInfo != null) currentRangeInfo.StopTasks();

			_currentRangeRequestInfo = currentRangeInfo = new RangeRequestInfo(handler);

			DateTime maxTime = DateTime.MinValue, minTime = DateTime.MaxValue;
			foreach (var item in list)
			{
				if (maxTime < item.Date) maxTime = item.Date;
				if (minTime > item.Date) minTime = item.Date;
			}
			var infos = await _readonlyStore.ReadImageInfosAsync(minTime, maxTime);

			if (infos != null)
			{
				var llist = new LinkedList<Task>();
				for (int i = 0; i < infos.Length; i++)
				{
					var info = infos[i];
					llist.AddLast(Task.Run(async () => await RequestImagePathForCalendarStartRequestAsync(info, currentRangeInfo)));
					if (llist.Count == 9)
					{
						// limit to 9 running at the same time
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
				await Task.WhenAll(llist);
			}
		}

		#region request helpers

		#region gallery request starter
		private static object galleryRequestLock = new object();
		private static int galleryRequestCount = 0;
		private static WebRequest galleryRequest;
		private static string galleryRequestToken = null;

		private static async Task<string> RequestImagePathForGalleryStartRequestAsync(BingImageInfo info)
		{
			string path = null;
			DateTime date;
			Trace.WriteLine("Started for " + info.StartDate, "ModelManager().RequestImagePathForGalleryStartRequestAsync");
			if (!BingDataHelper.TryConvertStartdate(info.StartDate, out date)) return null;

			await IOAccessControl.WaitForAccessAsync(date);
			try
			{
				if (RequestImagePath_TryFindValidImage(info.StartDate, out path))
					return path;

				galleryRequestToken = info.StartDate; // sets the token for latest date

				while (true)
				{
					lock (galleryRequestLock)
					{
						if (galleryRequestCount == 0) { galleryRequestCount++; break; }
					}
					if (galleryRequestToken != info.StartDate) return null; // only keep looping if we are the latest request
					var request = galleryRequest;
					if (request != null) request.Abort();
					await Task.Yield();
				}

				try
				{
					var fileName = info.StartDate + ".jpg";
					var filePath = Path.Combine(Setting.GetCurrentSetting().TempPath.LocalPath, fileName);
					if (File.Exists(filePath)) File.Delete(filePath);

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

			await IOAccessControl.WaitForAccessAsync(date);

			try
			{

				if (RequestImagePath_TryFindValidImage(info.StartDate, out path))
				{
					rangeInfo.Callback(date, true, path);
					return;
				}

				await calendarRequestSemaphore.WaitAsync();

				try
				{
					if (rangeInfo.IsCancelled) return;
					var fileName = info.StartDate + ".jpg";
					var filePath = Path.Combine(Setting.GetCurrentSetting().TempPath.LocalPath, fileName);
					if (File.Exists(filePath)) File.Delete(filePath);

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

		private static async Task<bool> RequestImagePath_TryBingApiAsync(Uri uri, string path)
		{
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
				galleryRequest = null;
			}

			return false;
		}

		private static async Task<bool> RequestImagePath_TryBingApiAsync(Uri uri, DateTime date, RangeRequestInfo rangeInfo, string path)
		{
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
							// we will downscale image to 800x450
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

		public static void Cleanup()
		{
			if (_readonlyStore != null)
			{
				_readonlyStore.Dispose();
				_readonlyStore = null;
				_isInitialized = false;
			}
		}

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
			catch { }

			try
			{
				if (File.Exists(cachePath) && Setting.GetCurrentSetting().IsUsingCacheHd) File.Copy(cachePath, saveLocation);
				return true;
			}
			catch { }

			// need to redownload the file
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
			catch (Exception) { }
			return false;

		}

		private class RangeRequestInfo
		{
			public RangedQueryIndivisualResultCallback handler { get; private set; }
			public bool IsCancelled { get; private set; }

			private object myLock = new object();
			private Dictionary<DateTime, WebRequest> RequestHash = new Dictionary<DateTime, WebRequest>();
			public RangeRequestInfo(RangedQueryIndivisualResultCallback handler)
			{
				this.handler = handler;
				this.IsCancelled = false;
			}

			public void Callback(DateTime date, bool isSuccess, string path)
			{
				var internalhandler = handler;
				RequestHash.Remove(date);
				if (!IsCancelled && internalhandler != null)
				{
					internalhandler(date, isSuccess, path);
				}
			}

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
			public void SaveRequest(DateTime date, WebRequest request)
			{
				lock (myLock) RequestHash[date] = request;
			}

			public void RemoveRequest(DateTime date)
			{
				lock (myLock) RequestHash.Remove(date);
			}
		}
	}
}
