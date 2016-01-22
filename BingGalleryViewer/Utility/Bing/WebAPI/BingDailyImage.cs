using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace BingGalleryViewer.Utility.Bing.WebAPI
{
	internal class BingDailyImage
	{
		private const string BingUrl = "http://www.bing.com";
		private const string ImageAPI =
			"http://www.bing.com/HPImageArchive.aspx?format=js&idx={0}&n={1}&mkt={2}";
		private const string MarketUS = "en-US";
		private const int MaxBacktrackDate = 18;

		public static BingImageInfo[] RequestImages(int i, int n)
		{
			if (i < 0 ) throw new ArgumentException("i >= 0");
			if (n < 1) throw new ArgumentException("n >=1");
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
			catch { }
			return new BingImageInfo[0];

		}
	}
}
