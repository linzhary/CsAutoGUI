using CsAutoGUI;

var handle = AutoWindow.FindWindowLikeTitle("修仙聊天群MOD反馈群");
var region = AutoWindow.GetWindowRegion(handle);
AutoWindow.SetForegroundWindow(handle);
foreach(var ch in "buzhidao")
{
    AutoKeyboard.Press(Enum.Parse<VirtualKey>(ch.ToString(), ignoreCase: true));
}
AutoKeyboard.Press(VirtualKey.SPACE);
AutoKeyboard.Press(VirtualKey.RETURN);