using System;
using System.Collections.Generic;
using System.Linq;

namespace lvlw
{
    public class R
    {
        Random m_r = new Random(1);
        public double[] SampleDenseCategorical(int k)
        {
            double[] w = new double[k];
            double sum = 0;
            for (int i = 0; i < k; ++i)
                sum += w[i] = m_r.NextDouble();
            for (int i = 0; i < k; ++i)
                w[i] /= sum;
            return w;
        }
        public int SampleFromCategorical(double[] rgd)
        {
            double sum = 0;
            for (int i = 0; i < rgd.Length; ++i)
                sum += rgd[i];
            double val = m_r.NextDouble() * sum;
            for (int i = 0; i < rgd.Length - 1; ++i)
            {
                var x = rgd[i];
                if (val <= x) return i;
                val -= x;
            }
            return rgd.Length - 1;
        }
        public int Sample(int k)
        {
            return m_r.Next(0, k);
        }

        public double[] SampleDenseCategorical(int[] c, double alpha)
        {
            double[] alphas = new double[c.Length];
            for (int i = 0; i < c.Length; ++i)
                alphas[i] = c[i] + alpha;
            return SampleDirichlet(alphas);
        }

        public SparseCategorical SampleSparseCategorical(Counter<int> c, double alpha, double floor)
        {
            int dim = c.Keys.Max() + 1;
            double[] alphas = new double[dim];
            for (int i = 0; i < dim; ++i) alphas[i] = alpha;
            foreach (var p in c)
                alphas[p.Key] += p.Value;
            double[] dist = SampleDirichlet(alphas);
            double sum = dist.Sum();
            var sc = new SparseCategorical();
            for (int i = 0; i < dim; ++i)
            {
                double val = dist[i] / sum;
                if (val > floor)
                    sc.W[i] = val;
            }
            return sc;
        }

        public double Uniform()
        {
            return m_r.NextDouble();
        }

        public double[] SampleDirichlet(double[] alphas)
        {
            double[] x = new double[alphas.Length];
            double sum = 0;
            for (int i = 0; i < x.Length; ++i)
                sum += x[i] = SampleGamma(alphas[i], 1.0);
            for (int i = 0; i < x.Length; ++i)
                x[i] /= sum;
            return x;
        }

