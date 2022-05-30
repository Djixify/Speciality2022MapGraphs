using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static SpecialityWebService.MathObjects;

namespace SpecialityWebService.Generation
{
    public class RedBlackRangeTree2D<T> : IQueryStructure<T>
    {
        public RedBlackRangeNode<KeyValuePair<Point, T>> _root;
        private Rectangle _boundarybox = new Rectangle();
        private bool _invalidBBox = true;

        public Rectangle BoundaryBox { 
            get
            {
                if (!_invalidBBox)
                {
                    var tmp = ReportChildren(_root, Region.Infinite());
                    _boundarybox = Rectangle.FromLTRB(_root.Region.Left, tmp.Select(item => item.Key.Y).Max(), _root.Region.Right, tmp.Select(item => item.Key.Y).Min());
                }
                _invalidBBox = false;
                return _boundarybox;
            }
            set { }
        }

        public RedBlackRangeTree2D()
        {
            _root = null;
        }

        public void Insert(IQueryItem<T> item)
        {
            Insert(item.BoundaryBox.Center.X, item.BoundaryBox.Center.Y, item.Item);
        }

        public void Insert(double keyX, double keyY, T value)
        {
            _invalidBBox = true;
            RedBlackRangeNode<KeyValuePair<Point, T>> y = null;
            RedBlackRangeNode<KeyValuePair<Point, T>> x = _root;
            while (x != null)
            {
                y = x;
                if (keyX <= x.Key)
                {
                    x.Region.Left = Math.Min(keyX, x.Region.Left);
                    x = x.Left;
                }
                else
                {
                    x.Region.Right = Math.Max(keyX, x.Region.Right);
                    x = x.Right;
                }
            }
            var z = new RedBlackRangeNode<KeyValuePair<Point, T>>(y, keyX, KeyValuePair.Create(new Point(keyX, keyY), value));
            if (y == null)
                _root = z;
            else if (z.Key < y.Key)
            {
                y.Left = z;
            }
            else
            {
                y.Right = z;
            }
            z.IsBlack = false;

            var zp = z.Parent;
            var zpp = zp?.Parent;
            while (zpp != null && zp != null && !zp.IsBlack)
            {
                // If in left subtree
                if (zp == zpp.Left)
                {
                    var y_ = zpp.Right;
                    if (y_ != null && !y_.IsBlack)
                    {
                        zp.IsBlack = true;
                        y_.IsBlack = true;
                        zpp.IsBlack = false;
                        z = zpp;
                    }
                    else
                    {
                        if (z == zp.Right)
                        {
                            z = zp;
                            LeftRotate(z);
                        }
                        zp.IsBlack = true;
                        zpp.IsBlack = false;
                        RightRotate(zpp);
                    }
                }
                else// If in right subtree
                {
                    var y_ = zpp.Left;
                    if (y_ != null && !y_.IsBlack)
                    {
                        zp.IsBlack = true;
                        y_.IsBlack = true;
                        zpp.IsBlack = false;
                        z = zpp;
                    }
                    else
                    {
                        if (z == zp.Left)
                        {
                            z = zp;
                            RightRotate(z);
                        }
                        zp.IsBlack = true;
                        zpp.IsBlack = false;
                        LeftRotate(zpp);
                    }
                }
                zp = z.Parent;
                zpp = zp?.Parent;
            }

            _root.IsBlack = true;
        }

        public void LeftRotate(RedBlackRangeNode<KeyValuePair<Point, T>> x)
        {
            var y = x.Right;

            //x.Region.Left = x.Left != null ? x.Left.Region.Left : x.Key;
            x.Region.Right = y.Left != null ? y.Left.Region.Right : y.Key;

            y.Region.Left = x.Region.Left;
            //y.Region.Right = x.Region.Right;

            x.Right = y.Left;
            if (y.Left != null)
                y.Left.Parent = x;
            y.Parent = x.Parent;
            if (x.Parent == null)
                _root = y;
            else if (x == x.Parent.Left)
                x.Parent.Left = y;
            else x.Parent.Right = y;
            y.Left = x;
            x.Parent = y;
        }

