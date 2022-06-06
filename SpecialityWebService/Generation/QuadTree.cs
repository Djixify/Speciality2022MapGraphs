using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using static SpecialityWebService.MathObjects;

namespace SpecialityWebService.Generation
{
    public class QuadTree<T> : IQueryStructure<T>
    {
        private QuadTreeNode<T> _root;

        private int _count = 0;
        public int Count => _count;
        public double MinimumWidth { get; private set; } = 0.0;
        public int MaxDepth { get; private set; } = int.MaxValue;
        public Rectangle BoundaryBox { get { return _root.BoundaryBox; } set { } }

        public QuadTree(Rectangle bounds, double minimumquadsize = 16.0)
        {
            MinimumWidth = minimumquadsize;
            MaxDepth = (int)Math.Ceiling(Math.Log2(Math.Max(bounds.Width, bounds.Height)) - Math.Log2(minimumquadsize));

            _root = new QuadTreeNode<T>(null, bounds, MaxDepth);
        }

        private QuadTree(Rectangle bounds, int depth)
        {
            MaxDepth = depth;
            BoundaryBox = bounds;
            MinimumWidth = Math.Min(bounds.Width, bounds.Height) / MaxDepth;
            _root = new QuadTreeNode<T>(null, bounds, MaxDepth);
        }

        public List<T> Query(Rectangle rect)
        {
            return _root.Query(rect).Select(item => item.Value).ToList();
        }

        public List<T> Query(Point p, double tolerance)
        {
            return _root.Query(new Rectangle(p, tolerance)).Where(item => item.Key.ClosestDistanceToPoint(p) <= tolerance).Select(item => item.Value).ToList();
        }

        public Tuple<double, T> QueryClosest(Point p, double tolerance)
        {
            var result = _root.Query(new Rectangle(p, tolerance));
            return result.Count > 0 ? result.Select(item => Tuple.Create(item.Key.ClosestDistanceToPoint(p), item.Value)).MinBy(item => item.Item1) : Tuple.Create(double.PositiveInfinity, default(T));
        }

        public List<T> QueryAll()
        {
            return _root.Query(Rectangle.Infinite()).Select(item => item.Value).ToList();
        }

        public void Insert(IQueryItem<T> item)
        {
            if (_root.Insert(item))
                _count++;
        }

        public void InsertAll(IEnumerable<IQueryItem<T>> items)
        {
            foreach (IQueryItem<T> item in items)
                if (_root.Insert(item))
                    _count++;
        }

        public void Clear()
        {
            _root = new QuadTreeNode<T>(null, _root.BoundaryBox, MaxDepth);
        }

        
    }

    public class QuadTreeNode<T> : IBound
    {
        public int Depth { get; private set; }
        public QuadTreeNode<T> Parent, NW, NE, SW, SE;
        public List<KeyValuePair<Rectangle, T>> Content = new List<KeyValuePair<Rectangle, T>>();

        public Rectangle BoundaryBox { get; set; }

        public QuadTreeNode(QuadTreeNode<T> parent, Rectangle bounds, int depth)
        {
            Parent = parent;
            BoundaryBox = bounds;
            Depth = depth;
        }

        public bool Insert(IQueryItem<T> geom)
        {
            if (!BoundaryBox.Overlapping(geom.BoundaryBox))
                return false;

            if (Depth > 0 && NW == null)
                SpawnChildren();

            //Try to insert into children first at the lowest level
            if (NW != null && (NW.Insert(geom) || NE.Insert(geom) || SW.Insert(geom) || SE.Insert(geom)))
                return true;

            //If no children accepted the item, then put it into the current (ex. 
            Content.Add(KeyValuePair.Create(geom.BoundaryBox, geom.Item));
            return true;
        }

        public List<KeyValuePair<Rectangle, T>> Query(Rectangle query)
        {
            if (!this.BoundaryBox.Overlapping(query))
                return new List<KeyValuePair<Rectangle, T>>();

            List<KeyValuePair<Rectangle, T>> results = new List<KeyValuePair<Rectangle, T>>();
            if (NW != null)
            {
                results.AddRange(NW.Query(query));
                results.AddRange(NE.Query(query));
                results.AddRange(SW.Query(query));
                results.AddRange(SE.Query(query));
            }
            results.AddRange(this.Content.Where(item => item.Key.Overlapping(query)));
            return results;
        }

        public void SpawnChildren()
        {
            double halfwidth = BoundaryBox.Width / 2.0, halfheight = BoundaryBox.Height / 2.0;
            NW = new QuadTreeNode<T>(this, Rectangle.FromLTRB(BoundaryBox.Left + halfwidth, BoundaryBox.Top, BoundaryBox.Right, BoundaryBox.Bottom + halfheight), Depth - 1);
            NE = new QuadTreeNode<T>(this, Rectangle.FromLTRB(BoundaryBox.Left, BoundaryBox.Top, BoundaryBox.Left + halfwidth, BoundaryBox.Bottom + halfheight), Depth - 1);
            SW = new QuadTreeNode<T>(this, Rectangle.FromLTRB(BoundaryBox.Left + halfwidth, BoundaryBox.Bottom + halfheight, BoundaryBox.Right, BoundaryBox.Bottom), Depth - 1);
            SE = new QuadTreeNode<T>(this, Rectangle.FromLTRB(BoundaryBox.Left, BoundaryBox.Bottom + halfheight, BoundaryBox.Left + halfwidth, BoundaryBox.Bottom), Depth - 1);
        }
    }
}

