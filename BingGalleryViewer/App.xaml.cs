using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using BingGalleryViewer.Model;
using BingGalleryViewer.Utility;
using BingGalleryViewer.Utility.SQLite;

namespace BingGalleryViewer
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		private Task AppInitializationTask = null;

		private void App_Startup(object sender, StartupEventArgs e)
		{
			if (AppInitializationTask == null)
			{
				IsReady = false;
				StartInitializationTask();
			}
		}

		private void StartInitializationTask()
		{
			AppInitializationTask = Task.Run(async() =>
			{
				PrepareSetting();
				System.Net.ServicePointManager.DefaultConnectionLimit = ModelManager.RequiredConcurrentWebConnection;
				Datastore.PrepareDatabaseIfNotExist();
				await ModelManager.InitializeAsync();
				IsReady = true;
			});
		}

		private void PrepareSetting()
		{
			Setting.GetCurrentSetting().Initialize();
			try
			{
				if (!Directory.Exists(Setting.AppDataDirectory.LocalPath))
				{
					Directory.CreateDirectory(Setting.AppDataDirectory.LocalPath);
				}
				if (!Directory.Exists(Setting.GetCurrentSetting().CachePath.LocalPath))
				{
					Directory.CreateDirectory(Setting.GetCurrentSetting().CachePath.LocalPath);
				}
				if (!Directory.Exists(Setting.GetCurrentSetting().TempPath.LocalPath))
				{
					Directory.CreateDirectory(Setting.GetCurrentSetting().TempPath.LocalPath);
				}
			}
			catch { }
		}

		public bool IsReady { get; private set; }

		public void WaitForInitializationComplete()
		{
			if (!IsReady)
			{
				Task.WaitAll(AppInitializationTask);
			}
		}
	}
}
