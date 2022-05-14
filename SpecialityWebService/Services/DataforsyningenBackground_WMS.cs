using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using static SpecialityWebService.MathObjects;

namespace SpecialityWebService
{
    public class DataforsyningenBackground_WMS
    {
        //{0}: Token; Token generated from webservice to access
        //{1}: BBox minx; format UTM32N
        //{2}: BBox miny; format UTM32N
        //{3}: BBox maxx; format UTM32N
        //{4}: BBox maxy; format UTM32N
        //{5}: Resolution width; Pixels
        //{6}: Resolution height; Pixels

        public string Unavailable = "Dataforsyning: Down";

        //https://api.dataforsyningen.dk/topo_skaermkort_DAF?ignoreillegallayers=TRUE&transparent=TRUE&token=024b9d34348dd56d170f634e067274c6&SERVICE=WMS&VERSION=1.3.0&REQUEST=GetMap&BBOX=588352.5683496139245,6136975.095706283115,588872.8597855410771,6138732.095496748574&CRS=EPSG:25832&WIDTH=512&HEIGHT=1730&LAYERS=dtk_skaermkort_daempet&STYLES=&FORMAT=image/jpeg&DPI=96&MAP_RESOLUTION=96&FORMAT_OPTIONS=dpi:96
        public string Url { get; set; } = @"https://api.dataforsyningen.dk/topo_skaermkort_DAF?ignoreillegallayers=TRUE&transparent=TRUE&token={0}&SERVICE=WMS&VERSION=1.3.0&REQUEST=GetMap&BBOX={1},{2},{3},{4}&CRS=EPSG:25832&WIDTH={5}&HEIGHT={6}&LAYERS=dtk_skaermkort_daempet&STYLES=&FORMAT=image/jpeg&DPI=96&MAP_RESOLUTION=96&FORMAT_OPTIONS=dpi:96";
        public string Token { get; set; } = null;

        public DataforsyningenBackground_WMS(string token = "024b9d34348dd56d170f634e067274c6")
        {
            Token = token;
        }

        public string GenerateUrl(Camera camera) {
            Rectangle worldview = camera.WorldViewPort;
            Rectangle screenview = camera.ScreenViewPort;
            return string.Format(Url, Token, worldview.MinX.ToString(CultureInfo.InvariantCulture), worldview.MinY.ToString(CultureInfo.InvariantCulture), worldview.MaxX.ToString(CultureInfo.InvariantCulture), worldview.MaxY.ToString(CultureInfo.InvariantCulture), (int)screenview.Width, (int)screenview.Height);
        }

        public string GenerateUrl(double minx, double miny, double maxx, double maxy, int minresolution)
        {
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

            return string.Format(Url, Token, minx.ToString(CultureInfo.InvariantCulture), miny.ToString(CultureInfo.InvariantCulture), maxx.ToString(CultureInfo.InvariantCulture), maxy.ToString(CultureInfo.InvariantCulture), pixelWidth, pixelHeight);
        }

        public byte[] GetImageBytes(Camera camera)
        {
            List<byte> image = new List<byte>();
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(GenerateUrl(camera));
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (BinaryReader reader = new BinaryReader(response.GetResponseStream()))
                {
                    byte[] bytes = null;
                    do
                    {
                        bytes = reader.ReadBytes(10 * 1024 * 1024); //10 MB
                        image.AddRange(bytes);
                    } while (bytes.Length > 0);
                }
            } 
            catch (WebException wex)
            {
                return null;
            }

            return image.ToArray();
        }

        public byte[] GetImageBytes(double minx, double miny, double maxx, double maxy, int minresolution)
        {
            List<byte> image = new List<byte>();
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(GenerateUrl(minx, miny, maxx, maxy, minresolution));
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (BinaryReader reader = new BinaryReader(response.GetResponseStream()))
                {
                    byte[] bytes = null;
                    do
                    {
                        bytes = reader.ReadBytes(10 * 1024 * 1024); //10 MB
                        image.AddRange(bytes);
                    } while (bytes.Length > 0);
                }
            }
            catch (WebException wex)
            {
                return null;
            }

            return image.ToArray();
        }
    }
}
