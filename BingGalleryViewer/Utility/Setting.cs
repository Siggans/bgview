using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace BingGalleryViewer.Utility
{
	public class Setting
	{

		private class IniNames
		{
			public const string UseCache = "UseCache";
			public const string UseCacheHd = "UseCacheHD";
			public const string CachePath = "CachePath";
			public static readonly string MatchUseCache = UseCache.ToLower();
			public static readonly string MatchUseCacheHd = UseCacheHd.ToLower();
			public static readonly string MatchCachePath = CachePath.ToLower();
		}

		private static Setting _currentSetting = null;
		public static Setting GetCurrentSetting()
		{
			if (_currentSetting == null)
			{
				_currentSetting = new Setting();
			}
			return _currentSetting;
		}

		public static void CreateFoldersIfNotExist(string path)
		{
			var pathName = Path.GetDirectoryName(path);
			if (Directory.Exists(pathName)) return;
			Directory.CreateDirectory(pathName);
		}

		public static readonly Uri AppDataDirectory = new Uri(
			Path.Combine(Environment.ExpandEnvironmentVariables("%AppData%"),
				"..", "LocalLow", "BingGalleryViewer"));
		public static readonly Uri DefaultCachePath = new Uri(
			Path.Combine(AppDataDirectory.LocalPath, "Cache"));
		public static readonly Uri SettingFilePath = new Uri(
			Path.Combine(AppDataDirectory.LocalPath, "config.ini"));

		private DateTime _dateMax = DateTime.Now.Date;
		public DateTime BingDateMax { get { return _dateMax; } }
		public void SetBingDateMax(DateTime newDate) { _dateMax = newDate.Date; }

		private DateTime _dateMin = DateTime.Now.AddDays(-18).Date;
		public DateTime BingDateMin { get { return _dateMin; } }
		public void SetBingDateMin(DateTime newDate) { _dateMin = newDate.Date; }

		public bool IsMinDateFound = false;
		public bool IsUsingCache = true;
		public bool IsUsingCacheHd = false;
		public Uri CachePath;

		private Uri _tempPath = null;

		public readonly Uri DatastoreFilePath = new Uri(
			Path.Combine(AppDataDirectory.LocalPath, "local.sqlite"));


		public Uri TempPath { get { return _tempPath; } }

		private Uri _settingFile;
		public Uri FilePath { get { return _settingFile; } }

		private bool _isInitialized = false;
		public bool IsInitialized { get { return _isInitialized; } }


		private Setting()
		{
			this._settingFile = SettingFilePath;
		}

		public void Initialize()
		{

			if (!IsInitialized)
			{
				SetDefaultValues();

				if (File.Exists(_settingFile.LocalPath))
				{
					ReadSettings(_settingFile.LocalPath);
				}
				_isInitialized = true;

			}
		}

		public void SaveSetting()
		{
			try
			{
				if (!File.Exists(_settingFile.LocalPath)) CreateFoldersIfNotExist(_settingFile.LocalPath);
				using (var writer = new StreamWriter(_settingFile.LocalPath, false, Encoding.Default))
				{
					writer.WriteLine("[Settings]");
					SaveSettingAsync_WriteLine(writer, IniNames.UseCache, IsUsingCache.ToString());
					SaveSettingAsync_WriteLine(writer, IniNames.UseCacheHd, IsUsingCacheHd.ToString());
					SaveSettingAsync_WriteLine(writer, IniNames.CachePath, CachePath.LocalPath);
				}
			}
			catch (Exception)
			{
			}

		}

		private void SetDefaultValues()
		{
			IsMinDateFound = false;
			IsUsingCache = true;
			IsUsingCacheHd = false;
			CachePath = DefaultCachePath;
			_tempPath = new Uri(Path.Combine(Path.GetTempPath(), "BGVCACHE"));
		}

		private void ReadSettings(string path)
		{
			try
			{
				using (var reader = new StreamReader(path))
				{
					string cachePath = null;
					bool? isUsingCache = null;
					bool? isUsingCacheHd = null;
					while (!reader.EndOfStream)
					{
						var line = reader.ReadLine().Trim().ToLower();
						if (cachePath == null && line.StartsWith(IniNames.MatchCachePath))
						{
							cachePath = ReadSetting_GetLineValue(line);
						}
						else if (!isUsingCache.HasValue && line.StartsWith(IniNames.MatchUseCache))
						{
							var token = ReadSetting_GetLineValue(line);
							if (token == "true") isUsingCache = true;
							else if (token == "false") isUsingCache = false;
						}
						else if (!isUsingCacheHd.HasValue && line.StartsWith(IniNames.MatchUseCacheHd))
						{
							var token = ReadSetting_GetLineValue(line);
							if (token == "true") isUsingCacheHd = true;
							else if (token == "false") isUsingCacheHd = false;
						}

						if (cachePath != null && isUsingCache.HasValue && isUsingCacheHd.HasValue) break;
					}
					if (!string.IsNullOrEmpty(cachePath) && Directory.Exists(cachePath)) this.CachePath = new Uri(cachePath);
					if (isUsingCache.HasValue) IsUsingCache = isUsingCache.Value;
					if (isUsingCacheHd.HasValue) IsUsingCacheHd = isUsingCacheHd.Value;
				}
			}
			catch (Exception)
			{
				// any sort of exception we just quit;
			}
		}
		private string ReadSetting_GetLineValue(string line)
		{
			var ind = line.IndexOf('=');
			if (ind > 0 && ind < line.Length)
			{
				if (ind < line.Length - 1)
				{
					return line.Substring(ind + 1).Trim();
				}
				return "";
			}
			return null;
		}
		private void SaveSettingAsync_WriteLine(StreamWriter writer, string name, string value)
		{
			writer.Write(name);
			writer.Write('=');
			writer.WriteLine(value);
		}
	}
}
