﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qwack.Excel.Utils
{
    public interface ISessionItem<T> : ISessionItem
    {
        T Value { get;set;}
    }
}
