﻿using System;
using System.Collections.Generic;


namespace FliDynModule.ODE
{
    public static partial class Ode
    {
        public static IEnumerable<SolPoint> Euler(double t0, Vector x0, Func<double, Vector, Vector> f)
        {
            return Euler(t0, x0, f, Options.Default);
        }
        public static IEnumerable<SolPoint> Euler(double t0, Vector x0, Func<double, Vector, Vector> f, Options opts)
        {
            double t = t0;
            Vector x = x0;
            int n = x0.Length;
            double dt = opts.InitialStep;
            yield return new SolPoint(t0, x0.Clone());

            while (true)
            {
                Vector x1 = f(t, x);
                x = x + dt * x1;
                t = t + dt;
                yield return new SolPoint(t, x);
            }
        }        
        public static IEnumerable<SolPoint> RK45(double t0, Vector x0, Func<double, Vector, Vector> f)
        {
            return RK45(t0, x0, f, Options.Default);
        }
        public static IEnumerable<SolPoint> RK45(double t0, Vector x0, Func<double, Vector, Vector> f, Options opts)
        {
            double t = t0;
            Vector x = x0;
            int n = x0.Length;
            double dt = opts.InitialStep;
            yield return new SolPoint(t0, x0.Clone());

            while (true)
            {
                Vector x1 = f(t, x);
                Vector xx = x + x1 * (dt / 2.0);
                Vector x2 = f(t + dt / 2.0, xx);
                xx = x + x2 * (dt / 2.0);
                Vector x3 = f(t + dt / 2.0, xx);
                xx = x + x3 * dt;
                Vector x4 = f(t + dt, xx);
                x = x + (dt / 6.0) * (x1 + 2.0 * x2 + 2.0 * x3 + x4);
                t = t + dt;
                yield return new SolPoint(t, x);
            }
        }
        public static IEnumerable<SolPoint> RK547M(double t0, Vector x0, Func<double, Vector, Vector> f)
        {
            return RK547M(t0, x0, f, Options.Default);
        }
        public static IEnumerable<SolPoint> RK547M(double tstart, double tfinal, Vector x0, Func<double, Vector, Vector> f)
        {
            return RK547M(tstart, tfinal, x0, f, Options.Default);
        }
        public static IEnumerable<SolPoint> RK547M(double t0, Vector x0, Func<double, Vector, Vector> f, Options opts)
        {
            const double SafetyFactor = 0.8;
            const double MaxFactor = 5.0d;
            const double MinFactor = 0.2d;
            int Refine = 4;
            Vector S = Vector.Zeros(Refine - 1);
            for (int i = 0; i < Refine - 1; i++)
                S[i] = (double)(i + 1) / Refine;

            double t = t0;
            Vector x = x0.Clone();
            Vector x1 = x0.Clone();
            int n = x0.Length;
            double dt = opts.InitialStep;
            double[][] a = new double[6][];
            a[0] = new double[] { 1 / 5d };
            a[1] = new double[] { 3 / 40d, 9 / 40d };
            a[2] = new double[] { 44 / 45d, -56 / 15d, 32 / 9d };
            a[3] = new double[] { 19372 / 6561d, -25360 / 2187d, 64448 / 6561d, -212 / 729d };
            a[4] = new double[] { 9017 / 3168d, -355 / 33d, 46732 / 5247d, 49 / 176d, -5103 / 18656d };
            a[5] = new double[] { 35 / 384d, 0, 500 / 1113d, 125 / 192d, -2187 / 6784d, 11 / 84d };
            Vector c = new Vector(0, 1 / 5d, 3 / 10d, 4 / 5d, 8 / 9d, 1, 1);
            Vector b1 = new Vector(5179 / 57600d, 0, 7571 / 16695d, 393 / 640d, -92097 / 339200d, 187 / 2100d, 1 / 40d);
            Vector b = new Vector(35 / 384d, 0, 500 / 1113d, 125 / 192d, -2187 / 6784d, 11 / 84d, 0);
            const int s = 7;
            double dt0;
            if (opts.InitialStep == 0)
            {
                double d0 = 0.0d, d1 = 0.0d;
                double[] sc = new double[n];
                var f0 = f(t0, x0);
                for (int i = 0; i < n; i++)
                {
                    sc[i] = opts.AbsoluteTolerance + opts.RelativeTolerance * Math.Abs(x0[i]);
                    d0 = Math.Max(d0, Math.Abs(x0[i]) / sc[i]);
                    d1 = Math.Max(d1, Math.Abs(f0[i]) / sc[i]);
                }
                var h0 = Math.Min(d0, d1) < 1e-5 ? 1e-6 : 1e-2 * (d0 / d1);

                var f1 = f(t0 + h0, x0 + h0 * f0);
                double d2 = 0;
                for (int i = 0; i < n; i++)
                    d2 = Math.Max(d2, Math.Abs(f0[i] - f1[i]) / sc[i] / h0);
                dt = Math.Max(d1, d2) <= 1e-15 ? Math.Max(1e-6, h0 * 1e-3) : Math.Pow(1e-2 / Math.Max(d1, d2), 1 / 5d);
                if (dt > 100 * h0)
                    dt = 100 * h0;
            }
            else
                dt = opts.InitialStep;
            dt0 = dt;
            double tout = t0;
            Vector xout = x0.Clone();
            if (opts.OutputStep > 0)
            {
                tout += opts.OutputStep;
            }
            yield return new SolPoint(t0, x0.Clone());
            
            Vector[] k = new Vector[s];
            Vector[] x2 = new Vector[s - 1];
            for (int i = 0; i < s - 1; i++)
                x2[i] = Vector.Zeros(n);

            Vector prevX = Vector.Zeros(n);
            double prevErr = 1.0d;
            double prevDt;

            while (true)
            {
                Vector.Copy(x1, prevX);
                double e = 0.0d;
                do
                {
                    prevDt = dt;
                    Vector.Copy(prevX, x1);
                    k[0] = dt * f(t, x1);
                    for (int i = 1; i < s; i++)
                    {
                        Vector.Copy(x1, x2[i - 1]);
                        for (int j = 0; j < i; j++)
                        {
                            x2[i - 1].MulAdd(k[j], a[i - 1][j]);
                        }
                        k[i] = dt * f(t + dt * c[i], x2[i - 1]);
                    }
                    Vector.Copy(prevX, x);
                    for (int l = 0; l < s; l++)
                    {
                        x.MulAdd(k[l], b[l]);
                        x1.MulAdd(k[l], b1[l]);
                    }
                    e = Math.Abs(x[0] - x1[0]) / Math.Max(opts.AbsoluteTolerance, opts.RelativeTolerance * Math.Max(Math.Abs(prevX[0]), Math.Abs(x1[0])));
                    for (int i = 1; i < n; i++)
                        e = Math.Max(e, Math.Abs(x[i] - x1[i]) / Math.Max(opts.AbsoluteTolerance, opts.RelativeTolerance * Math.Max(Math.Abs(prevX[i]), Math.Abs(x1[i]))));
                    dt = e == 0 ? dt : dt * Math.Min(MaxFactor, Math.Max(MinFactor, SafetyFactor * Math.Pow(1.0d / e, 1.0d / 5.0d) * Math.Pow(prevErr, 0.08d)));

                    if (opts.MaxStep < Double.MaxValue) dt = Math.Min(dt, opts.MaxStep);
                    if (Double.IsNaN(dt)) throw new ArgumentException("Derivatives function returned NaN");
                    if (dt < 1e-12) throw new ArgumentException("Cannot generate numerical solution");

                } while (e > 1.0d);
                prevErr = e;               
                if (opts.OutputStep > 0) 
                {
                    while (t <= tout && tout <= t + prevDt)
                    {
                        yield return new SolPoint(tout, Vector.Lerp(tout, t, xout, t + prevDt, x1));
                        tout += opts.OutputStep;
                    }
                }
                else
                    if (Refine > 1)
                {
                    Vector ts = Vector.Zeros(S.Length);
                    for (int i = 0; i < S.Length; i++)
                        ts[i] = t + prevDt * S[i];

                    var ys = RKinterp(S, xout, k);
                    for (int i = 0; i < S.Length; i++)
                        yield return new SolPoint(ts[i], ys[i]);
                }
                yield return new SolPoint(t + prevDt, x1.Clone());
                t = t + prevDt;
                Vector.Copy(x1, xout);
            }
        }
        
