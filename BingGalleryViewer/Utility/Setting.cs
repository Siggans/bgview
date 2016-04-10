using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace BingGalleryViewer.Utility
{
	/// <summary>
	/// Program configuration settings.
	/// </summary>
	public class Setting
	{
		/// <summary>
		/// parameter names for setting file
		/// </summary>
		private class IniNames
		{
			public const string UseCache = "UseCache";
			public const string UseCacheHd = "UseCacheHD";
			public const string CachePath = "CachePath";
			public static readonly string MatchUseCache = UseCache.ToLower();
			public static readonly string MatchUseCacheHd = UseCacheHd.ToLower();
			public static readonly string MatchCachePath = CachePath.ToLower();
		}

		// singleton design
		private static Setting _currentSetting = null;
		/// <summary>
		/// Get the current project setting
		/// </summary>
		/// <returns>current setting</returns>
		public static Setting GetCurrentSetting()
		{
			if (_currentSetting == null)
			{
				_currentSetting = new Setting();
			}
			return _currentSetting;
		}

		/// <summary>
		/// save a default setting if no setting file exists
		/// </summary>
		/// <param name="path">location of the config file</param>
		public static void CreateFoldersIfNotExist(string path)
		{
			var pathName = Path.GetDirectoryName(path);
			if (Directory.Exists(pathName)) return;
			Directory.CreateDirectory(pathName);
		}

		/// <summary>
		/// Location of the AppData where configuration will be stored
		/// </summary>
		public static readonly Uri AppDataDirectory = new Uri(
			Path.Combine(Environment.ExpandEnvironmentVariables("%AppData%"),
				"..", "LocalLow", "BingGalleryViewer"));
		/// <summary>
		/// Default cache location
		/// </summary>
		public static readonly Uri DefaultCachePath = new Uri(
			Path.Combine(AppDataDirectory.LocalPath, "Cache"));
		/// <summary>
		/// Default configuration file save location
		/// </summary>
		public static readonly Uri SettingFilePath = new Uri(
			Path.Combine(AppDataDirectory.LocalPath, "config.ini"));

		private DateTime _dateMax = DateTime.Now.Date;
		/// <summary>
		/// Max date that is usable in this app
		/// </summary>
		public DateTime BingDateMax { get { return _dateMax; } }
		/// <summary>
		/// Update the max date
		/// </summary>
		/// <param name="newDate">new date</param>
		public void SetBingDateMax(DateTime newDate) { _dateMax = newDate.Date; }



		private DateTime _dateMin = DateTime.Now.AddDays(-18).Date;
		/// <summary>
		/// Min date that is usable in this app
		/// </summary>
		public DateTime BingDateMin { get { return _dateMin; } }
		/// <summary>
		/// Update the min date
		/// </summary>
		/// <param name="newDate">new date</param>
		public void SetBingDateMin(DateTime newDate) { _dateMin = newDate.Date; }

		/// <summary>
		/// Allows caching of image data to specific folder
		/// </summary>
		public bool IsUsingCache = true;
		/// <summary>
		/// Determines if the image saved should be of high quality
		/// </summary>
		public bool IsUsingCacheHd = false;
		/// <summary>
		/// cache folder location
		/// </summary>
		public Uri CachePath;

		
		/// <summary>
		/// path to sqlite database for this application
		/// </summary>
		public readonly Uri DatastoreFilePath = new Uri(
			Path.Combine(AppDataDirectory.LocalPath, "local.sqlite"));


		private Uri _tempPath = null;
		/// <summary>
		/// temp folder for this application
		/// </summary>
		public Uri TempPath { get { return _tempPath; } }

		private Uri _settingFile;
		/// <summary>
		/// Setting file path
		/// </summary>
		public Uri FilePath { get { return _settingFile; } }

		private bool _isInitialized = false;
		/// <summary>
		/// Check if setting file has been read.
		/// </summary>
		public bool IsInitialized { get { return _isInitialized; } }


		/// <summary>
		/// Constructor, singleton pattern
		/// </summary>
		private Setting()
		{
			this._settingFile = SettingFilePath;
		}


		/// <summary>
		/// Initialize the setting from file, or default values if setting file is missing
		/// </summary>
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

		/// <summary>
		/// Save settings to file
		/// </summary>
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

		/// <summary>
		/// Default values for setting if no configuration exists
		/// </summary>
		private void SetDefaultValues()
		{
			IsUsingCache = true;
			IsUsingCacheHd = false;
			CachePath = DefaultCachePath;
			_tempPath = new Uri(Path.Combine(Path.GetTempPath(), "BGVCACHE"));
		}

		/// <summary>
		/// Read in setting file
		/// </summary>
		/// <param name="path"></param>
		private void ReadSettings(string path)
		{
			try
			{
				// read in file and parse line by line
				using (var reader = new StreamReader(path))
				{
					string cachePath = null;
					bool? isUsingCache = null;
					bool? isUsingCacheHd = null;
					// read til end of file
					// only first setting will take effect.  subsequence settings of the same value will be ignored.
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
						// terminate early if all settings  had been read.
						if (cachePath != null && isUsingCache.HasValue && isUsingCacheHd.HasValue) break;
					}

					if (!string.IsNullOrEmpty(cachePath) && Directory.Exists(cachePath)) this.CachePath = new Uri(cachePath);
					if (isUsingCache.HasValue) IsUsingCache = isUsingCache.Value;
					if (isUsingCacheHd.HasValue) IsUsingCacheHd = isUsingCacheHd.Value;
				}
			}
			catch (Exception)
			{
				// any sort of exception we use default value
			}
		}

		// simple parsing of the line from read file to retrieve rhs value.
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

		// simple writer for key-value pair for ini file.
		private void SaveSettingAsync_WriteLine(StreamWriter writer, string name, string value)
		{
			writer.Write(name);
			writer.Write('=');
			writer.WriteLine(value);
		}
	}
}
