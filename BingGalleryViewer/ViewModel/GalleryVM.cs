using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using BingGalleryViewer.Model;
using BingGalleryViewer.Utility;
using BingGalleryViewer.Utility.Bing;
using BingGalleryViewer.View;
using Microsoft.Win32;
using VMBase.ViewModel;

namespace BingGalleryViewer.ViewModel
{
	/// <summary>
	/// View model for gallery view
	/// </summary>
	public class GalleryVM : ViewModelBase
	{
		
		private string _dateCaptionText = "Monday, March 01, 2000";
		/// <summary>
		/// Date display for gallery view
		/// </summary>
		public string DateCaptionText
		{
			get { return _dateCaptionText; }
			set { this.SetAndRaise(out _dateCaptionText, "DateCaptionText", value); }
		}

		private string _captionText = "This is a Test Caption";
		/// <summary>
		/// Description text for gallery view
		/// </summary>
		public string CaptionText
		{
			get { return _captionText; }
			set { this.SetAndRaise(out _captionText, "CaptionText", value); }
		}

		private string _copyrightText = "© CopyRight Test Output";
		/// <summary>
		/// Copyright text for gallery view
		/// </summary>
		public string CopyrightText
		{
			get { return _copyrightText; }
			set { this.SetAndRaise(out _copyrightText, "CopyrightText", value); }
		}

		private string _errorMessageText = string.Empty;
		/// <summary>
		/// Error message display for gallery view
		/// </summary>
		public string ErrorMessageText
		{
			get { return _errorMessageText; }
			set { this.SetAndRaise(out _errorMessageText, "ErrorMessageText", value); }
		}

		private string _imagePathText = string.Empty;
		/// <summary>
		/// link to image path
		/// </summary>
		public string ImagePathText
		{
			get { return _imagePathText; }
			set { this.SetAndRaise(out _imagePathText, "ImagePathText", value); }
		}

		private DateTime _currentDate;
		/// <summary>
		/// DateTime for the current date displayed by gallery
		/// </summary>
		public DateTime CurrentDateNotifyOnly
		{
			get { return _currentDate; }
			set { SetCurrentDate(value); }
		}

		private bool _isSaveEnabled;
		/// <summary>
		/// Enable control for image to be saved
		/// </summary>
		public bool IsSaveEnabled
		{
			get { return _isSaveEnabled; }
			set { this.SetAndRaise(out _isSaveEnabled, "IsSaveEnabled", value); }
		}

		private bool _isInfoEnabled;
		/// <summary>
		/// Enable control for image info to be linked
		/// </summary>
		public bool IsInfoEnabled
		{
			get { return _isInfoEnabled; }
			set { this.SetAndRaise(out _isInfoEnabled, "IsInfoEnabled", value); }
		}


		// helper to set current date.
		private void SetCurrentDate(DateTime date)
		{
			// reset fields
			_currentDate = date.Date;
			CaptionText = CopyrightText = "";
			IsInfoEnabled = IsSaveEnabled = false;
			_info = null;

			// setting up fields with provided date
			DateCaptionText = date.ToString("dddd, MMM dd, yyyy");
			if (Setting.GetCurrentSetting().IsInitialized && ModelManager.IsInitialized
				&& Setting.GetCurrentSetting().BingDateMin <= date && date <= Setting.GetCurrentSetting().BingDateMax)
			{
				// once app is initialize,  get data from model
				Task.Run(() => RequestDateFromModel(_currentDate));
			}
		}

		// current date
		private BingImageInfo _info;
		// helper to set current date.
		private async void RequestDateFromModel(DateTime date, int retryCount = 0)
		{
			// only perform this task if no new date were entered
			if (date != _currentDate) return;
			if (retryCount == 3)
			{
				UpdateFailedStatusMessage("Server timeout");
				return;
			}

			await App.WaitForCurrentAppInitializationAsync();

			try
			{
				// query model manager and initialize the display
				while (!ModelManager.IsInitialized) await Task.Yield();
				_info = await ModelManager.RequestImageInfoAsync(date);

				if (date != _currentDate) return;
				if (_info == null)
				{
					UpdateFailedStatusMessage("No image found");
					return;
				}

				IsInfoEnabled = true;
				UpdateCaptions(_info.Copyright);
				var tuple = await ModelManager.RequestImagePathForGalleryAsync(_info);

				if (date != _currentDate) return;
				if (tuple == null || tuple.Item2 == null)
				{
					UpdateFailedStatusMessage("No image found");
					return;
				}
				IsSaveEnabled = true;
				ImagePathText = tuple.Item2;
			}
			catch (WebException e)
			{
				if (e.Status != WebExceptionStatus.RequestCanceled)
				{
					if (e.Status == WebExceptionStatus.Timeout)
						RequestDateFromModel(date, retryCount + 1);
					else throw new InvalidOperationException("Unhandled Web exception", e);
				}
			}
		}

		// helper to modify image caption on UI thread
		private void UpdateCaptions(string p)
		{
			if (p != null)
			{
				if (App.IsOnAppThread())
				{
					// divid the copyright text to be displayed where the author/company holding
					// the copyright is prefixed by "(©"
					var index = p.LastIndexOf("(©");
					if (index != -1)
					{
						CaptionText = p.Substring(0, index);
						CopyrightText = p.Substring(index);
					}
					else CaptionText = p;
				}
				else App.UIThreadInvoke(() => UpdateCaptions(p));
			
			}
		}

		// display error message
		private void UpdateFailedStatusMessage(string p)
		{
				if (App.IsOnAppThread())
				{
					this.ErrorMessageText = p;
				}
				else App.UIThreadInvoke(() => this.ErrorMessageText = p);
		}

		private RelayCommand _saveCommand;
		/// <summary>
		/// Command for saving image
		/// </summary>
		public ICommand SaveCommand
		{
			get
			{
				if (_saveCommand == null)
				{
					_saveCommand = new RelayCommand(this.SaveCommandExecute);
				}
				return _saveCommand;
			}
		}
		
		/// <summary>
		/// Save image to disk
		/// </summary>
		private async void SaveCommandExecute()
		{
			var info = _info;

			if(info != null)
			{
				// open save file dialog to pick a save location
				// FUTURE:  should find another plugin for save file dialog that does not take
				// ages to initialize.
				var window = App.GetCurrentMainWindow();
				if (window == null) return;
				var dialog = new SaveFileDialog()
				{
					DefaultExt = ".jpg",
					Filter = "Jpeg Images (*.jpg)|*.jpg",
					InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
					OverwritePrompt=true,
					Title = "Save Image ...",
					FileName = CaptionText+".jpg",
				};
				var result = dialog.ShowDialog(window);
				if(result.HasValue && result.Value)
				{
					if(!await ModelManager.SaveFileAsync(info, dialog.FileName))
					{
						MessageBox.Show(window, "Cannot save file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
					}
				}
			}
			else
			{
				IsSaveEnabled=false;
			}
		}

		private RelayCommand _infoCommand;
		/// <summary>
		/// Opens browser to bing's page regarding to this image.
		/// </summary>
		public ICommand InfoCommand
		{
			get
			{
				if(_infoCommand == null)
				{
					_infoCommand = new RelayCommand(this.InfoCommandExecute);
				}
				return _infoCommand;
			}
		}

		// open link on browser to link to bing's description on the image.
		private void InfoCommandExecute()
		{
			var info = _info;
			if (info != null)
			{
				var url = new Uri(new Uri("http://www.bing.com/"), info.CopyrightLink);
				System.Diagnostics.Process.Start(url.AbsoluteUri);
			}
			else IsInfoEnabled = false;
		}

	}
}
