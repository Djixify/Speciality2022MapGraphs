using RBush;
using SpecialityWebService.Generation;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using static SpecialityWebService.MathObjects;

namespace SpecialityWebService.Generation
{
    public class QGISReferenceAlgorithm : INetworkGenerator
    {
        public QGISReferenceAlgorithm() { }

        public Tuple<List<Vertex>, List<Edge>> Generate(IEnumerable<Path> paths, double tolerance, List<KeyValuePair<string, string>> weightcalculations, string directioncolumn, string forwardsdirection, string backwardsdirection) => Generate(paths, tolerance, tolerance, weightcalculations, directioncolumn, forwardsdirection, backwardsdirection);
        public Tuple<List<Vertex>, List<Edge>> Generate(IEnumerable<Path> paths, double endpointtolerance, double midpointtolerance, List<KeyValuePair<string, string>> weightcalculations, string directioncolumn, string forwardsdirection, string backwardsdirection)
        {
            int total = paths.Count();
            int count = 1;
            Rtree<int> rtree = new Rtree<int>();
            List<Vertex> V = new List<Vertex>();
            int vertexid = 0;
            int edgeid = 0;
            foreach (Path path in paths)
            {
                System.Diagnostics.Debug.WriteLine($"1/2: Inserting into R-tree: {Math.Round((double)count / (double)total * 100.0, 1)}%");
                Vertex pt1 = null, pt2 = null;
                foreach (Point p in path.Points)
                {
                    pt2 = new Vertex(vertexid, p, new List<int>(), path.Id, path.Fid);
                    pt2.IsEndpoint = p == path.Points.First() || p == path.Points.Last();
                    (double _, int ext_p) = rtree.QueryClosest(p, endpointtolerance);
                    if (ext_p == -1)
                    {
                        rtree.Insert(new IntEnvelop(pt2));
                        V.Add(pt2);
                        vertexid++;
                    }
                    else
                        pt2 = V[ext_p];
                    pt1 = pt2;
                }
                count++;
            }
            total = paths.Count();
            count = 1;
            //Skip insertion of tiepoints as I dont support it
            List<Edge> E = new List<Edge>();
            foreach (Path path in paths)
            {
                System.Diagnostics.Debug.WriteLine($"2/2: Adding edges based on R-tree: {Math.Round((double)count / (double)total * 100.0, 1)}%");
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
                            if (!isFirstPoint2)
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
                        }
                    }
                    pt1 = pt2;
                    isFirstPoint1 = false;
                }
                count++;
            }
            return new Tuple<List<Vertex>, List<Edge>>(V, E);
        }
    }
}
