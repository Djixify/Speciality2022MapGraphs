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
                    _backgroundcolor = GML is GeoDanmark60_GML ? System.Drawing.Color.FromArgb(60, 35, 171, 255) : System.Drawing.Color.FromArgb(60, 219, 30, 42);
                    _foregroundcolor = GML is GeoDanmark60_GML ? System.Drawing.Color.FromArgb(150, 35, 171, 255) : System.Drawing.Color.FromArgb(150, 219, 30, 42);
                    _vertexstrokecolor = GML is GeoDanmark60_GML ? System.Drawing.Color.FromArgb(150, 25, 124, 185) : System.Drawing.Color.FromArgb(150, 132, 0, 0);

                    _pathpen = new System.Drawing.Pen(_foregroundcolor);
                    _pathpen.LineJoin = LineJoin.Bevel;
                    _edgepen = new System.Drawing.Pen(_foregroundcolor);
                    _edgepen.LineJoin = LineJoin.Bevel;
                    _edgeghostpen = new System.Drawing.Pen(_backgroundcolor);
                    _edgeghostpen.DashStyle = DashStyle.Dash;
                    _edgeghostpen.LineJoin = LineJoin.Bevel;

                    _endpointimage = System.Drawing.Image.FromFile(@"Resources\Images\" + (GML is GeoDanmark60_GML ? "endpoint.png" : "vmendpoint.png"));
                    _midpointimage = System.Drawing.Image.FromFile(@"Resources\Images\" + (GML is GeoDanmark60_GML ? "midpoint.png" : "vmmidpoint.png"));

                    _activedataset = value;
                }
            }
        }

        public bool Debug { get; set; } = true;
        public IGMLReader GML { get; set; } = null;
        public Camera Camera { get; set; }

        public Dictionary<string, Network> Networks { get; set; } = new Dictionary<string, Network>();
        public string RenderNetwork = null;

        private System.Drawing.Color _backgroundcolor;
        private System.Drawing.Color _foregroundcolor;
        private System.Drawing.Color _vertexstrokecolor;

        private System.Drawing.Pen _pathpen;
        private System.Drawing.Pen _edgepen;
        private System.Drawing.Pen _edgeghostpen;
        private System.Drawing.Pen _networkhighlightpen = new System.Drawing.Pen(System.Drawing.Color.LimeGreen);

        private System.Drawing.Image _endpointimage;
        private System.Drawing.Image _midpointimage;


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

        public System.Drawing.PointF[] ConvertToPointF(Path path) => 
            path.Points.Select(p => Camera.ToScreenF(p)).ToArray();

        public byte[] RenderImage(System.Drawing.Imaging.ImageFormat format, bool rendernetwork = true, HttpContext context = null)
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

                if (background == null)
                    g.DrawString(BackgroundWMS.Unavailable, new System.Drawing.Font("Ariel", 30, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel), System.Drawing.Brushes.Red, new System.Drawing.PointF(10, (int)(screenview.Height - 40)));


                var before = g.Save();

                //this.Camera.TransformSpaceToWorld(g);

                bool shouldrendernetwork = rendernetwork && RenderNetwork != null && Networks.ContainsKey(RenderNetwork);
                Network network = shouldrendernetwork ? Networks[RenderNetwork] : null;

                int rendercount = 0;
                int renderthreshold = 100;

                int numberofpaths = 0;
                int numberofpoints = 0;
                int numberofvertices = 0;

                int scale = (int)(worldview.Width / screenview.Width * 37.795275591 * 100);
                if (shouldrendernetwork)
                {
                    _edgepen.Width = (float)8;
                    _edgeghostpen.Width = (float)8;
                    foreach (Edge e in network.E.Where(e => e.BoundaryBox.Overlapping(worldview)))
                    {
                        
                        Point prevp = new Point(0,0);
                        bool firstpoint = true;
                        foreach (Point p in e.RenderPoints)
                        {
                            if (!firstpoint)
                                g.DrawLine(_edgeghostpen, Camera.ToScreenF(prevp), Camera.ToScreenF(p));
                            
                            if (rendercount > renderthreshold)
                            {
                                rendercount = 0;
                                g.Flush();
                            }
                            else
                                rendercount++;

                            prevp = p;
                            firstpoint = false;
                        }
                         g.DrawLine(_edgepen, Camera.ToScreenF(e.P1), Camera.ToScreenF(e.P2));

                        //g.DrawLines(_edgeghostpen, e.RenderPoints.Select(p => Camera.ToScreenF(p)).ToArray());
                        g.Flush();
                        numberofpaths++;
                        numberofpoints += e.RenderPoints.Count;
                    }

                    int vertexsize = 16;
                    int vertexsizestep = 4;
                    foreach (Vertex v in network.V.Where(v => v.BoundaryBox.Overlapping(worldview)))
                    {
                        int circlewidth = vertexsize + v.Edges.Count * vertexsizestep;

                        var location = Camera.ToScreenF(v.Location);
                        location.X -= circlewidth / 2;
                        location.Y -= circlewidth / 2;

                        g.FillEllipse(new System.Drawing.SolidBrush(v.Index == network.SelectedStartVertex || v.Index == network.SelectedEndVertex ? _networkhighlightpen.Color : _vertexstrokecolor), new System.Drawing.RectangleF(location, new System.Drawing.SizeF(circlewidth, circlewidth)));
                        if (rendercount > renderthreshold)
                        {
                            rendercount = 0;
                            g.Flush();
                        }
                        else
                            rendercount++;
                        numberofvertices++;
                    }

                    foreach (int eid in network.EdgesBetween ?? new List<int>())
                    {
                        Edge e = network.E[eid];
                        _networkhighlightpen.Width = _edgepen.Width;
                        g.DrawLines(_networkhighlightpen, e.RenderPoints.Select(p => Camera.ToScreenF(p)).ToArray());
                        g.Flush();
                    }

                    numberofvertices = network.V.Count(v => v.BoundaryBox.Overlapping(worldview));
                }
                else
                {
                    if (GML != null)
                    {
                        _pathpen.Width = (float)8;
                        IEnumerable<Path> paths = GML.GetPathEnumerator(worldview);
                        foreach (Path path in GML.GetPathEnumerator(worldview, new List<string>() { }))
                        {
                            g.DrawLines(_pathpen, ConvertToPointF(path));
                            g.Flush();
                            numberofpaths++;
                            numberofpoints += path.Points.Count;
                        }
                        if (scale <= 2500)
                        {
                            foreach (Path path in GML.GetPathEnumerator(worldview, new List<string>() { }))
                            {
                                foreach (Point p in path.Points)
                                {
                                    bool isendpoint = p == path.Points.Last() || p == path.Points.First();
                                    var image = isendpoint ? _endpointimage : _midpointimage;
                                    var location = Camera.ToScreenF(p);
                                    location.X -= image.Width / 2;
                                    location.Y -= image.Height / 2;
                                    g.DrawImage(image, location);
                                    g.Flush();
                                    numberofpoints++;
                                }
                            }
                        }
                    }
                }

                //g.Restore(before);

                //Point p1 = this.Camera.ToWorld(400, 400);

                //g.FillRectangle(System.Drawing.Brushes.Beige, (int)p1.X, (int)p1.Y, 200, 100);

                sw.Stop();

                
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Rendertime: " + sw.ElapsedMilliseconds + "ms");
                if (numberofpoints > 0)
                    sb.AppendLine("Number of points rendered: " + numberofpoints);
                if (numberofpaths > 0)
                    sb.AppendLine("Number of " + (shouldrendernetwork ? "edges" : "paths") + " rendered: " + numberofpaths);
                if (numberofvertices > 0)
                    sb.AppendLine("Number of vertices rendered: " + numberofvertices);
                sb.AppendLine("Position(center): " + Math.Round(Camera.Center.X,2) + "x " + Math.Round(Camera.Center.Y,2) + "y");
                sb.AppendLine("Scale: ~1:" + scale);

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
