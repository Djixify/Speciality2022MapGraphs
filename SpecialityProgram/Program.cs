// See https://aka.ms/new-console-template for more information
using SpecialityWebService;
using SpecialityWebService.Generation;
using System.Net;
using static SpecialityWebService.Map;
using static SpecialityWebService.MathObjects;
using static SpecialityWebService.Generation.Parser;
using RBush;
using static SpecialityWebService.Generation.Lexer;

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
                new Tuple<string, double>("TILKM == 'road'", 0),
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



            foreach(Tuple<string, double> testcase in testcases.GetRange(9,0))
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

            try
            {
                //Token test = Lexer.GetTokenExpression("(distance * 1000) / ((HAST_GAELD != \"NULL\") * HAST_GAELD + (HAST_GAELD == \"NULL\") * 50) * 60");
                Token test = Lexer.GetTokenExpression("HAST_GAELD != \"NULL\" ? HAST_GAELD : 50");
                Dictionary<string, ColumnData> env = new Dictionary<string, ColumnData>(new KeyValuePair<string, ColumnData>[]
                {
                    KeyValuePair.Create("HAST_GAELD", new ColumnData("NULL"))
                });
                ReturnValue val = Parser.ExecuteExpression("HAST_GAELD != \"NULL\" ? HAST_GAELD : 50", ref env);
                //Token test = Lexer.GetTokenExpression("((H == J) * 2)");
            }
            catch (ParseException pex)
            {
                System.Diagnostics.Debug.WriteLine(pex.ToString());
                System.Diagnostics.Debug.WriteLine(pex.StackTrace);
            }

            //Parser.ExecuteExpression()
        }

        public static async void TestNetwork()
        {
            var rtree = new SpecialityWebService.Generation.Rtree<Vertex>();
            Vertex v1 = new Vertex(0, new Point(0, 0), new List<int>(), 0, null);
            Vertex v2 = new Vertex(1, new Point(1, 1), new List<int>(), 0, null);
            Vertex v3 = new Vertex(2, new Point(2, 2), new List<int>(), 0, null);
            rtree.Insert(v1);
            rtree.Insert(v2);
            rtree.Insert(v3);
            List<Vertex> items = rtree.Query(new Rectangle(1, 1, 0.5));

            Map map = new Map("024b9d34348dd56d170f634e067274c6", "testsession", Dataset.VejmanHastigheder, DatasetSize.Small, 1280, 960);
            var paths = new List<SpecialityWebService.Path>() { 
                new SpecialityWebService.Path() {
                    //Points = new List<Point>() { new Point(0,0), new Point(1,1), new Point(2,2) },
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

            Network network = new Network("testnetwork", "testdataset", "testsession", Generator.QGIS, new Rtree<int>(), new Rtree<int>());
            network.EndPointTolerance = 0.5;
            network.MidPointTolerance = 0.5;
            network.Weights = new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("euclidean distance", "distance") };

            network.Generate(paths).Wait();

            (double _, int startv) = network.ClosestVertex(new Point(0, 0), 0.5);
            (double _, int endv) = network.ClosestVertex(new Point(-1, 3), 0.5);
            List<Edge> edges = network.FindDijkstraPath(network.V[startv], network.V[endv], "euclidean distance");
            List<Point> expectedpath = new List<Point>() { new Point(0,0), new Point(1,1), new Point(0,2), new Point(-1,3) };
            if (edges.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("Failed to generated graph");
                return;
            }
            List<Point> resultpath = new List<Point>() { edges[0].RenderPoints.First() };
            resultpath.AddRange(edges.Select(e => e.RenderPoints.Last()));
            foreach (Edge e in edges)
            {
                System.Diagnostics.Debug.WriteLine($"{e.RenderPoints.First()} -> {e.RenderPoints.Last()}");
            }
            System.Diagnostics.Debug.WriteLine("Matched test path: " + expectedpath.SequenceEqual(resultpath));

            Network ownnetwork = new Network("testnetwork2", "testdataset", "testsession", Generator.Proposed, new Rtree<int>(), new Rtree<int>());
            ownnetwork.EndPointTolerance = 0.5;
            ownnetwork.MidPointTolerance = 0.5;
            ownnetwork.Weights = new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("euclidean distance", "distance") };

            ownnetwork.Generate(paths).Wait(); ;

            (double _, startv) = ownnetwork.ClosestVertex(new Point(0, 0), 0.5);
            (double _, endv) = ownnetwork.ClosestVertex(new Point(-1, 3), 0.5);
            edges = ownnetwork.FindDijkstraPath(ownnetwork.V[startv], ownnetwork.V[endv], "euclidean distance");
            expectedpath = new List<Point>() { new Point(0, 0), new Point(1, 1), new Point(0, 2), new Point(-1, 3) };
            if (edges.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("Failed to generated graph");
                return;
            }
            resultpath = new List<Point>() { edges[0].RenderPoints.First() };
            resultpath.AddRange(edges.Select(e => e.RenderPoints.Last()));
            foreach (Edge e in edges)
            {
                System.Diagnostics.Debug.WriteLine($"{e.RenderPoints.First()} -> {e.RenderPoints.Last()}");
            }
            System.Diagnostics.Debug.WriteLine("Matched test path: " + expectedpath.SequenceEqual(resultpath));
            //Network network = qgis.Generate(map.GML.GetPathEnumerator(Rectangle.Infinite()), 0.5, null, null, new List<string>() { "TILKM" });

            paths = new List<SpecialityWebService.Path>() {
                new SpecialityWebService.Path() {
                    Points = new List<Point>() { new Point(0,0), new Point(2,2) },
                    //Points = new List<Point>() { new Point(0,0), new Point(1,1), new Point(2,2) },
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

            Network network2 = new Network("testnetwork3", "testdataset", "testsession", Generator.QGIS, new Rtree<int>(), new Rtree<int>());
            network2.EndPointTolerance = 0.5;
            network2.MidPointTolerance = 0.5;
            network2.Weights = new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("euclidean distance", "distance") };

            network2.Generate(paths).Wait();

            (double _, startv) = network2.ClosestVertex(new Point(0, 0), 0.5);
            (double _, endv) = network2.ClosestVertex(new Point(-1, 3), 0.5);
            edges = network2.FindDijkstraPath(network2.V[startv], network2.V[endv], "euclidean distance");
            if (edges.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("No path found as expected");
            }

            Network ownnetwork2 = new Network("testnetwork4", "testdataset", "testsession", Generator.Proposed, new Rtree<int>(), new Rtree<int>());
            ownnetwork2.EndPointTolerance = 0.5;
            ownnetwork2.MidPointTolerance = 0.5;
            ownnetwork2.Weights = new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("euclidean distance", "distance") };

            ownnetwork2.Generate(paths).Wait();

            (double _, startv) = ownnetwork2.ClosestVertex(new Point(0, 0), 0.5);
            (double _, endv) = ownnetwork2.ClosestVertex(new Point(-1, 3), 0.5);
            edges = ownnetwork2.FindDijkstraPath(ownnetwork2.V[startv], ownnetwork2.V[endv], "euclidean distance");
            expectedpath = new List<Point>() { new Point(0, 0), new Point(1, 1), new Point(0, 2), new Point(-1, 3) };
            if (edges.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("Failed to generated graph");
                return;
            }
            resultpath = new List<Point>() { edges[0].RenderPoints.First() };
            resultpath.AddRange(edges.Select(e => e.RenderPoints.Last()));
            foreach (Edge e in edges)
            {
                System.Diagnostics.Debug.WriteLine($"{e.RenderPoints.First()} -> {e.RenderPoints.Last()}");
            }
            System.Diagnostics.Debug.WriteLine("Matched test path: " + expectedpath.SequenceEqual(resultpath));

            var paths1 = map.GML.GetPathEnumerator(Rectangle.Infinite()).Where(path => path.Fid == "hastighedsgraenser.fid-5e12b4c2_180dec518fa_-462d" || path.Fid == "hastighedsgraenser.fid-5e12b4c2_180dec518fa_-658b").ToList();
            Network ownnetwork3 = new Network("testnetwork5", "testdataset", "testsession", Generator.Proposed, new Rtree<int>(), new Rtree<int>());
            ownnetwork3.EndPointTolerance = 2.5;
            ownnetwork3.MidPointTolerance = 2.5;
            ownnetwork3.Weights = new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("euclidean distance", "distance") };

            ownnetwork3.Generate(paths1).Wait();

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
            var tree2 = new RedBlackRangeTree<int>();
            int range = 1000000;
            int i;
            for (i = 0; i < range; i++)
            {
                tree2.Insert(i, i);
            }
            System.Diagnostics.Debug.WriteLine(tree2.ComputeDepth() + " should be less than " + Math.Ceiling(2 * Math.Log(range + 1, 2)));
            var nodestest = new Queue<RedBlackRangeNode<int>>();
            nodestest.Enqueue(tree2._root);
            while (nodestest.Count > 0)
            {
                var node = nodestest.Dequeue();
                Region tmp = new Region(node.Key, node.Key);

                if (node.Left != null)
                {
                    nodestest.Enqueue(node.Left);
                    tmp.Left = node.Left.Region.Left;
                }
                if (node.Right != null)
                {
                    nodestest.Enqueue(node.Right);
                    tmp.Right = node.Right.Region.Right;
                }

                if (tmp.Left != node.Region.Left && tmp.Right != node.Region.Right)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to have correct bound");
                    break;
                }
            }

            var tree3 = new RedBlackRangeTree<int>();
            double[] nodes = new double[] { 
                11,
                2,
                14,
                1,
                7,
                15,
                5, 
                8,
                4
            };
            i = 0;
            foreach (double node in nodes)
            {
                tree3.Insert(node, i);
                //System.Diagnostics.Debug.WriteLine(tree3);
                i++;
            }
            System.Diagnostics.Debug.WriteLine(tree3.ComputeDepth() + " should be less than " + Math.Ceiling(2 * Math.Log(range + 1, 2)));


            var tree4 = new RedBlackRangeTree<int>();
            Random rnd = new Random();
            range = 100;
            for (i = 0; i < range; i++)
            {
                double key = rnd.NextDouble() * range;
                tree4.Insert(key, i);
            }
            System.Diagnostics.Debug.WriteLine(tree4.ComputeDepth() + " should be less than " + Math.Ceiling(2 * Math.Log(range + 1, 2)));

            List<double> doubles = new List<double>();
            for (double j = 0.0; j < 100; j += 2.0)
            {
                doubles.Add(j);
            }
            System.Diagnostics.Debug.WriteLine(FindLeftMost(doubles, 1.5));
            System.Diagnostics.Debug.WriteLine(FindLeftMost(doubles, 3.5));
            System.Diagnostics.Debug.WriteLine(FindLeftMost(doubles, 5.5));
            System.Diagnostics.Debug.WriteLine(FindLeftMost(doubles, 55.5));

            Point[] testpoints = new Point[]
            {
                new Point(5.0,1.0),
                new Point(9.0,1.0),
                new Point(3.0,3.0),
                new Point(5.0,4.0),
                new Point(4.0,8.0),
                new Point(6.0,2.0),
                new Point(8.0,1.0),
                new Point(1.0,4.0),
            };
            RedBlackRangeTree2D<int> tree5 = new RedBlackRangeTree2D<int>();
            i = 1;
            var testquery = Rectangle.FromLTRB(0.5, 3.5, 8.5, 0.5);
            foreach (Point p in testpoints)
            {
                tree5.Insert(p.X, p.Y, i++);
            }
            System.Diagnostics.Debug.WriteLine("Found the following 2d points(RedBlackTree):");
            foreach(int j in tree5.Query(testquery))
            {
                System.Diagnostics.Debug.Write(j + ", ");
            }

            Rtree<int> tree6 = new Rtree<int>();
            i = 1;
            foreach (Point p in testpoints)
            {
                tree6.Insert(new IntEnvelop(i++, new Rectangle(p, 0.00001)));
            }
            System.Diagnostics.Debug.WriteLine("Found the following 2d points(RTree):");
            foreach (int j in tree6.Query(testquery))
            {
                System.Diagnostics.Debug.Write(j + ", ");
            }

            QuadTree<int> tree7 = new QuadTree<int>(new Rectangle(5,5,5), 10);
            i = 1;
            foreach (Point p in testpoints)
            {
                tree7.Insert(new IntEnvelop(i++, new Rectangle(p, 0.00001)));
            }
            System.Diagnostics.Debug.WriteLine("Found the following 2d points(Quadtree):");
            foreach (int j in tree7.Query(testquery))
            {
                System.Diagnostics.Debug.Write(j + ", ");
            }

            testpoints = new Point[]
            {
                new Point(1.0,0.0),
                new Point(2.0,1.0),
                new Point(3.0,2.0),
                new Point(1.0,2.0),
                new Point(0.0,3.0)
            };

            RedBlackRangeTree2D<int> tree8 = new RedBlackRangeTree2D<int>();
            i = 1;
            foreach (Point p in testpoints)
            {
                tree8.Insert(p.X, p.Y, i++);
            }
            System.Diagnostics.Debug.WriteLine("Found the following 2d points(RedBlackTree):");
            foreach (Point p in testpoints)
            {
                (double _, int j) = tree8.QueryClosest(p, 0.5);
                System.Diagnostics.Debug.Write(j + ", ");
            }

            Rtree<int> tree9 = new Rtree<int>();
            i = 1;
            foreach (Point p in testpoints)
            {
                tree9.Insert(new IntEnvelop(i++, new Rectangle(p, 0.00001)));
            }
            System.Diagnostics.Debug.WriteLine("Found the following 2d points(RTree):");
            foreach (Point p in testpoints)
            {
                (double _, int j) = tree9.QueryClosest(p, 0.5);
                System.Diagnostics.Debug.Write(j + ", ");
            }
        }
       
        private static int FindLeftMost(List<double> list, double keyY)
        {
            int left = 0;
            int right = list.Count - 1;
            while (left < right)
            {
                int mid = (left + right) / 2;
                if (keyY < list[mid])
                    right = mid - 1;
                else
                    left = mid + 1;
            }
            return list[left] < keyY ? left + 1 : left;
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
