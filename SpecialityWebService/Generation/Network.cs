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

    public class Network : IFileItem<Network>, IDisposable
    {
        public const string NetworkFolder = @"Resources\Generated";
        public string NetworkFileName => Name + ".network";
        public string Name { get; set; }
        public string DatasetName { get; set; }
        public string SessionId { get; set; }

        public VertexArray V { get; set; }
        public EdgeArray E { get; set; }

        public int SelectedStartVertex { get; set; }
        public int SelectedEndVertex { get; set; }
        public bool ShouldOverrideStart { get; set; } = true;
        public Dictionary<string, List<int>> EdgesBetween { get; set; }

        public double EndPointTolerance { get; set; } = 1.0;
        public double MidPointTolerance { get; set; } = 1.0;
        public string DirectionColumn { get; set; } = null;
        public string DirectionForwardsValue { get; set; } = null;
        public string DirectionBackwardsValue { get; set; } = null;
        public string ProgressString => _generator != null ? string.Format(@"{0}: Step: {1}/{2} Progress: {3}/{4}", _generator.StepInfo, _generator.CurrentStep, _generator.TotalSteps, _generator.CurrentPath, _generator.TotalPaths) : "No generator associated";


        public Direction DefaultDirection { get; set; } = Direction.Both;
        public List<KeyValuePair<string, string>> Weights { get; set;  } = new List<KeyValuePair<string, string>>();
        public List<string> WeightColumns => Weights.Select(w => Lexer.ExtractPrimitiveTokens(Lexer.Primitive.Variable, w.Value)).SelectMany(i => i).Select(token => (string)token.Value).ToList();
        
        public Generator Generator { get; set; } = Generator.Proposed;

        public long GenerationTime { get; set; } = -1; 

        private IQueryStructure<int> _Equery { get; set; } = null;
        private IQueryStructure<int> _Vquery { get; set; } = null;
        private INetworkGenerator _generator { get; set; } = null;

        public bool HasGenerated = false;

        public string StatusString { 
            get {
                StringBuilder sb = new StringBuilder();

                if (_generator.IsGenerating)
                    sb.AppendLine("Generating:").Append(ProgressString);
                else
                {
                    sb.Append("Generation time: ").Append(GenerationTime).AppendLine("ms");
                    sb.Append("|V|: ").AppendLine(V.Count.ToString());
                    sb.Append("|E|: ").AppendLine(E.Count.ToString());
                }
                return sb.ToString();
            } 
        }

        public Tuple<double, int> ClosestEdge(Point p, double tolerance) => _Equery == null ? Tuple.Create(double.PositiveInfinity, -1) : _Equery.QueryClosest(p, tolerance);

        public Tuple<double, int> ClosestVertex(Point p, double tolerance) => _Vquery == null ? Tuple.Create(double.PositiveInfinity, -1) : _Vquery.QueryClosest(p, tolerance);

        public List<int> QueryVertices(Rectangle query) => _Vquery == null ? new List<int>() : _Vquery.Query(query);

        public List<int> QueryEdges(Rectangle query) => _Equery == null ? new List<int>() : _Equery.Query(query);

        private Network()
        {
        }
        public Network(string name, string datasetname, string sessionid, Generator generator) : this(name, datasetname, sessionid, generator, new Rtree<int>(), new Rtree<int>()){}
        public Network(string name, string datasetname, string sessionid, Generator generator, IQueryStructure<int> edgequerystructure, IQueryStructure<int> vertexquerystructure)
        {
            Name = name;
            DatasetName = datasetname;
            SessionId = sessionid;
            Generator = generator;
            switch (generator)
            {
                case Generator.Proposed:
                    _generator = new ProposedAlgorithm();
                    break;
                case Generator.QGIS:
                    _generator = new QGISReferenceAlgorithm();
                    break;
            };
            InstanciateQueryStructures();
        }

        public void InstanciateQueryStructures()
        {
            V = new VertexArray(System.IO.Path.Combine(NetworkFolder, SessionId, DatasetName), Name);
            E = new EdgeArray(System.IO.Path.Combine(NetworkFolder, SessionId, DatasetName), Name);

            _Equery = new Rtree<int>();
            _Vquery = new Rtree<int>();
            int vcount = V.Count; //Do not execute every check (expensive)
            for (int i = 0; i < vcount; i++)
            {
                Vertex v = V[i];
                if (v != null)
                    _Vquery.Insert(new IntEnvelop(i, v.BoundaryBox));
            }

            int ecount = E.Count; //Do not execute every check (expensive)
            for (int i = 0; i < ecount; i++)
            {
                Edge e = E[i];
                if (e != null)
                    _Equery.Insert(new IntEnvelop(i, e.BoundaryBox));
            }
        }

        public Task Generate(Map map)
        {
            if (string.IsNullOrEmpty(DirectionColumn))
                return Generate(map.GML.GetPathEnumerator(Rectangle.Infinite(), WeightColumns));
            else
                return Generate(map.GML.GetPathEnumerator(Rectangle.Infinite(), WeightColumns.Append(DirectionColumn).ToList()));
        }

        public Task Generate(IEnumerable<Path> paths)
        {
            return _generator.Generate(paths, EndPointTolerance, MidPointTolerance, Weights, DirectionColumn, DirectionForwardsValue, DirectionBackwardsValue).ContinueWith(res =>
            {
                (List<Vertex> V, List<Edge> E) = res.Result;
                //this.V.AddRange(V);
                //this.E.AddRange(E);
                _Equery.Clear();
                foreach (Edge e in E)
                {
                    this.E.Add(e);
                    _Equery.Insert(new IntEnvelop(e));
                }
                _Vquery.Clear();
                foreach (Vertex v in V)
                {
                    this.V.Add(v);
                    _Vquery.Insert(new IntEnvelop(v));
                }
                GenerationTime = _generator.TimeElapsed;
                HasGenerated = true;
                Save();
            });
        }

        public void Save()
        {
            Directory.CreateDirectory(NetworkFolder);
            Directory.CreateDirectory(System.IO.Path.Combine(NetworkFolder, SessionId, DatasetName));
            string filepath = System.IO.Path.Combine(NetworkFolder, SessionId, DatasetName, NetworkFileName);
            if (File.Exists(filepath))
                File.Delete(filepath);
            using (BinaryWriter bw = new BinaryWriter(new FileStream(filepath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None)))
            {
                this.Write(bw);
                bw.Close();
                bw.Dispose();
            }
        }

        public static Network Load(string filepath)
        {
            Network network = new Network();
            if (File.Exists(filepath))
            {
                using (BinaryReader br = new BinaryReader(new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.None)))
                {
                    network.Read(br);
                    network.InstanciateQueryStructures();
                    br.Close();
                    br.Dispose();
                }
            }
            return network;
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

            var heap = new DynamicMinHeap<DijkstraNode>(1024); //Instanciate with some size to not cause too many small dynamic reallocations

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

        public List<Edge> FindDijkstraPath(Vertex startvert, Vertex endvert, string weightname)
        {
            if (startvert == null || endvert == null)
                return new List<Edge>();
            return FindDijkstraPath(startvert.Index, endvert.Index, weightname).Select(e => E[e]).ToList();
        }

        public static Network FromReader(BinaryReader br) => FromReader(br, new Rtree<int>(), new Rtree<int>());
        public static Network FromReader(BinaryReader br, IQueryStructure<int> vquery, IQueryStructure<int> equery)
        {
            Network network = new Network();
            network.Read(br);
            network._Equery = equery;
            network._Vquery = vquery;
            network.InstanciateQueryStructures();
            return network;
        }

        public void Read(BinaryReader br)
        {
            Name = br.ReadString();
            DatasetName = br.ReadString();
            SessionId = br.ReadString();
            EndPointTolerance = br.ReadDouble();
            MidPointTolerance = br.ReadDouble();
            DirectionColumn = br.ReadString();
            DirectionColumn = string.IsNullOrEmpty(DirectionColumn) ? null : DirectionColumn;
            DirectionForwardsValue = br.ReadString();
            DirectionForwardsValue = string.IsNullOrEmpty(DirectionForwardsValue) ? null : DirectionForwardsValue;
            DirectionBackwardsValue = br.ReadString();
            DirectionBackwardsValue = string.IsNullOrEmpty(DirectionBackwardsValue) ? null : DirectionBackwardsValue;
            int b1 = br.ReadInt32();
            DefaultDirection = (Direction)b1;
            int i1 = br.ReadInt32();
            Weights = new List<KeyValuePair<string, string>>();
            int wcount = i1;
            for (int i = 0; i < wcount; i++)
            {
                string label = br.ReadString();
                string formula = br.ReadString();
                Weights.Add(KeyValuePair.Create(label, formula));
            }
            Generator gen = (Generator)br.ReadInt32();
            switch (gen)
            {
                case Generator.Proposed:
                    _generator = new ProposedAlgorithm();
                    break;
                case Generator.QGIS:
                    _generator = new QGISReferenceAlgorithm();
                    break;
            };
            HasGenerated = br.ReadBoolean();
            GenerationTime = br.ReadInt64();
            InstanciateQueryStructures();
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write(Name);
            bw.Write(DatasetName);
            bw.Write(SessionId);
            bw.Write(BitConverter.GetBytes(EndPointTolerance));
            bw.Write(BitConverter.GetBytes(MidPointTolerance));
            bw.Write(DirectionColumn ?? "");
            bw.Write(DirectionForwardsValue ?? "");
            bw.Write(DirectionBackwardsValue ?? "");
            int direct = (int)DefaultDirection;
            bw.Write(BitConverter.GetBytes(direct));
            int wcount = Weights.Count;
            bw.Write(BitConverter.GetBytes(wcount));
            foreach (KeyValuePair<string, string> w in Weights)
            {
                bw.Write(w.Key);
                bw.Write(w.Value);
            }
            Generator g = _generator == null ? Generator.Proposed : (_generator is QGISReferenceAlgorithm ? Generator.QGIS : Generator.Proposed);
            int gen = (int)g;
            bw.Write(BitConverter.GetBytes(gen));
            bw.Write(BitConverter.GetBytes(HasGenerated));
            bw.Write(BitConverter.GetBytes(GenerationTime));
        }

        public void Dispose()
        {
            V.Dispose();
            E.Dispose();
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
