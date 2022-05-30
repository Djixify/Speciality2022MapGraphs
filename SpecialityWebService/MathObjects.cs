using RBush;
using SpecialityWebService.Generation;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace SpecialityWebService
{
    public class MathObjects
    {
        public struct Point : IFileItem<Point>
        {
            public double X, Y;
            public Point(double x, double y) { X = x; Y = y; }

            public static Point operator +(Point me, Point other) => new Point(me.X + other.X, me.Y + other.Y);
            public static Point operator -(Point me, Point other) => new Point(me.X - other.X, me.Y - other.Y);
            public static Point operator *(double s, Point other) => new Point(s * other.X, s * other.Y);

            public static bool operator ==(Point left, Point right) => left.X == right.X && left.Y == right.Y;
            public static bool operator !=(Point left, Point right) => left.X != right.X || left.Y != right.Y;

            public double Distance(Point other) {
                double distx = X - other.X;
                double disty = Y - other.Y;
                return Math.Sqrt(distx * distx + disty * disty);
            }

            public override string ToString()
            {
                return $"({X.ToString(CultureInfo.InvariantCulture)},{Y.ToString(CultureInfo.InvariantCulture)})";
            }

            public static Point FromReader(BinaryReader br)
            {
                Point p = new Point(0, 0);
                p.Read(br);
                return p;
            }

            public void Read(BinaryReader br)
            {
                X = BitConverter.ToDouble(br.ReadBytes(8));
                Y = BitConverter.ToDouble(br.ReadBytes(8));
            }

            public void Write(BinaryWriter bw)
            {
                bw.Write(BitConverter.GetBytes(X));
                bw.Write(BitConverter.GetBytes(Y));
            }
        }

        public struct Rectangle : IFileItem<Rectangle>
        {
            public double MinX, MinY, MaxX, MaxY;
            public Rectangle(Point center, double inflate) : this(center.X, center.Y, inflate, inflate) { }
            public Rectangle(double centerX, double centerY, double inflate) : this(centerX, centerY, inflate, inflate) { }
            public Rectangle(Point center, double inflateX, double inflateY) : this(center.X, center.Y, inflateX, inflateY) { }
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


            public Point LeftTop => new Point(MinX, MaxY);
            public Point RightTop => new Point(MaxX, MaxY);
            public Point LeftBottom => new Point(MinX, MinY);
            public Point RightBottom => new Point(MaxX, MinY);

            public Point Center => new Point(MinX + (MaxX - MinX) / 2.0, MinY + (MaxY - MinY) / 2.0);

            public Region HorizontalRegion => new Region(MinX, MaxX);
            public Region VerticalRegion => new Region(MinY, MaxY);


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

            public Rectangle Union(Rectangle other) =>
                Rectangle.FromLTRB(Math.Min(this.MinX, other.MinX), Math.Max(this.MaxY, other.MaxY), Math.Max(this.MaxX, other.MaxX), Math.Min(this.MinY, other.MinY));

            public override string ToString()
            {
                return $"ll=({Left.ToString(CultureInfo.InvariantCulture)},{Bottom.ToString(CultureInfo.InvariantCulture)}),ur=({Right.ToString(CultureInfo.InvariantCulture)},{Top.ToString(CultureInfo.InvariantCulture)}),w={Width.ToString(CultureInfo.InvariantCulture)},h={Height.ToString(CultureInfo.InvariantCulture)}";
            }

            public static Rectangle FromPoints(IEnumerable<Point> points)
            {
                double minx = double.PositiveInfinity, miny = double.PositiveInfinity, maxx = double.NegativeInfinity, maxy = double.NegativeInfinity;
                foreach (Point p in points)
                {
                    minx = Math.Min(minx, p.X);
                    miny = Math.Min(miny, p.Y);
                    maxx = Math.Max(maxx, p.X);
                    maxy = Math.Max(maxy, p.Y);
                }
                return Rectangle.FromLTRB(minx, maxy, maxx, miny);
            }

            public static Rectangle FromReader(BinaryReader br)
            {
                Rectangle rect = Rectangle.Zero();
                rect.Read(br);
                return rect;
            } 

            public void Read(BinaryReader br)
            {
                MinX = br.ReadDouble();
                MinY = br.ReadDouble();
                MaxX = br.ReadDouble();
                MaxY = br.ReadDouble();
            }

            public void Write(BinaryWriter bw)
            {
                bw.Write(BitConverter.GetBytes(MinX));
                bw.Write(BitConverter.GetBytes(MinY));
                bw.Write(BitConverter.GetBytes(MaxX));
                bw.Write(BitConverter.GetBytes(MaxY));
            }

            public static implicit operator Envelope(Rectangle rectangle) => new Envelope(rectangle.MinX, rectangle.MinY, rectangle.MaxX, rectangle.MaxY);

            public static implicit operator Rectangle(Envelope envelope) => Rectangle.FromLTRB(envelope.MinX, envelope.MaxY, envelope.MaxX, envelope.MinY);
        }

        public struct Region
        {
            private double _left, _right;
            public double Left { 
                get { return _left; } 
                set { 
                    _left = value;
                    Mid = _left + (_right - _left) / 2.0;
                    Width = _right - _left;
                }
            }
            public double Right
            {
                get { return _right; }
                set
                {
                    _right = value;
                    Mid = _left + (_right - _left) / 2.0;
                    Width = _right - _left;
                }
            }
            public double Width { get; private set; }
            public double Mid { get; private set; }
            private Region (double left, double right, double width, double mid) 
            {
                _left = left;
                _right = right;
                Width = width;
                Mid = mid;
            }
            public Region(double left, double right)
            {
                _left = left;
                _right = right;
                Width = right - left;
                Mid = left + Width / 2.0;
            }

            public static Region Infinite() => new Region(double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity, 0);

            public bool Intersects(Region other) => Intersects(other.Left, other.Right);
            public bool Intersects(double left, double right) => !(this.Right < left || this.Left > right); //this.Right >= left && this.Left <= right
            public bool IntersectsBiased(Region other) => IntersectsBiased(other.Left, other.Right);
            public bool IntersectsBiased(double left, double right) => !(this.Right < left || this.Left >= right); //this.Right >= left && this.Left < right
            public bool SubsetOf(Region other) => SubsetOf(other.Left, other.Right);
            public bool SubsetOf(double left, double right) => !(this.Left < left || this.Right > right); // this.Left >= left && this.Right <= right
            public bool SubsetOfBiased(Region other) => SubsetOfBiased(other.Left, other.Right);
            public bool SubsetOfBiased(double left, double right) => !(this.Left < left || this.Right >= right); // this.Left >= left && this.Right < right
            public bool Contains(double val) => Intersects(val, val);
            public bool ContainsBiased(double val) => IntersectsBiased(val, val);
        }
    }
}
