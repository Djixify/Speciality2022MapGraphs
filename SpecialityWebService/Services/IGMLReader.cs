using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using static SpecialityWebService.MathObjects;

namespace SpecialityWebService.Services
{
    public interface IGMLReader
    {
        public Rectangle GetBoundaryBox();
        public int GetFeatureCount();
        public IEnumerable<XElement> GetFeatureEnumerator();
        public IEnumerable<Path> GetPathEnumerator(Rectangle bbox);
        public IEnumerable<Path> GetPathEnumerator(Rectangle bbox, List<string> columns2extract);
    }
}
