
namespace CsAutoGUI;

/// <summary>
/// Smart Rect
/// <br/> implicit <see cref="Tuple{int,int,int,int}"/>
/// <br/> implicit <see cref="OpenCvSharp.Rect"/>
/// </summary>
/// <param name="X"></param>
/// <param name="Y"></param>
/// <param name="Width"></param>
/// <param name="Height"></param>
public record SmartRect(int X, int Y, int Width, int Height)
{
    public static implicit operator OpenCvSharp.Rect(SmartRect rect)
    {
        return new OpenCvSharp.Rect(rect.X, rect.Y, rect.Width, rect.Height);
    }

    public static implicit operator SmartRect(OpenCvSharp.Rect rect)
    {
        return new SmartRect(rect.X, rect.Y, rect.Width, rect.Height);
    }

    public static implicit operator (int, int, int, int)(SmartRect rect)
    {
        return (rect.X, rect.Y, rect.Width, rect.Height);
    }

    public static implicit operator SmartRect((int, int, int, int) rect)
    {
        return new SmartRect(rect.Item1, rect.Item2, rect.Item3, rect.Item4);
    }

    private SmartPoint? _center = null;
    public SmartPoint Center
    {
        get
        {
            _center ??= new(X + Width / 2, Y + Height / 2);
            return _center;
        }
    }
}