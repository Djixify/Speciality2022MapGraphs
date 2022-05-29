using RBush;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SpecialityWebService.MathObjects;

namespace SpecialityWebService.Generation
{
    public class Vertex : IQueryItem<Vertex>, ISpatialData
    {
        public int Index { get; set; } = -1;
        public bool IsEndpoint { get; set; } = false;

        private const double _boundsradius = 0.00000001;
        private Point _location;
        public Point Location 
        { 
            get { return _location; } 
            set 
            { 
                _location = value;
                _envelope = new Envelope(_location.X - _boundsradius, _location.Y - _boundsradius, _location.X + _boundsradius, _location.Y + _boundsradius);
            } 
        }
        public List<int> Edges { get; set; }
        public string Fid { get; set; }
        public int PathId { get; set; }

        private Envelope _envelope;
        public ref readonly Envelope Envelope => ref _envelope;
        public Rectangle BoundaryBox { get { return _envelope; } set { } }

        public Vertex Item { get { return this; } }

        private Vertex() { }

        public Vertex(int index, Point location, IEnumerable<int> edges, int pathid, string fid = null)
        {
            Index = index;
            Location = location;
            Edges = new List<int>(edges);
            PathId = pathid;
            Fid = fid;
        }

        public static Vertex FromReader(BinaryReader br)
        {
            Vertex v = new Vertex();
            v.Read(br);
            return v;
        }
        public void Read(BinaryReader br)
        {
            Index = br.ReadInt32();
            IsEndpoint = br.ReadInt32() == 1 ? true : false;
            Point tmp = new Point();
            tmp.Read(br);
            Location = tmp;
            Edges = new List<int>();
            int edgecount = br.ReadInt32();
            for (int i = 0; i < edgecount; i++)
                Edges.Add(br.ReadInt32());
            Fid = br.ReadString();
            PathId = br.ReadInt32();
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write(BitConverter.GetBytes(Index));
            int endpoint = IsEndpoint ? 1 : 0;
            bw.Write(BitConverter.GetBytes(endpoint));
            Location.Write(bw);
            int edgecount = Edges.Count;
            bw.Write(BitConverter.GetBytes(edgecount));
            foreach (int i in Edges)
                bw.Write(BitConverter.GetBytes(i));
            bw.Write(Fid ?? "");
            bw.Write(BitConverter.GetBytes(PathId));
        }
    }

    public class VertexArray : IFileArray<Vertex>, IDisposable
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

        public VertexArray(string path, string filename, bool overrideexisting = false)
        {
            _path = path;
            _filename = filename;

            IndexFile = System.IO.Path.Combine(_path, _filename) + ".vertexids";
            DataFile = System.IO.Path.Combine(_path, _filename) + ".vertexdat";

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

        public int Add(Vertex item)
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

        public void AddRange(List<Vertex> items)
        {
            lock (IndexFileStream)
            {
                lock (DataFileStream)
                {
                    DataFileWriter.BaseStream.Seek(0, SeekOrigin.End);
                    IndexFileWriter.BaseStream.Seek(0, SeekOrigin.End);
                    foreach (Vertex item in items)
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

        public Vertex this[int i]
        {
            get
            {
                Vertex v = null;
                int j = i;

                //Read from index file till next valid entry found
                bool valid = false;
                long offset = 0;
                try
                {
                    lock(IndexFileStream)
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
                                    v = Vertex.FromReader(DataFileReader);
                                }
                                catch (ArgumentOutOfRangeException ex)
                                {
                                    System.Diagnostics.Debug.WriteLine("Invalid offset provided: " + offset + " total length: " + DataFileReader.BaseStream.Length + " Repeated fails: " + (j-i));
                                }
                            }
                        }
                    }
                } 
                catch (EndOfStreamException ex)
                {
                    System.Diagnostics.Debug.WriteLine("Attempt to read index " + i + " of V, but was beyond end of stream: " + this.Count);
                }
                /*
                IndexFileReader.Close();
                DataFileReader.Close();
                IndexFileReader.Dispose();
                DataFileReader.Dispose();
                */
                return v;
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
            lock (DataFileStream) 
            {
                lock (IndexFileStream)
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
                            Vertex v = Vertex.FromReader(DataFileReader);

                            offset = bwdat.BaseStream.Position;

                            bwids.Write(1);
                            bwids.Write(offset);
                            v.Write(bwdat);
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

            IndexFileReader?.Dispose();
            DataFileReader?.Dispose();
            IndexFileWriter?.Dispose();
            DataFileWriter?.Dispose();
            IndexFileStream?.Dispose();
            DataFileStream?.Dispose();
        }
    }
}
