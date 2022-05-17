using RBush;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using static SpecialityWebService.MathObjects;

namespace SpecialityWebService.Network
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

        public List<IQueryItem<T>> Query(MathObjects.Rectangle rect)
        {
            Envelope e = rect;
            return _rtree.Search(in e).ToList();
        }

        public List<IQueryItem<T>> Query(MathObjects.Point p, double tolerance)
        {
            Envelope e = new Envelope(p.X - tolerance, p.Y - tolerance, p.X + tolerance, p.Y + tolerance);
            //Ensure it is a circular area, not square as it was in QGIS implementation
            return _rtree.Search(e).Where(item => item.BoundaryBox.ClosestDistanceToPoint(p) <= tolerance).ToList();
        }

        public IQueryItem<T> QueryClosest(MathObjects.Point p, double tolerance)
        {
            Envelope e = new Envelope(p.X - tolerance, p.Y - tolerance, p.X + tolerance, p.Y + tolerance);
            //Ensure it is a circular area, not square as it was in QGIS implementation
            List<IQueryItem<T>> items = Query(p, tolerance);
            return items.Count == 0 ? null : items.Aggregate(new KeyValuePair<double, IQueryItem<T>>(double.PositiveInfinity, null), (acc, item) => 
            {
                double distance = item.BoundaryBox.ClosestDistanceToPoint(p);
                return distance < acc.Key ? new KeyValuePair<double, IQueryItem<T>>(distance, item) : acc;
            }).Value;
        }

        public List<IQueryItem<T>> QueryAll()
        {
            return _rtree.Search().ToList();
        }
    }
}
