using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpecialityWebService.Network
{
    public class OwnAlgorithm : INetworkGenerator
    {

        public Network Generate(IEnumerable<Path> paths, double tolerance, string directioncolumn, Dictionary<string, Direction> directionconvert, List<string> weightcalculations)
        {

            return new Network(new List<Vertex>(), new List<Edge>());
        }
    }
}
