using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static SpecialityWebService.MathObjects;

namespace SpecialityWebService.Network
{

    public class Edge : IFileItem<Edge>, ISegment
    {
        public int Index { get; set; } = -1;

        public int V1 { get; set; }
        public int V2 { get; set; }

        public Point P1 { get; set; }
        public Point P2 { get; set; }

        public Direction Direction { get; set; }

        public string Fid { get; set; }
        public int PathId { get; set; }

        public List<Point> RenderPoints { get; set; }

        public Rectangle BoundaryBox { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public Dictionary<string, double> Weights;

        public Edge(int index, Vertex v1, Vertex v2, Direction direction, IEnumerable<KeyValuePair<string, double>> weights, int pathid, string fid, IEnumerable<Point> renderpoints = null)
        {
            Index = index;
            V1 = v1.Index;
            V2 = v2.Index;
            P1 = v1.Location;
            P2 = v2.Location;
            Direction = direction;
            PathId = pathid;
            Fid = fid;
            Weights = new Dictionary<string, double>(weights);
            if (renderpoints != null)
                RenderPoints = new List<Point>(renderpoints);
        }

        public Edge Read(BinaryReader br)
        {
            throw new NotImplementedException();
        }

        public Edge Write(BinaryWriter bw)
        {
            throw new NotImplementedException();
        }
    }
}
