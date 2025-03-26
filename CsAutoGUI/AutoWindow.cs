using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace CsAutoGUI;

public class AutoWindow
{
    public static IntPtr FindWindowLikeTitle(string titleLike)
    {
        IntPtr hWndResult = IntPtr.Zero;
        // 调用 EnumWindows 枚举所有窗口
        PInvoke.EnumWindows((hWnd, lParam) =>
        {
            if (PInvoke.IsWindowVisible(hWnd)) // 只获取可见窗口
            {
                int length = PInvoke.GetWindowTextLength(hWnd);
                if (length > 0)
                {
                    var chars = new char[length + 1];
                    _ = PInvoke.GetWindowText(hWnd, chars);
                    if (string.Concat(chars).Contains(titleLike))
                    {
                        hWndResult = hWnd;
                        return false;
                    }
                }
            }

            return true; // 继续枚举
        }, IntPtr.Zero);
        return hWndResult;
    }

    public static void SetForegroundWindow(IntPtr hWnd)
    {
        PInvoke.SetForegroundWindow((HWND)hWnd);
    }

    public static Rect GetWindowRect(IntPtr hWnd)
    {
        PInvoke.GetWindowRect((HWND)hWnd, out var rect);
        return new Rect(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);
    }

    private static System.Drawing.Bitmap CapitureWindow(Rect rect)
    {
        var desktopWnd = PInvoke.GetDesktopWindow();
        var windowDC = PInvoke.GetDC(desktopWnd);
        var memoryDC = PInvoke.CreateCompatibleDC(windowDC);
        var hBitmap = PInvoke.CreateCompatibleBitmap(windowDC, rect.Width, rect.Height);
        var oldBitmap = PInvoke.SelectObject(memoryDC, hBitmap);
        // 复制屏幕到内存 DC
        PInvoke.BitBlt(memoryDC, 0, 0, rect.Width, rect.Height, windowDC, rect.X, rect.Y, ROP_CODE.SRCCOPY);
        // 创建 Bitmap 并保存
        var bmp = System.Drawing.Image.FromHbitmap(hBitmap);
        // 释放资源
        PInvoke.SelectObject(memoryDC, oldBitmap);
        PInvoke.DeleteObject(hBitmap);
        PInvoke.DeleteDC(memoryDC);
        PInvoke.ReleaseDC(desktopWnd, windowDC);
        return bmp;
    }

    public static Box? LocateOnScreen(
        string imagePach,
        bool grayscale = true,
        Rect? rect = null,
        double confidence = 0.999)
    {
        var haystackImage = default(Mat?);
        if (rect is null)
        {
            var width = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXSCREEN);
            var height = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYSCREEN);
            rect = new Rect(0, 0, width, height);
        }

        haystackImage ??= CapitureWindow(rect.Value).ToMat();
        var needleImage = Cv2.ImRead(imagePach);
        if (grayscale)
        {
            needleImage.CvtColor(ColorConversionCodes.BGR2GRAY);
            haystackImage.CvtColor(ColorConversionCodes.BGR2GRAY);
        }

        if (haystackImage.Rows < needleImage.Rows || haystackImage.Cols < needleImage.Cols)
        {
            throw new ArgumentException("Needle dimensions exceed the haystack image or region dimensions");
        }

        // 进行模板匹配
        var result = haystackImage.MatchTemplate(needleImage, TemplateMatchModes.CCoeffNormed);
        result.MinMaxLoc(out _, out double maxVal, out _, out var maxLoc);
        
        // 如果未找到匹配
        if (maxVal >= confidence)
        {
            return new Box(maxLoc.X, maxLoc.Y, needleImage.Cols, needleImage.Rows);
        }

        return null;
    }
}