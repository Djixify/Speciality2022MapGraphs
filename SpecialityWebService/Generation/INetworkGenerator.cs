using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpecialityWebService.Generation
{
    public interface INetworkGenerator
    {
        public Tuple<List<Vertex>, List<Edge>> Generate(IEnumerable<Path> paths, double tolerance, List<KeyValuePair<string, string>> weightcalculations, string directioncolumn = null, string forwardsdirection = null, string backwardsdirection = null);

        public Tuple<List<Vertex>, List<Edge>> Generate(IEnumerable<Path> paths, double endpointtolerance, double midpointtolerance, List<KeyValuePair<string, string>> weightcalculations, string directioncolumn = null, string forwardsdirection = null, string backwardsdirection = null);
    }
}
