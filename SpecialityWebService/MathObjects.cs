using System.Globalization;

namespace SpecialityWebService
{
    public class MathObjects
    {
        public struct Point
        {
            public double X, Y;
            public Point(double x, double y) { X = x; Y = y; }

            public static Point operator +(Point me, Point other) => new Point(me.X + other.X, me.Y + other.Y);
            public static Point operator -(Point me, Point other) => new Point(me.X - other.X, me.Y - other.Y);

            public override string ToString()
            {
                return $"({X.ToString(CultureInfo.InvariantCulture)},{Y.ToString(CultureInfo.InvariantCulture)})";
            }
        }

        public struct Rectangle
        {
            public double MinX, MinY, MaxX, MaxY;
            public Rectangle(double centerX, double centerY, double inflate) : this(centerX, centerY, inflate, inflate) { }
            public Rectangle(double centerX, double centerY, double inflateX, double inflateY)
            {
                MinX = centerX - inflateX;
                MinY = centerY - inflateY;
                MaxX = centerX + inflateX;
                MaxY = centerY + inflateY;
            }

            public static Rectangle FromLTRB(double minx, double maxy, double maxx, double miny)
            {
                var r = new Rectangle();
                r.MinX = minx;
                r.MinY = miny;
                r.MaxX = maxx;
                r.MaxY = maxy;
                return r;
            }

            public static Rectangle Zero() => Rectangle.FromLTRB(0.0, 0.0, 0.0, 0.0);
            public static Rectangle Infinite() => Rectangle.FromLTRB(double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity, double.NegativeInfinity);
            public static Rectangle InfiniteInverse() => Rectangle.FromLTRB(double.PositiveInfinity, double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity);

            public Point GetCenter() => new Point(MinX + (MaxX - MinX) / 2.0, MinY + (MaxY - MinY) / 2.0);

            public bool Overlapping(Rectangle other) => !(this.Right < other.Left || this.Left > other.Right || this.Bottom > other.Top || this.Top < other.Bottom);

            public override string ToString()
            {
                return $"ll=({Left.ToString(CultureInfo.InvariantCulture)},{Bottom.ToString(CultureInfo.InvariantCulture)}),ur=({Right.ToString(CultureInfo.InvariantCulture)},{Top.ToString(CultureInfo.InvariantCulture)}),w={Width.ToString(CultureInfo.InvariantCulture)},h={Height.ToString(CultureInfo.InvariantCulture)}";
            }

            public double Width => MaxX - MinX;
            public double Height => MaxY - MinY;

            public double Left => MinX;
            public double Top => MaxY;
            public double Right => MaxX;
            public double Bottom => MinY;
        }
    }
}
