namespace CsAutoGUI.Test;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        var box = AutoWindow.LocateOnScreen("1.png", confidence: 0.9);
        if (box is not null)
        {
            AutoMouse.MoveTo(box.X, box.Y);
        }
    }
}