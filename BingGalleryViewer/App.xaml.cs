using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
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
	/// BingGalleryViewer application control logics.  Handles app initialization .
	/// </summary>
	public partial class App : Application
	{


		/// <summary>
		/// Retrieve the MainWindow of the current application
		/// </summary>
		/// <returns>Current mainwindow.  May be null if the application is in process of shutting down</returns>
		public static Window GetCurrentMainWindow()
		{
			var app = App.Current;
			if (app == null) return null;
			return app.MainWindow;
		}

		/// <summary>
		/// Checks the calling thread to see if it is the main UI (App) thread.
		/// </summary>
		/// <returns>true if the current invoking thread is the UI thread.</returns>
		public static bool IsOnAppThread()
		{
			// app value can be null if called after App shutdown is called
			var app = App.Current;
			if (app == null) return false;
			return app.Dispatcher.Thread == System.Threading.Thread.CurrentThread;
		}

		/// <summary>
		/// Invoke action on UI thread.
		/// </summary>
		/// <param name="action">action to perform</param>
		public static void UIThreadInvoke(Action action)
		{
			var app = App.Current;
			if (app != null) app.Dispatcher.Invoke(action);
		}

		/// <summary>
		/// Waits for application initialization to be completed before returning.
		/// </summary>
		/// <returns></returns>
		public static async Task WaitForCurrentAppInitializationAsync()
		{
			try
			{
				await ((App)Current).WaitForInitializationCompleteAsync();
			}
			catch (ArgumentNullException) { } // don't care if currentApp returns null, which indicates that application is shutting down
		}

		private bool _isReady = false;	// true when initialized
		private bool _isInitializatinoStarted = false; // true when initialization task started

		// store the application initialization async task for checking if the task has been completed
		private Task AppInitializationTask = null;

		// Overloaded startup method to start custom app initialization.
		private void App_Startup(object sender, StartupEventArgs e)
		{
			// call once
			if (!_isInitializatinoStarted)
			{
				_isInitializatinoStarted = true;
				// and save initialization
				AppInitializationTask = StartInitializationTask();
			}
		}

		// any system and database preparation steps should be initialized here.
		private async Task StartInitializationTask()
		{

			PrepareSetting(); // config setting and directory creations

			// set the number of concurrent http connection that this app would require
			System.Net.ServicePointManager.DefaultConnectionLimit = ModelManager.RequiredConcurrentWebConnection;

			// prepare database if none exists
			Datastore.PrepareDatabaseIfNotExist();

			// initialize model manager to be used with the database
			await ModelManager.InitializeAsync();

			// application ready.
			_isReady = true;
		}

		// Get the configuration setting and create all folders as needed.
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
			catch (Exception e) { Trace.WriteLine(e.Message); } // swallow I/O error. 
			// FUTURE:  reinvestigate possible IO errors that could occure should there be problem
			// Consider failing application if IO fails
		}

		/// <summary>
		/// Check if appliation has completed the necessary initialzation.  True if completed
		/// </summary>
		public bool IsReady { get { return _isReady; } }

		// wait for initialization task completeion.  Non blocking.
		private async Task WaitForInitializationCompleteAsync()
		{
			if (!_isInitializatinoStarted) throw new ApplicationException("App_Startup has not been called");
			if (!IsReady)
				await Task.WhenAll(AppInitializationTask);
		}
	}
}
