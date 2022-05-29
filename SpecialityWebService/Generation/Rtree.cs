using RBush;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using static SpecialityWebService.MathObjects;

namespace SpecialityWebService.Generation
{
    //Using package from: https://github.com/viceroypenguin/RBush
    public class Rtree<T> : IQueryStructure<T>
    {
        private RBush<IQueryItem<T>> _rtree;

        public Rtree() { _rtree = new RBush<IQueryItem<T>>(); }

        public Rectangle BoundaryBox { get { return _rtree.Envelope; } set { } }

        public void Insert(IQueryItem<T> item)
        {
            _rtree.Insert(item);
        }

        public List<T> Query(MathObjects.Rectangle rect)
        {
            Envelope e = rect;
            return _rtree.Search(in e).Select(item => item.Item).ToList();
        }

        public List<T> Query(MathObjects.Point p, double tolerance)
        {
            Envelope e = new Envelope(p.X - tolerance, p.Y - tolerance, p.X + tolerance, p.Y + tolerance);
            //Ensure it is a circular area, not square as it was in QGIS implementation
            return _rtree.Search(e).Where(item => ((Rectangle)item.Envelope).ClosestDistanceToPoint(p) <= tolerance).Select(item => item.Item).ToList();
        }

        public Tuple<double, T> QueryClosest(MathObjects.Point p, double tolerance)
        {
            Envelope e = new Envelope(p.X - tolerance, p.Y - tolerance, p.X + tolerance, p.Y + tolerance);
            //Ensure it is a circular area, not square as it was in QGIS implementation
            return _rtree.Search(e).Aggregate(new Tuple<double, T>(double.PositiveInfinity, default(T)), (acc, item) => 
            {
                double distance = ((Rectangle)item.Envelope).ClosestDistanceToPoint(p);
                return distance <= tolerance && distance < acc.Item1 ? new Tuple<double, T>(distance, item.Item) : acc;
            });
        }

        public void InsertAll(IEnumerable<IQueryItem<T>> items) => _rtree.BulkLoad(items);

        public List<T> QueryAll() => _rtree.Search().Select(item => item.Item).ToList();

        public void Clear()
        {
            _rtree.Clear();
        }
    }
}
