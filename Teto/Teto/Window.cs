using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;
using System;
using NAudio.Wave;

namespace Teto {
    internal sealed class Window {
        private const string ClassName = "TetoWindow";
        private readonly Bitmap _bitmap;
        private readonly Native.WindowProc _windowProcedure;
        private nint _hwnd;
        private float _opacity = 1.0f;
        private readonly Stopwatch _stopwatch = new();
        private const float FadeDuration = 1.0f;
        private Mp3FileReader? _audio;
        private WaveOut? _player;

        private void PlayAudio() {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Stream audioStream = assembly.GetManifestResourceStream("Teto.teto.mp3")
                ?? throw new InvalidOperationException("Audio resource not found");

            _audio = new Mp3FileReader(audioStream);
            _player = new WaveOut();

            _player.Init(_audio);
            _player.Play();
        }

        private void StopAudio() {
            _player.Stop();
            _player.Dispose();
            _player = null;

            _audio?.Dispose();
            _audio = null;
        }

        public Window() {
            _windowProcedure = WndProc;

            Assembly assembly = Assembly.GetExecutingAssembly();
            using Stream imageStream = assembly.GetManifestResourceStream("Teto.teto.png")
                ?? throw new System.Exception("err : Resource Missing - teto.png");

            using Bitmap source = new(imageStream);
            _bitmap = new Bitmap(source);

            System.Diagnostics.Debug.WriteLine($"Bitmap Size: {_bitmap.Width}x{_bitmap.Height}"); // debug

            _stopwatch.Restart();
        }

        public void Run() {
            nint instance = Native.GetModuleHandleW(null);

            RegisterWindowClass(instance);
            _hwnd = CreateWindow(instance);

            System.Diagnostics.Debug.WriteLine($"Window Created: 0x{_hwnd:X}"); // debug

            //Native.ShowWindow(_hwnd, Native.SW_SHOW);
            //Native.UpdateWindow(_hwnd);

            StartFadeTimer(_hwnd);
            UpdateLayeredFrame();
            System.Diagnostics.Debug.WriteLine("Window Shown"); // debug

            PlayAudio();

            MessageLoop();
        }

        private void MessageLoop() {
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
            switch (msg) {
                case Native.WM_PAINT:
                    //Paint(hwnd);
                    return 0;

                case Native.WM_TIMER:
                    UpdateFade();
                    return 0;

                case Native.WM_DESTROY:
                    Native.PostQuitMessage(0);
                    return 0;

                case Native.WM_ERASEBKGND:
                    return 1;
            }

            return Native.DefWindowProcW(hwnd, msg, wParam, lParam);
        }

        private void StartFadeTimer(nint hwnd) {
            // 16 = ~60fps
            Native.SetTimer(hwnd, 1, 16, 0);
        }

        private void Paint(nint hwnd) {
            // is _surface worth considering here?
            using Bitmap surface = new(_bitmap.Width, _bitmap.Height);
            using Graphics graphics = Graphics.FromImage(surface);

            DrawBitmap(graphics);

            nint hdc = Native.BeginPaint(hwnd, out var paint);

            try {
                using Graphics windowGraphics = Graphics.FromHdc(hdc);
                windowGraphics.DrawImage(surface, 0, 0);
                //windowGraphics.Clear(Color.FromArgb(1, 0, 0, 0));
            } finally {
                Native.EndPaint(hwnd, ref paint);
            }
        }

        private void DrawBitmap(Graphics graphics) {
            ColorMatrix matrix = new() {
                Matrix00 = 1.0f,
                Matrix11 = 1.0f,
                Matrix22 = 1.0f,
                Matrix33 = _opacity,
                Matrix44 = 1.0f
            };

            using ImageAttributes attributes = new();
            attributes.SetColorMatrix(matrix);

            graphics.DrawImage(
                _bitmap,
                new Rectangle(0, 0, _bitmap.Width, _bitmap.Height),
                0,
                0,
                _bitmap.Width,
                _bitmap.Height,
                GraphicsUnit.Pixel,
                attributes
                );
        }

        private void UpdateFade() {
            float progress = _stopwatch.ElapsedMilliseconds / (FadeDuration * 1000.0f);
            _opacity = Math.Clamp(1.0f - progress, 0.0f, 1.0f);

            System.Diagnostics.Debug.WriteLine($"Opacity: {_opacity}"); // debug

            UpdateLayeredFrame();

            if (_opacity <= 0) {
                Native.DestroyWindow(_hwnd);
                return;
            }
        }

        private void RegisterWindowClass(nint instance) {
            var wc = new Native.WindowClass {
                cbSize = (uint)Marshal.SizeOf<Native.WindowClass>(),
                lpfnWndProc = _windowProcedure,
                hInstance = instance,
                hbrBackground = (nint)1,
                lpszClassName = ClassName
            };

            if (Native.RegisterClassExW(ref wc) == 0) {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        private void UpdateLayeredFrame() {
            System.Diagnostics.Debug.WriteLine("UpdateLayeredFrame"); // debug

            using Bitmap surface = new Bitmap(
                _bitmap.Width,
                _bitmap.Height,
                PixelFormat.Format32bppArgb);

            using Graphics graphics = Graphics.FromImage(surface);

            graphics.Clear(Color.Transparent);
            DrawBitmap(graphics);

            nint screenDc = Native.GetDC(0);
            nint memoryDc = Native.CreateCompatibleDC(screenDc);

            nint hBitmap = surface.GetHbitmap(Color.FromArgb(0));
            nint oldBitmap = Native.SelectObject(memoryDc, hBitmap);

            try {
                var size = new Native.Size {
                    cx = surface.Width,
                    cy = surface.Height
                };

                var sourcePoint = new Native.Point {
                    x = 0,
                    y = 0
                };

                var destinationPoint = new Native.Point {
                    x = 100,
                    y = 100
                };

                var blend = new Native.BlendFunction {
                    BlendOp = Native.AC_SRC_OVER,
                    BlendFlags = 0,
                    SourceConstantAlpha = 255,
                    AlphaFormat = Native.AC_SRC_ALPHA
                };


                bool success = Native.UpdateLayeredWindow(
                    _hwnd,
                    screenDc,
                    ref destinationPoint,
                    ref size,
                    memoryDc,
                    ref sourcePoint,
                    0,
                    ref blend,
                    Native.ULW_ALPHA);

                System.Diagnostics.Debug.WriteLine(
                    $"UpdateLayeredWindow = {success}");

                if (!success) {
                    System.Diagnostics.Debug.WriteLine(
                        $"Error = {Marshal.GetLastWin32Error()}");
                }
            } finally {
                Native.SelectObject(
                    memoryDc,
                    oldBitmap);

                Native.DeleteObject(hBitmap);
                Native.DeleteDC(memoryDc);
                Native.ReleaseDC(
                    0,
                    screenDc);
            }
        }

        private nint CreateWindow(nint instance) {
            nint hwnd = Native.CreateWindowExW(
                Native.WS_EX_LAYERED,
                ClassName,
                "Teto",
                Native.WS_POPUP | Native.WS_VISIBLE,
                100, 100,
                _bitmap.Width, _bitmap.Height,
                0, 0,
                instance,
                0
            );

            if (hwnd == 0) {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            return hwnd;
        }
    }
}