        public void RightRotate(RedBlackRangeNode<KeyValuePair<Point, T>> x)
        {
            var y = x.Left;

            x.Region.Left = y.Right != null ? y.Right.Region.Left : x.Key;
            //x.Region.Right = y.Right != null ? y.Right.Region.Left : x.Key;

            //y.Region.Left = y.Left != null ? y.Left.Region.Left : y.Key; 
            y.Region.Right = x.Region.Right;

            x.Left = y.Right;
            if (y.Right != null)
                y.Right.Parent = x;
            y.Parent = x.Parent;
            if (x.Parent == null)
                _root = y;
            else if (x == x.Parent.Right)
                x.Parent.Right = y;
            else x.Parent.Left = y;
            y.Right = x;
            x.Parent = y;

        }

        private List<KeyValuePair<Point, T>> MergeY(List<KeyValuePair<Point, T>> left, List<KeyValuePair<Point, T>> right)
        {
            if (left.Count == 0)
                return right;
            if (right.Count == 0)
                return left;
            List<KeyValuePair<Point, T>> merged = new List<KeyValuePair<Point, T>>(left.Count + right.Count);
            int i = 0, l = 0, r = 0, max = left.Count + right.Count;
            while (i < max)
            {
                if (r >= right.Count || (l < left.Count && left[l].Key.Y <= right[r].Key.Y))
                    merged.Add(left[l++]);
                else merged.Add(right[r++]);
                i++;
            }
            return merged;
        }

        public List<T> Query(Rectangle query)
        {
            return Query(_root, query).Select(item => item.Value).ToList();
        }


        public List<T> Query(Point p, double tolerance)
        {
            var query = new Rectangle(p.X, p.Y, tolerance);
            return Query(_root, query).Select(item => item.Value).ToList();
        }

        public Tuple<double, T> QueryClosest(Point p, double tolerance)
        {
            var query = new Rectangle(p.X, p.Y, tolerance);
            List<KeyValuePair<Point, T>> result = Query(_root, query);

            Tuple<double, T> miny = new Tuple<double, T>(double.PositiveInfinity, default(T));
            foreach (var relevantY in result)
            {
                double dist;
                if ((dist = relevantY.Key.Distance(p)) < miny.Item1)
                    miny = new Tuple<double, T>(dist, relevantY.Value);
            }

            return miny;
        }

        public List<KeyValuePair<Point, T>> Query(RedBlackRangeNode<KeyValuePair<Point, T>> node, Rectangle query)
        {
            if (node == null)
                return new List<KeyValuePair<Point, T>>();
            Region horizontal = query.HorizontalRegion;
            if (node.Region.SubsetOf(horizontal)) //All children inside, report all
                return ReportChildren(node, query.VerticalRegion);
            else if (!node.Region.Intersects(horizontal)) //Not in query area, terminate
                return new List<KeyValuePair<Point, T>>();
            else //Otherwise get left/right subtrees
            {
                List<KeyValuePair<Point, T>> result = new List<KeyValuePair<Point, T>>();
                if (node.Left != null && node.Left.Region.Intersects(horizontal))
                    result.AddRange(Query(node.Left, query));
                if (query.Contains(node.Value.Key))
                    result = MergeY(result, new List<KeyValuePair<Point, T>>() { node.Value });
                if (node.Right != null && node.Right.Region.Intersects(horizontal))
                    result = MergeY(result, Query(node.Right, query));
                return result;
            }
        }


        private List<KeyValuePair<Point, T>> ReportChildren(RedBlackRangeNode<KeyValuePair<Point, T>> node, Region vertical)
        {
            if (node == null)
                return new List<KeyValuePair<Point, T>>();
            List<KeyValuePair<Point, T>> result = new List<KeyValuePair<Point, T>>();
            if (node.Left != null)
                result.AddRange(ReportChildren(node.Left, vertical));
            if (vertical.Contains(node.Value.Key.Y))
                result = MergeY(result, new List<KeyValuePair<Point, T>>() { node.Value });
            if (node.Right != null)
                result = MergeY(result, ReportChildren(node.Right, vertical));
            return result;
        }

