using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects.Interpolators
{
    public class TO_Interpolator2d
    {
        public TO_Interpolator2d_Jagged Jagged { get; set; }
        public TO_Interpolator2d_Square Square { get; set; }
        public bool IsJagged { get; set; }
    }
}

