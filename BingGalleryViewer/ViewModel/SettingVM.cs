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
	public class SettingVM : ViewModelBase
	{
		private Setting currentSetting = Setting.GetCurrentSetting();

		#region viewmodel properties

		public bool UseCache
		{
			get { return currentSetting.IsUsingCache; }
			set { this.SetAndRaise(out currentSetting.IsUsingCache, "UseCache", value); }
		}

		public bool UseCacheHd
		{
			get { return currentSetting.IsUsingCacheHd; }
			set { currentSetting.IsUsingCacheHd = value; }
		}

		public void SetUseCacheHdAndNotify(bool b)
		{
			if (UseCacheHd != b)
			{
				UseCacheHd = b;
				this.RaisePropertyChanged("UseCacheHd");
			}
		}

		public string CachePath
		{
			get { return currentSetting.CachePath.LocalPath; }
			set { if (!VerifyAndSetCachePath(value)) this.RaisePropertyChanged("CachePath"); }
		}
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
			catch (Exception)
			{
			}
			return false;

		}
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

		public SettingVM()
		{
			this.UseCache=this.UseCache;
		}
	}
}