        private int FindLeftMost(List<KeyValuePair<Point, T>> list, double keyY)
        {
            int left = 0;
            int right = list.Count - 1;
            while (left < right)
            {
                int mid = (left + right) / 2;
                if (keyY < list[mid].Key.Y)
                    right = mid - 1;
                else
                    left = mid + 1;
            }
            return list[left].Key.Y < keyY ? left + 1 : left;
        }

        public List<T> QueryAll() => Query(Rectangle.Infinite());

        public void InsertAll(IEnumerable<IQueryItem<T>> items)
        {
            foreach (IQueryItem<T> item in items)
                Insert(item);
        }

        public void Clear()
        {
            _root = null;
        }
    }


    /// <summary>
    /// Algorithm based on "Introduction to Algorithms" Third edition, Thomas H. Cormen, Charles E. Leiserson, Ronald L. Riverst, Clifford Stein
    /// Chaper 13, Red-black trees
    /// </summary>
    public class RedBlackRangeTree<T>
    {
        public RedBlackRangeNode<T> _root;
        public RedBlackRangeTree()
        {
            _root = null;
        }
        public void Insert(double key, T value)
        {
            RedBlackRangeNode<T> y = null;
            RedBlackRangeNode<T> x = _root;
            while (x != null)
            {
                y = x;
                if (key <= x.Key)
                    x = x.Left;
                else
                    x = x.Right;
            }
            var z = new RedBlackRangeNode<T>(y, key, value);
            if (y == null)
                _root = z;
            else if (z.Key < y.Key)
            {
                y.Region.Left = Math.Min(key, y.Region.Left);
                y.Left = z;
            }
            else
            {
                y.Region.Right = Math.Max(key, y.Region.Right);
                y.Right = z;
            }
            z.IsBlack = false;

            var zp = z.Parent;
            var zpp = zp?.Parent;
            while (zpp != null && zp != null && !zp.IsBlack)
            {
                // If in left subtree
                if (zp == zpp.Left)
                {
                    var y_ = zpp.Right;
                    if (y_ != null && !y_.IsBlack)
                    {
                        zp.IsBlack = true;
                        y_.IsBlack = true;
                        zpp.IsBlack = false;
                        z = zpp;
                    }
                    else
                    {
                        if (z == zp.Right)
                        {
                            z = zp;
                            LeftRotate(z);
                        }
                        zp.IsBlack = true;
                        zpp.IsBlack = false;
                        RightRotate(zpp);
                    }
                }
                else// If in right subtree
                {
                    var y_ = zpp.Left;
                    if (y_ != null && !y_.IsBlack)
                    {
                        zp.IsBlack = true;
                        y_.IsBlack = true;
                        zpp.IsBlack = false;
                        z = zpp;
                    }
                    else
                    {
                        if (z == zp.Left)
                        {
                            z = zp;
                            RightRotate(z);
                        }
                        zp.IsBlack = true;
                        zpp.IsBlack = false;
                        LeftRotate(zpp);
                    }
                }
                zp = z.Parent;
                zpp = zp?.Parent;
            }

            _root.IsBlack = true;
        }

        public void LeftRotate(RedBlackRangeNode<T> x)
        {
            var y = x.Right;
            x.Right = y.Left;
            if (y.Left != null)
                y.Left.Parent = x;
            y.Parent = x.Parent;
            if (x.Parent == null)
                _root = y;
            else if (x == x.Parent.Left)
                x.Parent.Left = y;
            else x.Parent.Right = y;
            y.Left = x;
            x.Parent = y;

            x.Region.Left = x.Left != null ? x.Left.Region.Left : x.Key;
            x.Region.Right = x.Right != null ? x.Right.Region.Right : x.Key;

            y.Region.Left = y.Left != null ? y.Left.Region.Left : y.Key;
            y.Region.Right = x.Region.Right;
        }

        public void RightRotate(RedBlackRangeNode<T> x)
        {
            var y = x.Left;
            x.Left = y.Right;
            if (y.Right != null)
                y.Right.Parent = x;
            y.Parent = x.Parent;
            if (x.Parent == null)
                _root = y;
            else if (x == x.Parent.Right)
                x.Parent.Right = y;
            else x.Parent.Left = y;
            y.Right = x;
            x.Parent = y;

            x.Region.Left = x.Left != null ? x.Left.Region.Left : x.Key;
            x.Region.Right = x.Right != null ? x.Right.Region.Right : x.Key;

            y.Region.Left = y.Left != null ? y.Left.Region.Left : y.Key;
            y.Region.Right = x.Region.Right;
        }

