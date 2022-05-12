using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static SpecialityWebService.MathObjects;

namespace SpecialityWebService
{
    public class Camera
    {
        public Point Center { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public double Zoom { get; private set; }
        public double ZoomSentivity { get; private set; }
        public Rectangle WorldViewPort
        {
            get
            {
                double halfwidth = Width / Zoom / 2.0, halfheight = Height / Zoom / 2.0;
                return new Rectangle(Center.X - halfwidth, Center.Y - halfheight, Width / Zoom, Height / Zoom);
            }
        }
        public Rectangle ScreenViewPort { get => new Rectangle(0, 0, Width, Height); }

        public Camera(int pixelwidth, int pixelheight, double cx = 0.0, double cy = 0.0, double zoom = 1.0, double zoomsentivity = 0.5)
        {
            Center = new Point(cx, cy);
            Zoom = zoom;
            ZoomSentivity = zoomsentivity;
        }

        public Point ToWorld(int x, int y)
        {
            return new Point((x / Zoom) + Center.X - WorldViewPort.Width / 2.0, -(y / Zoom) + Center.Y + WorldViewPort.Height / 2.0);
        }

        public Point ToWorld(Point screenpos)
        {
            return ToWorld((int)Math.Round(screenpos.X, 0), (int)Math.Round(screenpos.Y, 0));
        }

        public Point ToScreen(double x, double y)
        {
            return new Point((x + WorldViewPort.Width / 2.0 - Center.X) * Zoom, -(y - WorldViewPort.Height / 2.0 - Center.Y) * Zoom);
        }

        public Point ToScreen(Point worldpos)
        {
            return ToScreen(worldpos.X, worldpos.Y);
        }

        public System.Drawing.Rectangle ToSystemRectangle(Rectangle rect)
        {
            return System.Drawing.Rectangle.FromLTRB((int)rect.Left, (int)rect.Bottom, (int)rect.Right, (int)rect.Top);
        }

        public System.Drawing.RectangleF ToSystemRectangleF(Rectangle rect)
        {
            return System.Drawing.RectangleF.FromLTRB((float)rect.Left, (float)rect.Bottom, (float)rect.Right, (float)rect.Top);
        }

        public void TransformSpaceToWorld(System.Drawing.Graphics g)
        {
            g.ScaleTransform((float)Zoom, (float)Zoom);
            g.TranslateTransform((float)(WorldViewPort.Width / 2.0 - Center.X), (float)(WorldViewPort.Height / 2.0 + Center.Y));
            g.ScaleTransform(1, -1);
        }

        /*
        public void HandleMouseDown(object sender, MouseEventArgs e)
        {
            _prev_location = new Point(-e.X, e.Y);
            _dragging = true;
        }

        public void HandleMouseMove(object sender, MouseEventArgs e)
        {
            Mouse_location = new Point(e.X, e.Y);
            Point newmouseworldpos = ToWorld(Mouse_location);
            _locationlabel.Text = $"Center location: ({Math.Round(Center.x, 2)},{Math.Round(Center.y, 2)}) Mouse location: World: ({Math.Round(newmouseworldpos.x, 2)},{Math.Round(newmouseworldpos.y, 2)}) Screen: ({Math.Round(Mouse_location.x, 2)},{Math.Round(Mouse_location.y, 2)}) Zoom: {Math.Round(Zoom, 2)}";
            if (_dragging)
            {
                Point newpos = new Point(-e.X, e.Y);
                Point delta = newpos - _prev_location;
                Center += delta * (1.0 / Zoom);
                _prev_location = newpos;
                _parent.Invalidate();
            }
        }

        public void HandleMouseUp(object sender, MouseEventArgs e)
        {
            if (_dragging)
            {
                Point delta = new Point(-e.X, e.Y) - _prev_location;
                Center += delta * (1.0 / Zoom);
                _parent.Invalidate();
            }

            _dragging = false;
        }

        public void HandleMouseWheel(object sender, MouseEventArgs e)
        {
            Point oldworldmousepos = ToWorld(Mouse_location);
            Zoom *= e.Delta < 0 ? 1 * ZoomSentivity : 1.0 / ZoomSentivity;
            Point newmouseworldpos = ToWorld(Mouse_location);
            Center += (oldworldmousepos - newmouseworldpos);
            _locationlabel.Text = $"Center location: ({Math.Round(Center.x, 2)},{Math.Round(Center.y, 2)}) Mouse location: World: ({Math.Round(newmouseworldpos.x, 2)},{Math.Round(newmouseworldpos.y, 2)}) Screen: ({Math.Round(Mouse_location.x, 2)},{Math.Round(Mouse_location.y, 2)}) Zoom: {Math.Round(Zoom, 2)}";
            _parent.Invalidate();
        }*/
    }
}
