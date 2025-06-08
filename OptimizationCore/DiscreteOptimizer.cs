using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OptimizationCore
{
    public class DiscreteOptimizer : IOptimizer
    {
        private readonly IOptimizer _baseOpt;

        public DiscreteOptimizer(IOptimizer baseOpt) => _baseOpt = baseOpt;

        public async Task<(double[] BestSolution, double BestValue)> OptimizeAsync(
            Func<double[], double> objectiveDiscrete,
            (double Min, double Max)[] boundsDummy,
            OptimizerParameters parameters,
            Action<int, double>? progressCallback = null,
            CancellationToken token = default
        )
        {
            int n = (int)parameters.Params["PermutationLength"];
            var bounds = Enumerable.Repeat((0.0, 1.0), n).ToArray();

            Func<double[], double> obj = real =>
            {
                var perm = real.Select((v, i) => (v, i))
                    .OrderBy(t => t.v)
                    .Select(t => t.i)
                    .ToArray();
                return objectiveDiscrete(perm.Select(i => (double)i).ToArray());
            };

            var (bestReal, bestVal) = await _baseOpt.OptimizeAsync(
                obj,
                bounds,
                parameters,
                progressCallback,
                token
            );

            var bestPerm = bestReal
                .Select((v, i) => (v, i))
                .OrderBy(t => t.v)
                .Select(t => (double)t.i)
                .ToArray();

            return (bestPerm, bestVal);
        }
    }
}
