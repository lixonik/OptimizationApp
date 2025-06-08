using System;
using System.Linq;
using System.Threading;
using OptimizationCore;

namespace OptimizationUI.Solvers
{
    public class EulerPathSolver : IGraphSolver
    {
        public event ProgressHandler Progress;
        private readonly OptimizerParameters _p;
        private readonly CancellationToken _token;

        public EulerPathSolver(OptimizerParameters p, CancellationToken t)
        {
            _p = p;
            _token = t;
        }

        public void Solve(Graph graph)
        {
            int n = graph.VerticesCount;
            IOptimizer baseOpt = _p.Algorithm switch
            {
                "PSO" => (IOptimizer)new PSOOptimizer(),
                "SA" => new SAOptimizer(),
                "DE" => new DEOptimizer(),
                _ => throw new InvalidOperationException(),
            };
            var opt = new DiscreteOptimizer(baseOpt);
            Func<double[], double> obj = real =>
            {
                var perm = real.Select((v, i) => (v, i))
                    .OrderBy(x => x.v)
                    .Select(x => x.i)
                    .ToArray();
                int missing = 0;
                for (int i = 0; i < perm.Length - 1; i++)
                    if (!graph.Edges.Contains((perm[i], perm[i + 1])))
                        missing++;
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
