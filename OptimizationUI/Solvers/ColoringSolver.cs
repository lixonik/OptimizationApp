using System;
using System.Linq;
using System.Threading;
using System.Windows.Media;
using OptimizationCore;

namespace OptimizationUI.Solvers
{
    public class ColoringSolver : IGraphSolver
    {
        public event ProgressHandler Progress;
        private readonly OptimizerParameters _p;
        private readonly CancellationToken _token;

        public ColoringSolver(OptimizerParameters p, CancellationToken t)
        {
            _p = p;
            _token = t;
        }

        public void Solve(Graph graph)
        {
            int n = graph.VerticesCount;
            int k = (int)_p.Params["ColorsCount"];
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
                    .OrderBy(t => t.v)
                    .Select(t => t.i)
                    .ToArray();
                // assign colors by perm mod k
                int[] color = perm.Select((v, idx) => idx % k).ToArray();
                int conflicts = 0;
                foreach (var (u, v) in graph.Edges)
                    if (color[u] == color[v])
                        conflicts++;
                return conflicts;
            };

            var bounds = Enumerable.Repeat((0.0, 1.0), n).ToArray();
            opt.OptimizeAsync(
                    obj,
                    bounds,
                    _p,
                    (it, val) =>
                    {
                        var st = new GraphState();
                        // map best to color and fill st.VertexColors
                        Progress?.Invoke(it, st);
                        _token.ThrowIfCancellationRequested();
                    },
                    _token
                )
                .Wait(_token);
        }
    }
}
