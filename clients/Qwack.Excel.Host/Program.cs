using System;

namespace Qwack.Excel.Host
{
    class Program
    {
        static void Main(string[] args)
        {
            var response = Curves.IRCurveFunctions.CreateDiscountCurveFromDFs("Test", "TestCurve", DateTime.UtcNow.Date, new double[] { 1, 2, 3 }, new double[] { 0.94, 1.02, 1.04 }, "CubicSpline", "USD", null, "DailyCompounded");
            Console.WriteLine(response);
        }
    }
}