        private RedBlackRangeNode<T> FindSplitNode(RedBlackRangeNode<T> root, Rectangle rect)
        {
            var v = root;
            bool toleft;
            while (!v.IsLeaf && ((toleft = rect.MaxX <= v.Region.Left) || rect.MinX > v.Region.Right))
                if (toleft) v = v.Left;
                else v = v.Right;
            return v;
        }

        private bool ContainedInRange(RedBlackRangeNode<T> node, Rectangle rect) =>
            node.Region.Left >= rect.MinX && node.Region.Right <= rect.MaxX;

        private IEnumerable<T> ReportChildren(RedBlackRangeNode<T> node)
        {
            IEnumerable<T> result = new List<T>();
            if (node.Left != null)
                result = result.Concat(ReportChildren(node.Left));
            result = result.Append(node.Value);
            if (node.Right != null)
                result = result.Concat(ReportChildren(node.Right));
            return result;
        }


        public IEnumerable<T> Query(Region query)
        {
            return Query(_root, query);
        }

        public IEnumerable<T> Query(RedBlackRangeNode<T> node, Region query)
        {
            if (node.Region.SubsetOf(query)) //All children inside, report all
                return ReportChildren(node);
            else if (!node.Region.Intersects(query)) //Not in query area, terminate
                return new T[0];
            else //Otherwise get left/right subtrees
            {
                IEnumerable<T> result = new List<T>();
                if (node.Left != null)
                    result = result.Concat(Query(node.Left, query));
                if (query.Contains(node.Key))
                    result = result.Append(node.Value);
                if (node.Right != null)
                    result = result.Concat(Query(node.Right, query));
                return result;
            }
        }

        public override string ToString()
        {
            Queue<RedBlackRangeNode<T>> nodes = new Queue<RedBlackRangeNode<T>>();
            StringBuilder sb = new StringBuilder();
            nodes.Enqueue(_root);
            int keyspacing = (int) ((Math.Round(_root.Key, 1).ToString().Length) * 1.2);
            int valuespacing = (int)((_root.Value.ToString().Length) * 1.2);
            string formatstring = @"[id:{0," + keyspacing + "};val:{1," + valuespacing + "};col:{2};p:{3," + keyspacing + "};lc:{4," + keyspacing + "};rc:{5," + keyspacing + "}]";
            while (nodes.Count > 0)
            {
                int count = nodes.Count;
                for (int i = 0; i < count; i++)
                {
                    var node = nodes.Dequeue();
                    if (node.Left != null)
                        nodes.Enqueue(node.Left);
                    if (node.Right != null)
                        nodes.Enqueue(node.Right);
                    sb.Append(String.Format(formatstring, node.Key, node.Value, node.IsBlack ? "b" : "r", node.Parent != null ? node.Parent.Key : "-", node.Left != null ? node.Left.Key : "-", node.Right != null ? node.Right.Key : "-"));
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        public int ComputeDepth(RedBlackRangeNode<T> node, int depth = 0) => 
            Math.Max(node.Left != null ? ComputeDepth(node.Left, depth + 1) : depth, node.Right != null ? ComputeDepth(node.Right, depth + 1) : depth);

        public int ComputeDepth() => ComputeDepth(_root);
    }

    public class RedBlackRangeNode<T>
    {
        public Region Region;
        public double Key;
        public T Value;
        public RedBlackRangeNode<T> Parent, Left, Right;
        public bool IsLeaf => Left == null && Right == null;
        public bool IsBlack;
        public RedBlackRangeNode(RedBlackRangeNode<T> parent, double key, T value)
        {
            Parent = parent;
            Key = key;
            Region = new Region(key, key);
            Value = value;
            IsBlack = false;
            Left = null;
            Right = null;
        }

        public override string ToString()
        {
            return $"[{Key},{(IsBlack ? "B" : "R")},{Region}]";
        }
    }
}
