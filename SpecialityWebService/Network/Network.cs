using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static SpecialityWebService.MathObjects;

namespace SpecialityWebService.Network
{
    public enum Direction
    {
        Forward = 1,
        Backward = 2,
        Both = 3
    }

    public class Network : IFileItem<Network>
    {
        public IFileArray<Vertex> V { get; }
        public IFileArray<Edge> E { get; }
        private IQueryStructure<int> _Vquery { get; }

        public int ClosestVertex(Point p, double tolerance) => _Vquery.QueryClosest(p, tolerance).Item;

        public Network(IEnumerable<Vertex> V, IEnumerable<Edge> E)
        {
            
        }

        public Network Read(BinaryReader br)
        {
            throw new NotImplementedException();
        }

        public Network Write(BinaryWriter bw)
        {
            throw new NotImplementedException();
        }
    }
}
