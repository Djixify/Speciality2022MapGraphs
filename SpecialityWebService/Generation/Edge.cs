using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static SpecialityWebService.MathObjects;

namespace SpecialityWebService.Generation
{

    public class Edge : IFileItem<Edge>
    {
        public int Index { get; set; } = -1;

        public int V1 { get; set; }
        public int V2 { get; set; }

        public Direction Direction { get; set; }

        public string Fid { get; set; }
        public int PathId { get; set; }

        public List<Point> RenderPoints { get; set; }

        public Rectangle BoundaryBox { get; set; }

        public Dictionary<string, double> Weights;

        private Edge() { }
        public Edge(int index, Vertex v1, Vertex v2, Direction direction, IEnumerable<KeyValuePair<string, double>> weights, int pathid, string fid, IEnumerable<Point> renderpoints)
        {
            Index = index;
            V1 = v1.Index;
            V2 = v2.Index;
            Direction = direction;
            PathId = pathid;
            Fid = fid;
            Weights = new Dictionary<string, double>(weights);
            if (renderpoints != null && renderpoints.Count() >= 2 && (direction == Direction.Forward ? renderpoints.First() == v1.Location && renderpoints.Last() == v2.Location : renderpoints.Last() == v1.Location && renderpoints.First() == v2.Location))
            {
                RenderPoints = new List<Point>(renderpoints);
                BoundaryBox = Rectangle.FromPoints(RenderPoints);
            }
            else
                throw new ArgumentException("Invalid render points added to edge");
        }

        public static Edge FromReader(BinaryReader br)
        {
            Edge e = new Edge();
            e.Read(br);
            return e;
        }

        public void Read(BinaryReader br)
        {
            Index = br.ReadInt32();
            V1 = br.ReadInt32();
            V2 = br.ReadInt32();
            int direct = br.ReadInt32();
            Direction = (Direction)direct;
            Fid = br.ReadString();
            PathId = br.ReadInt32();
            RenderPoints = new List<Point>();
            int pointcount = br.ReadInt32();
            for (int i = 0; i < pointcount; i++)
                RenderPoints.Add(Point.FromReader(br));
            Weights = new Dictionary<string, double>();
            int weightcount = br.ReadInt32();
            for (int i = 0; i < weightcount; i++)
            {
                string label = br.ReadString();
                double weight = br.ReadDouble();
                Weights.Add(label, weight);
            }
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write(BitConverter.GetBytes(Index));
            bw.Write(BitConverter.GetBytes(V1));
            bw.Write(BitConverter.GetBytes(V2));
            int direct = (int)Direction;
            bw.Write(BitConverter.GetBytes(direct));
            bw.Write(Fid ?? "");
            bw.Write(BitConverter.GetBytes(PathId));
            int pointcount = RenderPoints.Count;
            bw.Write(BitConverter.GetBytes(pointcount));
            foreach (Point p in RenderPoints)
                p.Write(bw);

            int weightcount = Weights.Count;
            bw.Write(BitConverter.GetBytes(weightcount));
            foreach (KeyValuePair<string, double> weight in Weights)
            {
                bw.Write(weight.Key);
                bw.Write(weight.Value);
            }
        }
    }

    public class EdgeArray : IFileArray<Edge>, IDisposable
    {
        private string _filename { get; set; }
        private string _path { get; set; }
        private int _indexItemSize = 12;

        public string IndexFile { get; private set; }
        public string DataFile { get; private set; }

        private FileStream IndexFileStream, DataFileStream;
        private BinaryWriter IndexFileWriter, DataFileWriter;
        private BinaryReader IndexFileReader, DataFileReader;

        public int Count => (int)(new FileInfo(IndexFile).Length / _indexItemSize);

