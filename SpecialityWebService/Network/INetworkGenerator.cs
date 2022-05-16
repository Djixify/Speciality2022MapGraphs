using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpecialityWebService.Network
{
    public interface INetworkGenerator
    {
        public Network Generate(IEnumerable<Path> paths, double tolerance, string directioncolumn, Dictionary<string, Direction> directionconvert, List<string> weightcalculations);
    }
}
