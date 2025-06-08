using System;
using System.Linq;
using System.Threading;
using OptimizationCore;

namespace OptimizationUI.Solvers
{
    public class TspSolver : IGraphSolver
    {
        public event ProgressHandler Progress;
        private readonly OptimizerParameters _p;
        private readonly CancellationToken _token;

        public TspSolver(OptimizerParameters p, CancellationToken token)
        {
            _p = p;
            _token = token;
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

            Func<double[], double> obj = solArr =>
            {
                var perm = solArr.Select(d => (int)d).ToArray();
                double sum = 0;
                for (int i = 0; i < n; i++)
                {
                    int u = perm[i],
                        v = perm[(i + 1) % n];
                    // find edge weight if needed; here unit weight
                    sum += 1;
                }
                return sum;
            };

            var bounds = Enumerable.Repeat((0.0, 1.0), n).ToArray();
            var task = opt.OptimizeAsync(
                obj,
                bounds,
                _p,
                (it, val) =>
                {
                    Progress?.Invoke(it, new GraphState());
                    _token.ThrowIfCancellationRequested();
                },
                _token
            );
            task.Wait(_token);
        }
    }
}
