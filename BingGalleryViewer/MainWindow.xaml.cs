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
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : MetroWindow
	{
		private const string PART_WindowButtonCommands = "PART_WindowButtonCommands";
		private const string PART_Close = "PART_Close";
		public MainWindow()
		{
			InitializeComponent();
		}

		private Button CloseButton = null;

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			var commands = this.GetTemplateChild(PART_WindowButtonCommands) as WindowButtonCommands;
			if (commands != null)
			{
				commands.ClosingWindow += WindowButtonCommands_ClosingWindows;
			}
			var button = this.GetTemplateChild(PART_Close) as Button;
			if (button != null)
			{
				CloseButton = button;
			}
		}

		private void WindowButtonCommands_ClosingWindows(object sender, ClosingWindowEventHandlerArgs e)
		{
			if (_isDoneClosing) return;
			e.Cancelled = true;
			if (!_isClosing)
			{
				_isClosing = true;
				StartClosingTasks();
			}
		}

		private bool _isClosing = false;
		private bool _isDoneClosing = false;
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
		private void StartClosingTasks()
		{
			Setting.GetCurrentSetting().SaveSetting();
			_isDoneClosing = true;
			if (!TryClickCloseButton()) this.Close();
		}
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

		internal void SwitchToView(DateTime dateTime)
		{
			if (Setting.GetCurrentSetting().BingDateMin <= dateTime && dateTime<=Setting.GetCurrentSetting().BingDateMax)
			{
				this.TabMenu.SelectedIndex = 0;
				this.GalleryView.CurrentCalendarTime = dateTime;
			}
		}

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
