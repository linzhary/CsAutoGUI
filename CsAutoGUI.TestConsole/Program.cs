using CsAutoGUI;


var window = AutoGUI.FindWindowLikeTitle("内鬼情报交流群");
if (window is null) return;
var region = window.GetRegion();
window.Active();
foreach (var ch in "buzhidao")
{
    AutoGUI.Press(Enum.Parse<VirtualKey>(ch.ToString(), ignoreCase: true));
}
AutoGUI.Press(VirtualKey.SPACE);
AutoGUI.Press(VirtualKey.RETURN);