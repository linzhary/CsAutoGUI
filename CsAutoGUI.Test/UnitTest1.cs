using System.Threading.Tasks;

namespace CsAutoGUI.Test;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        var handle = AutoWindow.FindWindowLikeTitle("正规大猫部落");
        var region = AutoWindow.GetWindowRegion(handle);
        AutoWindow.SetForegroundWindow(handle);
        Task.Delay(1000).Wait();
        var pinyin = "buzhidao";
        foreach (var ch in pinyin)
        {
            AutoKeyboard.Press(Enum.Parse<VirtualKey>(ch.ToString(), true));
        }
        AutoKeyboard.Press(VirtualKey.SPACE);
        AutoKeyboard.Press(VirtualKey.RETURN);
        //var box = AutoWindow.LocateOnRegion("2.png", region, confidence: 0.9);
        //if (box is not null)
        //{
        //    AutoMouse.MoveToCenter(box);
        //}
    }
}