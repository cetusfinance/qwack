namespace Qwack.Options
{
    public class TermStructureBridge(double[] times, double[] mu, double[] sigma)
    {
        // Integral of mu(t) - 0.5 * sigma(t)^2 from 0 to t
        public double DriftIntegral(double t)
        {
            if (t == 0)
                return 0;

            var sum = 0.0;
            for (var i = 1; i < times.Length; i++)
            {
                double ta = times[i-1], tb = times[i];
                if (t <= ta) break;

                var dt = System.Math.Min(tb, t) - ta;
                var drift = mu[i] - 0.5 * sigma[i] * sigma[i];
                sum += drift * dt;

                if (t < tb) break;
            }
            return sum; 
        }

        // Integral of sigma^2 from t1 to t2
        public double SigmaSqIntegral(double t1, double t2)
        {
            var sum = 0.0;
            for (var i = 1; i < times.Length; i++)
            {
                double a = times[i-1], b = times[i];
                if (t2 <= a) break;
                if (b <= t1) continue;

                var dt = System.Math.Min(b, t2) - System.Math.Max(a, t1);
                if (dt > 0)
                    sum += sigma[i] * sigma[i] * dt;
            }
            return sum;
        }
    }
}
