// See https://aka.ms/new-console-template for more information
using SpecialityWebService;
using SpecialityWebService.Network;
using System.Net;
using static SpecialityWebService.Map;
using static SpecialityWebService.MathObjects;
using static SpecialityWebService.Network.Parser;

namespace SpecialityProgram
{
    public class Program
    {
        public static void Main(string[] args)
        {
            TestLexer();

            TestNetwork();

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
            var rtree = new SpecialityWebService.Network.Rtree<Vertex>();
            Vertex v1 = new Vertex(0, new Point(0, 0), new List<int>(), 0, null);
            Vertex v2 = new Vertex(1, new Point(1, 1), new List<int>(), 0, null);
            Vertex v3 = new Vertex(2, new Point(2, 2), new List<int>(), 0, null);
            rtree.Insert(v1);
            rtree.Insert(v2);
            rtree.Insert(v3);
            List<IQueryItem<Vertex>> items = rtree.Query(new Rectangle(1, 1, 0.5));

            Map map = new Map("024b9d34348dd56d170f634e067274c6", Dataset.VejmanHastigheder, 1280, 960);
            INetworkGenerator qgis = new QGISReferenceAlgorithm();
            var paths = new List<SpecialityWebService.Path>() { 
                new SpecialityWebService.Path() {
                    Points = new List<Point>() { new Point(0,0), new Point(1,1), new Point(2,2) },
                    Id = 0,
                    Fid = null,
                    ColumnValues = new Dictionary<string, string>(new List<KeyValuePair<string, string>>() {
                        new KeyValuePair<string, string>("TILKM", "30")
                    })
                },
                new SpecialityWebService.Path() {
                    Points = new List<Point>() { new Point(1,1), new Point(0,2), new Point(-1,3) },
                    Id = 0,
                    Fid = null,
                    ColumnValues = new Dictionary<string, string>(new List<KeyValuePair<string, string>>() {
                        new KeyValuePair<string, string>("TILKM", "50")
                    })
                }
            };

            foreach (SpecialityWebService.Path path in paths)
                path.UpdateBoundaryBox();

            Network network = qgis.Generate(map.GML.GetPathEnumerator(Rectangle.Infinite()), 0.5, null, null, new List<string>() { "TILKM" });

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
