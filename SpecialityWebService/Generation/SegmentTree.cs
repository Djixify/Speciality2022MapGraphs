using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static SpecialityWebService.MathObjects;

namespace SpecialityWebService.Generation
{
    public class SegmentTree<T> : IQueryStructure<T> where T : IQueryItem<T>
    {
        private List<T> _items;
        public int MaxDepth;
        public double MinimumWidth;

        public MathObjects.Rectangle BoundaryBox { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public SegmentTree() { _items = new List<T>(); }

        public List<T> Query(Rectangle rect)
        {
            throw new NotImplementedException();
        }

        public List<T> Query(Point p, double tolerance)
        {
            throw new NotImplementedException();
        }

        public Tuple<double, T> QueryClosest(Point p, double tolerance)
        {
            throw new NotImplementedException();
        }

        public List<T> QueryAll()
        {
            throw new NotImplementedException();
        }

        public void Insert(IQueryItem<T> item)
        {
            throw new NotImplementedException();
        }

        public void InsertAll(IEnumerable<IQueryItem<T>> items)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public T Read(BinaryReader br)
        {
            throw new NotImplementedException();
        }

        public T Write(BinaryWriter bw)
        {
            throw new NotImplementedException();
        }
    }

    public struct Region
    {
        public double Left, Right, Width, Mid;
        public Region(double left, double right)
        {
            Left = left;
            Right = right;
            Width = right - left;
            Mid = left + Width / 2.0;
        }

        public bool Intersects(Region other) => Intersects(other.Left, other.Right);
        public bool Intersects(double left, double right) => !(this.Right < left || this.Left > right);
        public bool IntersectsBiased(Region other) => IntersectsBiased(other.Left, other.Right);
        public bool IntersectsBiased(double left, double right) => !(this.Right < left || this.Left >= right);
        public bool Subset(Region other) => Subset(other.Left, other.Right);
        public bool Subset(double left, double right) => !(this.Left < left || this.Right > right);
        public bool SubsetBiased(Region other) => SubsetBiased(other.Left, other.Right);
        public bool SubsetBiased(double left, double right) => !(this.Left < left || this.Right > right);
    }

    public class SegmentTreeNode<T> where T : IQueryItem<T>
    {
        Region Interval;
        SegmentTreeNode<T> LeftChild, RightChild;
        int Depth;
        SegmentTree<T> ParentTree = null;
        List<int> Elements;
        public SegmentTreeNode(in SegmentTree<T> parentTree, Region region, int depth)
        {
            this.ParentTree = parentTree;
            this.Interval = region;
            this.Depth = depth;
            this.Elements = new List<int>();
        }

        public void Insert(Region region, int index)
        {
            Region tmp;
            if (Interval.Subset(region))
                Elements.Add(index);
            else
            {
                if ((tmp = LeftChild != null ? LeftChild.Interval : new Region(Interval.Left, Interval.Mid)).Intersects(region))
                    (LeftChild == null ? (LeftChild = new SegmentTreeNode<T>(ParentTree, tmp, Depth + 1)) : LeftChild).Insert(region, index);
                if ((tmp = RightChild != null ? RightChild.Interval : new Region(Interval.Mid, Interval.Right)).Intersects(region))
                    (RightChild == null ? (RightChild = new SegmentTreeNode<T>(ParentTree, tmp, Depth + 1)) : RightChild).Insert(region, index);
            }
        }

        public IEnumerable<int> Search(Region region)
        {
            bool intersectsregion = Interval.Intersects(region);
            if (!intersectsregion)
                return Enumerable.Empty<int>();
            return LeftChild.Search(region).Concat(RightChild.Search(region)).Concat(Elements);
        }
    }
}
