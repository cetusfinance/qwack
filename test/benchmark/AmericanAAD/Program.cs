using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiffSharp.Interop.Float64;

namespace AmericanAAD
{
    class Program
    {
        static void Main(string[] args)
        {
            var sw = new System.Diagnostics.Stopwatch();

            int nSims = 10;

            var f = 100.0;
            var k = 150.0;
            var t = 5.0;
            var sigma = 0.16;

            sw.Restart();
            for (int i = 0; i < nSims; i++)
            {
                var pvT = Options.TrinomialTree.EuropeanFuturePV(t, f, k, 0, sigma, Options.OptionType.C, 1000);
                var pvT1 = Options.TrinomialTree.EuropeanFuturePV(t, f * 1.0001, k, 0, sigma, Options.OptionType.C, 1000);
                var pvT2 = Options.TrinomialTree.EuropeanFuturePV(t, f * 0.9999, k, 0, sigma, Options.OptionType.C, 1000);
                var pvT3 = Options.TrinomialTree.EuropeanFuturePV(t, f, k, 0, sigma + 0.01, Options.OptionType.C, 1000);
                var deltaT = (pvT1 - pvT2) / (0.0002 * f);
                var gammaT = (pvT1 - 2 * pvT + pvT2) / (0.0001 * f) / (0.0001 * f);
                var vegaT = (pvT3 - pvT);
                Console.WriteLine($"{pvT} - {deltaT} - {gammaT} - {vegaT}");
            }
            var pvT_time = sw.Elapsed;

            sw.Restart();
            for (int i = 0; i < nSims; i++)
            {
                DV vectorInput = new DV(new double[] { t, f, k, 0, sigma, 0 });

                var func = new Func<D, D>((s) => { return Options.TrinomialTree.VanillaPV_AD(t, s, k, 0, sigma, Options.OptionType.C, 0, 1000, false); });
                var funcV = new Func<D, D>((v) => { return Options.TrinomialTree.VanillaPV_AD(t, f, k, 0, v, Options.OptionType.C, 0, 1000, false); });
                var pvTAD = (double)func(f);
                var deltaTAD = (double)AD.Diff(func, f);
                var vegaTAD = (double)AD.Diff(funcV, sigma);
                Console.WriteLine($"{pvTAD} - {deltaTAD} - na - {vegaTAD}");
            }
            var pvTAD_time = sw.Elapsed;

            sw.Restart();
            for (int i = 0; i < nSims; i++)
            {
                var pvB = Options.BlackFunctions.BlackPV(f, k, 0, t, sigma, Options.OptionType.C);
                var deltaB = Options.BlackFunctions.BlackDelta(f, k, 0, t, sigma, Options.OptionType.C);
                var gammaB = Options.BlackFunctions.BlackGamma(f, k, 0, t, sigma);
                var vegaB = Options.BlackFunctions.BlackVega(f, k, 0, t, sigma);

                Console.WriteLine($"{pvB} - {deltaB} - {gammaB} - {vegaB}");
            }
            var pvB_time = sw.Elapsed;


            Console.WriteLine($"Black: {pvB_time} Trinomial: {pvT_time} Trinomial AD: {pvTAD_time}");
            Console.ReadLine();
        }
    }
}
