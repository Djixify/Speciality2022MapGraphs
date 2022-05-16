using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SpecialityWebService.Network
{
    public interface IFileItem<T>
    {
        public T Read(BinaryReader br);
        public T Write(BinaryWriter bw);
    }
}
