using RBush;
using SpecialityWebService.Generation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static SpecialityWebService.MathObjects;

namespace SpecialityWebService.Generation
{
    public class QGISReferenceAlgorithm : INetworkGenerator
    {
        public QGISReferenceAlgorithm() { }


        public int TotalSteps { get; private set; } = 0;
        public int CurrentStep { get; private set; } = 0;
        public string StepInfo { get; private set; } = "";
        public int TotalPaths { get; private set; } = 0;
        public int CurrentPath { get; private set; } = 0;
        public bool IsGenerating { get; private set; } = false;
        public bool Done { get; private set; } = false;
        public long TimeElapsed { get; private set; } = 0;

        private CancellationTokenSource cts = null;
        private CancellationToken ct;

        public Task<(List<Vertex>, List<Edge>)> Generate(IEnumerable<Path> paths, double tolerance, List<KeyValuePair<string, string>> weightcalculations, string directioncolumn, string forwardsdirection, string backwardsdirection) => Generate(paths, tolerance, tolerance, weightcalculations, directioncolumn, forwardsdirection, backwardsdirection);
        public Task<(List<Vertex>, List<Edge>)> Generate(IEnumerable<Path> paths, double endpointtolerance, double midpointtolerance, List<KeyValuePair<string, string>> weightcalculations, string directioncolumn, string forwardsdirection, string backwardsdirection)
        {
            cts?.Cancel();
            cts = new CancellationTokenSource();
            ct = cts.Token;
            Done = false;

            return Task.Run(() =>
            {
                try
                {
                    Stopwatch sw = new Stopwatch();
                    sw.Restart();
                    IsGenerating = true;
                    TotalSteps = 2;
                    CurrentStep = 1;
                    TotalPaths = paths.Count();
                    StepInfo = "Adding vertices to R-tree";
                    CurrentPath = 1;

                    Rtree<int> rtree = new Rtree<int>();
                    List<Vertex> V = new List<Vertex>();
                    int vertexid = 0;
                    int edgeid = 0;
                    foreach (Path path in paths)
                    {
                        Vertex pt1 = null, pt2 = null;
                        foreach (Point p in path.Points)
                        {
                            pt2 = new Vertex(vertexid, p, new List<int>(), path.Id, path.Fid);
                            pt2.IsEndpoint = p == path.Points.First() || p == path.Points.Last();
                            (double dist, int ext_p) = rtree.QueryClosest(p, endpointtolerance);
                            if (double.IsPositiveInfinity(dist))
                            {
                                rtree.Insert(new IntEnvelop(pt2));
                                V.Add(pt2);
                                vertexid++;
                            }
                            else
                            {
                                V[ext_p].IsEndpoint |= pt2.IsEndpoint;
                                pt2 = V[ext_p];
                            }
                            pt1 = pt2;
                        }
                        CurrentPath++;
                    }
                    CurrentStep = 2;
                    CurrentPath = 1;
                    StepInfo = "Adding edges based on R-tree vertices";
                    //Skip insertion of tiepoints as I dont support it
                    List<Edge> E = new List<Edge>();
                    foreach (Path path in paths)
                    {
                        Vertex pt1 = null, pt2 = null;
                        bool isFirstPoint1 = true;
                        foreach (Point p in path.Points)
                        {
                            //Assume a vertex now exists at the location
                            pt2 = V[rtree.QueryClosest(p, endpointtolerance).Item2];

                            if (!isFirstPoint1)
                            {
                                List<Tuple<double, Vertex>> orderedVertices = new List<Tuple<double, Vertex>>() { Tuple.Create(0.0, pt1), Tuple.Create(pt1.Location.Distance(pt2.Location), pt2) };
                                //No tie points so above is already ordered
                                Vertex v1 = null, v2 = null;
                                bool isFirstPoint2 = true;
                                foreach (Vertex v in orderedVertices.Select(tup => tup.Item2))
                                {
                                    v2 = v;
                                    if (!isFirstPoint2 && v2.Index != v1.Index)
                                    {
                                        List<KeyValuePair<string, double>> weights = WeightCalculator.ComputeWeight(orderedVertices.Select(v => v.Item2.Location), path, weightcalculations, path.ColumnValues);
                                        bool forwards = directioncolumn == null || (path.ColumnValues[directioncolumn].Value == forwardsdirection);
                                        bool backwards = directioncolumn == null || (path.ColumnValues[directioncolumn].Value == backwardsdirection);
                                        bool both = !(forwards || backwards);
                                        if (forwards || both) //If neither forwards or backwards, add both
                                        {
                                            Edge e = new Edge(edgeid, v1, v2, Direction.Forward, weights, path.Id, path.Fid, orderedVertices.Select(v => v.Item2.Location));
                                            v1.Edges.Add(e.Index);
                                            E.Add(e);
                                            edgeid++;
                                        }
                                        if (backwards || both)
                                        {
                                            Edge e = new Edge(edgeid, v2, v1, Direction.Backward, weights, path.Id, path.Fid, orderedVertices.Select(v => v.Item2.Location));
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
                        }
                        CurrentPath++;
                    }
                    sw.Stop();
                    TimeElapsed = sw.ElapsedMilliseconds;
                    Done = true;
                    IsGenerating = false;
                    return (V, E);
                }
                catch(Exception ex)
                {
                    return (new List<Vertex>(), new List<Edge>());
                }
            }, ct).WaitAsync(ct);
        }

        public void Cancel()
        {
            cts?.Cancel();
        }
    }
}
