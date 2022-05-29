using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SpecialityWebService.Generation
{
    public interface IFileItem<T>
    {
        public void Read(BinaryReader br);
        public void Write(BinaryWriter bw);
    }
}
