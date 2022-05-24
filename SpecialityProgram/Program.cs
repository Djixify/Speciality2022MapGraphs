// See https://aka.ms/new-console-template for more information
using SpecialityWebService;
using SpecialityWebService.Generation;
using System.Net;
using static SpecialityWebService.Map;
using static SpecialityWebService.MathObjects;
using static SpecialityWebService.Generation.Parser;
using RBush;

namespace SpecialityProgram
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //TestLexer();

            //TestNetwork();

            TestRangeTree();
            //while (true)
                //GetWMS().Wait();
        }

        public static void TestLexer()
        {
            var testcases = new List<Tuple<string, double>>()
            {
                new Tuple<string, double>("0", 0),
                new Tuple<string, double>("9", 9),
                new Tuple<string, double>("2 + 2", 4),
                new Tuple<string, double>("2 * 2", 4),
                new Tuple<string, double>("8 / 2", 4),
                new Tuple<string, double>("16 % 12", 4),
                new Tuple<string, double>("TILKM == 30", 1),
                new Tuple<string, double>("TILKM == 31", 0),
                new Tuple<string, double>("TILKM == 29", 0),
                new Tuple<string, double>("4 + TILKM == 34", 1),
                new Tuple<string, double>("4 + TILKM == 25 + 9", 1),
                new Tuple<string, double>("4 + (TILKM == 30)", 5),
                new Tuple<string, double>("4 + (TILKM == 29)", 4),
                new Tuple<string, double>("(TYPE == 'road') * TILKM", 30),
                new Tuple<string, double>("(TYPE == 'path') * infty", 0),
                new Tuple<string, double>("(TYPE == 'road') * infty", double.PositiveInfinity),
                new Tuple<string, double>("(TYPE == 'path') * infty + (TYPE == 'road') * TILKM", 30)
            };

            var environment = new Dictionary<string, ColumnData>(new List<KeyValuePair<string, ColumnData>>()
            {
                new KeyValuePair<string, ColumnData>("TILKM", new ColumnData("30", "0")),
                new KeyValuePair<string, ColumnData>("TYPE", new ColumnData("road", "path"))
            });

            foreach(Tuple<string, double> testcase in testcases.GetRange(13,4))
            {
                try 
                { 
                    double computed = Convert.ToDouble(Parser.ExecuteExpression(testcase.Item1, ref environment).Value);
                    bool match = double.IsFinite(computed) ? Math.Abs(computed - testcase.Item2) < 0.00000001 : double.IsPositiveInfinity(computed) && double.IsPositiveInfinity(testcase.Item2) || double.IsNegativeInfinity(computed) && double.IsNegativeInfinity(testcase.Item2);
                    System.Diagnostics.Debug.WriteLine($"Test: {testcase.Item1}, expected: {testcase.Item2}, result: {(match ? "Matched!" : "No match, got: " + Lexer.GetTokenExpression(testcase.Item1))} = {computed}");
                    if (!match)
                        return;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in testcase: {testcase.Item1}: Tokenized as: {Lexer.GetTokenExpression(testcase.Item1).ToString()}, Error: {ex.ToString()}\n{ex.StackTrace}");
                }
            }

            //Parser.ExecuteExpression()
        }

        public static void TestNetwork()
        {
            var rtree = new SpecialityWebService.Generation.Rtree<Vertex>();
            Vertex v1 = new Vertex(0, new Point(0, 0), new List<int>(), 0, null);
            Vertex v2 = new Vertex(1, new Point(1, 1), new List<int>(), 0, null);
            Vertex v3 = new Vertex(2, new Point(2, 2), new List<int>(), 0, null);
            rtree.Insert(v1);
            rtree.Insert(v2);
            rtree.Insert(v3);
            List<Vertex> items = rtree.Query(new Rectangle(1, 1, 0.5));

            Map map = new Map("024b9d34348dd56d170f634e067274c6", Dataset.VejmanHastigheder, 1280, 960);
            var paths = new List<SpecialityWebService.Path>() { 
                new SpecialityWebService.Path() {
                    Points = new List<Point>() { new Point(0,0), new Point(1,1), new Point(2,2) },
                    Id = 0,
                    Fid = null,
                    ColumnValues = new Dictionary<string, ColumnData>(new List<KeyValuePair<string, ColumnData>>() {
                        new KeyValuePair<string, ColumnData>("TILKM", new ColumnData("30"))
                    })
                },
                new SpecialityWebService.Path() {
                    Points = new List<Point>() { new Point(1,1), new Point(0,2), new Point(-1,3) },
                    Id = 0,
                    Fid = null,
                    ColumnValues = new Dictionary<string, ColumnData>(new List<KeyValuePair<string, ColumnData>>() {
                        new KeyValuePair<string, ColumnData>("TILKM", new ColumnData("50"))
                    })
                }
            };

            foreach (SpecialityWebService.Path path in paths)
                path.UpdateBoundaryBox();

            INetworkGenerator qgis = new QGISReferenceAlgorithm();
            Network network = new Network("testnetwork", qgis, new Rtree<int>(), new Rtree<int>());
            network.EndPointTolerance = 0.5;
            network.MidPointTolerance = 0.5;
            network.Weights = new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("euclidean distance", "distance") };

            network.Generate(paths);

            (double _, int startv) = network.ClosestVertex(new Point(0, 0), 0.5);
            (double _, int endv) = network.ClosestVertex(new Point(-1, 3), 0.5);
            List<Edge> edges = network.FindDijkstraPath(network.V[startv], network.V[endv], "euclidean distance");
            edges.Reverse();
            List<Point> expectedpath = new List<Point>() { new Point(0,0), new Point(1,1), new Point(0,2), new Point(-1,3) };
            List<Point> resultpath = new List<Point>() { edges[0].P1 };
            resultpath.AddRange(edges.Select(e => e.P2));
            foreach (Edge e in edges)
            {
                System.Diagnostics.Debug.WriteLine($"{e.P1} -> {e.P2}");
            }
            System.Diagnostics.Debug.WriteLine("Matched test path: " + expectedpath.SequenceEqual(resultpath));

            //Network network = qgis.Generate(map.GML.GetPathEnumerator(Rectangle.Infinite()), 0.5, null, null, new List<string>() { "TILKM" });

        }


        private class QueryPoint : IBound
        {
            public Point Point { get; set; }

            public Rectangle BoundaryBox { get { return new Rectangle(Point.X, Point.Y, 0.0000001); } set { } }

            public QueryPoint(double x, double y)
            {
                Point = new Point(x, y);
            }
        }

        private static void TestRangeTree()
        {
            RangeTree<QueryPoint> tree = new RangeTree<QueryPoint>(new List<QueryPoint>()
            {
                new QueryPoint(0.0,4.0),
                new QueryPoint(1.0,5.0),
                new QueryPoint(7.0,3.0),
                new QueryPoint(3.0,2.0)
            });
        }

        public static async Task GetWMS()
        {
            
            await Task.Delay(1000);

            List<byte> image = new List<byte>();
            HttpClient client = new HttpClient();
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(@"https://localhost:44342/Map/service=wms");

            using (HttpResponseMessage response = await client.GetAsync(@"https://localhost:44342/Map/service=wms"))
            using (BinaryWriter writer = new BinaryWriter(new FileStream(@"..\..\..\image.jpg", FileMode.OpenOrCreate)))
                writer.Write(await response.Content.ReadAsByteArrayAsync());
            /*
            using (HttpResponseMessage response = await client.GetAsync(@"https://localhost:44342/Map/service=wms"))
            using (BinaryReader reader = new BinaryReader(response.Content.ReadAsStream()))
            using (BinaryWriter writer = new BinaryWriter(new FileStream(@"..\..\..\image.jpg", FileMode.OpenOrCreate)))
            {
                byte[] bytes = null;
                do
                {
                    bytes = reader.ReadBytes(10 * 1024 * 1024); //10 MB
                    writer.Write(bytes);
                } while (bytes.Length > 0);
            }*/

            Console.ReadKey();
        }
    }
}
