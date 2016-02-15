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
	public class GalleryVM : ViewModelBase
	{
		private string _dateCaptionText = "Monday, March 01, 2000";
		public string DateCaptionText
		{
			get { return _dateCaptionText; }
			set { this.SetAndRaise(out _dateCaptionText, "DateCaptionText", value); }
		}

		private string _captionText = "This is a Test Caption";
		public string CaptionText
		{
			get { return _captionText; }
			set { this.SetAndRaise(out _captionText, "CaptionText", value); }
		}

		private string _copyrightText = "© CopyRight Test Output";
		public string CopyrightText
		{
			get { return _copyrightText; }
			set { this.SetAndRaise(out _copyrightText, "CopyrightText", value); }
		}

		private string _errorMessageText = string.Empty;
		public string ErrorMessageText
		{
			get { return _errorMessageText; }
			set { this.SetAndRaise(out _errorMessageText, "ErrorMessageText", value); }
		}

		private string _imagePathText = string.Empty;
		public string ImagePathText
		{
			get { return _imagePathText; }
			set { this.SetAndRaise(out _imagePathText, "ImagePathText", value); }
		}

		private DateTime _currentDate;
		public DateTime CurrentDateNotifyOnly
		{
			get { return _currentDate; }
			set { SetCurrentDate(value); }
		}

		private bool _isSaveEnabled;
		public bool IsSaveEnabled
		{
			get { return _isSaveEnabled; }
			set { this.SetAndRaise(out _isSaveEnabled, "IsSaveEnabled", value); }
		}

		private bool _isInfoEnabled;
		public bool IsInfoEnabled
		{
			get { return _isInfoEnabled; }
			set { this.SetAndRaise(out _isInfoEnabled, "IsInfoEnabled", value); }
		}


		private void SetCurrentDate(DateTime date)
		{
			_currentDate = date.Date;
			CaptionText = CopyrightText = "";
			IsInfoEnabled = IsSaveEnabled = false;
			_info = null;
			DateCaptionText = date.ToString("dddd, MMM dd, yyyy");
			if (Setting.GetCurrentSetting().IsInitialized && ModelManager.IsInitialized
				&& Setting.GetCurrentSetting().BingDateMin <= date && date <= Setting.GetCurrentSetting().BingDateMax)
			{
				Task.Run(() => RequestDateFromModel(_currentDate));
			}
		}

		private BingImageInfo _info;
		private async void RequestDateFromModel(DateTime date, int retryCount = 0)
		{
			if (date != _currentDate) return;
			if (retryCount == 3)
			{
				UpdateFailedStatusMessage("Server timeout");
				return;
			}

			await App.WaitForCurrentAppInitializationAsync();

			try
			{
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

		private void UpdateCaptions(string p)
		{
			if (p != null)
			{
				if (App.IsOnAppThread())
				{
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

		private void UpdateFailedStatusMessage(string p)
		{
				if (App.IsOnAppThread())
				{
					this.ErrorMessageText = p;
				}
				else App.UIThreadInvoke(() => this.ErrorMessageText = p);
		}

		private RelayCommand _saveCommand;
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
		
		private async void SaveCommandExecute()
		{
			var info = _info;

			if(info != null)
			{
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
