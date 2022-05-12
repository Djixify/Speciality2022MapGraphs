using SpecialityWebService.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public IGMLReader GML { get; set; } = null;
        public Camera Camera { get; set; }

        public Map(string token, Dataset dataset, int minresolution, double minx, double miny, double maxx, double maxy)
        {
            BackgroundWMS = new DataforsyningenBackground_WMS(token); //Remember parse token
            BackgroundWMS.SetBoundaryBox(minx, miny, maxx, maxy);
            double width, height;
            if (BackgroundWMS.BBox.Width >= BackgroundWMS.BBox.Height)
            {
                width = minresolution;
                height = width * (BackgroundWMS.BBox.Height / BackgroundWMS.BBox.Width);
            }
            else
            {
                height = minresolution;
                width = height * (BackgroundWMS.BBox.Width / BackgroundWMS.BBox.Height);
            }
            BackgroundWMS.PixelWidth = (int)width;
            BackgroundWMS.PixelHeight = (int)height;

            Camera = new Camera(BackgroundWMS.PixelWidth, BackgroundWMS.PixelHeight, BackgroundWMS.BBox.GetCenter().X, BackgroundWMS.BBox.GetCenter().Y);

            ActiveDataset = dataset;

            System.Diagnostics.Debug.WriteLine(GML.GetFeatureCount());
            System.Diagnostics.Debug.WriteLine(GML.GetBoundaryBox());
            Path p = Path.FromXML(0, GML.GetFeatureEnumerator().ElementAt(0), new List<string>() { "TILKMT", "TILKM" });
        }

        public byte[] RenderImage(System.Drawing.Imaging.ImageFormat format)
        {
            byte[] background = BackgroundWMS.GetImageBytes();
            using (MemoryStream ms = background == null ? new MemoryStream() : new MemoryStream(background))
            using (System.Drawing.Bitmap bm = background == null ? new System.Drawing.Bitmap(BackgroundWMS.PixelWidth, BackgroundWMS.PixelHeight) : new System.Drawing.Bitmap(ms))
            {
                System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bm);
                g.FillRectangle(System.Drawing.Brushes.Beige, 200, 200, 200, 200);

                if (background == null)
                {
                    g.DrawString(BackgroundWMS.Unavailable, new System.Drawing.Font("Ariel", 30, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel), System.Drawing.Brushes.Red, new System.Drawing.PointF(10, BackgroundWMS.PixelHeight - 40));
                }

                var before = g.Save();

                this.Camera.TransformSpaceToWorld(g);

                //Point p1 = this.Camera.ToWorld(400, 400);

                //g.FillRectangle(System.Drawing.Brushes.Beige, (int)p1.X, (int)p1.Y, 200, 100);


                g.Restore(before);


                MemoryStream ms2 = new MemoryStream();
                bm.Save(ms2, format);
                return ms2.ToArray();
            }
        }
    }
}
