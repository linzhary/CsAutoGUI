using CsAutoGUI;
using System.Reflection.Metadata;

using var capture = new GameWindowCapture();

// 初始化
if (!capture.Initialize())
{
    Console.WriteLine("初始化失败，请确保：");
    Console.WriteLine("1. Windows 10 1903 或更高版本");
    Console.WriteLine("2. 项目 TargetFramework 为 net8.0-windows10.0.19041.0");
    return;
}
var window = AutoGUI.FindWindowLikeTitle("Endfield");
if (window is null) return;
var bmp = await capture.CaptureWindowAsync(window.Handle);

//string savePath = Path.Combine(Environment.CurrentDirectory, "screenshot.png");
//Console.WriteLine($"截图将保存到: {savePath}");

//// 执行截图
//Console.WriteLine("正在捕获...");
//bool success = await capture.CaptureWindowAsync(window.Handle, savePath);

//if (success)
//{
//    Console.WriteLine($"截图成功！");

//    // 尝试打开文件
//    if (File.Exists(savePath))
//    {
//        Console.WriteLine($"文件大小: {new FileInfo(savePath).Length} 字节");
//    }
//}
//else
//{
//    Console.WriteLine("截图失败");
//}

//Console.WriteLine("\n按任意键退出...");
//Console.ReadKey();
//window.Active();

//window.KeyPress(VirtualKey.T);
//Task.Delay(1000).Wait();
//window.KeyPress(VirtualKey.T);

//window.Active(false);

using var bmp = window.Capiture();
bmp.Save("test.png", System.Drawing.Imaging.ImageFormat.Png);
//var botton = window.Locate("2.png");
//while (true)
//{
//    var rect = window.Locate("1.png");
//    if(rect is not null)
//    {
//        Console.WriteLine("找到了");
//        break;
//    }
//    var wndRect = window.GetRegion();
//    window.Active();
//    window.LeftClick(botton.Center.X + wndRect.X, botton.Center.Y + wndRect.Y);
//    window.Active(false);

//    Task.Delay(100).Wait();
//}

//// Use static GDI-based capture for non-foreground window
//using var bmp = FastScreenCapture.CaptureWindowNonForeground(window.Handle);
//bmp.Save("test.png", ImageFormat.Png);