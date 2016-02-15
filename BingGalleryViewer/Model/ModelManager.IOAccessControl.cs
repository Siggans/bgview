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
		private class IOAccessControl
		{

			private static Dictionary<DateTime, SemaphoreSlim> SemaphoreDict = new Dictionary<DateTime, SemaphoreSlim>();

			private static SemaphoreSlim GetSemaphoreUnsafe(DateTime date)
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

			public static async Task WaitForAccessAsync(DateTime date )
			{
				date = date.Date;
				var semaphore = GetSemaphoreUnsafe(date);
				await semaphore.WaitAsync();
			}

			public static void ReleaseAccess(DateTime date)
			{
				date = date.Date;
				if (!SemaphoreDict.ContainsKey(date)) throw new InvalidOperationException("GainControlAsync was not called");
				SemaphoreDict[date.Date].Release();
				
			}
		}
	}
}
