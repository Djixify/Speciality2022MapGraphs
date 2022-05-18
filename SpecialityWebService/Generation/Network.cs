using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static SpecialityWebService.MathObjects;

namespace SpecialityWebService.Generation
{
    public enum Direction
    {
        Forward = 1,
        Backward = 2,
        Both = 3
    }

    public class Network : IFileItem<Network>
    {
        public string Name { get; set; }
        public IFileArray<Vertex> Vfile { get; set; }
        public IFileArray<Edge> Efile { get; set; }
        public List<Vertex> V { get; private set; }
        public List<Edge> E { get; private set; }

        public double EndPointTolerance { get; set; }
        public double MidPointTolerance { get; set; }
        public string DirectionColumn { get; set; }
        public string DirectionForwardsValue { get; set; }
        public string DirectionBackwardsValue { get; set; }
        public Direction DefaultDirection { get; set; } = Direction.Both;
        public List<KeyValuePair<string, string>> Weights { get; set;  } = new List<KeyValuePair<string, string>>();

        private IQueryStructure<int> _Equery { get; set; }
        private INetworkGenerator _generator { get; set; }

        public Tuple<double, int> ClosestEdge(Point p, double tolerance) => _Equery == null ? Tuple.Create(double.PositiveInfinity, -1) : _Equery.QueryClosest(p, tolerance);

        public Network(string name, INetworkGenerator generator)
        {
            Name = name;
            _generator = generator;
            _Equery = new Rtree<int>();
        }

        public Network(string name, INetworkGenerator generator, IQueryStructure<int> vertexquerystructure)
        {
            Name = name;
            _generator = generator;
            _Equery = vertexquerystructure;
        }

        public void Generate(Map map)
        {
            List<string> extractionColumns = Weights.Select(w => Lexer.ExtractPrimitiveTokens(Lexer.Primitive.Variable, w.Value)).SelectMany(i => i).Select(token => (string)token.Value).ToList();
            (V, E) = _generator.Generate(map.GML.GetPathEnumerator(Rectangle.Infinite(), extractionColumns), EndPointTolerance, MidPointTolerance, Weights, DirectionColumn, DirectionForwardsValue, DirectionBackwardsValue);
            _Equery.Clear();
            foreach (Edge e in E)
                _Equery.Insert(new IntEnvelop(e));
        }

        public Network Read(BinaryReader br)
        {
            throw new NotImplementedException();
        }

        public Network Write(BinaryWriter bw)
        {
            throw new NotImplementedException();
        }
    }
}
