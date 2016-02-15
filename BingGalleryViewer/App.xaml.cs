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

		public static Window GetCurrentMainWindow()
		{
			var app = App.Current;
			if (app == null) return null;
			return app.MainWindow;
		}

		public static bool IsOnAppThread()
		{
			var app = App.Current;
			if (app == null) return false;
			return app.Dispatcher.Thread == System.Threading.Thread.CurrentThread;
		}

		public static void UIThreadInvoke(Action action)
		{
			var app = App.Current;
			if (app != null) app.Dispatcher.Invoke(action);
		}

		public static async Task WaitForCurrentAppInitializationAsync()
		{
			try
			{
				await ((App)Current).WaitForInitializationCompleteAsync();
			}
			catch (ArgumentNullException) { } // don't care if currentApp returns null
		}

		private Task AppInitializationTask = null;

		private void App_Startup(object sender, StartupEventArgs e)
		{
			if (!_isInitializatinoStarted)
			{
				_isInitializatinoStarted = true;
				AppInitializationTask = StartInitializationTask();
			}
		}

		private async Task StartInitializationTask()
		{
			PrepareSetting();
			System.Net.ServicePointManager.DefaultConnectionLimit = ModelManager.RequiredConcurrentWebConnection;
			Datastore.PrepareDatabaseIfNotExist();
			await ModelManager.InitializeAsync();
			_isReady = true;
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

		private bool _isReady = false;
		private bool _isInitializatinoStarted = false;

		public bool IsReady { get { return _isReady; } }

		private async Task WaitForInitializationCompleteAsync()
		{
			if (!_isInitializatinoStarted) throw new ApplicationException("App_Startup has not been called");
			if (!IsReady)
				await Task.WhenAll(AppInitializationTask);
		}
	}
}
