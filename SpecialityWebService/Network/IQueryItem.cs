﻿using RBush;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpecialityWebService.Network
{
    public interface IQueryItem<T> : IFileItem<T>, IBound, ISpatialData
    {
        public T Item { get; }
    }
}
