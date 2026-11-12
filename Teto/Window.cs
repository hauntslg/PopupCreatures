using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Teto {
    internal sealed class Window {
        private const string ClassName = "TetoWindow";

        // Keep the delegate alive for the lifetime of the window.
        private readonly Native.WindowProc _windowProcedure;

        public Window() {
            _windowProcedure = WndProc;
        }

        public void Run() {
            nint instance = Native.GetModuleHandleW(null);

            var wc = new Native.WindowClass {
                cbSize = (uint)Marshal.SizeOf<Native.WindowClass>(),
                lpfnWndProc = _windowProcedure,
                hInstance = instance,
                hbrBackground = (nint)6,
                lpszClassName = ClassName
            };

            if (Native.RegisterClassExW(ref wc) == 0) {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            nint hwnd = Native.CreateWindowExW(
                Native.WS_EX_TOPMOST |
                Native.WS_EX_TOOLWINDOW |
                Native.WS_EX_LAYERED,
                ClassName,
                "Teto",
                Native.WS_POPUP,
                100, 100,
                640, 480,
                0, 0,
                instance,
                0
            );

            if (hwnd == 0) {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            Native.ShowWindow(hwnd, Native.SW_SHOW);
            Native.UpdateWindow(hwnd);
 
            while (true) {
                int result = Native.GetMessageW(out var msg, 0, 0, 0);

                if (result == 0) {
                    break;
                }

                if (result == -1) {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                Native.TranslateMessage(ref msg);
                Native.DispatchMessageW(ref msg);
            }
        }

        private nint WndProc(nint hwnd, uint msg, nuint wParam, nint lParam) {
            if (msg == Native.WM_DESTROY) {
                Native.PostQuitMessage(0);
                return 0;
            }

            return Native.DefWindowProcW(hwnd, msg, wParam, lParam);
        }
    }
}
