using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BingGalleryViewer.Model
{
	internal partial class ModelManager
	{
		/// <summary>
		/// Controls the IO access to harddrive for any given date.
		/// </summary>
		private class IOAccessControl
		{

			// date tracker
			private static Dictionary<DateTime, SemaphoreSlim> SemaphoreDict = new Dictionary<DateTime, SemaphoreSlim>();

			//  Retrieve Semaphore
			private static SemaphoreSlim GetSemaphore(DateTime date)
			{

				if (!SemaphoreDict.ContainsKey(date))
					lock (SemaphoreDict)
					{
						if (!SemaphoreDict.ContainsKey(date))
						{
							SemaphoreDict[date] = new SemaphoreSlim(1);
						}
					}

				return SemaphoreDict[date];
			}

			/// <summary>
			/// Get access to IO for a particular image of the date, or wait till access is granted
			/// </summary>
			/// <param name="date">date of the image access to get</param>
			/// <returns>task to wait</returns>
			public static async Task WaitForAccessAsync(DateTime date )
			{
				date = date.Date;
				var semaphore = GetSemaphore(date);
				await semaphore.WaitAsync();
			}

			/// <summary>
			/// Release control of the IO for a particular date
			/// </summary>
			/// <param name="date">date to release</param>
			public static void ReleaseAccess(DateTime date)
			{
				date = date.Date;
				if (!SemaphoreDict.ContainsKey(date)) throw new InvalidOperationException("WaitForAccessAsync was not called");
				SemaphoreDict[date.Date].Release();
				
			}
		}
	}
}
