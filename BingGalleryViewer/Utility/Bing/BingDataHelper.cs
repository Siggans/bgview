using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BingGalleryViewer.Utility.Bing
{
	/// <summary>
	/// Helper to conver bing date to id, string, and actual date.
	/// </summary>
	internal class BingDataHelper
	{
		/// <summary>
		/// Convert string to int value
		/// </summary>
		/// <param name="s">string</param>
		/// <param name="intDate">result int</param>
		/// <returns>true if successful</returns>
		public static bool TryConvertStartdate(string s, out int intDate)
		{
			return Int32.TryParse(s, out intDate);
		}

		/// <summary>
		/// convert datetime to int
		/// </summary>
		/// <param name="date">date struct</param>
		/// <param name="intDate">result int</param>
		/// <returns>true if successful</returns>
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

		/// <summary>
		/// Convert string to date struct
		/// </summary>
		/// <param name="s">string</param>
		/// <param name="date">date result</param>
		/// <returns>true if successful</returns>
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

		/// <summary>
		/// Convert int to date struct
		/// </summary>
		/// <param name="intDate">int date </param>
		/// <param name="date">date result</param>
		/// <returns>true if successful</returns>
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

		/// <summary>
		/// Convert datetime to string
		/// </summary>
		/// <param name="date">date</param>
		/// <param name="s">result string</param>
		/// <returns>true if successful</returns>
		public static bool TryConvertStartdate(DateTime date, out string s)
		{
			s = date.ToString("yyyyMMdd");
			return true;
		}
		/// <summary>
		/// Convert int to string
		/// </summary>
		/// <param name="intDate">int date</param>
		/// <param name="s">result string</param>
		/// <returns>true if successful</returns>
		public static bool TryConvertStartdate(int intDate, out string s)
		{
			s = intDate.ToString();
			return true;
		}
	}
}
