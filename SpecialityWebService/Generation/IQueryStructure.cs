using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static SpecialityWebService.MathObjects;

namespace SpecialityWebService.Generation
{
    public interface IQueryStructure<T> : IFileItem<T>, IBound
    {
        public List<T> Query(Rectangle rect);
        public List<T> Query(Point p, double tolerance);
        public Tuple<double, T> QueryClosest(Point p, double tolerance);
        public List<T> QueryAll();
        public void Insert(IQueryItem<T> item);
        public void InsertAll(IEnumerable<IQueryItem<T>> items);
        public void Clear();
    }
}
