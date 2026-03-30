using OpenCvSharp;
using SharpGen.Runtime;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using WinRT;

namespace CsAutoGUI;

public class GameWindowCapture : IDisposable
{
    // Win32 API for finding window
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
    [DllImport("d3d11.dll")]
    static extern int CreateDirect3D11DeviceFromDXGIDevice(IntPtr dxgiDevice, out IntPtr graphicsDevice);

    [ComImport]
    [Guid("3628e81b-3cac-4c60-b7f4-23ce0e0c3356")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IGraphicsCaptureItemInterop
    {
        IntPtr CreateForWindow(
            IntPtr window,
            ref Guid iid);
    }

    private ID3D11Device _d3dDevice;
    private IDirect3DDevice _direct3DDevice;
    private Direct3D11CaptureFramePool _framePool;
    private GraphicsCaptureSession _session;

    /// <summary>
    /// 初始化 WGC 捕获器
    /// </summary>
    public bool Initialize()
    {
        try
        {
            // 1. 创建 D3D11 设备（需要 BGRA 支持）
            _d3dDevice = D3D11.D3D11CreateDevice(
                DriverType.Hardware,
                DeviceCreationFlags.BgraSupport
            );

            // 2. 获取 DXGI 设备
            using IDXGIDevice dxgiDevice = _d3dDevice.QueryInterface<IDXGIDevice>();
            _ = CreateDirect3D11DeviceFromDXGIDevice(dxgiDevice.NativePointer, out var pUnknown);

            _direct3DDevice = MarshalInterface<IDirect3DDevice>.FromAbi(pUnknown);

            Marshal.Release(pUnknown);

            // 3. 使用 Vortice 的互操作方法创建 IDirect3DDevice

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"初始化失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 捕获指定窗口的一帧
    /// </summary>
    public async Task<Bitmap?> CaptureWindowAsync(IntPtr hwnd)
    {
        // 2. 创建 CaptureItem
        var captureItem = CaptureUtils.CreateItemForWindow(hwnd);

        Console.WriteLine($"窗口大小: {captureItem.Size.Width} x {captureItem.Size.Height}");

        // 3. 创建 FramePool
        _framePool = Direct3D11CaptureFramePool.CreateFreeThreaded(
            _direct3DDevice,
            DirectXPixelFormat.B8G8R8A8UIntNormalized,  // BGRA 格式
            2,  // 2个缓冲区
            captureItem.Size
        );

        // 4. 创建捕获会话
        _session = _framePool.CreateCaptureSession(captureItem);

        // 5. 使用 TaskCompletionSource 等待帧到达
        var tcs = new TaskCompletionSource<Direct3D11CaptureFrame>();

        _framePool.FrameArrived += (sender, args) =>
        {
            if (sender != null)
            {
                var frame = sender?.TryGetNextFrame();
                if (frame != null)
                {
                    tcs.TrySetResult(frame);
                }
            }
        };

        // 6. 开始捕获
        _session.StartCapture();

        try
        {
            // 7. 等待帧到达（5秒超时）
            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(5000));
            if (completedTask != tcs.Task)
            {
                Console.WriteLine("等待帧超时");
            }

            // 8. 处理帧
            using var frame = await tcs.Task;
            return await SaveFrameAsync(frame);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"捕获失败: {ex.Message}");
        }
        finally
        {
            // 9. 停止捕获并清理
            StopCapture();
        }
        return null;
    }

    /// <summary>
    /// 将帧保存为图片文件
    /// </summary>
    private static async Task<Bitmap> SaveFrameAsync(Direct3D11CaptureFrame frame)
    {
        // 1. 从帧表面创建 SoftwareBitmap
        var softwareBitmap = await SoftwareBitmap.CreateCopyFromSurfaceAsync(frame.Surface);
        if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8 ||
        softwareBitmap.BitmapAlphaMode == BitmapAlphaMode.Premultiplied)
        {
            softwareBitmap = SoftwareBitmap.Convert(
                softwareBitmap,
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Ignore);
        }

        int width = softwareBitmap.PixelWidth;
        int height = softwareBitmap.PixelHeight;

        byte[] pixels = new byte[width * height * 4];

        softwareBitmap.CopyToBuffer(pixels.AsBuffer());

        Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

        var bmpData = bitmap.LockBits(
            new Rectangle(0, 0, width, height),
            ImageLockMode.WriteOnly,
            PixelFormat.Format32bppArgb);

        Marshal.Copy(pixels, 0, bmpData.Scan0, pixels.Length);

        bitmap.UnlockBits(bmpData);

        return bitmap;
    }

