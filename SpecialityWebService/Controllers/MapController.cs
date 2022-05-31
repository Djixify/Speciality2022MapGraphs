using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using SpecialityWebService.Generation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static SpecialityWebService.Generation.Lexer;
using static SpecialityWebService.Generation.Parser;
using static SpecialityWebService.MathObjects;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SpecialityWebService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MapController : ControllerBase
    {
        private static Dictionary<string, Tuple<Timer,Map>> maps = new Dictionary<string, Tuple<Timer, Map>>();

        private readonly ILogger<MapController> _logger;

        public MapController(ILogger<MapController> logger)
        {
            _logger = logger;
        }

        private List<string> dfvalidcolumns = new List<string>() { "distance", "gml_id", "objectid", "id_lokalid", "id_namespa", "tempid", "g_status", "virk_fra", "virk_til", "virk_akt", "reg_fra", "reg_til", "reg_akt", "f_omr", "f_haendels", "f_proces", "status", "reg_spec", "dataansvar", "p_noej", "v_noej", "p_smetode", "v_smetode", "kommentar", "app", "vejmidtety", "vejmyndigh", "cvfadmnr", "kommunekod", "vejkode", "vejkategor", "trafikart", "niveau", "overflade", "tilogfrako", "rundkoerse", "gmlid", "layer", "path" };
        private List<string> vmvalidcolumns = new List<string>() { "distance", "gml_id", "BESTYRER", "ADMVEJNR", "ADMVEJDEL", "FRAKMT", "FRAKM", "FRAM", "TILKMT", "TILKM", "TILM", "CPR_VEJNAV", "HAST_VEJSI", "KODE_HAST_", "SIDEOFROAD", "ID_HAST_VE", "SIDEOFRO_1", "OFFSET", "HAST_GENER", "KODE_HAS_1", "HAST_LOKAL", "HAST_GAELD", "HAST_ANBEF", "HAST_VAR_H", "KODE_HAS_2", "HAST_BYKOD", "KODE_HAS_3", "VEJSTIKLAS", "KODE_VEJST", "VEJTYPESKI", "KODE_VEJTY", "HAST_BEMAE", "HAST_SENES", "HAST_BRUGE" };

        private bool TryConvertDataset(string input, out Map.Dataset dataset, out Map.DatasetSize datasetsize)
        {
            string[] dsinfo = input.Split('-');
            bool success = true;
            switch (dsinfo[0])
            {
                case "geodanmark60":
                    dataset = Map.Dataset.GeoDanmark60;
                    break;
                case "vejmanhastigheder":
                    dataset = Map.Dataset.VejmanHastigheder;
                    break;
                default:
                    dataset = Map.Dataset.VejmanHastigheder;
                    success = false;
                    break;
            }
            switch (dsinfo[1])
            {
                case "small":
                    datasetsize = Map.DatasetSize.Small;
                    return success;
                case "bridge":
                    datasetsize = Map.DatasetSize.Bridge;
                    return success;
                case "medium":
                    datasetsize = Map.DatasetSize.Medium;
                    return success;
                case "large":
                    datasetsize = Map.DatasetSize.Large;
                    return success;
                default:
                    datasetsize = Map.DatasetSize.Medium;
                    return false;
            }
        }

        private void NoMapInstanciatedStatus()
        {
            Debug.WriteLine("Invalid request received, no map with client id present");
            HttpContext.Response.StatusCode = 400;
            HttpContext.Response.Headers.Add(new KeyValuePair<string, StringValues>("reason", new StringValues("No map instantiated, please call post on 'startsession/token={wmstoken};dataset={dataset};minres={minresolution}' first")));
        }


        private int usertimeoutminutes = 240;
        private void ResetTimer(string id)
        {
            if (maps.ContainsKey(id))
            {
                maps[id].Item1.Change(TimeSpan.FromMinutes(usertimeoutminutes), TimeSpan.Zero);
            }
        }

        private void RemoveTimer(object? obj)
        {
            if (obj is string id && maps.ContainsKey(id))
            {
                maps[id].Item1.Dispose();
                maps[id].Item2.Dispose();
                maps.Remove(id);

                string sessionpath = System.IO.Path.Combine(@"Resources\Generated", id);
                if (Directory.Exists(sessionpath))
                    Directory.Delete(sessionpath, true);   
            }
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
            ResetTimer(usertoken);

            return File(maps[usertoken].Item2.RenderImage(System.Drawing.Imaging.ImageFormat.Jpeg, HttpContext), "image/jpg");
        }

        // GET: /Map
        [HttpGet("token={wmstoken};dataset={dataset};bbox={minx},{miny},{maxx},{maxy}")]
        public FileContentResult GetMapBoundarybox(string wmstoken = "024b9d34348dd56d170f634e067274c6", string dataset = "geodanmark60/vejmanhastigheder", double minx = 586835.1, double miny = 6135927.2, double maxx = 591812.3, double maxy = 6139738.0)
        {
            TryConvertDataset(dataset, out Map.Dataset ds, out Map.DatasetSize dssize);

            Map map = new Map(wmstoken, "testsession", ds, dssize, 1280, minx, miny, maxx, maxy);

            return File(map.RenderImage(System.Drawing.Imaging.ImageFormat.Jpeg, HttpContext), "image/jpg");
        }

        [HttpGet("{usertoken}/mapsize")]
        public ContentResult GetMapScreenSize(string usertoken = "testuser")
        {
            if (!maps.ContainsKey(usertoken))
            {
                NoMapInstanciatedStatus();
                return Content("");
            }

            Rectangle screenview = maps[usertoken].Item2.Camera.ScreenViewPort;
            ResetTimer(usertoken);
            return Content((int)screenview.Width + "," + (int)screenview.Height);
        }

        [HttpGet("{usertoken}/mapdetails")]
        public ContentResult GetMapDetails(string usertoken)
        {
            if (!maps.ContainsKey(usertoken))
            {
                NoMapInstanciatedStatus();
                return Content("");
            }

            HttpContext.Response.StatusCode = 200;
            ResetTimer(usertoken);
            return Content("<p id='stats'>" + maps[usertoken].Item2.ToString().Replace("\n", "<br>") + "</p>");
        }

        [HttpGet("{usertoken}/listnetworks")]
        public ContentResult GetNetworks(string usertoken = "testuser")
        {
            if (!maps.ContainsKey(usertoken))
            {
                NoMapInstanciatedStatus();
                return Content("");
            }


            ResetTimer(usertoken);
            if (maps[usertoken].Item2.Networks.Count > 0)
            {
                HttpContext.Response.StatusCode = 200;
                return Content(maps[usertoken].Item2.Networks.Aggregate("", (acc, item) => acc + "," + item.Key).Substring(1) + ",Custom (below)");
            }
            else
            {
                HttpContext.Response.StatusCode = 400;
                return Content("Custom (below)");
            }
        }

        [HttpGet("{usertoken}/getnetworkpreset={preset}")]
        public ContentResult GetNetworkPreset(string usertoken = "testuser", string preset = "none")
        {
            if (!maps.ContainsKey(usertoken))
            {
                NoMapInstanciatedStatus();
                return Content("");
            }

            if (!maps[usertoken].Item2.Networks.ContainsKey(preset))
            {
                return Content("");
            }

            ResetTimer(usertoken);
            Network network = maps[usertoken].Item2.Networks[preset];
            maps[usertoken].Item2.RenderDataset = false;
            maps[usertoken].Item2.SelectedNetwork = preset;
            network.SelectedStartVertex = 0;
            network.SelectedEndVertex = 0;
            StringBuilder sb = new StringBuilder();
            sb.Append(network.Name).Append(",")
                .Append(network.Generator == Generator.QGIS ? "QGIS" : "Proposed").Append(",")
                .Append(network.MidPointTolerance.ToString(CultureInfo.InvariantCulture)).Append(",")
                .Append(network.EndPointTolerance.ToString(CultureInfo.InvariantCulture)).Append(",")
                .Append(network.DirectionColumn).Append(",")
                .Append(network.DirectionForwardsValue).Append(",")
                .Append(network.DirectionBackwardsValue);
            foreach (KeyValuePair<string, string> w in network.Weights) {
                sb.Append(",").Append(w.Key);
                sb.Append(",").Append(WebUtility.UrlEncode(w.Value.Replace("+", "]")));
            }

            return Content(sb.ToString());
        }

        [HttpGet("{usertoken}/getnetworkstatus")]
        public ContentResult GetNetworkStatus(string usertoken = "testuser")
        {
            if (!maps.ContainsKey(usertoken))
            {
                NoMapInstanciatedStatus();
                return Content("");
            }

            ResetTimer(usertoken);
            if (maps[usertoken].Item2.Networks.ContainsKey(maps[usertoken].Item2.SelectedNetwork))
            {
                HttpContext.Response.StatusCode = 200;
                Network network = maps[usertoken].Item2.Networks[maps[usertoken].Item2.SelectedNetwork];
                return Content(network.StatusString);
            }
            HttpContext.Response.StatusCode = 400;
            return Content("");
        }

        [HttpGet("{usertoken}/validateformula='{formula}'")]
        public ContentResult ValidateFormula(string usertoken = "testuser", string formula = "test")
        {
            if (!maps.ContainsKey(usertoken))
            {
                NoMapInstanciatedStatus();
                return Content("");
            }
            string formulaparsed = System.Web.HttpUtility.UrlDecode(formula);
            formulaparsed = formulaparsed.Replace(']', '+');

            List<string> validvariables = null;
            if (maps[usertoken].Item2.GML is GeoDanmark60_GML)
                validvariables = dfvalidcolumns;
            else validvariables = vmvalidcolumns;

            ResetTimer(usertoken);
            HttpContext.Response.StatusCode = 200;
            try
            {
                Token tokens = Lexer.GetTokenExpression(formulaparsed);
                if (tokens.Type == Token.Kind.Primitive && tokens.Primitive == Primitive.String)
                    return Content("Failed as the input was just a string: " + tokens.Value);
                List<Token> variables = Lexer.ExtractPrimitiveTokens(Lexer.Primitive.Variable, tokens);


                Dictionary<string, ColumnData> env = new Dictionary<string, ColumnData>();
                foreach (string variable in validvariables)
                {
                    env.Add(variable, new ColumnData("0"));
                }

                Parser.ExecuteExpression(tokens, ref env);
                return Content("Success parsing: " + formulaparsed);
            }
            catch(ParseException pex)
            {
                System.Diagnostics.Debug.WriteLine(pex.ToString());
                System.Diagnostics.Debug.WriteLine(pex.StackTrace);
                return Content(pex.ToString());
            }
            catch(RuntimeException rex)
            {
                System.Diagnostics.Debug.WriteLine(rex.ToString());
                System.Diagnostics.Debug.WriteLine(rex.StackTrace);
                return Content(rex.ToString());
            }
            catch(KeyNotFoundException ex)
            {
                var message = ex.ToString();
                int variablestart = message.IndexOf('\'');
                int variableend = message.LastIndexOf('\'');
                message = message.Substring(variablestart + 1, variableend - variablestart - 1);
                return Content("Given variable: '" + message + "' is not valid, only valid options are: " + (validvariables.Aggregate("", (acc, item) => acc + ", " + item).Substring(2)));
            }
        }

        [HttpGet("{usertoken}/validatecolumn={column}")]
        public ContentResult ValidateColumn(string usertoken = "testuser", string column = "test")
        {
            if (!maps.ContainsKey(usertoken))
            {
                NoMapInstanciatedStatus();
                return Content("");
            }

            List<string> validvariables = null;
            if (maps[usertoken].Item2.GML is GeoDanmark60_GML)
                validvariables = dfvalidcolumns;
            else validvariables = vmvalidcolumns;

            HttpContext.Response.StatusCode = 200;
            return Content(validvariables.Contains(column) ? "Success" : ("Given direction column: '" + column + "' is not valid, only valid options are: " + (validvariables.Aggregate("", (acc, item) => acc + ", " + item).Substring(2))));
        }

        [HttpGet("{usertoken}/networkstats")]
        public ContentResult GetNetworkStatistic(string usertoken = "testuser")
        {
            if (!maps.ContainsKey(usertoken))
            {
                NoMapInstanciatedStatus();
                return Content("");
            }

            if (maps[usertoken].Item2.SelectedNetwork != null && maps[usertoken].Item2.Networks.ContainsKey(maps[usertoken].Item2.SelectedNetwork))
            {
                HttpContext.Response.StatusCode = 200;
                Network network = maps[usertoken].Item2.Networks[maps[usertoken].Item2.SelectedNetwork];
                StringBuilder sb = new StringBuilder();
                if (!network.HasGenerated)
                    sb.AppendLine("Still generating:").Append("  ").Append(network.ProgressString);
                else
                {
                    sb.AppendLine("Name: " + network.Name);
                    sb.AppendLine("Dataset: " + network.DatasetName);
                    sb.AppendLine("#Paths = " + network.PathCount);
                    sb.AppendLine("#Points = " + network.PointCount);
                    sb.AppendLine("Generation time: " + network.GenerationTime + "ms");
                    sb.AppendLine("|V| = " + network.V.Count);
                    sb.AppendLine("|E| = " + network.E.Count);
                    if (network.Generator == Generator.Proposed)
                    {
                        sb.AppendLine("Path query area = " + Math.Round(network.QueriedAreaFullPaths / 1000000, 3).ToString(CultureInfo.InvariantCulture) + "km2");
                        sb.AppendLine("Segment queries area = " + Math.Round(network.QueriedAreaSegments / 1000000, 3).ToString(CultureInfo.InvariantCulture) + "km2");
                    }
                    sb.AppendLine();
                    sb.AppendLine("Start vertex: " + network.SelectedStartVertex);
                    sb.AppendLine("End vertex:   " + network.SelectedEndVertex);
                    sb.AppendLine("Dijkstra on weights:");
                    int j = 0;
                    string[] color = new string[] { "Cyan", "Magenta", "Yellow" };
                    foreach (KeyValuePair<string, List<int>> path in network.EdgesBetween ?? new Dictionary<string, List<int>>())
                    {
                        double weight = 0.0;
                        foreach (int i in path.Value)
                        {
                            Edge e = network.E[i];
                            weight += e.Weights[path.Key];
                        }

                        sb.AppendLine(color[j % 3] + " path:");
                        sb.Append(path.Key).Append(": |E|=").Append(path.Value.Count).Append(" cost=").AppendLine(Math.Round(weight, 2).ToString(CultureInfo.InvariantCulture));
                        j++;
                    }
                }
                return Content(sb.ToString());
            }
            else
            {
                return Content("");
            }
        }

        [HttpPost("{usertoken}/startsession/token={wmstoken};dataset={dataset};minres={minresolution};bbox={minx},{miny},{maxx},{maxy}")]
        public void StartSession(string usertoken = "testuser", string wmstoken = "024b9d34348dd56d170f634e067274c6", string dataset = "vejmanhastigheder-small", int minresolution = 1280, double minx = 586835.1, double miny = 6135927.2, double maxx = 591812.3, double maxy = 6139738.0)
        {
            TryConvertDataset(dataset, out Map.Dataset ds, out Map.DatasetSize dssize);
            if (maps.ContainsKey(usertoken))
            {
                maps[usertoken].Item2.Dispose();
                maps[usertoken].Item1.Dispose();
            }
            maps[usertoken] = Tuple.Create(new Timer(RemoveTimer, usertoken, 0, 0), new Map(wmstoken, usertoken, ds, dssize, minresolution, minx, miny, maxx, maxy));
            ResetTimer(usertoken);
            HttpContext.Response.StatusCode = 200;
        }

        [HttpPost("{usertoken}/startsession/token={wmstoken};dataset={dataset};minres={minresolution}")]
        public void StartSession(string usertoken = "testuser", string wmstoken = "024b9d34348dd56d170f634e067274c6", string dataset = "vejmanhastigheder-small", int minresolution = 1280)
        {
            TryConvertDataset(dataset, out Map.Dataset ds, out Map.DatasetSize dssize);
            if (maps.ContainsKey(usertoken))
            {
                maps[usertoken].Item1.Dispose();
                maps[usertoken].Item2.Dispose();
            }
            maps[usertoken] = Tuple.Create(new Timer(RemoveTimer, usertoken, 0, 0), new Map(wmstoken, usertoken, ds, dssize, minresolution));
            ResetTimer(usertoken);
            HttpContext.Response.StatusCode = 200;
        }

        [HttpPost("{usertoken}/startsession/token={wmstoken};dataset={dataset};width={width},height={height}")]
        public void StartSession(string usertoken = "testuser", string wmstoken = "024b9d34348dd56d170f634e067274c6", string dataset = "vejmanhastigheder-small", int width = 1280, int height = 960)
        {
            TryConvertDataset(dataset, out Map.Dataset ds, out Map.DatasetSize dssize);
            if (maps.ContainsKey(usertoken))
            {
                maps[usertoken].Item1.Dispose();
                maps[usertoken].Item2.Dispose();
            }
            maps[usertoken] = Tuple.Create(new Timer(RemoveTimer, usertoken, 0, 0), new Map(wmstoken, usertoken, ds, dssize, width, height));
            ResetTimer(usertoken);
            HttpContext.Response.StatusCode = 200;
        }

        [HttpPost("{usertoken}/changedataset={dataset}")]
        public void ChangeDataset(string usertoken = "testuser", string dataset = "vejmanhastigheder-small")
        {
            if (maps.ContainsKey(usertoken))
            {
                HttpContext.Response.StatusCode = 200;
                TryConvertDataset(dataset, out Map.Dataset ds, out Map.DatasetSize dssize);
                System.Diagnostics.Debug.WriteLine("Changed dataset to " + ds.ToString() + " " + dssize.ToString());
                maps[usertoken].Item2.SetDataset(ds, dssize);
                ResetTimer(usertoken);
            }
            else
                NoMapInstanciatedStatus();
        }

        [HttpPost("{usertoken}/move={x},{y}")]
        public void Move(string usertoken = "testuser", int x=0, int y=0)
        {
            if (maps.ContainsKey(usertoken))
            {
                HttpContext.Response.StatusCode = 200;
                maps[usertoken].Item2.Camera.MoveScreen(x, y);
                ResetTimer(usertoken);
            }
            else
                NoMapInstanciatedStatus();
        }

        [HttpPost("{usertoken}/zoom={zoom}")]
        public void Zoom(string usertoken = "testuser", double zoom=1.0)
        {
            if (maps.ContainsKey(usertoken))
            {
                HttpContext.Response.StatusCode = 200;
                maps[usertoken].Item2.Camera.ZoomView(zoom);
                ResetTimer(usertoken);
            }
            else
                NoMapInstanciatedStatus();
        }

        [HttpPost("{usertoken}/debug={debug}")]
        public void SetDebug(string usertoken = "testuser", bool debug=true)
        {
            if (maps.ContainsKey(usertoken))
            {
                HttpContext.Response.StatusCode = 200;
                maps[usertoken].Item2.Debug = debug;
                ResetTimer(usertoken);
            }
            else
                NoMapInstanciatedStatus();
        }

        [HttpPost("{usertoken}/generatenetworksimple/{type}/name={name};endpointtolerance={endpointtolerance};midpointtolerance={midpointtolerance};weights={weights}")]
        public void GenerateNetwork(string usertoken = "testuser", string type = "QGIS", string name = "Testnetwork", double endpointtolerance = 2.5, double midpointtolerance = 2.5, string weights = "")
        {
            GenerateNetwork(usertoken, type, name, endpointtolerance, midpointtolerance, null, null, null, weights);
        }
        //     " + sessiontoke/generatenetwork/" + g /name=" + n";endpointtolerance=" + endpointradius ;midpointtolerance=" + midpointradius ;directioncolumn="                ,forwardsval=" + direction,backwardsval=" + directionb;weights=" + weights;
        [HttpPost("{usertoken}/generatenetwork/{type}/name={name};endpointtolerance={endpointtolerance};midpointtolerance={midpointtolerance};directioncolumn={directioncolumn},forwardsval={forwardsval},backwardsval={backwardsval};weights={weights}")]
        public async void GenerateNetwork(string usertoken = "testuser", string type = "QGIS", string name = "Testnetwork", double endpointtolerance = 2.5, double midpointtolerance = 2.5, string directioncolumn="", string forwardsval="", string backwardsval="", string weights="")
        {
            if (maps.ContainsKey(usertoken))
            {
                Generator gen;
                switch(type)
                {
                    case "QGIS":
                        HttpContext.Response.StatusCode = 200;
                        gen = Generator.QGIS;
                        break;
                    case "Proposed":
                        HttpContext.Response.StatusCode = 200;
                        gen = Generator.Proposed;
                        break;
                    default:
                        HttpContext.Response.StatusCode = 400;
                        return;
                }
                ResetTimer(usertoken);

                if (maps[usertoken].Item2.Networks.ContainsKey(name))
                {
                    maps[usertoken].Item2.Networks[name].Dispose();
                }

                Network network = new Network(name, maps[usertoken].Item2.ActiveDatasetName, usertoken, gen);
                network.EndPointTolerance = endpointtolerance;
                network.MidPointTolerance = midpointtolerance;
                network.DirectionColumn = directioncolumn;
                network.DirectionForwardsValue = forwardsval;
                network.DirectionBackwardsValue = backwardsval;
                network.Weights = new List<KeyValuePair<string, string>>();
                string[] weightssplit = WebUtility.UrlDecode(weights.Replace("]", "+")).Split(";");
                foreach(string weightsplit in weightssplit)
                {
                    string[] labelformula = weightsplit.Split(",");
                    if (labelformula.Length == 2)
                        network.Weights.Add(new KeyValuePair<string, string>(labelformula[0], labelformula[1]));
                }
                if (network.Weights.Count == 0)
                    network.Weights.Add(new KeyValuePair<string, string>("Euclidean distance", "distance"));

                maps[usertoken].Item2.Networks[network.Name] = network;

                try
                {
                    await network.Generate(maps[usertoken].Item2);
                }
                catch(RuntimeException rex)
                {
                    System.Diagnostics.Debug.WriteLine(rex.ToString());
                    System.Diagnostics.Debug.WriteLine(rex.StackTrace.ToString());
                    HttpContext.Response.StatusCode = 400;
                }

                maps[usertoken].Item2.SelectedNetwork = network.Name;

                ResetTimer(usertoken);
            }
            else
                NoMapInstanciatedStatus();
        }

        [HttpPost("{usertoken}/selectvertex={x},{y}")]
        public void SelectVertex(string usertoken = "testuser", int x = 0, int y = 0)
        {
            Debug.WriteLine("SelectVertex called x:" + x + " y:" + y);
            if (maps.ContainsKey(usertoken) 
                && !maps[usertoken].Item2.RenderDataset 
                && maps[usertoken].Item2.SelectedNetwork != null 
                && maps[usertoken].Item2.Networks.ContainsKey(maps[usertoken].Item2.SelectedNetwork))
            {
                HttpContext.Response.StatusCode = 200;

                Point worldpos = maps[usertoken].Item2.Camera.ToWorld(x, y);
                double widthquery = (200 / maps[usertoken].Item2.Camera.Zoom);

                Network network = maps[usertoken].Item2.Networks[maps[usertoken].Item2.SelectedNetwork];
                int startvertex = -1;
                int endvertex = -1;
                if (network.ShouldOverrideStart)
                {
                    (double dist, startvertex) = network.ClosestVertex(worldpos, widthquery);
                    if (startvertex != endvertex)
                    {
                        bool found = !(double.IsPositiveInfinity(dist));
                        network.SelectedStartVertex = found ? startvertex : -1;
                        Debug.WriteLine("SelectVertex updating start vertex to " + network.SelectedStartVertex);
                        if (found)
                            network.ShouldOverrideStart = !network.ShouldOverrideStart;
                    }
                }
                else
                {
                    (double dist, endvertex) = network.ClosestVertex(worldpos, widthquery);
                    if (endvertex != startvertex)
                    {
                        bool found = !(double.IsPositiveInfinity(dist));
                        network.SelectedEndVertex = found ? endvertex : -1;
                        Debug.WriteLine("SelectVertex updating end vertex to " + network.SelectedStartVertex);
                        if (found)
                            network.ShouldOverrideStart = !network.ShouldOverrideStart;
                    }
                }

                if (network.SelectedStartVertex > -1 && network.SelectedEndVertex > -1)
                {
                    Debug.WriteLine("SelectVertex both vertices present, generating path...");
                    network.EdgesBetween = new Dictionary<string, List<int>>();
                    foreach (KeyValuePair<string, string> weight in network.Weights)
                    {
                        network.EdgesBetween[weight.Key] = network.FindDijkstraPath(network.SelectedStartVertex, network.SelectedEndVertex, weight.Key);
                        Debug.WriteLine("SelectVertex both vertices present, generated path, total edges: " + network.EdgesBetween[weight.Key].Count);
                    }
                }
                ResetTimer(usertoken);
            }
            else
                NoMapInstanciatedStatus();
        }

        [HttpPost("{usertoken}/toggledatasetrender")]
        public void ToggleDatasetRender(string usertoken = "testuser")
        {
            Debug.WriteLine("ToggleDatasetRender called");
            if (maps.ContainsKey(usertoken))
            {
                HttpContext.Response.StatusCode = 200;
                maps[usertoken].Item2.RenderDataset = !maps[usertoken].Item2.RenderDataset;
                Debug.WriteLine("ToggleDatasetRender rendering dataset: " + maps[usertoken].Item2.RenderDataset);
                ResetTimer(usertoken);
            }
            else
                NoMapInstanciatedStatus();
        }

        [HttpPost("{usertoken}/rendernetwork={network}")]
        public void RenderNetwork(string usertoken = "testuser", string network = null)
        {
            if (maps.ContainsKey(usertoken) && network != null)
            {
                if (maps[usertoken].Item2.Networks.ContainsKey(network))
                {
                    HttpContext.Response.StatusCode = 200;
                    maps[usertoken].Item2.SelectedNetwork = network;
                }
                else
                {
                    HttpContext.Response.StatusCode = 400;
                    HttpContext.Response.Headers.Add(KeyValuePair.Create("reason", new StringValues("Network did not exist")));
                }
                ResetTimer(usertoken);
            }
            else
                NoMapInstanciatedStatus();
        }


        [HttpPost("{usertoken}/setscreensize={width},{height}")]
        public void SetScreenSize(string usertoken = "testuser", int width = 1280, int height = 960)
        {
            if (maps.ContainsKey(usertoken))
            {
                HttpContext.Response.StatusCode = 200;
                maps[usertoken].Item2.Camera.SetScreenSize(width, height);
                ResetTimer(usertoken);
            }
            else
                NoMapInstanciatedStatus();
        }
    }
}