        public double SampleGamma(double alpha, double lambda)
        {
            /******************************************************************
             *                                                                *
             *    Gamma Distribution - Acceptance Rejection combined with     *
             *                         Acceptance Complement                  *
             *                                                                *
             ******************************************************************
             *                                                                *
             * FUNCTION:    - gds samples a random number from the standard   *
             *                gamma distribution with parameter  a > 0.       *
             *                Acceptance Rejection  gs  for  a < 1 ,          *
             *                Acceptance Complement gd  for  a >= 1 .         *
             * REFERENCES:  - J.H. Ahrens, U. Dieter (1974): Computer methods *
             *                for sampling from gamma, beta, Poisson and      *
             *                binomial distributions, Computing 12, 223-246.  *
             *              - J.H. Ahrens, U. Dieter (1982): Generating gamma *
             *                variates by a modified rejection technique,     *
             *                Communications of the ACM 25, 47-54.            *
             * SUBPROGRAMS: - drand(seed) ... (0,1)-Uniform generator with    *
             *                unsigned long integer *seed                     *
             *              - NORMAL(seed) ... Normal generator N(0,1).       *
             *                                                                *
             ******************************************************************/
            double a = alpha;
            double aa = -1.0, aaa = -1.0, 
                   b=0.0, c=0.0, d=0.0, e, r, s=0.0, si=0.0, ss=0.0, q0=0.0,
                   q1 = 0.0416666664, q2 =  0.0208333723, q3 = 0.0079849875,
                   q4 = 0.0015746717, q5 = -0.0003349403, q6 = 0.0003340332,
                   q7 = 0.0006053049, q8 = -0.0004701849, q9 = 0.0001710320,
                   a1 = 0.333333333,  a2 = -0.249999949,  a3 = 0.199999867,
                   a4 =-0.166677482,  a5 =  0.142873973,  a6 =-0.124385581,
                   a7 = 0.110368310,  a8 = -0.112750886,  a9 = 0.104089866,
                   e1 = 1.000000000,  e2 =  0.499999994,  e3 = 0.166666848,
                   e4 = 0.041664508,  e5 =  0.008345522,  e6 = 0.001353826,
                   e7 = 0.000247453;

            double gds,p,q,t,sign_u,u,v,w,x;
            double v1,v2,v12;

            // Check for invalid input values

            if (a <= 0.0) throw new ArgumentException("SampleGamma: alpha must be positive"); 
            if (lambda <= 0.0) throw new ArgumentException("SampleGamma: lambda must be positive");

            if (a < 1.0)
            {
                // CASE A: Acceptance rejection algorithm gs

                // step 1
                b = 1.0 + 0.36788794412 * a;
                for(;;) {
                    p = b * Uniform();

                    if (p <= 1.0)
                    {
                        // step 2: gds <= 1
                        gds = Math.Exp(Math.Log(p) / a);
                        if (Math.Log(Uniform()) <= -gds)
                            return(gds/lambda);
                    }
                    else
                    {
                        // step 3: Case gds > 1
                        gds = - Math.Log ((b - p) / a);
                        if (Math.Log(Uniform()) <= ((a - 1.0) * Math.Log(gds)))
                            return(gds/lambda);
                    }
                }
            }
            else
            {
                // CASE B: Acceptance complement algorithm gd (gaussian distribution, box muller transformation)
                // Step 1. Preparations
                if (a != aa)
                {
                    aa = a;
                    ss = a - 0.5;
                    s = Math.Sqrt(ss);
                    d = 5.656854249 - 12.0 * s;
                }
                // Step 2. Normal deviate
                do {
                    v1 = 2.0 * Uniform() - 1.0;
                    v2 = 2.0 * Uniform() - 1.0;
                    v12 = v1*v1 + v2*v2;
                } while ( v12 > 1.0 );
                t = v1*Math.Sqrt(-2.0*Math.Log(v12)/v12);
                x = s + 0.5 * t;
                gds = x * x;
                if (t >= 0.0)
                {
                    // Immediate acceptance
                    return(gds/lambda);
                }

                // Step 3. Uniform random number
                u = Uniform();
                if (d * u <= t * t * t)
                {
                    // Squeeze acceptance
                    return(gds/lambda);
                }

                // Step 4. Set-up for hat case
                if (a != aaa)
                {
                    aaa = a;
                    r = 1.0 / a;
                    q0 = ((((((((q9 * r + q8) * r + q7) * r + q6) * r + q5) * r + q4) *
                                    r + q3) * r + q2) * r + q1) * r;
                    if (a > 3.686)
                    {
                        if (a > 13.022)
                        {
                            b = 1.77;
                            si = 0.75;
                            c = 0.1515 / s;
                        }
                        else
                        {
                            b = 1.654 + 0.0076 * ss;
                            si = 1.68 / s + 0.275;
                            c = 0.062 / s + 0.024;
                        }
                    }
                    else
                    {
                        b = 0.463 + s - 0.178 * ss;
                        si = 1.235;
                        c = 0.195 / s - 0.079 + 0.016 * s;
                    }
                }
                if (x > 0.0)
                {
                    // Step 5. Calculation of q
                    v = t / (s + s);
                    // Step 6.
                    if (Math.Abs(v) > 0.25)
                    {
                        q = q0 - s * t + 0.25 * t * t + (ss + ss) * Math.Log(1.0 + v);
                    }
                    else
                    {
                        q = q0 + 0.5 * t * t * ((((((((a9 * v + a8) * v + a7) * v + a6) *
                                                v + a5) * v + a4) * v + a3) * v + a2) * v + a1) * v;
                    }
                    // Step 7. Quotient acceptance
                    if (Math.Log(1.0 - u) <= q)
                        return(gds/lambda);
                }

                for(;;)
                {
                    // Step 8. Double exponential deviate t
                    // Step 9. Rejection of t
                    do
                    {
                        e = -Math.Log(Uniform());
                        u = Uniform();
                        u = u + u - 1.0;
                        sign_u = (u > 0)? 1.0 : -1.0;
                        t = b + (e * si) * sign_u;
                    } while (t <= -0.71874483771719);
                    v = t / (s + s);

                    // Step 10. New q(t)
                    if (Math.Abs(v) > 0.25)
                    {
                        q = q0 - s * t + 0.25 * t * t + (ss + ss) * Math.Log(1.0 + v);
                    }
                    else
                    {
                        q = q0 + 0.5 * t * t * ((((((((a9 * v + a8) * v + a7) * v + a6) *
                                                v + a5) * v + a4) * v + a3) * v + a2) * v + a1) * v;
                    }
                    // Step 11.
                    if (q <= 0.0) continue;
                    if (q > 0.5)
                    {
                        w = Math.Exp(q) - 1.0;
                    }
                    else
                    {
                        w = ((((((e7 * q + e6) * q + e5) * q + e4) * q + e3) * q + e2) *
                                q + e1) * q;
                    }
                    // Step 12. Hat acceptance
                    if ( c * u * sign_u <= w * Math.Exp(e - 0.5 * t * t))
                    {
                        x = s + 0.5 * t;
                        return(x*x/lambda);
                    }
                }
            }
        }
    }
}
