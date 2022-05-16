using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static SpecialityWebService.MathObjects;

namespace SpecialityWebService.Network
{
    public interface IQueryStructure<T> : IBound
    {
        public List<IQueryItem<T>> Query(Rectangle rect);
        public List<IQueryItem<T>> Query(Point p, double tolerance);
        public IQueryItem<T> QueryClosest(Point p, double tolerance);
        public List<IQueryItem<T>> QueryAll();
        public void Insert(IQueryItem<T> item);
    }
}
