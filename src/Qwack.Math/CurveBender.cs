using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Qwack.Math
{
    public class CurveBender
    {
        public static double[] Bend(double[] inSpreads, double?[] sparseNewSpreads)
        {
            var o = new double[inSpreads.Length];

            //first flat-shift the front
            var firstPoint = sparseNewSpreads.Select((x, ix) => x.HasValue ? ix : int.MaxValue).Min();
            var delta = inSpreads[firstPoint] - sparseNewSpreads[firstPoint].Value;
            for (var i = 0; i <= firstPoint; i++)
            {
                o[i] = inSpreads[i] - delta;
            }
            var lastKnownPoint = firstPoint;
            var nextPoint = sparseNewSpreads.Select((x, ix) => ix > lastKnownPoint && x.HasValue ? ix : int.MaxValue).Min();
            var nextDelta = 0.0;
            if (nextPoint == int.MaxValue) //reached the last bumped spread
            {
                nextPoint = o.Length - 1;
                nextDelta = delta; //flat shift on the back
            }
            else
                nextDelta = inSpreads[nextPoint] - sparseNewSpreads[nextPoint].Value;
            var deltaDelta = (nextDelta - delta) / (nextPoint - lastKnownPoint);
            for (var j = lastKnownPoint + 1; j <= nextPoint; j++)
            {
                o[j] = inSpreads[j] - (delta + deltaDelta * (j - lastKnownPoint));
            }


            for (var i = firstPoint + 1; i < o.Length; i++)
            {
                if (sparseNewSpreads[i].HasValue)
                {
                    delta = inSpreads[i] - sparseNewSpreads[i].Value;
                    o[i] = sparseNewSpreads[i].Value;
                    lastKnownPoint = i;
                    if (i >= lastKnownPoint)
                    {
                        nextPoint = sparseNewSpreads.Select((x, ix) => ix > lastKnownPoint && x.HasValue ? ix : int.MaxValue).Min();
                        nextDelta = 0.0;
                        if (nextPoint == int.MaxValue) //reached the last bumped spread
                        {
                            nextPoint = o.Length - 1;
                            nextDelta = delta; //flat shift on the back
                        }
                        else
                            nextDelta = inSpreads[nextPoint] - sparseNewSpreads[nextPoint].Value;
                        deltaDelta = (nextDelta - delta) / (nextPoint - lastKnownPoint);
                        for (var j = lastKnownPoint+1; j <= nextPoint; j++)
                        {
                            o[j] = inSpreads[j] - (delta + deltaDelta * (j-lastKnownPoint));
                        }
                    }
                }
            }

            return o;
        }
    }
}
