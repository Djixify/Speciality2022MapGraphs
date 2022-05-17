using SpecialityWebService.Network;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using static SpecialityWebService.MathObjects;

namespace SpecialityWebService.Network
{
    public class QGISReferenceAlgorithm : INetworkGenerator
    {
        public QGISReferenceAlgorithm() { }

        public Network Generate(IEnumerable<Path> paths, double tolerance, string directioncolumn, Dictionary<string, Direction> directionconvert, List<string> weightformulas)
        {
            int total = paths.Count();
            int count = 1;
            Rtree<Vertex> rtree = new Rtree<Vertex>();
            int vertexid = 0;
            int edgeid = 0;
            foreach (Path path in paths)
            {
                System.Diagnostics.Debug.WriteLine($"1/2: Inserting into R-tree: {Math.Round((double)count / (double)total * 100.0, 1)}%");
                Vertex pt1 = null, pt2 = null;
                foreach (Point p in path.Points)
                {
                    pt2 = new Vertex(vertexid, p, new List<int>(), path.Id, path.Fid);
                    Vertex ext_p = rtree.QueryClosest(p, tolerance)?.Item;
                    if (ext_p == null)
                    {
                        rtree.Insert(pt2);
                        vertexid++;
                    }
                    else
                        pt2 = ext_p;
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
                    pt2 = rtree.QueryClosest(p, tolerance).Item;

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
                                List<KeyValuePair<string, double>> weights = WeightCalculator.ComputeWeight(orderedVertices.Select(v => v.Item2.Location), path, weightformulas, path.ColumnValues);
                                weights.Add(new KeyValuePair<string, double>("distance", v1.Location.Distance(v2.Location)));
                                //Forwards: 01, Backwards: 10, Both: 11, hence checks both forwards and both below
                                if (directioncolumn == null || !directionconvert.ContainsKey(path.ColumnValues[directioncolumn]) || (directionconvert[path.ColumnValues[directioncolumn]] & Direction.Forward) == Direction.Forward)
                                {
                                    Edge e = new Edge(edgeid, v1, v2, Direction.Forward, weights, path.Id, path.Fid, orderedVertices.Select(v => v.Item2.Location));
                                    v1.Edges.Add(e.Index);
                                    E.Add(e);
                                }
                                if (directioncolumn == null || !directionconvert.ContainsKey(path.ColumnValues[directioncolumn]) || (directionconvert[path.ColumnValues[directioncolumn]] & Direction.Backward) == Direction.Backward)
                                {
                                    Edge e = new Edge(edgeid, v2, v1, Direction.Forward, weights, path.Id, path.Fid, orderedVertices.Select(v => v.Item2.Location));
                                    v2.Edges.Add(e.Index);
                                    E.Add(e);
                                }
                                edgeid++;
                            }
                            v1 = v2;
                        }
                    }
                    pt1 = pt2;
                    isFirstPoint1 = false;
                }
                count++;
            }
            return new Network(rtree.QueryAll().Select(v => v.Item), E);
        }
    }
}
