using OpenCvSharp;

namespace CsAutoGUI;

public record Box(int X, int Y, int Width, int Height)
{
    public static implicit operator Rect(Box box)
    {
        return new Rect(box.X, box.Y, box.Width, box.Height);
    }

    public static implicit operator Box(Rect rect)
    {
        return new Box(rect.X, rect.Y, rect.Width, rect.Height);
    }
}