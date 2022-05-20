using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

        public int SelectedStartVertex { get; set; }
        public int SelectedEndVertex { get; set; }
        public List<int> EdgesBetween { get; set; }

        public double EndPointTolerance { get; set; }
        public double MidPointTolerance { get; set; }
        public string DirectionColumn { get; set; }
        public string DirectionForwardsValue { get; set; }
        public string DirectionBackwardsValue { get; set; }
        public Direction DefaultDirection { get; set; } = Direction.Both;
        public List<KeyValuePair<string, string>> Weights { get; set;  } = new List<KeyValuePair<string, string>>();

        private IQueryStructure<int> _Equery { get; set; }
        private IQueryStructure<int> _Vquery { get; set; }
        private INetworkGenerator _generator { get; set; }

        public Tuple<double, int> ClosestEdge(Point p, double tolerance) => _Equery == null ? Tuple.Create(double.PositiveInfinity, -1) : _Equery.QueryClosest(p, tolerance);

        public Tuple<double, int> ClosestVertex(Point p, double tolerance) => _Vquery == null ? Tuple.Create(double.PositiveInfinity, -1) : _Vquery.QueryClosest(p, tolerance);

        public Network(string name, INetworkGenerator generator)
        {
            Name = name;
            _generator = generator;
            _Equery = new Rtree<int>();
            _Vquery = new Rtree<int>();
        }

        public Network(string name, INetworkGenerator generator, IQueryStructure<int> edgequerystructure, IQueryStructure<int> vertexquerystructure)
        {
            Name = name;
            _generator = generator;
            _Equery = edgequerystructure;
            _Vquery = vertexquerystructure;
        }

        public void Generate(Map map)
        {
            List<string> extractionColumns = Weights.Select(w => Lexer.ExtractPrimitiveTokens(Lexer.Primitive.Variable, w.Value)).SelectMany(i => i).Select(token => (string)token.Value).ToList();
            (V, E) = _generator.Generate(map.GML.GetPathEnumerator(Rectangle.Infinite(), extractionColumns), EndPointTolerance, MidPointTolerance, Weights, DirectionColumn, DirectionForwardsValue, DirectionBackwardsValue);
            _Equery.Clear();
            foreach (Edge e in E)
                _Equery.Insert(new IntEnvelop(e));
            _Vquery.Clear();
            foreach (Vertex v in V)
                _Vquery.Insert(new IntEnvelop(v));
        }

        public void Generate(IEnumerable<Path> paths)
        {
            List<string> extractionColumns = Weights.Select(w => Lexer.ExtractPrimitiveTokens(Lexer.Primitive.Variable, w.Value)).SelectMany(i => i).Select(token => (string)token.Value).ToList();
            (V, E) = _generator.Generate(paths, EndPointTolerance, MidPointTolerance, Weights, DirectionColumn, DirectionForwardsValue, DirectionBackwardsValue);
            _Equery.Clear();
            foreach (Edge e in E)
                _Equery.Insert(new IntEnvelop(e));
            _Vquery.Clear();
            foreach (Vertex v in V)
                _Vquery.Insert(new IntEnvelop(v));
        }

        internal class DijkstraNode
        {
            public int CurrentVertex, ParentEdge;
            public DijkstraNode(int currentVert, int parentEdge)
            {
                CurrentVertex = currentVert;
                ParentEdge = parentEdge;
            }
        }

        public List<int> FindDijkstraPath(int startvert, int endvert, string weightname)
        {
            if (startvert == endvert)
                return new List<int>();

            Vertex start = V[startvert];
            Vertex end = V[endvert];

            var heap = new DynamicMinHeap<DijkstraNode>(512);

            //Keeps account of the current shortest path to the vertex (key) and which edge (item1) to traverse
            var knownVertices = new Dictionary<int, Tuple<int, double>>();

            //Insert initial vertex with weight 0
            heap.Insert(new DijkstraNode(start.Index, int.MaxValue), 0.0);
            knownVertices.Add(start.Index, Tuple.Create(-1, 0.0));
            while (heap.Count > 0)
            {
                Tuple<DijkstraNode, IComparable> minVertex = heap.ExtractMin();
                if (minVertex.Item1.CurrentVertex == end.Index)
                    break;
                Vertex v = V[minVertex.Item1.CurrentVertex];
                foreach (int e in v.Edges)
                {
                    Edge edge = E[e];
                    double weight = edge.Weights[weightname];

                    double oldweight = knownVertices[minVertex.Item1.CurrentVertex].Item2 + weight;
                    if (knownVertices.ContainsKey(edge.V2))
                    {
                        if (knownVertices[edge.V2].Item2 > oldweight)
                        {
                            knownVertices[edge.V2] = Tuple.Create(e, oldweight);
                            heap.Insert(new DijkstraNode(edge.V2, e), oldweight);
                        }
                    }
                    else
                    {
                        knownVertices.Add(edge.V2, Tuple.Create(e, oldweight));
                        heap.Insert(new DijkstraNode(edge.V2, e), oldweight);
                    }
                }
            }

            List<int> path = new List<int>();
            if (knownVertices.ContainsKey(end.Index))
            {
                int currentVertex = end.Index;
                int parentEdge = knownVertices[currentVertex].Item1;
                path.Add(parentEdge);
                Edge prevEdge = E[parentEdge];
                while (true)
                {
                    currentVertex = prevEdge.V1;
                    parentEdge = knownVertices[currentVertex].Item1;
                    if (parentEdge != -1)
                    {
                        path.Add(parentEdge);
                        prevEdge = E[parentEdge];
                    }
                    else
                        break;
                }
            }

            path.Reverse(); //Go from start to end
            return path.ToList();
        }

        public List<Edge> FindDijkstraPath(Vertex startvert, Vertex endvert, string weightname) => FindDijkstraPath(startvert.Index, endvert.Index, weightname).Select(e => E[e]).ToList();

        public Network Read(BinaryReader br)
        {
            throw new NotImplementedException();
        }

        public Network Write(BinaryWriter bw)
        {
            throw new NotImplementedException();
        }
    }
    public class DynamicMinHeap<T>
    {
        internal class Node<T> : IComparable
        {
            public T Value;
            public IComparable Weight;
            public Node(T value, IComparable weight)
            {
                Value = value;
                Weight = weight;
            }

            public int CompareTo(object obj) => obj is Node<T> other ? this.Weight.CompareTo(other.Weight) : 0;
        }

        private Node<T>[] _heap = null;
        public int Count { get; private set; } = 0;
        public int Capacity => _heap.Length;

        public DynamicMinHeap(int size)
        {
            _heap = new Node<T>[1 << (int)Math.Ceiling(Math.Log(size + 1, 2))];
        }

        public void Insert(T value, IComparable weight)
        {
            //If limit reached, double size
            if (Count + 1 == _heap.Length)
            {
                Node<T>[] tmp = new Node<T>[_heap.Length << 1];
                for (int i = 1; i < _heap.Length; i++)
                    tmp[i] = _heap[i];
                _heap = tmp;
            }

            //Place at end of heap
            int current = Count + 1;
            while (current > 1 && weight.CompareTo(_heap[current >> 1].Weight) < 0)
            {
                _heap[current] = _heap[current >> 1];
                current = current >> 1;
            }
            _heap[current] = new Node<T>(value, weight);
            Count++;
        }

        public Tuple<T, IComparable> ExtractMin()
        {
            if (Count == 0)
                return Tuple.Create(default(T), default(IComparable));

            T min = _heap[1].Value;
            IComparable weight = _heap[1].Weight;
            _heap[1] = _heap[Count];
            int current = 1;
            while (current * 2 <= Count)
            {
                int nextChild;
                if (current * 2 + 1 > Count)
                    nextChild = current * 2;
                else if (_heap[current * 2].CompareTo(_heap[current * 2 + 1]) <= 0)
                    nextChild = current * 2;
                else
                    nextChild = current * 2 + 1;
                if (_heap[current].CompareTo(_heap[nextChild]) > 0)
                {
                    Node<T> tmp = _heap[current];
                    _heap[current] = _heap[nextChild];
                    _heap[nextChild] = tmp;
                }
                current = nextChild;
            }
            Count--;
            return Tuple.Create(min, weight);
        }
    }

}
