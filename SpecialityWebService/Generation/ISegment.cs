using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static SpecialityWebService.MathObjects;

namespace SpecialityWebService.Generation
{
    public interface ISegment : IBound
    {
        Point P1 { get; set; }
        Point P2 { get; set; }
    }
}
