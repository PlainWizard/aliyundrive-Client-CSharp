using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace aliyundrive_Client_CSharp
{
    class WinAPI
    {
        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        public static extern int SetWindowLong(IntPtr hMenu, int nIndex, int dwNewLong);

    }
    class WinMethod
    {
        public static void DisableMinmizebox(Window window)
        {
            Task.Run(()=> {
                window.Dispatcher.Invoke(() => {
                    WinAPI.SetWindowLong(new WindowInteropHelper(window).Handle, -16, 0x16CD0000);//设定一个新的窗口风格
                });
            });
        }
    }

    class ClipboardWatcher
    {
        [DllImport("user32.dll", SetLastError = true)]
        private extern static void AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        private extern static void RemoveClipboardFormatListener(IntPtr hwnd);

        private const int WM_CLIPBOARDUPDATE = 0x031D;
        private IntPtr handle;
        private HwndSource hwndSource = null;
        public event EventHandler DrawClipboard;
        private void RaiseClipboardUpdata()
        {
            DrawClipboard?.Invoke(this, EventArgs.Empty);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_CLIPBOARDUPDATE)
            {
                this.RaiseClipboardUpdata();
                handled = true;
            }
            return IntPtr.Zero;
        }
        public ClipboardWatcher(Window window)
        {
            handle = new System.Windows.Interop.WindowInteropHelper(window).Handle;
            hwndSource = HwndSource.FromHwnd(handle);
            hwndSource.AddHook(WndProc);
        }
        ~ClipboardWatcher(){
            Stop();
        }
        public void Start()
        {
            AddClipboardFormatListener(handle);
        }
        public void Stop()
        {
            RemoveClipboardFormatListener(handle);
        }
    }
}
