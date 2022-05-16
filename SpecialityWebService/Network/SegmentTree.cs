using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpecialityWebService.Network
{
    public class SegmentTree<T> : IQueryStructure<T>
    {
        private List<ISegment> _items;
        public MathObjects.Rectangle BoundaryBox { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public SegmentTree() { _items = new List<ISegment>(); }

        public void Insert(IQueryItem<T> item)
        {

        }

        public List<IQueryItem<T>> Query(MathObjects.Rectangle rect)
        {
            throw new NotImplementedException();
        }

        public List<IQueryItem<T>> Query(MathObjects.Point p, double tolerance)
        {
            throw new NotImplementedException();
        }

        public IQueryItem<T> QueryClosest(MathObjects.Point p, double tolerance)
        {
            throw new NotImplementedException();
        }

        public List<IQueryItem<T>> QueryAll()
        {
            throw new NotImplementedException();
        }
    }
}
