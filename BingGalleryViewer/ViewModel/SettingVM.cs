using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BingGalleryViewer.Utility;
using VMBase.ViewModel;

namespace BingGalleryViewer.ViewModel
{
	/// <summary>
	/// View model that adapts program's settings to user
	/// </summary>
	public class SettingVM : ViewModelBase
	{
		private Setting currentSetting = Setting.GetCurrentSetting();

		#region viewmodel properties

		/// <summary>
		/// flag to enable cache
		/// </summary>
		public bool UseCache
		{
			get { return currentSetting.IsUsingCache; }
			set { this.SetAndRaise(out currentSetting.IsUsingCache, "UseCache", value); }
		}

		/// <summary>
		/// flag to enable high quality cache.  UseCache must be true before this flag will take effect.
		/// </summary>
		public bool UseCacheHd
		{
			get { return currentSetting.IsUsingCacheHd; }
			set { currentSetting.IsUsingCacheHd = value; }
		}

		/// <summary>
		/// Update hd flag and notify view of the change
		/// </summary>
		/// <param name="b"></param>
		public void SetUseCacheHdAndNotify(bool b)
		{
			if (UseCacheHd != b)
			{
				UseCacheHd = b;
				this.RaisePropertyChanged("UseCacheHd");
			}
		}

		/// <summary>
		/// cache folder location
		/// </summary>
		public string CachePath
		{
			get { return currentSetting.CachePath.LocalPath; }
			set { if (!VerifyAndSetCachePath(value)) this.RaisePropertyChanged("CachePath"); }
		}

		// ensure that directory is write enabled
		private bool VerifyAndSetCachePath(string s)
		{
			try
			{
				if (!string.IsNullOrEmpty(s))
				{
					if (!Directory.Exists(s)) Directory.CreateDirectory(s);
					if (VMBase.Utilities.PathUtilities.VerifyDirectoryWritePermission(s))
					{
						currentSetting.CachePath = new Uri(s, UriKind.Absolute);
						return true;
					}
				}
			}
			catch { }
			return false;

		}

		/// <summary>
		/// Sets Cache path and ensure that it is write enabled
		/// </summary>
		/// <param name="s">path to be used for cache</param>
		/// <returns>true if valid path and is write enabled</returns>
		public bool VerifyAndSetSetCachePathAndNotify(string s)
		{
			if (VerifyAndSetCachePath(s))
			{
				this.RaisePropertyChanged("CachePath");
				return true;
			}
			return false;
		}

		#endregion viewmodel properties

		/// <summary>
		/// Constructor
		/// </summary>
		public SettingVM()
		{
			this.UseCache = this.UseCache;
		}
	}
}
