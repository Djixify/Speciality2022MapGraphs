using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using SpecialityWebService.Generation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        [HttpGet("{usertoken}")]
        public FileContentResult GetMap(string usertoken)
        {
            if (!maps.ContainsKey(usertoken))
            {
                NoMapInstanciatedStatus();
                //Gandalf momento
                Byte[] b = System.IO.File.ReadAllBytes(@".\Resources\Images\gandalf_nyhed.jpg");
                return File(b, "image/jpeg");
            }
            HttpContext.Response.StatusCode = 200;
            return File(maps[usertoken].RenderImage(System.Drawing.Imaging.ImageFormat.Jpeg, true, HttpContext), "image/jpg");
        }

        // GET: /Map
        [HttpGet("token={wmstoken};dataset={dataset};bbox={minx},{miny},{maxx},{maxy}")]
        public FileContentResult GetMapBoundarybox(string wmstoken = "024b9d34348dd56d170f634e067274c6", string dataset = "geodanmark60/vejmanhastigheder", double minx = 586835.1, double miny = 6135927.2, double maxx = 591812.3, double maxy = 6139738.0)
        {
            Map.Dataset ds;
            TryConvertDataset(dataset, out ds);

            Map map = new Map(wmstoken, ds, 1280, minx, miny, maxx, maxy);

            return File(map.RenderImage(System.Drawing.Imaging.ImageFormat.Jpeg, true, HttpContext), "image/jpg");
        }

        [HttpGet("{usertoken}/mapsize")]
        public StringContent GetMapScreenSize(string usertoken = "testuser")
        {
            if (!maps.ContainsKey(usertoken))
            {
                NoMapInstanciatedStatus();
                return new StringContent("");
            }

            Rectangle screenview = maps[usertoken].Camera.ScreenViewPort;
            return new StringContent((int)screenview.Width + "," + (int)screenview.Height);
        }

        [HttpPost("{usertoken}/startsession/token={wmstoken};dataset={dataset};minres={minresolution};bbox={minx},{miny},{maxx},{maxy}")]
        public void StartSession(string usertoken = "testuser", string wmstoken = "024b9d34348dd56d170f634e067274c6", string dataset = "geodanmark60/vejmanhastigheder", int minresolution = 1280, double minx = 586835.1, double miny = 6135927.2, double maxx = 591812.3, double maxy = 6139738.0)
        {
            TryConvertDataset(dataset, out Map.Dataset ds);
            maps[usertoken] = new Map(wmstoken, ds, minresolution, minx, miny, maxx, maxy);
            HttpContext.Response.StatusCode = 200;
        }

        [HttpPost("{usertoken}/startsession/token={wmstoken};dataset={dataset};minres={minresolution}")]
        public void StartSession(string usertoken = "testuser", string wmstoken = "024b9d34348dd56d170f634e067274c6", string dataset = "geodanmark60/vejmanhastigheder", int minresolution = 1280)
        {
            TryConvertDataset(dataset, out Map.Dataset ds);
            maps[usertoken] = new Map(wmstoken, ds, minresolution);
            HttpContext.Response.StatusCode = 200;
        }

        [HttpPost("{usertoken}/startsession/token={wmstoken};dataset={dataset};width={width},height={height}")]
        public void StartSession(string usertoken = "testuser", string wmstoken = "024b9d34348dd56d170f634e067274c6", string dataset = "geodanmark60/vejmanhastigheder", int width = 1280, int height = 960)
        {
            TryConvertDataset(dataset, out Map.Dataset ds);
            maps[usertoken] = new Map(wmstoken, ds, width, height);
            HttpContext.Response.StatusCode = 200;
        }

        [HttpPost("{usertoken}/changedataset={dataset}")]
        public void Post(string usertoken = "testuser", string dataset = "geodanmark60/vejmanhastigheder")
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
            if (maps.ContainsKey(usertoken))
            {
                System.Diagnostics.Debug.WriteLine("Changed dataset to " + ds.ToString());
                HttpContext.Response.StatusCode = 200;
                maps[usertoken].ActiveDataset = ds;
            }
            else
                NoMapInstanciatedStatus();
        }

        [HttpPost("{usertoken}/move={x},{y}")]
        public void Post(string usertoken = "testuser", int x=0, int y=0)
        {
            if (maps.ContainsKey(usertoken))
            {
                HttpContext.Response.StatusCode = 200;
                maps[usertoken].Camera.MoveScreen(x, y);
            }
            else
                NoMapInstanciatedStatus();
        }

        [HttpPost("{usertoken}/zoom={zoom}")]
        public void Post(string usertoken = "testuser", double zoom=1.0)
        {
            if (maps.ContainsKey(usertoken))
            {
                HttpContext.Response.StatusCode = 200;
                maps[usertoken].Camera.ZoomView(zoom);
            }
            else
                NoMapInstanciatedStatus();
        }

        [HttpPost("{usertoken}/debug={debug}")]
        public void Post(string usertoken = "testuser", bool debug=true)
        {
            if (maps.ContainsKey(usertoken))
            {
                HttpContext.Response.StatusCode = 200;
                maps[usertoken].Debug = debug;
            }
            else
                NoMapInstanciatedStatus();
        }

        [HttpPost("{usertoken}/generatenetwork/{type}/name={name};endpointtolerance={endpointtolerance};midpointtolerance={midpointtolerance}")]
        public void GenerateNetwork(string usertoken = "testuser", string type = "QGIS", string name = "Testnetwork", double endpointtolerance = 2.5, double midpointtolerance = 2.5)
        {
            GenerateNetwork(usertoken, type, name, endpointtolerance, midpointtolerance, null, null, null);
        }

        [HttpPost("{usertoken}/generatenetwork/{type}/name={name};endpointtolerance={endpointtolerance};midpointtolerance={midpointtolerance};directioncolumn={directioncolumn},forwardsval={forwardsval},backwardsval={backwardsval}")]
        public void GenerateNetwork(string usertoken = "testuser", string type = "QGIS", string name = "Testnetwork", double endpointtolerance = 2.5, double midpointtolerance = 2.5, string directioncolumn="", string forwardsval="", string backwardsval="")
        {
            if (maps.ContainsKey(usertoken))
            {
                switch(type)
                {
                    case "QGIS":
                        HttpContext.Response.StatusCode = 200;
                        INetworkGenerator qgis = new QGISReferenceAlgorithm();
                        Network network = new Network(name, qgis, new Rtree<int>(), new Rtree<int>());
                        network.EndPointTolerance = endpointtolerance;
                        network.MidPointTolerance = midpointtolerance;
                        network.DirectionColumn = directioncolumn;
                        network.DirectionForwardsValue = forwardsval;
                        network.DirectionBackwardsValue = backwardsval;
                        network.Weights = new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("euclidean distance", "distance") };

                        Stopwatch sw = new Stopwatch();
                        sw.Restart();
                        network.Generate(maps[usertoken]);
                        sw.Stop();

                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine($"Generation time elapsed: {sw.ElapsedMilliseconds}ms");
                        sb.AppendLine($"Original complexity: |V_paths| = {maps[usertoken].GML.GetFeatureCount()}");
                        sb.AppendLine($"Network complexity: |V| = {network.V.Count}, |E| = {network.E.Count}");
                        System.Diagnostics.Debug.WriteLine(sb.ToString());
                        HttpContext.Response.Headers.Add("networkstats", new StringValues(System.Net.WebUtility.UrlEncode(sb.ToString())));

                        IEnumerable<double> distances = network.E.Select(e => e.Weights["euclidean distance"]);
                        double min = distances.Min(), max = distances.Max();
                        double range = max - min;
                        int[] buckets = new int[101];
                        double stepsize = range / (buckets.Length - 1);
                        foreach (double dist in distances)
                            buckets[(int)((dist - min) / stepsize + 0.49999)] += 1;

                        if (!Directory.Exists("./Users"))
                            Directory.CreateDirectory("./Users");
                        if (!Directory.Exists($"./Users/{usertoken}"))
                            Directory.CreateDirectory($"./Users/{usertoken.Replace(':', '.')}");

                        sb = new StringBuilder();
                        sb.AppendLine("Distance;Count");
                        for(int i = 0; i < buckets.Length; i++)
                        {
                            sb.AppendLine($"{Math.Round(stepsize * i + min, 2)};{buckets[i]}");
                        }
                        System.IO.File.WriteAllText($"./Users/{usertoken}_{name}_histogram.csv", sb.ToString());

                        maps[usertoken].Networks[network.Name] = network;
                        maps[usertoken].RenderNetwork = network.Name;

                        break;
                    case "Own":

                        break;
                }
            }
            else
                NoMapInstanciatedStatus();
        }

        private bool overridestart = true;
        [HttpPost("{usertoken}/selectvertex={x},{y}")]
        public void SelectVertex(string usertoken = "testuser", int x = 0, int y = 0)
        {
            if (maps.ContainsKey(usertoken) && maps[usertoken].RenderNetwork != null && maps[usertoken].Networks.ContainsKey(maps[usertoken].RenderNetwork))
            {
                HttpContext.Response.StatusCode = 200;

                Point worldpos = maps[usertoken].Camera.ToWorld(x, y);
                double widthquery = (20 / maps[usertoken].Camera.Zoom);
                int startvertex = -1;
                int endvertex = -1;

                Network network = maps[usertoken].Networks[maps[usertoken].RenderNetwork];
                if (overridestart)
                {
                    (double dist, startvertex) = network.ClosestVertex(worldpos, widthquery);
                    if (startvertex != endvertex)
                    {
                        bool found = double.IsPositiveInfinity(dist);
                        network.SelectedStartVertex = found ? startvertex : -1;
                        if (found)
                            overridestart = !overridestart;
                    }
                }
                else
                {
                    (double dist, endvertex) = network.ClosestVertex(worldpos, widthquery);
                    if (endvertex != startvertex)
                    {
                        bool found = double.IsPositiveInfinity(dist);
                        network.SelectedEndVertex = found ? endvertex : -1;
                        if (found)
                            overridestart = !overridestart;
                    }
                }

                if (startvertex > -1 && endvertex > -1)
                {
                    network.EdgesBetween = network.FindDijkstraPath(startvertex, endvertex, "euclidean distance");
                }
            }
            else
                NoMapInstanciatedStatus();
        }


        [HttpPost("{usertoken}/setscreensize={width},{height}")]
        public void Post2(string usertoken = "testuser", int width = 1280, int height = 960)
        {
            if (maps.ContainsKey(usertoken))
            {
                HttpContext.Response.StatusCode = 200;
                maps[usertoken].Camera.SetScreenSize(width, height);
            }
            else
                NoMapInstanciatedStatus();
        }
    }
}
