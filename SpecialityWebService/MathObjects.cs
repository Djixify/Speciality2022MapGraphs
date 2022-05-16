using RBush;
using System;
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

            public double Distance(Point other) {
                double distx = X - other.X;
                double disty = Y - other.Y;
                return Math.Sqrt(distx * distx + disty * disty);
            }

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

            public double Width => MaxX - MinX;
            public double Height => MaxY - MinY;

            public double Left => MinX;
            public double Top => MaxY;
            public double Right => MaxX;
            public double Bottom => MinY;


            public Point LeftTop => new Point(MinX, MaxX);
            public Point RightTop => new Point(MaxX, MaxX);
            public Point LeftBottom => new Point(MinX, MinX);
            public Point RightBottom => new Point(MaxX, MinX);


            public static Rectangle Zero() => Rectangle.FromLTRB(0.0, 0.0, 0.0, 0.0);
            public static Rectangle Infinite() => Rectangle.FromLTRB(double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity, double.NegativeInfinity);
            public static Rectangle InfiniteInverse() => Rectangle.FromLTRB(double.PositiveInfinity, double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity);
            public bool Contains(Point p) => !(p.X < MinX || p.X > MaxX || p.Y < MinY || p.Y > MaxY);
            public double ClosestDistanceToPoint(Point p)
            {
                if (Contains(p))
                    return 0.0;

                if (p.X >= MinX && p.X <= MaxX)
                    return p.Y > MaxY ? p.Y - MaxY : MinY - p.Y;
                else if (p.Y >= MinY && p.Y <= MaxY)
                    return p.X > MaxX ? p.X - MaxX : MinX - p.X;
                else if (p.X < MinX && p.Y < MinY)
                    return p.Distance(LeftBottom);
                else if (p.X > MaxX && p.Y < MinY)
                    return p.Distance(RightBottom);
                else if (p.X < MinX && p.Y > MaxY)
                    return p.Distance(LeftTop);
                else if (p.X > MaxX && p.Y > MaxY)
                    return p.Distance(RightTop);
                return -1.0; //Should not be reached
            }

            public Point GetCenter() => new Point(MinX + (MaxX - MinX) / 2.0, MinY + (MaxY - MinY) / 2.0);

            public bool Overlapping(Rectangle other) => !(this.Right < other.Left || this.Left > other.Right || this.Bottom > other.Top || this.Top < other.Bottom);

            public void Expand(double horizontally, double vertically)
            {
                double halfw = horizontally * 0.5;
                double halfh = vertically * 0.5;
                MinX -= halfw;
                MaxX += halfw;
                MinY -= halfh;
                MaxY += halfh;
            }

            public override string ToString()
            {
                return $"ll=({Left.ToString(CultureInfo.InvariantCulture)},{Bottom.ToString(CultureInfo.InvariantCulture)}),ur=({Right.ToString(CultureInfo.InvariantCulture)},{Top.ToString(CultureInfo.InvariantCulture)}),w={Width.ToString(CultureInfo.InvariantCulture)},h={Height.ToString(CultureInfo.InvariantCulture)}";
            }


            public static implicit operator Envelope(Rectangle rectangle) => new Envelope(rectangle.MinX, rectangle.MinY, rectangle.MaxX, rectangle.MaxY);

            public static implicit operator Rectangle(Envelope envelope) => Rectangle.FromLTRB(envelope.MinX, envelope.MaxY, envelope.MaxX, envelope.MinY);
        }
    }
}
