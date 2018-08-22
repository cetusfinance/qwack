using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Models;

namespace Qwack.Core.Instruments.Asset
{
    public class AsianSwapStrip : IInstrument
    {
        public AsianSwap[] Swaplets { get; set; }

        public double PV(IAssetFxModel model) => Swaplets.Sum(x => x.PV(model));
    }
}
