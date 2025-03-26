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

    public static Box GetWindowRegion(IntPtr hWnd)
    {
        PInvoke.GetWindowRect((HWND)hWnd, out var rect);
        return new Box(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);
    }

    private static System.Drawing.Bitmap CapitureWindow(Box region)
    {
        var desktopWnd = PInvoke.GetDesktopWindow();
        var windowDC = PInvoke.GetDC(desktopWnd);
        var memoryDC = PInvoke.CreateCompatibleDC(windowDC);
        var hBitmap = PInvoke.CreateCompatibleBitmap(windowDC, region.Width, region.Height);
        var oldBitmap = PInvoke.SelectObject(memoryDC, hBitmap);
        // 复制屏幕到内存 DC
        PInvoke.BitBlt(memoryDC, 0, 0, region.Width, region.Height, windowDC, region.X, region.Y, ROP_CODE.SRCCOPY);
        // 创建 Bitmap 并保存
        var bmp = System.Drawing.Image.FromHbitmap(hBitmap);
        // 释放资源
        PInvoke.SelectObject(memoryDC, oldBitmap);
        PInvoke.DeleteObject(hBitmap);
        PInvoke.DeleteDC(memoryDC);
        PInvoke.ReleaseDC(desktopWnd, windowDC);
        return bmp;
    }

    public static Box? LocateOnRegion(
        string imagePach,
        Box region,
        double confidence = 0.999,
        bool grayscale = true)
    {
        var haystackImage = CapitureWindow(region).ToMat();
        var needleImage = Cv2.ImRead(imagePach);
        var box = Locate(haystackImage, needleImage, grayscale, confidence);
        if (box is null) return null;
        return box with
        {
            X = box.X + region.X,
            Y = box.Y + region.Y,
        };
    }

    public static Box? LocateOnScreen(
        string imagePach,
        double confidence = 0.999,
        bool grayscale = true)
    {
        var screenWidth = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXSCREEN);
        var screenHeight = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYSCREEN);
        var region = new Box(0, 0, screenWidth, screenHeight);
        var haystackImage = CapitureWindow(region).ToMat();
        var needleImage = Cv2.ImRead(imagePach);
        return Locate(haystackImage, needleImage, grayscale, confidence);
    }

    private static Box? Locate(Mat haystackImage, Mat needleImage, bool grayscale, double confidence)
    {
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
            var box = new Box(maxLoc.X, maxLoc.Y, needleImage.Cols, needleImage.Rows);
            //// 在原图上绘制红色边框
            //Cv2.Rectangle(haystackImage, box, Scalar.Red, 2);
            //// 显示结果
            //Cv2.ImShow("匹配结果", haystackImage);
            //Cv2.WaitKey(0);
            //Cv2.DestroyAllWindows();
            return box;
        }

        return null;
    }
}