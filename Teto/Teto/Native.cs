using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace Teto {
    internal static class Native {
        private const string User32 = "user32.dll";
        private const string Kernel32 = "kernel32.dll";

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate nint WindowProc(nint hwnd, uint msg, nuint wParam, nint lParam);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct WindowClass {
            public uint cbSize;
            public uint style;
            public WindowProc lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public nint hInstance;
            public nint hIcon;
            public nint hCursor;
            public nint hbrBackground;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string? lpszMenuName;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpszClassName;

            public nint hIconSm;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Point {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Size {
            public int cx;
            public int cy;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct BlendFunction {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Message {
            public nint hwnd;
            public uint message;
            public nuint wParam;
            public nint lParam;
            public uint time;
            public Point pt;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Rect {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct PaintStruct {
            public nint hdc;
            public int fErase;
            public Rect rcPaint;
            public int fRestore;
            public int fIncUpdate;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] rgbReserved;
        }

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool UpdateLayeredWindow(
            nint hwnd,
            nint hdcdst,
            ref Point ptDst,
            ref Size size,
            nint hdcSrc,
            ref Point ptSrc,
            int crKey,
            ref BlendFunction blend,
            int flags );

        [DllImport("gdi32.dll")]
        internal static extern nint CreateCompatibleDC(nint hdc);

        [DllImport("gdi32.dll")]
        internal static extern bool DeleteDC(nint hdc);

        [DllImport("gdi32.dll")]
        internal static extern nint SelectObject(nint hdc, nint obj);

        [DllImport("gdi32.dll")]
        internal static extern bool DeleteObject(nint obj);

        [DllImport(User32)]
        internal static extern nuint SetTimer(nint hwnd, nuint id, uint interval, nint callback);

        [DllImport(User32)]
        internal static extern bool InvalidateRect(nint hwnd, nint rect, bool erase);

        [DllImport(User32)]
        internal static extern nint BeginPaint(nint hwnd, out PaintStruct paint);

        [DllImport(User32)]
        internal static extern bool EndPaint(nint hwnd, ref PaintStruct paint);

        [DllImport(Kernel32, CharSet = CharSet.Unicode)]
        internal static extern nint GetModuleHandleW(string? moduleName);

        [DllImport(User32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern ushort RegisterClassExW(ref WindowClass windowClass);

        [DllImport(User32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern nint CreateWindowExW(
            uint exStyle,
            string className,
            string windowName,
            uint style,
            int x, int y,
            int width, int height,
            nint parent,
            nint menu,
            nint instance,
            nint param
        );

        [DllImport(User32)]
        internal static extern bool ShowWindow(nint hwnd, int command);

        [DllImport(User32)]
        internal static extern bool UpdateWindow(nint hwnd);

        [DllImport(User32)]
        internal static extern int GetMessageW(
            out Message msg, nint hwnd, uint min, uint max
            );

        [DllImport(User32)]
        internal static extern bool TranslateMessage(ref Message msg);

        [DllImport(User32)]
        internal static extern nint DispatchMessageW(ref Message msg);

        [DllImport(User32)]
        internal static extern nint DefWindowProcW(
            nint hwnd, uint msg, nuint wParam, nint lParam
            );

        [DllImport(User32)]
        internal static extern nint GetDC(nint hwnd);

        [DllImport(User32)]
        internal static extern int ReleaseDC(nint hwnd, nint hdc);

        [DllImport(User32)]
        internal static extern bool DestroyWindow(nint hwnd);

        [DllImport(User32)]
        internal static extern void PostQuitMessage(int exitCode);

        internal const uint WM_DESTROY = 0x0002;
        // internal const uint WS_OVERLAPPEDWINDOW = 0x00CF0000;
        internal const int SW_SHOW = 5;
        // internal const int CW_USEDEFAULT = unchecked((int)0x80000000);
        internal const uint WS_POPUP = 0x80000000;
        internal const uint WS_EX_TOPMOST = 0x00000008;
        internal const uint WS_EX_TOOLWINDOW = 0x00000080;
        internal const uint WS_EX_LAYERED = 0x00080000;
        internal const uint WS_VISIBLE = 0x10000000;
        internal const uint WM_PAINT = 0x000F;
        internal const uint WM_TIMER = 0x113;
        internal const uint WM_ERASEBKGND = 0x0014;
        internal const byte AC_SRC_OVER = 0;
        internal const byte AC_SRC_ALPHA = 1;
        internal const int ULW_ALPHA = 2;
    }
}
