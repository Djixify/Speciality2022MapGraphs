using RBush;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static SpecialityWebService.MathObjects;

namespace SpecialityWebService.Generation
{
    public interface IQueryStructure<T> : IBound
    {
        public List<T> Query(Rectangle rect);
        public List<T> Query(Point p, double tolerance);
        public Tuple<double, T> QueryClosest(Point p, double tolerance);
        public List<T> QueryAll();
        public void Insert(IQueryItem<T> item);
        public void InsertAll(IEnumerable<IQueryItem<T>> items);
        public void Clear();
    }


    public class IntEnvelop : IQueryItem<int>
    {
        public int Item { get; set; } = -1;

        private Rectangle _boundaryBox;
        public Rectangle BoundaryBox
        {
            get { return _boundaryBox; }
            set
            {
                _boundaryBox = value;
                _envelope = value;
            }
        }

        private Envelope _envelope;
        public ref readonly Envelope Envelope => ref _envelope;

        private IntEnvelop() { }
        public IntEnvelop(int item, Rectangle boundaryBox)
        {
            Item = item;
            BoundaryBox = boundaryBox;
        }

        public IntEnvelop(Vertex v)
        {
            Item = v.Index;
            BoundaryBox = v.BoundaryBox;
        }
        public IntEnvelop(Edge e)
        {
            Item = e.Index;
            BoundaryBox = e.BoundaryBox;
        }

        public static IntEnvelop FromReader(BinaryReader br)
        {
            IntEnvelop ie = new IntEnvelop();
            ie.Read(br);
            return ie;
        }

        public void Read(BinaryReader br)
        {
            Item = br.ReadInt32();
            BoundaryBox = Rectangle.FromReader(br);
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write(BitConverter.GetBytes(Item));
            BoundaryBox.Write(bw);
        }
    }
}
