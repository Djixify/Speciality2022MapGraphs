using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static SpecialityWebService.MathObjects;

namespace SpecialityWebService.Generation
{
    public interface IBound
    {
        public Rectangle BoundaryBox { get; set; }
    }
}
