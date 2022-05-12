namespace SpecialityWebService
{
    public class MathObjects
    {
        public struct Point
        {
            public double X, Y;
            public Point(double x, double y) { X = x; Y = y; }
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

            public static Rectangle FromLBRT(double minx, double miny, double maxx, double maxy)
            {
                var r = new Rectangle();
                r.MinX = minx;
                r.MinY = miny;
                r.MaxX = maxx;
                r.MaxY = maxy;
                return r;
            }

            public static Rectangle Zero() => Rectangle.FromLBRT(0.0, 0.0, 0.0, 0.0);

            public Point GetCenter() => new Point(MinX + (MaxX - MinX) / 2.0, MinY + (MaxY - MinY) / 2.0);

            public double Width => MaxX - MinX;
            public double Height => MaxY - MinY;

            public double Left => MinX;
            public double Top => MaxY;
            public double Right => MaxX;
            public double Bottom => MinY;
        }
    }
}
