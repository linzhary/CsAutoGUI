using OpenCvSharp;
using OpenCvSharp.Extensions;
using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

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

    public static Rect? LocateOnScreen(
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
//#if DEBUG
//        haystackImage = Cv2.ImRead("temp.png");
//        rect = new Rect(0, 0, haystackImage.Rows, haystackImage.Height);
//#endif
        haystackImage ??= CapitureWindow(rect.Value).ToMat();
        var needleImage = Cv2.ImRead(imagePach);
        foreach (var box in LocateAllOpenCV(needleImage, haystackImage, grayscale, confidence))
        {
            return box;
        }
        return null;
    }

    private static IEnumerable<Rect> LocateAllOpenCV(
        Mat needleImage,
        Mat haystackImage,
        bool grayscale = true,
        double confidence = 0.999)
    {
        // 如果使用灰度模式，转换图像为灰度
        if (grayscale)
        {
            needleImage.CvtColor(ColorConversionCodes.BGR2GRAY);
            haystackImage.CvtColor(ColorConversionCodes.BGR2GRAY);
        }

        int needleHeight = needleImage.Rows;
        int needleWidth = needleImage.Cols;

        // 如果 `haystackCropped` 小于 `needleImage`，抛出异常
        if (haystackImage.Rows < needleImage.Rows || haystackImage.Cols < needleImage.Cols)
        {
            throw new ArgumentException("Needle dimensions exceed the haystack image or region dimensions");
        }

        // 进行模板匹配
        var result = haystackImage.MatchTemplate(needleImage, TemplateMatchModes.CCoeffNormed);

        result.MinMaxLoc(out var minVal, out double maxVal, out var minLoc, out var maxLoc);


        // 如果未找到匹配
        if (maxVal < confidence)
        {
            throw new Exception($"Could not locate the image (highest confidence = {maxVal:F3})");
        }
        else
        {
            yield return new Rect(maxLoc.X, maxLoc.Y, needleWidth, needleHeight);
        }
        yield break;
    }
}