    /// <summary>
    /// 停止捕获并释放帧池资源
    /// </summary>
    public void StopCapture()
    {
        try
        {
            _session?.Dispose();
            _session = null;

            _framePool?.Dispose();
            _framePool = null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"停止捕获时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 释放所有资源
    /// </summary>
    public void Dispose()
    {
        StopCapture();
        _direct3DDevice?.Dispose();
        _d3dDevice?.Dispose();
    }

    /// <summary>
    /// Capture辅助类
    /// </summary>
    public static class CaptureUtils
    {
        static readonly Guid GraphicsCaptureItemGuid = new Guid("79C3F95B-31F7-4EC2-A464-632EF5D30760");

        [ComImport]
        [Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IGraphicsCaptureItemInterop
        {
            IntPtr CreateForWindow([In] IntPtr window, [In] ref Guid iid);

            IntPtr CreateForMonitor([In] IntPtr monitor, [In] ref Guid iid);
        }


        [Guid("00000035-0000-0000-C000-000000000046")]
        internal unsafe struct IActivationFactoryVftbl
        {
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
            public readonly WinRT.IInspectable.Vftbl IInspectableVftbl;
            private readonly void* _ActivateInstance;
#pragma warning restore

            public readonly delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int> ActivateInstance => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)_ActivateInstance;
        }

        internal class Platform
        {
            [DllImport("api-ms-win-core-com-l1-1-0.dll")]
            internal static extern int CoDecrementMTAUsage(IntPtr cookie);

            [DllImport("api-ms-win-core-com-l1-1-0.dll")]
            internal static extern unsafe int CoIncrementMTAUsage(IntPtr* cookie);

            [DllImport("api-ms-win-core-winrt-l1-1-0.dll")]
            internal static extern unsafe int RoGetActivationFactory(IntPtr runtimeClassId, ref Guid iid, IntPtr* factory);
        }

        private static class WinRtModule
        {
            private static readonly Dictionary<string, ObjectReference<IActivationFactoryVftbl>> Cache = new Dictionary<string, ObjectReference<IActivationFactoryVftbl>>();

            public static ObjectReference<IActivationFactoryVftbl> GetActivationFactory(string runtimeClassId)
            {
                lock (Cache)
                {
                    if (Cache.TryGetValue(runtimeClassId, out var factory))
                        return factory;

                    var m = MarshalString.CreateMarshaler(runtimeClassId);

                    try
                    {
                        var instancePtr = GetActivationFactory(MarshalString.GetAbi(m));

                        factory = ObjectReference<IActivationFactoryVftbl>.Attach(ref instancePtr);
                        Cache.Add(runtimeClassId, factory);

                        return factory;
                    }
                    finally
                    {
                        m.Dispose();
                    }
                }
            }

            private static unsafe IntPtr GetActivationFactory(IntPtr hstrRuntimeClassId)
            {
                if (s_cookie == IntPtr.Zero)
                {
                    lock (s_lock)
                    {
                        if (s_cookie == IntPtr.Zero)
                        {
                            IntPtr cookie;
                            Marshal.ThrowExceptionForHR(Platform.CoIncrementMTAUsage(&cookie));

                            s_cookie = cookie;
                        }
                    }
                }

                Guid iid = typeof(IActivationFactoryVftbl).GUID;
                IntPtr instancePtr;
                int hr = Platform.RoGetActivationFactory(hstrRuntimeClassId, ref iid, &instancePtr);

                if (hr == 0)
                    return instancePtr;

                throw new Win32Exception(hr);
            }

            public static bool ResurrectObjectReference(IObjectReference objRef)
            {
                var disposedField = objRef.GetType().GetField("disposed", BindingFlags.NonPublic | BindingFlags.Instance)!;
                if (!(bool)disposedField.GetValue(objRef)!)
                    return false;
                disposedField.SetValue(objRef, false);
                GC.ReRegisterForFinalize(objRef);
                return true;
            }

            private static IntPtr s_cookie;
            private static readonly object s_lock = new object();
        }

        /// <summary>
        /// 根据窗口句柄创建 GraphicsCaptureItem 实例。
        /// </summary>
        /// <param name="hWnd">窗口句柄，指定要捕获的窗口。</param>
        /// <returns>返回一个 GraphicsCaptureItem 实例，表示捕获的窗口。</returns>
        /// <exception cref="Exception">当窗口不存在时抛出异常</exception>
        public static GraphicsCaptureItem CreateItemForWindow(IntPtr hWnd)
        {
            var factory = WinRtModule.GetActivationFactory("Windows.Graphics.Capture.GraphicsCaptureItem");
            var interop = factory.AsInterface<IGraphicsCaptureItemInterop>();
            var itemPointer = interop.CreateForWindow(hWnd, GraphicsCaptureItemGuid);
            var item = GraphicsCaptureItem.FromAbi(itemPointer);
            return item;
        }

        /// <summary>
        /// 根据显示器句柄创建 GraphicsCaptureItem 实例。
        /// </summary>
        /// <param name="hmon">显示器句柄，指定要捕获的显示器。</param>
        /// <returns>显示器句柄，指定要捕获的显示器。</returns>
        /// <exception cref="Exception">当显示器句柄错误时抛出异常。</exception>
        public static GraphicsCaptureItem CreateItemForMonitor(IntPtr hmon)
        {
            var factory = WinRtModule.GetActivationFactory("Windows.Graphics.Capture.GraphicsCaptureItem");
            var interop = factory.AsInterface<IGraphicsCaptureItemInterop>();
            var itemPointer = interop.CreateForMonitor(hmon, GraphicsCaptureItemGuid);
            var item = GraphicsCaptureItem.FromAbi(itemPointer);
            return item;
        }
    }
}