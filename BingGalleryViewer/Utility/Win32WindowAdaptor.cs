using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace BingGalleryViewer.Utility
{
	class Win32WindowAdaptor : IWin32Window
	{
		private readonly IntPtr _handle;

		public Win32WindowAdaptor() : this(App.GetCurrentMainWindow()) { }
		public Win32WindowAdaptor(Window window)
		{
			if (window != null)
			{
				_handle = (PresentationSource.FromVisual(window) as System.Windows.Interop.HwndSource).Handle;
			}
			else _handle = IntPtr.Zero;
		}

		public IntPtr Handle
		{
			get { return _handle; }
		}
	}
}
