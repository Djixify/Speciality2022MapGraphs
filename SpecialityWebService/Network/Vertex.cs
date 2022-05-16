using RBush;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static SpecialityWebService.MathObjects;

namespace SpecialityWebService.Network
{
    public class Vertex : IQueryItem<Vertex>, ISpatialData
    {
        public int Index { get; set; } = -1;

        private double _boundsradius = 0.00001;
        private Point _location;
        public Point Location 
        { 
            get { return _location; } 
            set 
            { 
                _location = value;
                _envelope = new Envelope(_location.X - _boundsradius, _location.Y - _boundsradius, _location.X + _boundsradius, _location.Y + _boundsradius);
            } 
        }
        public List<int> Edges { get; set; }
        public string Fid { get; set; }
        public int PathId { get; set; }

        private Envelope _envelope;
        public ref readonly Envelope Envelope => ref _envelope;
        public Rectangle BoundaryBox { get { return _envelope; } set { } }

        public Vertex Item { get { return this; } }

        public Vertex(int index, Point location, IEnumerable<int> edges, int pathid, string fid = null)
        {
            Index = index;
            Location = location;
            Edges = new List<int>(edges);
            PathId = pathid;
            Fid = fid;
        }


        public Vertex Read(BinaryReader br)
        {
            throw new NotImplementedException();
        }

        public Vertex Write(BinaryWriter bw)
        {
            throw new NotImplementedException();
        }
    }
}
