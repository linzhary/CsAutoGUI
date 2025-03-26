namespace CsAutoGUI;
public partial class AutoGUI
{
    public static bool MoveTo(int x, int y)
    {
        return PInvoke.SetCursorPos(x, y);
    }

    public static void LeftClick(int x, int y)
    {
        PInvoke.GetCursorPos(out var point);
        MoveTo(x, y);
        var screenWidth = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXSCREEN);
        var screenHeight = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYSCREEN);
        x = x * 65536 / screenWidth;
        y = y * 65535 / screenHeight;
        PInvoke.mouse_event(MOUSE_EVENT_FLAGS.MOUSEEVENTF_LEFTUP | MOUSE_EVENT_FLAGS.MOUSEEVENTF_LEFTDOWN | MOUSE_EVENT_FLAGS.MOUSEEVENTF_ABSOLUTE, x, y, 0, 0);
        MoveTo(point.X, point.Y);
    }

    public static void RightClick(int x, int y)
    {
        PInvoke.GetCursorPos(out var point);
        MoveTo(x, y);
        var screenWidth = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXSCREEN);
        var screenHeight = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYSCREEN);
        x = x * 65536 / screenWidth;
        y = y * 65535 / screenHeight;
        PInvoke.mouse_event(MOUSE_EVENT_FLAGS.MOUSEEVENTF_RIGHTUP | MOUSE_EVENT_FLAGS.MOUSEEVENTF_RIGHTDOWN | MOUSE_EVENT_FLAGS.MOUSEEVENTF_ABSOLUTE, x, y, 0, 0);
        MoveTo(point.X, point.Y);
    }
}