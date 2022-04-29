using System;

namespace Qwack.Math.Solvers
{
    //https://en.wikipedia.org/wiki/Nelder//E2//80//93Mead_method
    public static class NelderMead
    {
        public static double[] MethodSolve(Func<double[], double> fn, double[] startingPoint, double[] initialStep, double tollerance, int maxItterations)
        {
            var n = startingPoint == null ? 0 : startingPoint.Length;

            if (n < 1)
                throw new Exception("Must have at least one variable");

            var konvge = 10;
            var xmin = new double[n];
            var icount = 0;
            var numres = 0;
            var kcount = maxItterations;
            var ccoeff = 0.5;
            var ecoeff = 2.0;
            var eps = 0.001;
            var rcoeff = 1.0;
            var ynewlo = 0.0;

            // Check the input parameters.
            if (tollerance <= 0.0)
                throw new Exception("Tollerance must be positive");

            var jcount = konvge;
            var nn = n + 1;
            var dnn = nn;
            var del = 1.0;
            var rq = tollerance * n;

            // Initial or restarted loop.

            var retCode = 0;

            while (retCode == 0)
            {
                var p = new double[n, n + 1];
                for (var i = 0; i < n; i++)
                    p[i, nn - 1] = startingPoint[i];

                var y = new double[nn];
                y[nn - 1] = fn(startingPoint);
                icount++;

                for (var j = 0; j < n; j++)
                {
                    var x = startingPoint[j];
                    startingPoint[j] += initialStep[j] * del;
                    for (var i = 0; i < n; i++)
                    {
                        p[i, j] = startingPoint[i];
                    }
                    y[j] = fn(startingPoint);
                    icount++;
                    startingPoint[j] = x;
                }

                // The simplex construction is complete.


                //Find highest and lowest Y values.YNEWLO = Y(IHI) indicates
                //the vertex of the simplex to be replaced.

                var ylo = y[0];
                var ilo = 0;

                for (var i = 1; i < nn; i++)
                {
                    if (y[i] < ylo)
                    {
                        ylo = y[i];
                        ilo = i;
                    }
                }

                // Inner loop.
                while (1 == 1)
                {
                    if (kcount <= icount)
                        break;

                    ynewlo = y[0];
                    var ihi = 0;

                    for (var i = 1; i < nn; i++)
                    {
                        if (ynewlo < y[i])
                        {
                            ynewlo = y[i];
                            ihi = i;
                        }
                    }

                    // Calculate PBAR, the centroid of the simplex vertices
                    //excepting the vertex with Y value YNEWLO.
                    var pbar = new double[n];
                    var pstar = new double[n];
                    for (var i = 0; i < pbar.Length; i++)
                    {
                        var z = 0.0;
                        for (var j = 0; j < nn; j++)
                        {
                            z += p[i, j];
                        }
                        z -= p[i, ihi];
                        pbar[i] = z / n;
                    }

                    // Reflection through the centroid.

                    for (var i = 0; i < pstar.Length; i++)
                        pstar[i] = pbar[i] + rcoeff * (pbar[i] - p[i, ihi]);

                    var ystar = fn(pstar);
                    icount++;

                    // Successful reflection, so extension.
                    if (ystar < ylo)
                    {
                        var p2star = new double[n];

                        for (var i = 0; i < p2star.Length; i++)
                            p2star[i] = pbar[i] + ecoeff * (pstar[i] - pbar[i]);

                        var y2star = fn(p2star);
                        icount++;

                        // Check extension.
                        if (ystar < y2star)
                        {
                            for (var i = 0; i < pstar.Length; i++)
                                p[i, ihi] = pstar[i];
                            y[ihi] = ystar;

                        }
                        else // Retain extension or contraction.
                        {
                            for (var i = 0; i < n; i++)
                                p[i, ihi] = p2star[i];
                            y[ihi] = y2star;
                        }
                    }
                    else // No extension.
                    {
                        var l = 0;
                        for (var i = 0; i < nn; i++)
                            if (ystar < y[i])
                                l++;

                        if (1 < l)
                        {
                            for (var i = 0; i < pstar.Length; i++)
                                p[i, ihi] = pstar[i];
                            y[ihi] = ystar;
                        }
                        else if (l == 0) // Contraction on the Y(IHI) side of the centroid.
                        {
                            var p2star = new double[n];
                            for (var i = 0; i < p2star.Length; i++)
                                p2star[i] = pbar[i] + ccoeff * (p[i, ihi] - pbar[i]);

                            var y2star = fn(p2star);
                            icount++;

                            // Contract the whole simplex.
                            if (y[ihi] < y2star)
                            {
                                for (var j = 0; j < y.Length; j++)
                                {
                                    for (var i = 0; i < n; i++)
                                    {
                                        p[i, j] = (p[i, j] + p[i, ilo]) * 0.5;
                                        xmin[i] = p[i, j];
                                    }
                                    y[j] = fn(xmin);
                                    icount++;
                                }

                                ylo = y[0];
                                ilo = 0;

                                for (var i = 1; i < y.Length; i++)
                                {
                                    if (y[i] < ylo)
                                    {
                                        ylo = y[i];
                                        ilo = i;
                                    }
                                }
                                continue;
                            } //Retain contraction.
                            else
                            {
                                for (var i = 0; i < n; i++)
                                    p[i, ihi] = p2star[i];

                                y[ihi] = y2star;
                            }
                        }// Contraction on the reflection side of the centroid.
                        else if (l == 1)
                        {
                            var p2star = new double[n];
                            for (var i = 0; i < p2star.Length; i++)
                                p2star[i] = pbar[i] + ccoeff * (pstar[i] - pbar[i]);

                            var y2star = fn(p2star);
                            icount++;

                            // Retain reflection ?
                            if (y2star <= ystar)
                            {
                                for (var i = 0; i < n; i++)
                                    p[i, ihi] = p2star[i];
                                y[ihi] = y2star;
                            }
                            else
                            {
                                for (var i = 0; i < n; i++)
                                    p[i, ihi] = pstar[i];
                                y[ihi] = ystar;
                            }
                        }
                    }
                    //
                    // Check if YLO improved.
                    //
                    if (y[ihi] < ylo)
                    {
                        ylo = y[ihi];
                        ilo = ihi;
                    }

                    jcount--;

                    if (0 < jcount)
                    {
                        continue;
                    }

                    // Check to see if minimum reached.
                    if (icount <= kcount)
                    {
                        jcount = konvge;

                        var z = 0.0;
                        for (var i = 0; i < y.Length; i++)
                            z += y[i];

                        var x = z / nn;

                        z = 0.0;
                        for (var i = 0; i < y.Length; i++)
                            z += (y[i] - x) * (y[i] - x);

                        if (z <= rq)
                        {
                            break;
                        }
                    }
                }
                //
                // Factorial tests to check that YNEWLO is a local minimum.
                //
                for (var i = 0; i < n; i++)
                    xmin[i] = p[i, ilo];

                ynewlo = y[ilo];

                if (kcount < icount)
                {
                    retCode = 1;
                    break;
                }

                for (var i = 0; i < n; i++)
                {
                    del = initialStep[i] * eps;
                    xmin[i] += del;
                    var z = fn(xmin);
                    icount++;
                    if (z < ynewlo)
                    {
                        retCode = 1;
                        break;
                    }
                    xmin[i] -= 2.0 * del;
                    z = fn(xmin);
                    icount++;
                    if (z < ynewlo)
                    {
                        retCode = 1;
                        break;
                    }
                    xmin[i] += del;
                }

                if (retCode == 0)
                    break;

                // Restart the procedure.
                Array.Copy(xmin, startingPoint, n);

                del = eps;
                numres++;
            }

            return xmin;
        }
    }
}
