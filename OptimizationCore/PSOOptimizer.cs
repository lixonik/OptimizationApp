using System;
using System.Linq;
using System.Threading.Tasks;

namespace OptimizationCore
{
    public class PSOOptimizer : IOptimizer
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
                int swarm = (int)p.Params["SwarmSize"],
                    maxIter = p.MaxIterations;
                double w = p.Params["Inertia"],
                    c1 = p.Params["C1"],
                    c2 = p.Params["C2"];
                int dim = bounds.Length;
                var rnd = new Random();

                double[][] pos = new double[swarm][],
                    vel = new double[swarm][];
                double[][] pbestPos = new double[swarm][];
                double[] pbestVal = new double[swarm];
                double[] gbestPos = new double[dim];
                double gbestVal = double.MaxValue;

                for (int i = 0; i < swarm; i++)
                {
                    pos[i] = new double[dim];
                    vel[i] = new double[dim];
                    for (int d = 0; d < dim; d++)
                    {
                        pos[i][d] =
                            bounds[d].Min + rnd.NextDouble() * (bounds[d].Max - bounds[d].Min);
                        vel[i][d] = 0;
                    }
                    pbestPos[i] = (double[])pos[i].Clone();
                    pbestVal[i] = obj(pos[i]);
                    if (pbestVal[i] < gbestVal)
                    {
                        gbestVal = pbestVal[i];
                        gbestPos = (double[])pbestPos[i].Clone();
                    }
                }

                for (int it = 0; it < maxIter; it++)
                {
                    for (int i = 0; i < swarm; i++)
                    {
                        for (int d = 0; d < dim; d++)
                        {
                            double r1 = rnd.NextDouble(),
                                r2 = rnd.NextDouble();
                            vel[i][d] =
                                w * vel[i][d]
                                + c1 * r1 * (pbestPos[i][d] - pos[i][d])
                                + c2 * r2 * (gbestPos[d] - pos[i][d]);
                            pos[i][d] += vel[i][d];
                            pos[i][d] = Math.Clamp(pos[i][d], bounds[d].Min, bounds[d].Max);
                        }
                        double v = obj(pos[i]);
                        if (v < pbestVal[i])
                        {
                            pbestVal[i] = v;
                            pbestPos[i] = (double[])pos[i].Clone();
                        }
                        if (v < gbestVal)
                        {
                            gbestVal = v;
                            gbestPos = (double[])pos[i].Clone();
                        }
                    }
                    progress?.Invoke(it + 1, gbestVal);
                }
                return (gbestPos, gbestVal);
            });
        }
    }
}
