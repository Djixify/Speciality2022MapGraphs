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
    public class Map : IDisposable
    {
        public enum Dataset
        {
            GeoDanmark60,
            VejmanHastigheder
        }

        public enum DatasetSize
        {
            Small,
            Bridge,
            Medium,
            Large
        }

        public string ActiveDatasetName => System.IO.Path.GetFileNameWithoutExtension(Datasets[_activedataset][_activedatasetsize]);
        public string ActiveDatasetFilePath => Datasets[_activedataset][_activedatasetsize];
        public Dictionary<Dataset, Dictionary<DatasetSize, string>> Datasets = new Dictionary<Dataset, Dictionary<DatasetSize, string>>(
            new KeyValuePair<Dataset, Dictionary<DatasetSize, string>>[]
            {
                KeyValuePair.Create(Dataset.GeoDanmark60, new Dictionary<DatasetSize, string>(new KeyValuePair<DatasetSize, string>[]
                {
                    KeyValuePair.Create(DatasetSize.Small, @"Resources\Vectordata\dfvejedatatinystige.gml"),
                    KeyValuePair.Create(DatasetSize.Bridge, @"Resources\Vectordata\dfbridge.gml"),
                    KeyValuePair.Create(DatasetSize.Medium, @"Resources\Vectordata\dfvejedata.gml"),
                    KeyValuePair.Create(DatasetSize.Large, @"Resources\Vectordata\dfvejedatalarge.gml")
                })),
                KeyValuePair.Create(Dataset.VejmanHastigheder, new Dictionary<DatasetSize, string>(new KeyValuePair<DatasetSize, string>[]
                {
                    KeyValuePair.Create(DatasetSize.Small, @"Resources\Vectordata\vmvejedatatinystige.gml"),
                    KeyValuePair.Create(DatasetSize.Bridge, @"Resources\Vectordata\vmbridge.gml"),
                    KeyValuePair.Create(DatasetSize.Medium, @"Resources\Vectordata\vmvejedata.gml"),
                    KeyValuePair.Create(DatasetSize.Large, @"Resources\Vectordata\vmvejedatalarge.gml")
                }))
            }
        );

        private string SessionId = null;

        public DataforsyningenBackground_WMS BackgroundWMS { get; set; }
        private Dataset _activedataset = Dataset.VejmanHastigheder;
        private DatasetSize _activedatasetsize = DatasetSize.Small;

        public bool Debug { get; set; } = true;
        public IGMLReader GML { get; set; } = null;
        public Camera Camera { get; set; }

        public Dictionary<string, Network> Networks { get; set; } = new Dictionary<string, Network>();
        public bool RenderDataset = true;
        public string SelectedNetwork = null;

        private System.Drawing.Color _backgroundcolor;
        private System.Drawing.Color _foregroundcolor;
        private System.Drawing.Color _vertexstrokecolor;
        private System.Drawing.Color _networkhighlightcolor = System.Drawing.Color.Green;
        private System.Drawing.Color[] _pathhighlightcolors = new System.Drawing.Color[] {
            System.Drawing.Color.Cyan,
            System.Drawing.Color.Magenta,
            System.Drawing.Color.Yellow };

        private System.Drawing.Image _endpointimage;
        private System.Drawing.Image _midpointimage;

        private float pathwidth = 4f;
        private float edgewidth = 4f;
        private int vertexsize = 10;
        private int vertexsizestep = 2;

        public Map(string token, string sessionid, Dataset dataset, DatasetSize datasetsize, int minresolution)
        {
            BackgroundWMS = new DataforsyningenBackground_WMS(token); //Remember parse token
            SessionId = sessionid;

            SetDataset(dataset, datasetsize);

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


            Camera = new Camera(pixelWidth, pixelHeight, r.Center.X, r.Center.Y, pixelWidth / width);

            System.Diagnostics.Debug.WriteLine(GML.GetFeatureCount());
            System.Diagnostics.Debug.WriteLine(GML.GetBoundaryBox());
        }

        public Map(string token, string sessionid, Dataset dataset, DatasetSize datasetsize, int width, int height)
        {
            BackgroundWMS = new DataforsyningenBackground_WMS(token); //Remember parse token
            SessionId = sessionid;

            SetDataset(dataset, datasetsize);

            Rectangle r = GML.GetBoundaryBox();

            double worldwidth = r.MaxX - r.MinX;

            Camera = new Camera(width, height, r.Center.X, r.Center.Y, width / worldwidth);

            System.Diagnostics.Debug.WriteLine(GML.GetFeatureCount());
            System.Diagnostics.Debug.WriteLine(GML.GetBoundaryBox());
        }

        public Map(string token, string sessionid, Dataset dataset, DatasetSize datasetsize, int minresolution, double minx, double miny, double maxx, double maxy)
        {
            BackgroundWMS = new DataforsyningenBackground_WMS(token); //Remember parse token
            SessionId = sessionid;

            SetDataset(dataset, datasetsize);

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


        public void SetDataset(Dataset type, DatasetSize size)
        {
            _activedatasetsize = size;
            _activedataset = type;
            switch (_activedataset)
            {
                case Dataset.GeoDanmark60:
                    GML = new GeoDanmark60_GML(Datasets[_activedataset][_activedatasetsize]);
                    _backgroundcolor = System.Drawing.Color.FromArgb(60, 35, 171, 255);
                    _foregroundcolor = System.Drawing.Color.FromArgb(150, 35, 171, 255);
                    _vertexstrokecolor = System.Drawing.Color.FromArgb(255, 25, 124, 185);
                    _endpointimage = System.Drawing.Image.FromFile(@"Resources\Images\endpoint.png");
                    _midpointimage = System.Drawing.Image.FromFile(@"Resources\Images\midpoint.png");
                    break;
                case Dataset.VejmanHastigheder:
                    GML = new Vejmanhastigheder_GML(Datasets[_activedataset][_activedatasetsize]);
                    _backgroundcolor = System.Drawing.Color.FromArgb(60, 219, 30, 42);
                    _foregroundcolor = System.Drawing.Color.FromArgb(150, 219, 30, 42);
                    _vertexstrokecolor = System.Drawing.Color.FromArgb(255, 132, 0, 0);
                    _endpointimage = System.Drawing.Image.FromFile(@"Resources\Images\vmendpoint.png");
                    _midpointimage = System.Drawing.Image.FromFile(@"Resources\Images\vmmidpoint.png");
                    break;
            }

            foreach (Network network in Networks.Values)
                network.Dispose();
            Networks = new Dictionary<string, Network>();
            if (Directory.Exists(Network.NetworkFolder) && Directory.Exists(System.IO.Path.Combine(Network.NetworkFolder, SessionId)) && Directory.Exists(System.IO.Path.Combine(Network.NetworkFolder, SessionId, ActiveDatasetName)))
            {
                foreach (string file in Directory.EnumerateFiles(System.IO.Path.Combine(Network.NetworkFolder, SessionId, ActiveDatasetName)).Where(file => file.EndsWith(".network")))
                {
                    Networks.Add(System.IO.Path.GetFileNameWithoutExtension(file), Network.Load(file));
                }
            }

        }

        public System.Drawing.PointF[] ConvertToPointF(Path path) => 
            path.Points.Select(p => Camera.ToScreenF(p)).ToArray();

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            return sb.ToString();
        }

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

                if (background == null)
                    g.DrawString(BackgroundWMS.Unavailable, new System.Drawing.Font("Ariel", 30, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel), System.Drawing.Brushes.Red, new System.Drawing.PointF(10, (int)(screenview.Height - 40)));


                var before = g.Save();

                //this.Camera.TransformSpaceToWorld(g);

                bool shouldrendernetwork = !RenderDataset && SelectedNetwork != null && Networks.ContainsKey(SelectedNetwork);
                Network network = shouldrendernetwork ? Networks[SelectedNetwork] : null;

                int rendercount = 0;
                int renderthreshold = 100;

                int numberofpaths = 0;
                int numberofpoints = 0;
                int numberofvertices = 0;

                int scale = (int)(worldview.Width / screenview.Width * 37.795275591 * 100);
                if (shouldrendernetwork)
                {
                    var _edgepen = new System.Drawing.Pen(_foregroundcolor);
                    _edgepen.LineJoin = LineJoin.Bevel;
                    _edgepen.Width = edgewidth;
                    var _edgeghostpen = new System.Drawing.Pen(_backgroundcolor);
                    _edgeghostpen.LineJoin = LineJoin.Bevel;
                    _edgeghostpen.DashStyle = DashStyle.Dash;
                    _edgeghostpen.Width = edgewidth;
                    List<int> edges = network.QueryEdges(worldview);
                    foreach (int eindex in edges)
                    {
                        Edge e = network.E[eindex];
                        if (e == null || e.RenderPoints.Count == 0)
                            continue;
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
                        g.DrawLine(_edgepen, Camera.ToScreenF(e.RenderPoints.First()), Camera.ToScreenF(e.RenderPoints.Last()));

                        //g.DrawLines(_edgeghostpen, e.RenderPoints.Select(p => Camera.ToScreenF(p)).ToArray());
                        g.Flush();
                        numberofpaths++;
                        numberofpoints += e.RenderPoints.Count;
                    }

                    if (network.EdgesBetween != null)
                    {
                        int i = 0;
                        foreach (KeyValuePair<string, List<int>> path in network.EdgesBetween)
                        {
                            foreach (int eid in path.Value)
                            {
                                Edge e = network.E[eid];
                                var _networkhighlightpen = new System.Drawing.Pen(_pathhighlightcolors[i % 3]);
                                _networkhighlightpen.Width = _edgepen.Width + (8 - i * 4);
                                g.DrawLines(_networkhighlightpen, e.RenderPoints.Select(p => Camera.ToScreenF(p)).ToArray());
                                g.Flush();
                            }
                            i++;
                        }
                    }


                    List<int> vertices = network.QueryVertices(worldview);
                    foreach (int vindex in vertices)
                    {
                        Vertex v = network.V[vindex];
                        if (v == null)
                            continue;
                        int circlewidth = vertexsize + v.Edges.Count * vertexsizestep;

                        var location = Camera.ToScreenF(v.Location);
                        location.X -= circlewidth / 2;
                        location.Y -= circlewidth / 2;

                        if (scale > 2500 && !(v.Index == network.SelectedStartVertex || v.Index == network.SelectedEndVertex))
                            continue;

                        var color = _vertexstrokecolor;

                        g.FillEllipse(new System.Drawing.SolidBrush(color), new System.Drawing.RectangleF(location, new System.Drawing.SizeF(circlewidth, circlewidth)));
                        if (rendercount > renderthreshold)
                        {
                            rendercount = 0;
                            g.Flush();
                        }
                        else
                            rendercount++;
                        numberofvertices++;
                    }

                    if (network.EdgesBetween != null)
                    {
                        int i = 0;
                        foreach (KeyValuePair<string, List<int>> path in network.EdgesBetween)
                        {
                            foreach (int eid in path.Value)
                            {
                                Edge e = network.E[eid];
                                var _networkhighlightpen = new System.Drawing.Pen(_pathhighlightcolors[i % 3]);
                                int circlewidth = vertexsize + network.V[e.V1].Edges.Count * vertexsizestep + (3 - i * 4);
                                var location = Camera.ToScreenF(e.RenderPoints.First());
                                location.X -= circlewidth / 2;
                                location.Y -= circlewidth / 2;
                                g.FillEllipse(new System.Drawing.SolidBrush(_pathhighlightcolors[i % 3]), new System.Drawing.RectangleF(location, new System.Drawing.SizeF(circlewidth, circlewidth)));
                                if (rendercount > renderthreshold)
                                {
                                    rendercount = 0;
                                    g.Flush();
                                }
                                else
                                    rendercount++;
                                g.Flush();
                            }
                            i++;
                        }
                    }

                    if (network.SelectedStartVertex > -1)
                    {
                        Vertex v = network.V[network.SelectedStartVertex];
                        if (v != null)
                        {
                            int circlewidth = vertexsize + v.Edges.Count * vertexsizestep;
                            var location = Camera.ToScreenF(v.Location);
                            location.X -= circlewidth / 2;
                            location.Y -= circlewidth / 2;
                            var color = _networkhighlightcolor;
                            g.FillEllipse(new System.Drawing.SolidBrush(color), new System.Drawing.RectangleF(location, new System.Drawing.SizeF(circlewidth, circlewidth)));
                        }
                    }

                    if (network.SelectedEndVertex > -1)
                    {
                        Vertex v = network.V[network.SelectedEndVertex];
                        if (v != null)
                        {
                            int circlewidth = vertexsize + v.Edges.Count * vertexsizestep;
                            var location = Camera.ToScreenF(v.Location);
                            location.X -= circlewidth / 2;
                            location.Y -= circlewidth / 2;
                            var color = _networkhighlightcolor;
                            g.FillEllipse(new System.Drawing.SolidBrush(color), new System.Drawing.RectangleF(location, new System.Drawing.SizeF(circlewidth, circlewidth)));
                        }
                    }

                    g.Flush();

                    numberofvertices = vertices.Count;
                }
                else
                {
                    if (GML != null)
                    {
                        var _pathpen = new System.Drawing.Pen(_foregroundcolor);
                        _pathpen.Width = (float)4;
                        _pathpen.LineJoin = LineJoin.Bevel;
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
                                    System.Drawing.Image image = null;
                                    if (isendpoint)
                                    {
                                        lock (_endpointimage)
                                        {
                                            image = (System.Drawing.Image)_endpointimage.Clone();
                                        }
                                    }
                                    else
                                    {
                                        lock (_midpointimage)
                                        {
                                            image = (System.Drawing.Image)_midpointimage.Clone();
                                        }
                                    }
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

                MemoryStream ms2 = new MemoryStream();
                bm.Save(ms2, format);
                return ms2.ToArray();
            }
        }

        public void Dispose()
        {
            foreach (Network network in Networks.Values)
            {
                network.Dispose();
            }
            Networks.Clear();
        }
    }
}
