using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static SpecialityWebService.MathObjects;

namespace SpecialityWebService.Generation
{
    public class ProposedAlgorithm : INetworkGenerator
    {

        public int TotalSteps { get; private set; } = 0;
        public int CurrentStep { get; private set; } = 0;
        public string StepInfo { get; private set; } = "";
        public int TotalPaths { get; private set; } = 0;
        public int CurrentPath { get; private set; } = 0;
        public bool IsGenerating { get; private set; } = false;
        public bool Done { get; private set; } = false;
        public long TimeElapsed { get; private set; } = 0;
        public double QueriedAreaSegments { get; private set; } = 0;
        public double QueriedAreaPaths { get; private set; } = 0;

        public IQueryStructure<int> GenerationQueryStructure { get; set; } = new RedBlackRangeTree2D<int>();

        private CancellationTokenSource cts = null;
        private CancellationToken ct;

        public ProposedAlgorithm() { }

        public Task<(List<Vertex>, List<Edge>)> Generate(IEnumerable<Path> paths, double tolerance, List<KeyValuePair<string, string>> weightcalculations, string directioncolumn = null, string forwardsdirection = null, string backwardsdirection = null) => Generate(paths, tolerance, tolerance, weightcalculations, directioncolumn, forwardsdirection);

        public Task<(List<Vertex>, List<Edge>)> Generate(IEnumerable<Path> paths, double endpointtolerance, double midpointtolerance, List<KeyValuePair<string, string>> weightcalculations, string directioncolumn = null, string forwardsdirection = null, string backwardsdirection = null)
        {
            cts?.Cancel();
            cts = new CancellationTokenSource();
            ct = cts.Token;
            Done = false;

            return Task.Run(() => 
            {
                Stopwatch sw = new Stopwatch();
                sw.Restart();
                IsGenerating = true;
                TotalSteps = 2;
                CurrentStep = 1;
                QueriedAreaSegments = 0;
                QueriedAreaPaths = 0;
                TotalPaths = paths.Count();
                StepInfo = "Adding vertices to Range-tree";
                CurrentPath = 1;

                List<Vertex> V = new List<Vertex>();
                int vertexid = 0;
                int edgeid = 0;

                RedBlackRangeTree2D<int> rangetree = new RedBlackRangeTree2D<int>();
                int[] pathlookups = new int[paths.Aggregate(0, (acc, path) => acc + path.Points.Count)];

                int path2vert = 0;
                foreach (Path path in paths)
                {
                    ct.ThrowIfCancellationRequested();
                    int pointcount = 0;
                    foreach (Point p in path.Points)
                    {
                        Vertex v = new Vertex(vertexid, p, new List<int>(), path.Id, path.Fid);
                        v.IsEndpoint = pointcount == 0 || pointcount == path.Points.Count - 1;
                        double toldistance = (v.IsEndpoint ? endpointtolerance : midpointtolerance);

                        (double dist, int ext_p) = rangetree.QueryClosest(p, toldistance);
                        if (double.IsPositiveInfinity(dist))
                        {
                            rangetree.Insert(new IntEnvelop(v));
                            V.Add(v);
                            pathlookups[path2vert] = vertexid;
                            vertexid++;
                        }
                        else
                        {
                            V[ext_p].IsEndpoint |= v.IsEndpoint;
                            pathlookups[path2vert] = ext_p;
                        }
                        path2vert++;
                        pointcount++;
                    }
                    CurrentPath++;
                }

                CurrentStep = 2;
                CurrentPath = 1;
                StepInfo = "Adding edges based on Range-tree vertices";
                List<Edge> E = new List<Edge>();
                path2vert = 0;
                foreach (Path path in paths)
                {
                    ct.ThrowIfCancellationRequested();
                    Vertex pt1 = null, pt2 = null;
                    bool isFirstPoint1 = true;

                    Rectangle pathbounds = Rectangle.FromPoints(path.Points);
                    QueriedAreaPaths += pathbounds.Width * pathbounds.Height;
                    foreach (Point p in path.Points)
                    {
                        //int pt2index = pathlookups[pathcount][pointcount];
                        pt2 = V[pathlookups[path2vert]];
                        //pt2 = V[rangetree.QueryClosest(p, endpointtolerance).Item2];
                        if (!isFirstPoint1 && pathlookups[path2vert] != pathlookups[path2vert - 1]) //Same vertex, do not care
                        {
                            //int pt1index = pathlookups[pathcount][pointcount - 1];
                            List<KeyValuePair<double, Vertex>> orderedvertices = new List<KeyValuePair<double, Vertex>>() { KeyValuePair.Create(0.0, pt1), KeyValuePair.Create(pt2.Location.Distance(pt1.Location), pt2) };

                            Rectangle pt1rect = new Rectangle(pt1.Location, pt1.IsEndpoint ? endpointtolerance : midpointtolerance);
                            Rectangle pt2rect = new Rectangle(pt2.Location, pt2.IsEndpoint ? endpointtolerance : midpointtolerance);
                            Rectangle pt1pt2union = pt1rect.Union(pt2rect);
                            QueriedAreaSegments += pt1pt2union.Width * pt1pt2union.Height;
                            foreach (int vertind in rangetree.Query(pt1pt2union).Where(index => index != pt1.Index && index != pt2.Index))
                            {
                                Vertex mid = V[vertind];
                                if (mid.IsEndpoint) //Only perform segment binding when endpoint
                                {
                                    Point AB = pt2.Location - pt1.Location;
                                    Point BA = pt1.Location - pt2.Location;
                                    Point AP = mid.Location - pt1.Location;
                                    Point BP = mid.Location - pt2.Location;
                                    Point projected = ((AP.X * AB.X + AP.Y * AB.Y) / (AB.X * AB.X + AB.Y * AB.Y)) * AB;
                                    bool beforeA = (AB.X * AP.X + AB.Y * AP.Y) < 0.0; //Vector product, negative if point "behind" direction vector
                                    bool beforeB = (BA.X * BP.X + BA.Y * BP.Y) < 0.0; //Vector product, negative if point "behind" direction vector
                                    projected += pt1.Location;
                                    if (!beforeA && !beforeB && projected.Distance(mid.Location) <= endpointtolerance) 
                                    {
                                        orderedvertices.Add(KeyValuePair.Create(pt1.Location.Distance(projected), mid));
                                    }
                                }
                            }
                            orderedvertices = orderedvertices.OrderBy(item => item.Key).ToList();

                            Vertex v1 = null, v2 = null;
                            bool isFirstPoint2 = true;
                            foreach (KeyValuePair<double, Vertex> v in orderedvertices)
                            {
                                v2 = v.Value;
                                if (!isFirstPoint2)
                                {
                                    List<Point> edgepoints = new List<Point>() { v1.Location, v2.Location };
                                    List<KeyValuePair<string, double>> weights = WeightCalculator.ComputeWeight(new List<Point>() { v1.Location, v2.Location }, path, weightcalculations, path.ColumnValues);
                                    bool forwards = directioncolumn == null || (path.ColumnValues[directioncolumn].Value == forwardsdirection);
                                    bool backwards = directioncolumn == null || (path.ColumnValues[directioncolumn].Value == backwardsdirection);
                                    bool both = !(forwards || backwards);
                                    if (forwards || both) //If neither forwards or backwards, add both
                                    {
                                        Edge e = new Edge(edgeid, v1, v2, Direction.Forward, weights, path.Id, path.Fid, edgepoints);
                                        v1.Edges.Add(e.Index);
                                        E.Add(e);
                                        edgeid++;
                                    }
                                    if (backwards || both)
                                    {
                                        Edge e = new Edge(edgeid, v2, v1, Direction.Backward, weights, path.Id, path.Fid, edgepoints);
                                        v2.Edges.Add(e.Index);
                                        E.Add(e);
                                        edgeid++;
                                    }
                                }
                                v1 = v2;
                                isFirstPoint2 = false;
                            }
                        }
                        pt1 = pt2;
                        isFirstPoint1 = false;
                        path2vert++;
                    }
                    CurrentPath++;
                }
                sw.Stop();
                TimeElapsed = sw.ElapsedMilliseconds;
                Done = true;
                IsGenerating = false;
                return (V, E);
            }, ct).WaitAsync(ct);
        }
        

        public void Cancel()
        {
            cts?.Cancel();
        }
    }
}
