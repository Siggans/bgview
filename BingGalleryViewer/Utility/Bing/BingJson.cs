using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace BingGalleryViewer.Utility.Bing
{
	[DataContract]
	public class BingJson
	{
		[DataMember(IsRequired=true, Name="images")]
		public BingImageInfo[] Images { get; set; }
	}

	[DataContract]
	public class BingImageInfo
	{
		[DataMember(IsRequired = true, Name = "startdate")]
		public string StartDate { get; set; }

		[DataMember(IsRequired=true, Name = "url")]
		public string Url { get; set; }

		[DataMember(IsRequired=true, Name="urlbase")]
		public string UrlBase { get; set; }

		[DataMember(IsRequired=true, Name="copyright")]
		public string Copyright { get; set; }

		[DataMember(IsRequired = true, Name = "copyrightlink")]
		public string CopyrightLink { get; set; }

		public class Comparer :IComparer<BingImageInfo>
		{

			public int Compare(BingImageInfo x, BingImageInfo y)
			{
				if (x == null || y == null) throw new ArgumentNullException();
				if (x.StartDate == null || y.StartDate == null) throw new ArgumentNullException("StartDate null value");
				if (x == y) return 0;
				if (x.StartDate.Length > y.StartDate.Length) return 1;
				if (x.StartDate.Length < y.StartDate.Length) return -1;
				return x.StartDate.CompareTo(y.StartDate);
			}
		}


	}
}