        public static IEnumerable<SolPoint> RK547M(double tstart, double tfinal, Vector x0, Func<double, Vector, Vector> f, Options opts)
        {
            if (opts == null)
                throw new ArgumentException("opts");

            if (opts.MaxStep == Double.MaxValue) opts.MaxStep = (tfinal - tstart) * 1e-2;

            return Ode.RK547M(tstart, x0, f, opts).SolveFromTo(tstart, tfinal);
        }
        public static Vector[] RKinterp(Vector s, Vector y, Vector[] k)
        {
            if (k == null)
                throw new ArgumentNullException("k");
            int n = y.Length;
            int nk = k.Length;
            int ns = s.Length;

            Vector[] ys = new Vector[ns];
            for (int i = 0; i < ns; i++)
                ys[i] = y.Clone();
            double[,] sp = new double[4, ns];
            for (int i = 0; i < ns; i++)
            {
                sp[0, i] = s[i];
                for (int j = 1; j < 4; j++)
                    sp[j, i] = s[i] * sp[j - 1, i];
            }
            double[][] BI = new double[7][];
            BI[0] = new double[] { 1.0d, -183 / 64d, 37 / 12d, -145 / 128d };
            BI[1] = new double[] { 0.0d, 0.0d, 0.0d, 0.0d };
            BI[2] = new double[] { 0.0d, 1500 / 371d, -1000 / 159d, 1000 / 371d };
            BI[3] = new double[] { 0.0d, -125 / 32d, 125 / 12d, -375 / 64d };
            BI[4] = new double[] { 0.0d, 9477 / 3392d, -729 / 106d, 25515 / 6784d };
            BI[5] = new double[] { 0.0d, -11 / 7d, 11 / 3d, -55 / 28d };
            BI[6] = new double[] { 0.0d, 3 / 2d, -4d, 5 / 2d };
            double[,] kBI = new double[n, 4];
            var kBIs = new double[n, ns];
            for (int i1 = 0; i1 < ns; i1++)
                for (int i2 = 0; i2 < n; i2++)
                {
                    kBIs[i2, i1] = 0.0d;
                    for (int j = 0; j < 4; j++)
                    {
                        kBI[i2, j] = 0.0d;
                        for (int l = 0; l < nk; l++)
                            kBI[i2, j] += k[l][i2] * BI[l][j];
                        kBIs[i2, i1] += kBI[i2, j] * sp[j, i1];
                    }
                    ys[i1][i2] += kBIs[i2, i1];
                }

            return ys;
        }
    }
}
