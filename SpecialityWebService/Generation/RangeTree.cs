using RBush;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static SpecialityWebService.MathObjects;

namespace SpecialityWebService.Generation
{

    public class RangeTree<T> : IQueryStructure<T> where T : IBound
    {
        public List<T> Items;
        private RangeTreeNode<T> _root;

        public MathObjects.Rectangle BoundaryBox { get; set; }

        public RangeTree(List<T> items) 
        { 
            Items = items;
            List<int> Xsorteditems = items.Select((item, i) => (item, i)).OrderBy(x => x.item.BoundaryBox.GetCenter().X).Select(x => x.i).ToList();
            List<int> Ysorteditems = items.Select((item, i) => (item, i)).OrderBy(x => x.item.BoundaryBox.GetCenter().Y).Select(x => x.i).ToList();
            List<int> X2Ysorteditems = Xsorteditems.Zip(Ysorteditems).OrderBy(x => items[x.Second].BoundaryBox.GetCenter().Y).Select(x => x.First).ToList();

            //_root = new RangeTreeNode<T>(Items, Xsorteditems, Ysorteditems, X2Ysorteditems, 0, Xsorteditems.Count - 1);
        }

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

    public class RangeTreeNode<T>
    {
        bool IsLeaf => Left == Right;
        int Left = -2, Right = -1;
        T Item = default(T);
        RangeTreeNode<T> LeftChild = null, RightChild = null;
        List<int> AssociatedStructure = null;
        public RangeTreeNode(in List<T> Items, in List<int> Xsorted, in List<int> Ysorted, in List<int> X2Ysorted, int left, int right)
        {
            this.Left = left;
            this.Right = right;

            if (left > right)
                throw new ArgumentException("Invalid argument given, left cannot be greater than right");
            else if (left == right)
                Item = Items[Xsorted[left]];
            else if (left < right)
            {
                int mid = (right - left) / 2;
                LeftChild = new RangeTreeNode<T>(Items, Xsorted, Ysorted, X2Ysorted, left, mid);
                RightChild = new RangeTreeNode<T>(Items, Xsorted, Ysorted, X2Ysorted, mid, right);
            }
        }

        public void Insert(T item)
        {

        }

        public IEnumerable<int> Search(Rectangle rect)
        {
            return new List<int>();
        }
    }
}