        public EdgeArray(string path, string filename)
        {
            _path = path;
            _filename = filename;

            IndexFile = System.IO.Path.Combine(_path, _filename) + ".edgeids";
            DataFile = System.IO.Path.Combine(_path, _filename) + ".edgedat";

            Directory.CreateDirectory(_path);
            if (File.Exists(IndexFile))
                File.Delete(IndexFile);
            if (File.Exists(DataFile))
                File.Delete(DataFile);

            IndexFileStream = new FileStream(IndexFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            DataFileStream = new FileStream(DataFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            IndexFileWriter = new BinaryWriter(IndexFileStream);
            DataFileWriter = new BinaryWriter(DataFileStream);
            IndexFileReader = new BinaryReader(IndexFileStream);
            DataFileReader = new BinaryReader(DataFileStream);
        }

        public int Add(Edge item)
        {
            int indexoffset = -1;
            lock (IndexFileStream)
            {
                lock (DataFileStream)
                {
                    DataFileWriter.BaseStream.Seek(0, SeekOrigin.End);
                    long offset = DataFileWriter.BaseStream.Position;
                    IndexFileWriter.BaseStream.Seek(0, SeekOrigin.End);
                    indexoffset = (int)(IndexFileWriter.BaseStream.Position / _indexItemSize);

                    //Write to index file
                    IndexFileWriter.Write(1);
                    IndexFileWriter.Write(offset);
                    //Write to data file
                    item.Write(DataFileWriter);
                }
            }

            /*
            IndexFileWriter.Close();
            DataFileWriter.Close();
            IndexFileWriter.Dispose();
            DataFileWriter.Dispose();
            */
            
            return indexoffset;
        }

        public void AddRange(List<Edge> items)
        {
            lock (IndexFileStream)
            {
                lock (DataFileStream)
                {
                    DataFileWriter.BaseStream.Seek(0, SeekOrigin.End);
                    IndexFileWriter.BaseStream.Seek(0, SeekOrigin.End);
                    foreach (Edge item in items)
                    {
                        long offset = DataFileWriter.BaseStream.Position;
                        //Write to index file
                        IndexFileWriter.Write(1);
                        IndexFileWriter.Write(offset);
                        //Write to data file
                        item.Write(DataFileWriter);
                    }
                }
            }
            /*
            IndexFileWriter.Close();
            DataFileWriter.Close();
            IndexFileWriter.Dispose();
            DataFileWriter.Dispose();
            */
        }

        public Edge this[int i]
        {
            get
            {
                Edge e = null;
                int j = i;

                //Read from index file till next valid entry found
                bool valid = false;
                long offset = 0;
                try
                {
                    lock (IndexFileStream)
                    {

                        do
                        {
                            IndexFileReader.BaseStream.Seek(_indexItemSize * j, SeekOrigin.Begin);
                            valid = IndexFileReader.ReadInt32() == 1 ? true : false;
                            offset = IndexFileReader.ReadInt64();
                            j++;
                        } while (IndexFileReader.BaseStream.Position < IndexFileReader.BaseStream.Length && !valid);
                        if (IndexFileReader.BaseStream.Position <= IndexFileReader.BaseStream.Length)
                        {
                            lock(DataFileStream)
                            {
                                DataFileReader.BaseStream.Seek(offset, SeekOrigin.Begin);
                                //Read from data file
                                try
                                {
                                    e = Edge.FromReader(DataFileReader);
                                }
                                catch (EndOfStreamException ex)
                                {
                                    System.Diagnostics.Debug.WriteLine("Invalid offset provided: " + offset + " total length: " + DataFileReader.BaseStream.Length + " Repeated fails: " + (j - i));
                                }
                            }
                        }
                    }
                }
                catch (EndOfStreamException ex)
                {
                    System.Diagnostics.Debug.WriteLine("Attempt to read index " + i + " of E, but was beyond end of stream: " + this.Count);
                }
                /*
                IndexFileReader.Close();
                DataFileReader.Close();
                IndexFileReader.Dispose();
                DataFileReader.Dispose();
                */
                return e;
            }
        }

        public void Remove(int i, bool clean)
        {
            lock (IndexFileStream)
            {
                IndexFileWriter.BaseStream.Seek(_indexItemSize * i, SeekOrigin.Begin);
                //Write to index file
                IndexFileWriter.Write(0);
            }
            /*
            IndexFileWriter.Close();
            IndexFileWriter.Dispose();
            */
            if (clean)
                Clean();
        }

        private void Clean()
        {
            lock (IndexFileStream)
            {
                lock (DataFileStream)
                {
                    IndexFileReader.BaseStream.Seek(0, SeekOrigin.Begin);
                    using (BinaryWriter bwdat = new BinaryWriter(new FileStream(DataFile + "tmp", FileMode.OpenOrCreate, FileAccess.Write, FileShare.None)))
                    using (BinaryWriter bwids = new BinaryWriter(new FileStream(IndexFile + "tmp", FileMode.OpenOrCreate, FileAccess.Write, FileShare.None)))
                    {
                        while (IndexFileReader.BaseStream.Position < IndexFileReader.BaseStream.Length)
                        {
                            bool valid = IndexFileReader.ReadBoolean();
                            long offset = IndexFileReader.ReadInt64();
                            if (!valid)
                                continue;

                            DataFileReader.BaseStream.Seek(offset, SeekOrigin.Begin);
                            Edge e = Edge.FromReader(DataFileReader);

                            offset = bwdat.BaseStream.Position;

                            bwids.Write(1);
                            bwids.Write(offset);
                            e.Write(bwdat);
                        }
                        bwids.Close();
                        bwdat.Close();
                        bwids.Dispose();
                        bwdat.Dispose();
                    }
                    IndexFileStream.Close();
                    DataFileStream.Close();
                    IndexFileStream.Dispose();
                    DataFileStream.Dispose();
                    File.Move(DataFile + "tmp", DataFile, true);
                    File.Move(IndexFile + "tmp", IndexFile, true);
                    IndexFileStream = new FileStream(IndexFile, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                    DataFileStream = new FileStream(DataFile, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                }
            }
        }
        public void Dispose()
        {
            IndexFileReader.Close();
            DataFileReader.Close();
            IndexFileWriter.Close();
            DataFileWriter.Close();
            IndexFileStream.Close();
            DataFileStream.Close();

            IndexFileStream.Dispose();
            DataFileStream.Dispose();
        }
    }
}
