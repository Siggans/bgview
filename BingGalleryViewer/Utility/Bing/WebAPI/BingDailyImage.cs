using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace BingGalleryViewer.Utility.Bing.WebAPI
{
	/// <summary>
	/// Logic for interacting with Bing's daily image API.
	/// </summary>
	internal class BingDailyImage
	{
		// host.
		private const string BingUrl = "http://www.bing.com";
		// query string
		private const string ImageAPI =
			"http://www.bing.com/HPImageArchive.aspx?format=js&idx={0}&n={1}&mkt={2}";
		// desired market.  Currently US only
		private const string MarketUS = "en-US";

		// max allowed dates that can be queried
		private const int MaxBacktrackDate = 18;

		/// <summary>
		/// Perform Bing api call to retrieve date
		/// </summary>
		/// <param name="i">offset from today where the query return starts.  if i=1,  the date is today -1.</param>
		/// <param name="n">number of days to request</param>
		/// <returns>image info retrieved from bing</returns>
		/// <exception cref="ArgumentException"> parameter must satisfy the following: i>=0, n>=1</exception>
		public static BingImageInfo[] RequestImages(int i, int n)
		{
			if (i < 0) throw new ArgumentException("i >= 0");
			if (n < 1) throw new ArgumentException("n >= 1");
			string requestUrl = string.Format(ImageAPI, i, n, MarketUS);
			var request = WebRequest.Create(requestUrl);
			try
			{
				using (var response = request.GetResponse())
				using (var stream = response.GetResponseStream())
				{
					var serializer = new DataContractJsonSerializer(typeof(BingJson));
					var bingJson = serializer.ReadObject(stream) as BingJson;
					if (bingJson != null && bingJson.Images != null)
					{
						return bingJson.Images;
					}
				}

			}
			catch (Exception e) { Trace.WriteLine(e.Message); }
			return new BingImageInfo[0];

		}
	}
}
