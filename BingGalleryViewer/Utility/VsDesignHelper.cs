using System;
using System.ComponentModel;

using System.Windows.Forms;


namespace BingGalleryViewer.Utility
{
	/// <summary>
	/// Provieds some utility function for Visual Studio Designer Class
	/// </summary>
	public static class VsDesignHelper
	{
		#region private singleton

		private static Control _ctrlSingleton = null;

		private static Control _ctrl
		{
			get
			{
				if (_ctrlSingleton == null)
				{
					_ctrlSingleton = new Control();
				}
				return _ctrlSingleton;
			}
		}

		#endregion private singleton

		/// <summary>
		/// Check if the runtime is currently being executed in Visual Studio Design mode
		/// </summary>
		/// <returns>true if in design mode</returns>
		public static bool IsDesignerHosted
		{
			get
			{
				return IsDesignerHostedImpl(_ctrl);
			}

		}

		/// <summary>
		/// Check if the run time is currently being executed in design mode with supplied object
		/// The DesignMode property does not correctly tell you if
		/// you are in design mode.  IsDesignerHosted is a corrected
		/// version of that property.
		/// (see https://connect.microsoft.com/VisualStudio/feedback/details/553305
		/// and http://stackoverflow.com/a/2693338/238419 )
		/// </summary>
		/// <param name="ctrl">form control</param>
		/// <returns>>true if in design mode</returns>
		private static bool IsDesignerHostedImpl(Control ctrl)
		{

			if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
				return true;

			while (ctrl != null)
			{
				if ((ctrl.Site != null) && ctrl.Site.DesignMode)
					return true;
				ctrl = ctrl.Parent;
			}
			return false;

		}

		/// <summary>
		/// Perform action when in design mode by testing with the supplied object
		/// </summary>
		/// <param name="action">action to execute</param>
		/// <returns>true if in design mode</returns>
		public static bool TestDesignModeExecute(Action action)
		{
			bool isHosted = VsDesignHelper.IsDesignerHosted;
			if (isHosted)
			{
				action.Invoke();
			}
			return isHosted;
		}


		/// <summary>
		/// Execute either inDesignerModeInitialization or normalModeInitialization depending if 
		/// currently in design mode based on the dependency object
		/// </summary>
		/// <param name="inDesignModeAction"></param>
		/// <param name="normalModeAction"></param>
		public static void TestDesignModeExecute(Action inDesignModeAction, Action normalModeAction)
		{
			if (IsDesignerHosted)
			{
				inDesignModeAction.Invoke();
			}
			else
			{
				normalModeAction.Invoke();
			}
		}

	}
}
