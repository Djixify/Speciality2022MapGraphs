using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using SpecialityWebService.Generation;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static SpecialityWebService.MathObjects;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SpecialityWebService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MapController : ControllerBase
    {
        private static Dictionary<string, Map> maps = new Dictionary<string, Map>();
        private string IP => HttpContext.Connection.RemoteIpAddress.ToString() ?? HttpContext.Connection.LocalIpAddress.ToString();

        private readonly ILogger<MapController> _logger;

        public MapController(ILogger<MapController> logger)
        {
            _logger = logger;
        }

        private bool TryConvertDataset(string input, out Map.Dataset ds)
        {
            switch (input)
            {
                case "geodanmark60":
                    ds = Map.Dataset.GeoDanmark60;
                    return true;
                case "vejmanhastigheder":
                    ds = Map.Dataset.VejmanHastigheder;
                    return true;
                default:
                    ds = Map.Dataset.VejmanHastigheder;
                    return false;
            }
        }

        private void NoMapInstanciatedStatus()
        {
            HttpContext.Response.StatusCode = 400;
            HttpContext.Response.Headers.Add(new KeyValuePair<string, StringValues>("response", new StringValues("No map instantiated, please call post on 'startsession/token={wmstoken};dataset={dataset};minres={minresolution}' first")));
        }

        [HttpGet]
        public FileContentResult GetMap()
        {
            if (!maps.ContainsKey(IP))
            {
                NoMapInstanciatedStatus();
                //Gandalf momento
                Byte[] b = System.IO.File.ReadAllBytes(@".\Resources\Images\gandalf_nyhed.jpg");
                return File(b, "image/jpeg");
            }
            HttpContext.Response.StatusCode = 200;
            return File(maps[IP].RenderImage(System.Drawing.Imaging.ImageFormat.Jpeg, HttpContext), "image/jpg");
        }

        // GET: /Map
        [HttpGet("token={wmstoken};dataset={dataset};bbox={minx},{miny},{maxx},{maxy}")]
        public FileContentResult GetMapBoundarybox(string wmstoken = "024b9d34348dd56d170f634e067274c6", string dataset = "geodanmark60/vejmanhastigheder", double minx = 586835.1, double miny = 6135927.2, double maxx = 591812.3, double maxy = 6139738.0)
        {
            Map.Dataset ds;
            TryConvertDataset(dataset, out ds);

            Map map = new Map(wmstoken, ds, 1280, minx, miny, maxx, maxy);

            return File(map.RenderImage(System.Drawing.Imaging.ImageFormat.Jpeg, HttpContext), "image/jpg");
        }

        [HttpGet("mapsize")]
        public StringContent GetMapScreenSize()
        {
            if (!maps.ContainsKey(IP))
            {
                NoMapInstanciatedStatus();
                return new StringContent("");
            }

            Rectangle screenview = maps[IP].Camera.ScreenViewPort;
            return new StringContent(((int)screenview.Width + "," + (int)screenview.Height));
        }

        [HttpPost("startsession/token={wmstoken};dataset={dataset};minres={minresolution};bbox={minx},{miny},{maxx},{maxy}")]
        public void Post(string wmstoken = "024b9d34348dd56d170f634e067274c6", string dataset = "geodanmark60/vejmanhastigheder", int minresolution = 1280, double minx = 586835.1, double miny = 6135927.2, double maxx = 591812.3, double maxy = 6139738.0)
        {
            TryConvertDataset(dataset, out Map.Dataset ds);
            maps[IP] = new Map(wmstoken, ds, minresolution, minx, miny, maxx, maxy);
            HttpContext.Response.StatusCode = 200;
        }

        [HttpPost("startsession/token={wmstoken};dataset={dataset};minres={minresolution}")]
        public void Post(string wmstoken = "024b9d34348dd56d170f634e067274c6", string dataset = "geodanmark60/vejmanhastigheder", int minresolution = 1280)
        {
            TryConvertDataset(dataset, out Map.Dataset ds);
            maps[IP] = new Map(wmstoken, ds, minresolution);
            HttpContext.Response.StatusCode = 200;
        }

        [HttpPost("startsession/token={wmstoken};dataset={dataset};width={width},height={height}")]
        public void Post(string wmstoken = "024b9d34348dd56d170f634e067274c6", string dataset = "geodanmark60/vejmanhastigheder", int width = 1280, int height = 960)
        {
            TryConvertDataset(dataset, out Map.Dataset ds);
            maps[IP] = new Map(wmstoken, ds, width, height);
            HttpContext.Response.StatusCode = 200;
        }

        [HttpPost("changedataset={dataset}")]
        public void Post(string dataset = "geodanmark60/vejmanhastigheder")
        {
            Map.Dataset ds;
            switch (dataset)
            { 
                case "geodanmark60":
                    ds = Map.Dataset.GeoDanmark60;
                    HttpContext.Response.Headers.Add(new KeyValuePair<string, StringValues>("response", new StringValues("Changed dataset to geodanmark60")));
                    break;
                case "vejmanhastigheder":
                    ds = Map.Dataset.VejmanHastigheder;
                    HttpContext.Response.Headers.Add(new KeyValuePair<string, StringValues>("response", new StringValues("Changed dataset to vejmanhastigheder")));
                    break;
                default:
                    HttpContext.Response.StatusCode = 400;
                    HttpContext.Response.Headers.Add(new KeyValuePair<string, StringValues>("response", new StringValues("Expected either geodanmark60/vejmanhastigheder")));
                    return;
            }
            if (maps.ContainsKey(IP))
            {
                System.Diagnostics.Debug.WriteLine("Changed dataset to " + ds.ToString());
                HttpContext.Response.StatusCode = 200;
                maps[IP].ActiveDataset = ds;
            }
            else
                NoMapInstanciatedStatus();
        }

        [HttpPost("move={x},{y}")]
        public void Post(int x, int y)
        {
            if (maps.ContainsKey(IP))
            {
                HttpContext.Response.StatusCode = 200;
                maps[IP].Camera.MoveScreen(x, y);
            }
            else
                NoMapInstanciatedStatus();
        }

        [HttpPost("zoom={zoom}")]
        public void Post(double zoom)
        {
            if (maps.ContainsKey(IP))
            {
                HttpContext.Response.StatusCode = 200;
                maps[IP].Camera.ZoomView(zoom);
            }
            else
                NoMapInstanciatedStatus();
        }

        [HttpPost("debug={debug}")]
        public void Post(bool debug)
        {
            if (maps.ContainsKey(IP))
            {
                HttpContext.Response.StatusCode = 200;
                maps[IP].Debug = debug;
            }
            else
                NoMapInstanciatedStatus();
        }

        [HttpPost("generatenetwork/{type}/name={name};endpointtolerance={endpointtolerance};midpointtolerance={midpointtolerance};directioncolumn={directioncolumn},forwardsval={forwardsval},backwardsval={backwardsval}")]
        public async void Post(string type, string name, double endpointtolerance, double midpointtolerance, string directioncolumn, string forwardsval, string backwardsval)
        {
            if (maps.ContainsKey(IP))
            {
                switch(type)
                {
                    case "QGIS":
                        HttpContext.Response.StatusCode = 200;
                        INetworkGenerator qgis = new QGISReferenceAlgorithm();
                        Network network = new Network(name, qgis, new Rtree<int>());
                        network.EndPointTolerance = endpointtolerance;
                        network.MidPointTolerance = midpointtolerance;
                        network.DirectionColumn = directioncolumn;
                        network.DirectionForwardsValue = forwardsval;
                        network.DirectionBackwardsValue = backwardsval;
                        network.Weights = new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("euclidean distance", "distance") };
                        network.ExtractionColumns = network.Weights.Select(w => Lexer.ExtractPrimitiveTokens(Lexer.Primitive.Variable, w.Value)).SelectMany(i => i).Select(token => (string)token.Value).ToList();

                        network.Generate(maps[IP]);
                        break;
                    case "Own":

                        break;
                }
            }
            else
                NoMapInstanciatedStatus();
        }


        [HttpPost("setscreensize={width},{height}")]
        public void Post2(int width, int height)
        {
            if (maps.ContainsKey(IP))
            {
                HttpContext.Response.StatusCode = 200;
                maps[IP].Camera.SetScreenSize(width, height);
            }
            else
                NoMapInstanciatedStatus();
        }
    }
}
