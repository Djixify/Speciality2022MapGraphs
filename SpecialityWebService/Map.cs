using Microsoft.AspNetCore.Http;
using SpecialityWebService.Generation;
using SpecialityWebService.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SpecialityWebService.MathObjects;

namespace SpecialityWebService
{
    public class Map
    {
        public enum Dataset
        {
            GeoDanmark60,
            VejmanHastigheder
        }

        public DataforsyningenBackground_WMS BackgroundWMS { get; set; }
        private Dataset _activedataset = Dataset.VejmanHastigheder;
        public Dataset ActiveDataset
        {
            get { return _activedataset; }
            set
            {
                if (value != _activedataset || GML == null)
                {
                    switch (value)
                    {
                        case Dataset.GeoDanmark60:
                            GML = new GeoDanmark60_GML(@"Resources\Vectordata\dfvejedata.gml");
                            break;
                        case Dataset.VejmanHastigheder:
                            GML = new Vejmanhastigheder_GML(@"Resources\Vectordata\vmvejedata.gml");
                            break;
                    }
                    _activedataset = value;
                }
            }
        }

        public bool Debug { get; set; } = true;
        public IGMLReader GML { get; set; } = null;
        public Camera Camera { get; set; }

        public List<Network> Networks { get; set; } = new List<Network>();

        public Map(string token, Dataset dataset, int minresolution)
        {
            BackgroundWMS = new DataforsyningenBackground_WMS(token); //Remember parse token

            ActiveDataset = dataset;

            Rectangle r = GML.GetBoundaryBox();

            double width = r.MaxX - r.MinX;
            double height = r.MaxY - r.MinY;
            double wtmp, htmp;
            if (width < height)
            {
                wtmp = minresolution;
                htmp = minresolution * ((r.MaxY - r.MinY) / (r.MaxX - r.MinX));
            }
            else
            {
                htmp = minresolution;
                wtmp = minresolution * ((r.MaxX - r.MinX) / (r.MaxY - r.MinY));
            }
            int pixelWidth = (int)wtmp;
            int pixelHeight = (int)htmp;


            Camera = new Camera(pixelWidth, pixelHeight, r.GetCenter().X, r.GetCenter().Y, pixelWidth / width);

            System.Diagnostics.Debug.WriteLine(GML.GetFeatureCount());
            System.Diagnostics.Debug.WriteLine(GML.GetBoundaryBox());
        }

        public Map(string token, Dataset dataset, int width, int height)
        {
            BackgroundWMS = new DataforsyningenBackground_WMS(token); //Remember parse token

            ActiveDataset = dataset;

            Rectangle r = GML.GetBoundaryBox();

            double worldwidth = r.MaxX - r.MinX;

            Camera = new Camera(width, height, r.GetCenter().X, r.GetCenter().Y, width / worldwidth);

            System.Diagnostics.Debug.WriteLine(GML.GetFeatureCount());
            System.Diagnostics.Debug.WriteLine(GML.GetBoundaryBox());
        }

        public Map(string token, Dataset dataset, int minresolution, double minx, double miny, double maxx, double maxy)
        {
            BackgroundWMS = new DataforsyningenBackground_WMS(token); //Remember parse token

            ActiveDataset = dataset;

            double width = maxx - minx;
            double height = maxy - miny;
            double wtmp, htmp;
            if (width < height)
            {
                wtmp = minresolution;
                htmp = minresolution * ((maxy - miny) / (maxx - minx));
            }
            else
            {
                htmp = minresolution;
                wtmp = minresolution * ((maxx - minx) / (maxy - miny));
            }
            int pixelWidth = (int)wtmp;
            int pixelHeight = (int)htmp;


            Camera = new Camera(pixelWidth, pixelHeight, (minx + width / 2.0), (miny + height / 2.0), pixelWidth / width);

            System.Diagnostics.Debug.WriteLine(GML.GetFeatureCount());
            System.Diagnostics.Debug.WriteLine(GML.GetBoundaryBox());
            //Path p = Path.FromXML(0, GML.GetFeatureEnumerator().ElementAt(0), new List<string>() { "gml_id", "TILKMT", "TILKM" });
        }

        public System.Drawing.PointF[] ConvertToGraphicsPath(Path path) => 
            path.Points.Select(p => new System.Drawing.PointF((float)p.X, (float)p.Y)).ToArray();

        public System.Drawing.PointF[] ConcatenatePaths(IEnumerable<Path> paths) => 
            paths.Aggregate(new List<System.Drawing.PointF>(), (acc, path) => 
                { acc.AddRange(ConvertToGraphicsPath(path)); return acc; }).ToArray();

        public byte[] RenderImage(System.Drawing.Imaging.ImageFormat format, HttpContext context = null)
        {
            byte[] background = BackgroundWMS.GetImageBytes(Camera);
            Rectangle screenview = Camera.ScreenViewPort;
            Rectangle worldview = Camera.WorldViewPort;
            using (MemoryStream ms = background == null ? new MemoryStream() : new MemoryStream(background))
            using (System.Drawing.Bitmap bm = background == null ? new System.Drawing.Bitmap((int)screenview.Width, (int)screenview.Height) : new System.Drawing.Bitmap(ms))
            {
                Stopwatch sw = new Stopwatch();
                sw.Restart();

                System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bm);
                //g.FillRectangle(System.Drawing.Brushes.Beige, 200, 200, 200, 200);

                if (background == null)
                {
                    g.DrawString(BackgroundWMS.Unavailable, new System.Drawing.Font("Ariel", 30, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel), System.Drawing.Brushes.Red, new System.Drawing.PointF(10, (int)(screenview.Height - 40)));
                }

                var before = g.Save();

                this.Camera.TransformSpaceToWorld(g);

                int numberofpaths = 0;
                if (GML != null)
                {
                    IEnumerable<Path> paths = GML.GetPathEnumerator(this.Camera.WorldViewPort);
                    foreach (Path path in GML.GetPathEnumerator(this.Camera.WorldViewPort, new List<string>() { }))
                    {
                        g.DrawLines(new System.Drawing.Pen(System.Drawing.Color.FromArgb(50,200,40,40), (float)(3 / Camera.Zoom)), ConvertToGraphicsPath(path));
                        numberofpaths++;
                    }
                }

                //Point p1 = this.Camera.ToWorld(400, 400);

                //g.FillRectangle(System.Drawing.Brushes.Beige, (int)p1.X, (int)p1.Y, 200, 100);

                g.Restore(before);

                sw.Stop();

                
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Rendertime: " + sw.ElapsedMilliseconds + "ms");
                sb.AppendLine("Number of paths rendered: " + numberofpaths);
                sb.AppendLine("Position(center): " + Math.Round(Camera.Center.X,2) + "x " + Math.Round(Camera.Center.Y,2) + "y");
                sb.AppendLine("Scale: ~1:" + (int)(worldview.Width / screenview.Width * 37.795275591 * 100));

                if (Debug)
                    g.DrawString(sb.ToString(), new System.Drawing.Font("Ariel", 20, System.Drawing.FontStyle.Italic | System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel), System.Drawing.Brushes.LimeGreen, new System.Drawing.PointF(10, 10));
                if (context != null)
                    context.Response.Headers.Add("stats", new Microsoft.Extensions.Primitives.StringValues(System.Net.WebUtility.UrlEncode(sb.ToString())));

                MemoryStream ms2 = new MemoryStream();
                bm.Save(ms2, format);
                return ms2.ToArray();
            }
        }
    }
}
