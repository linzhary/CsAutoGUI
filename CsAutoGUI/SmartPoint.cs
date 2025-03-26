
namespace CsAutoGUI;

/// <summary>
/// Smart Point
/// <br/> implicit <see cref="Tuple{int,int}"/>
/// <br/> implicit <see cref="System.Drawing.Point"/>
/// <br/> implicit <see cref="OpenCvSharp.Point"/>
/// </summary>
/// <param name="X"></param>
/// <param name="Y"></param>
public record SmartPoint(int X, int Y)
{
    public static implicit operator System.Drawing.Point(SmartPoint point)
    {
        return new System.Drawing.Point(point.X, point.Y);
    }

    public static implicit operator SmartPoint(System.Drawing.Point point)
    {
        return new SmartPoint(point.X, point.Y);
    }

    public static implicit operator OpenCvSharp.Point(SmartPoint point)
    {
        return new OpenCvSharp.Point(point.X, point.Y);
    }

    public static implicit operator SmartPoint(OpenCvSharp.Point point)
    {
        return new SmartPoint(point.X, point.Y);
    }

    public static implicit operator (int, int)(SmartPoint point)
    {
        return (point.X, point.Y);
    }

    public static implicit operator SmartPoint((int, int) point)
    {
        return new SmartPoint(point.Item1, point.Item2);
    }
}
