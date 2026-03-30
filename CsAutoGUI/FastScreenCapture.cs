using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace CsAutoGUI
{
    public static class FastScreenCapture
    {
        // Fallback GDI capture for non-foreground windows when WinRT capture is not available.
        // Keep as compatibility shim; actual FramePool-based capture requires WinRT and extra packages.

        private const uint PW_RENDERFULLCONTENT = 0x00000002;
        private const int SRCCOPY = 0x00CC0020;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight,
                                          IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }

        public static Bitmap CaptureWindowNonForeground(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) throw new ArgumentNullException(nameof(hWnd));

            if (!GetWindowRect(hWnd, out RECT rect))
                throw new InvalidOperationException("无法获取窗口矩形。");

            int width = Math.Max(1, rect.Right - rect.Left);
            int height = Math.Max(1, rect.Bottom - rect.Top);

            IntPtr hdcWindow = IntPtr.Zero;
            IntPtr hdcMemDC = IntPtr.Zero;
            IntPtr hBitmap = IntPtr.Zero;
            IntPtr hOld = IntPtr.Zero;

            try
            {
                hdcWindow = GetWindowDC(hWnd);
                if (hdcWindow == IntPtr.Zero)
                    throw new InvalidOperationException("无法获取窗口 DC。");

                hdcMemDC = CreateCompatibleDC(hdcWindow);
                if (hdcMemDC == IntPtr.Zero)
                    throw new InvalidOperationException("无法创建兼容内存 DC。");

                hBitmap = CreateCompatibleBitmap(hdcWindow, width, height);
                if (hBitmap == IntPtr.Zero)
                    throw new InvalidOperationException("无法创建兼容位图。");

                hOld = SelectObject(hdcMemDC, hBitmap);

                bool pwSuccess = false;
                try
                {
                    pwSuccess = PrintWindow(hWnd, hdcMemDC, PW_RENDERFULLCONTENT);
                }
                catch
                {
                    pwSuccess = false;
                }

                if (!pwSuccess)
                {
                    if (!BitBlt(hdcMemDC, 0, 0, width, height, hdcWindow, 0, 0, SRCCOPY))
                    {
                        throw new InvalidOperationException("BitBlt 失败，无法捕获窗口像素。");
                    }
                }

                Bitmap bmp = Image.FromHbitmap(hBitmap);
                Bitmap result = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(result))
                {
                    g.DrawImage(bmp, 0, 0);
                }

                bmp.Dispose();

                return result;
            }
            finally
            {
                if (hOld != IntPtr.Zero && hdcMemDC != IntPtr.Zero)
                {
                    SelectObject(hdcMemDC, hOld);
                }

                if (hBitmap != IntPtr.Zero)
                {
                    DeleteObject(hBitmap);
                }

                if (hdcMemDC != IntPtr.Zero)
                {
                    DeleteDC(hdcMemDC);
                }

                if (hdcWindow != IntPtr.Zero)
                {
                    ReleaseDC(hWnd, hdcWindow);
                }
            }
        }
    }
}