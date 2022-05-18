using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpecialityWebService.Generation
{
    public class OwnAlgorithm : INetworkGenerator
    {
        public OwnAlgorithm() { }

        public Tuple<List<Vertex>, List<Edge>> Generate(IEnumerable<Path> paths, double tolerance, List<KeyValuePair<string, string>> weightcalculations, string directioncolumn = null, string forwardsdirection = null, string backwardsdirection = null) => Generate(paths, tolerance, tolerance, weightcalculations, directioncolumn, forwardsdirection);

        public Tuple<List<Vertex>, List<Edge>> Generate(IEnumerable<Path> paths, double endpointtolerance, double midpointtolerance, List<KeyValuePair<string, string>> weightcalculations, string directioncolumn = null, string forwardsdirection = null, string backwardsdirection = null)
        {
            List<Vertex> V = new List<Vertex>();
            List<Edge> E = new List<Edge>();


            return Tuple.Create(V, E);
        }
    }
}
