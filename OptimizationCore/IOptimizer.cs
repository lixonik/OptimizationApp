using System;
using System.Threading;
using System.Threading.Tasks;

namespace OptimizationCore
{
    public interface IOptimizer
    {
        Task<(double[] BestSolution, double BestValue)> OptimizeAsync(
            Func<double[], double> objective,
            (double Min, double Max)[] bounds,
            OptimizerParameters parameters,
            Action<int, double>? progressCallback = null,
            CancellationToken token = default
        );
    }
}
