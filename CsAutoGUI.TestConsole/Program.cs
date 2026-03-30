using CsAutoGUI;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using PaddleOCRSharp;
using System.Drawing.Imaging;

var window = AutoGUI.FindWindowByProcessName("Arknights");
if (window is null)
{
    Console.WriteLine("未找到 Arknights 进程");
    return;
}

Console.WriteLine("正在截图...");
using var bmp = window.Capiture();
if (bmp is null)
{
    Console.WriteLine("截图失败");
    return;
}

bmp.Save("明日方舟.png", ImageFormat.Png);
Console.WriteLine($"截图尺寸: {bmp.Width} x {bmp.Height}");
