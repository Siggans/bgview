using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace BingGalleryViewer.Utility.Bing
{
	/// <summary>
	/// Bing's Json reply model
	/// </summary>
	[DataContract]
	public class BingJson
	{
		/// <summary>
		/// List of the image infos.
		/// </summary>
		[DataMember(IsRequired = true, Name = "images")]
		public BingImageInfo[] Images { get; set; }
	}

	/// <summary>
	/// Bing's daily image data.
	/// </summary>
	[DataContract]
	public class BingImageInfo
	{

		/// <summary>
		/// Date Image is released
		/// </summary>
		[DataMember(IsRequired = true, Name = "startdate")]
		public string StartDate { get; set; }

		/// <summary>
		/// url to image
		/// </summary>
		[DataMember(IsRequired = true, Name = "url")]
		public string Url { get; set; }

		/// <summary>
		/// host name
		/// </summary>
		[DataMember(IsRequired = true, Name = "urlbase")]
		public string UrlBase { get; set; }

		/// <summary>
		/// Copyright flavor text
		/// </summary>
		[DataMember(IsRequired = true, Name = "copyright")]
		public string Copyright { get; set; }

		/// <summary>
		/// Link to Bing description of the image 
		/// </summary>
		[DataMember(IsRequired = true, Name = "copyrightlink")]
		public string CopyrightLink { get; set; }

		/// <summary>
		/// Compare image data for organizing the infos by their dates.
		/// </summary>
		public class Comparer : IComparer<BingImageInfo>
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
