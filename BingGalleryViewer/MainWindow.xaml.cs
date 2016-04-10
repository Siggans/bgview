using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BingGalleryViewer.Utility;
using BingGalleryViewer.Utility.Bing.WebAPI;
using MahApps.Metro.Controls;

namespace BingGalleryViewer
{
	/// <summary>
	/// WPF Main window for the application
	/// </summary>
	public partial class MainWindow : MetroWindow
	{

		// variables to be used to request value for MetroApp UI plugins.
		private const string PART_WindowButtonCommands = "PART_WindowButtonCommands";
		private const string PART_Close = "PART_Close";

		/// <summary>
		/// Constructor
		/// </summary>
		public MainWindow()
		{
			InitializeComponent();
		}

		// main window close button
		private Button CloseButton = null;

		/// <summary>
		/// Overrides the default applytemplate to intercept some command logic that
		/// is implemented in <see cref="MetroWindow"/>'s template
		/// </summary>
		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			// try get MetroWindow's buttonCommands.
			var commands = this.GetTemplateChild(PART_WindowButtonCommands) as WindowButtonCommands;
			if (commands != null)
			{
				// attach window closing event
				commands.ClosingWindow += WindowButtonCommands_ClosingWindows;
			}
			var button = this.GetTemplateChild(PART_Close) as Button;
			if (button != null)
			{
				// get the button for closing Window.
				CloseButton = button;
			}
		}

		// Intercept the window closing call so that we could save data before program closes.
		private void WindowButtonCommands_ClosingWindows(object sender, ClosingWindowEventHandlerArgs e)
		{
			// once the custom shutdown process is completed, the normal shutdown can proceed.
			if (_isDoneClosing) return;
			e.Cancelled = true; // prevent normal shutdown.
			// and start our own shutdown process.
			if (!_isClosing)
			{
				_isClosing = true;
				StartClosingTasks();
			}
		}

		private bool _isClosing = false; // window is in process of shutting down
		private bool _isDoneClosing = false; // custom window shutdown process is complete.  Normal shutdown can proceed.

		// Intercept the window closing call so that we could save data before program closes.
		private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (_isDoneClosing) return;
			e.Cancel = true;
			if (!_isClosing)
			{
				_isClosing = true;
				StartClosingTasks();
			}

		}

		// BGV custom shutdown process
		private void StartClosingTasks()
		{
			// save configuration
			Setting.GetCurrentSetting().SaveSetting();
			_isDoneClosing = true;

			// try to close via close button so MetroWindow can exit properly.  Otherwise force shutdown.
			if (!TryClickCloseButton()) this.Close();
		}

		// manually trigger the command associated with close button for application shutdown.
		// returns true if trigger successful,  false otherwise
		private bool TryClickCloseButton()
		{
			if (CloseButton != null)
			{
				var automationPeer = UIElementAutomationPeer.CreatePeerForElement(CloseButton);
				var invoker = automationPeer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
				invoker.Invoke();
				return true;
			}
			return false;
		}

		/// <summary>
		/// Swith to gallery view with the date in display
		/// </summary>
		/// <param name="dateTime">date to show on the gallery</param>
		internal void SwitchToView(DateTime dateTime)
		{
			// only execute if the sent date is between dates that our database has saved.
			if (Setting.GetCurrentSetting().BingDateMin <= dateTime && dateTime<=Setting.GetCurrentSetting().BingDateMax)
			{
				this.TabMenu.SelectedIndex = 0;
				this.GalleryView.CurrentCalendarTime = dateTime;
			}
		}

		// handles html web link and open browser.
		private void IconLink_Clicked(object sender, MouseButtonEventArgs e)
		{
			var txtBlock = sender as TextBlock;
			if(txtBlock!=null)
			{
				System.Diagnostics.Process.Start(txtBlock.Text);
			}
		}
	}
}
