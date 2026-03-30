using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Diagnostics;
using System.Drawing;
using System.Reflection.Metadata;

namespace CsAutoGUI;

public partial class AutoGUI
{
    /// <summary>
    /// 通过进程名查找窗口（不含 .exe 后缀）
    /// </summary>
    public static AutoWindow? FindWindowByProcessName(string processName)
    {
        var processes = Process.GetProcessesByName(processName);
        foreach (var proc in processes)
        {
            if (proc.MainWindowHandle != IntPtr.Zero)
            {
                return new AutoWindow((HWND)proc.MainWindowHandle);
            }
        }
        return null;
    }

    public static AutoWindow? FindWindowLikeTitle(string titleLike)
    {
        var hWndResult = HWND.Null;
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
                    var title = string.Concat(chars);
                    if (title.Contains(titleLike))
                    {
                        hWndResult = hWnd;
                        return false;
                    }
                }
            }

            return true; // 继续枚举
        }, IntPtr.Zero);
        return hWndResult == HWND.Null ? null : new AutoWindow(hWndResult);
    }

    private static System.Drawing.Bitmap CapitureWindow(SmartRect region)
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

    public static SmartRect? LocateOnRegion(
        string imagePach,
        SmartRect region,
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

    public static SmartRect? LocateOnScreen(
        string imagePatch,
        double confidence = 0.999,
        bool grayscale = true)
    {
        var screenWidth = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXSCREEN);
        var screenHeight = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYSCREEN);
        var region = new SmartRect(0, 0, screenWidth, screenHeight);
        var haystackImage = CapitureWindow(region).ToMat();
        var needleImage = Cv2.ImRead(imagePatch);
        return Locate(haystackImage, needleImage, grayscale, confidence);
    }

    internal static SmartRect? Locate(Mat haystackImage, Mat needleImage, bool grayscale, double confidence)
    {
        if (grayscale)
        {
            needleImage = needleImage.CvtColor(ColorConversionCodes.BGR2GRAY);
            haystackImage = haystackImage.CvtColor(ColorConversionCodes.BGR2GRAY);
        }
        else
        {
            needleImage = needleImage.CvtColor(ColorConversionCodes.BGRA2BGR);
            haystackImage = haystackImage.CvtColor(ColorConversionCodes.BGRA2BGR);
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
            var box = new SmartRect(maxLoc.X, maxLoc.Y, needleImage.Cols, needleImage.Rows);
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

    /// <summary>
    /// 在图像中查找所有匹配位置
    /// </summary>
    public static List<SmartRect> LocateAll(Mat haystackImage, Mat needleImage, bool grayscale, double confidence)
    {
        if (grayscale)
        {
            needleImage = needleImage.CvtColor(ColorConversionCodes.BGR2GRAY);
            haystackImage = haystackImage.CvtColor(ColorConversionCodes.BGR2GRAY);
        }
        else
        {
            needleImage = needleImage.CvtColor(ColorConversionCodes.BGRA2BGR);
            haystackImage = haystackImage.CvtColor(ColorConversionCodes.BGRA2BGR);
        }

        if (haystackImage.Rows < needleImage.Rows || haystackImage.Cols < needleImage.Cols)
        {
            throw new ArgumentException("Needle dimensions exceed the haystack image or region dimensions");
        }

        var result = haystackImage.MatchTemplate(needleImage, TemplateMatchModes.CCoeffNormed);
        var matches = new List<SmartRect>();

        while (true)
        {
            result.MinMaxLoc(out _, out double maxVal, out _, out var maxLoc);
            if (maxVal < confidence) break;

            matches.Add(new SmartRect(maxLoc.X, maxLoc.Y, needleImage.Cols, needleImage.Rows));

            // 将已匹配区域置零，避免重复匹配
            int x0 = Math.Max(0, maxLoc.X - needleImage.Cols / 2);
            int y0 = Math.Max(0, maxLoc.Y - needleImage.Rows / 2);
            int x1 = Math.Min(result.Cols, maxLoc.X + needleImage.Cols / 2);
            int y1 = Math.Min(result.Rows, maxLoc.Y + needleImage.Rows / 2);
            result[y0, y1, x0, x1].SetTo(new Scalar(0));
        }

        return matches;
    }
}

public class AutoWindow
{
    private readonly HWND _hWnd;
    private readonly GameWindowCapture _capture;

    public IntPtr Handle => (IntPtr)_hWnd;

    public SmartRect GetRegion()
    {
        PInvoke.GetWindowRect(_hWnd, out var rect);
        return new SmartRect(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);
    }

    internal AutoWindow(HWND hWnd)
    {
        _hWnd = hWnd;
        _capture = new GameWindowCapture();
        if (!_capture.Initialize())
        {
            Console.WriteLine("GameWindowCapture 初始化失败");
            return;
        }
    }

    public void Active(bool active=true)
    {
        PInvoke.PostMessage(_hWnd, PInvoke.WM_ACTIVATE, active ? PInvoke.WA_ACTIVE : PInvoke.WA_INACTIVE, 0);
        Task.Delay(10).Wait();
    }

    public void KeyPress(params VirtualKey[] keys)
    {
        AutoGUI.Press(_hWnd, keys);
    }
    public void LeftClick(int x, int y)
    {
        var lParam = (IntPtr)((y << 16) | (x & 0xFFFF));
        PInvoke.PostMessage(_hWnd, PInvoke.WM_LBUTTONDOWN, 1, lParam);
        Task.Delay(10).Wait();
        PInvoke.PostMessage(_hWnd, PInvoke.WM_LBUTTONUP, 0, lParam);
    }

    public SmartRect? Locate(
        string imagePach,
        double confidence = 0.999,
        bool grayscale = true)
    {
        using var bmp = _capture.CaptureWindowAsync(Handle).Result;
        var haystackImage = bmp.ToMat();
        var needleImage = Cv2.ImRead(imagePach);
        return AutoGUI.Locate(haystackImage, needleImage, grayscale, confidence);
    }

    public List<SmartRect> LocateAll(
        string imagePach,
        double confidence = 0.999,
        bool grayscale = true)
    {
        using var bmp = _capture.CaptureWindowAsync(Handle).Result;
        var haystackImage = bmp.ToMat();
        var needleImage = Cv2.ImRead(imagePach);
        return AutoGUI.LocateAll(haystackImage, needleImage, grayscale, confidence);
    }

    public Bitmap Capiture()
    {
        return _capture.CaptureWindowAsync(Handle).Result;
    }
}
