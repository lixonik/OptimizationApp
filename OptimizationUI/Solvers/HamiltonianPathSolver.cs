using System;
using System.Linq;
using System.Threading;
using OptimizationCore;

namespace OptimizationUI.Solvers
{
    public class HamiltonianPathSolver : IGraphSolver
    {
        public event ProgressHandler Progress;
        private readonly OptimizerParameters _p;
        private readonly CancellationToken _token;

        public HamiltonianPathSolver(OptimizerParameters p, CancellationToken token)
        {
            _p = p;
            _token = token;
        }

        public void Solve(Graph graph)
        {
            int n = graph.VerticesCount;
            IOptimizer baseOpt = _p.Algorithm switch
            {
                "PSO" => new PSOOptimizer(),
                "SA" => new SAOptimizer(),
                "DE" => new DEOptimizer(),
                _ => throw new InvalidOperationException(),
            };
            var opt = new DiscreteOptimizer(baseOpt);

            Func<double[], double> obj = real =>
            {
                var perm = real.Select((v, i) => (v, i))
                    .OrderBy(t => t.v)
                    .Select(t => t.i)
                    .ToArray();

                int missing = 0;
                for (int i = 0; i < perm.Length - 1; i++)
                {
                    if (!graph.Edges.Contains((perm[i], perm[i + 1])))
                        missing++;
                }
                // penalize shorter paths more heavily if missing edges
                return missing;
            };

            var bounds = Enumerable.Repeat((0.0, 1.0), n).ToArray();
            opt.OptimizeAsync(
                    obj,
                    bounds,
                    _p,
                    (it, val) =>
                    {
                        Progress?.Invoke(it, new GraphState());
                        _token.ThrowIfCancellationRequested();
                    },
                    _token
                )
                .Wait(_token);
        }
    }
}
