using System;
using System.Linq;
using System.Threading.Tasks;

namespace OptimizationCore
{
    public class DEOptimizer : IOptimizer
    {
        public async Task<(double[] BestSolution, double BestValue)> OptimizeAsync(
            Func<double[], double> obj,
            (double Min, double Max)[] bounds,
            OptimizerParameters p,
            Action<int, double>? progress = null
        )
        {
            return await Task.Run(() =>
            {
                int pop = (int)p.Params["PopSize"],
                    maxIter = p.MaxIterations;
                double F = p.Params["F"],
                    CR = p.Params["CR"];
                int dim = bounds.Length;
                var rnd = new Random();

                double[][] population = new double[pop][];
                double[] vals = new double[pop];
                for (int i = 0; i < pop; i++)
                {
                    population[i] = new double[dim];
                    for (int d = 0; d < dim; d++)
                        population[i][d] =
                            bounds[d].Min + rnd.NextDouble() * (bounds[d].Max - bounds[d].Min);
                    vals[i] = obj(population[i]);
                }

                for (int gen = 0; gen < maxIter; gen++)
                {
                    for (int i = 0; i < pop; i++)
                    {
                        int a,
                            b,
                            c;
                        do
                        {
                            a = rnd.Next(pop);
                        } while (a == i);
                        do
                        {
                            b = rnd.Next(pop);
                        } while (b == i || b == a);
                        do
                        {
                            c = rnd.Next(pop);
                        } while (c == i || c == a || c == b);

                        var trial = new double[dim];
                        int R = rnd.Next(dim);
                        for (int d = 0; d < dim; d++)
                        {
                            if (rnd.NextDouble() < CR || d == R)
                                trial[d] = Math.Clamp(
                                    population[a][d] + F * (population[b][d] - population[c][d]),
                                    bounds[d].Min,
                                    bounds[d].Max
                                );
                            else
                                trial[d] = population[i][d];
                        }
                        double ft = obj(trial);
                        if (ft < vals[i])
                        {
                            population[i] = trial;
                            vals[i] = ft;
                        }
                    }
                    double best = vals.Min();
                    progress?.Invoke(gen + 1, best);
                }

                int bestIdx = Array.IndexOf(vals, vals.Min());
                return (population[bestIdx], vals[bestIdx]);
            });
        }
    }
}
