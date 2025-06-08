using System;
using System.Threading;
using System.Threading.Tasks;

namespace OptimizationCore
{
    public class SAOptimizer : IOptimizer
    {
        public async Task<(double[] BestSolution, double BestValue)> OptimizeAsync(
            Func<double[], double> obj,
            (double Min, double Max)[] bounds,
            OptimizerParameters p,
            Action<int, double>? progress = null,
            CancellationToken token = default
        )
        {
            return await Task.Run(
                () =>
                {
                    double T = p.Params["TempStart"],
                        TEnd = p.Params["TempEnd"],
                        alpha = p.Params["CoolingRate"];
                    int inner = (int)p.Params["InnerIter"],
                        maxIter = p.MaxIterations;
                    int dim = bounds.Length;
                    var rnd = new Random();

                    double[] x = new double[dim];
                    for (int d = 0; d < dim; d++)
                        x[d] = bounds[d].Min + rnd.NextDouble() * (bounds[d].Max - bounds[d].Min);

                    double fx = obj(x),
                        bestF = fx;
                    var bestX = (double[])x.Clone();
                    int iter = 0;

                    while (T > TEnd && iter < maxIter)
                    {
                        token.ThrowIfCancellationRequested();
                        for (int i = 0; i < inner && iter < maxIter; i++)
                        {
                            var y = (double[])x.Clone();
                            int idx = rnd.Next(dim);
                            y[idx] +=
                                (rnd.NextDouble() * 2 - 1)
                                * (bounds[idx].Max - bounds[idx].Min)
                                * 0.1;
                            y[idx] = Math.Clamp(y[idx], bounds[idx].Min, bounds[idx].Max);

                            double fy = obj(y),
                                dE = fy - fx;
                            if (dE < 0 || Math.Exp(-dE / T) > rnd.NextDouble())
                            {
                                x = y;
                                fx = fy;
                                if (fx < bestF)
                                {
                                    bestF = fx;
                                    bestX = (double[])x.Clone();
                                }
                            }
                            iter++;
                            progress?.Invoke(iter, bestF);
                            token.ThrowIfCancellationRequested();
                        }
                        T *= alpha;
                    }
                    return (bestX, bestF);
                },
                token
            );
        }
    }
}
