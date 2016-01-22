using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BingGalleryViewer.Utility.Bing
{
	internal class BingDataHelper
	{
		public static bool TryConvertStartdate(string s, out int intDate)
		{
			return Int32.TryParse(s, out intDate);
		}

		public static bool TryConvertStartdate(DateTime date, out int intDate)
		{
			if(date != DateTime.MinValue)
			{
				intDate = date.Year * 10000 + date.Month * 100 + date.Day;
				return true;
			}
			intDate = 0;
			return false;
		}

		public static bool TryConvertStartdate(string s, out DateTime date)
		{
			int intDate;
			if(TryConvertStartdate(s, out intDate))
			{
				return TryConvertStartdate(intDate, out date);
			}
			date = DateTime.MinValue;
			return false;
		}

		public static bool TryConvertStartdate(int intDate, out DateTime date)
		{
			int day = intDate % 100;
			intDate /= 100;
			int month = intDate % 100;
			intDate /= 100;
			int year = intDate;
			try
			{
				date = new DateTime(year, month, day);
				return true;
			}
			catch 
			{
				date = DateTime.MinValue;
				return false;
			}
		}

		public static bool TryConvertStartdate(DateTime date, out string s)
		{
			s = date.ToString("yyyyMMdd");
			return true;
		}
		public static bool TryConvertStartdate(int intDate, out string s)
		{
			s = intDate.ToString();
			return true;
		}
	}
}
