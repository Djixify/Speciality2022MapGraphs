using SpecialityWebService.Network;
using System;
using System.Collections.Generic;
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
            Rtree<Vertex> rtree = new Rtree<Vertex>();
            int vertexid = 0;
            int edgeid = 0;
            foreach (Path path in paths)
            {
                Vertex pt1 = null, pt2 = null;
                foreach (Point p in path.Points)
                {
                    pt2 = new Vertex(vertexid, p, new List<int>(), path.Id, path.Fid);
                    Vertex ext_p = rtree.QueryClosest(p, tolerance).Item;
                    if (ext_p == null)
                    {
                        rtree.Insert(pt2);
                        vertexid++;
                    }
                    else
                        pt2 = ext_p;
                    pt1 = pt2;
                }
            }
            //Skip insertion of tiepoints as I dont support it
            List<Edge> E = new List<Edge>();
            foreach (Path path in paths)
            {
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
                                List<KeyValuePair<string, double>> weights = WeightCalculator.ComputeWeight(orderedVertices.Select(v => v.Item2.Location), path, weightformulas);
                                //Forwards: 01, Backwards: 10, Both: 11, hence checks both forwards and both below
                                if ((directionconvert[path.ColumnValues[directioncolumn]] & Direction.Forward) == Direction.Forward)
                                {
                                    Edge e = new Edge(edgeid, v1, v2, Direction.Forward, weights, path.Id, path.Fid, orderedVertices.Select(v => v.Item2.Location));
                                    v1.Edges.Add(e.Index);
                                    E.Add(e);
                                }
                                if ((directionconvert[path.ColumnValues[directioncolumn]] & Direction.Forward) == Direction.Backward)
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
            }
            return new Network(rtree.QueryAll().Select(v => v.Item), E);
        }
    }
}
