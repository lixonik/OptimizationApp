using System.Linq;
using System.Threading;
using System.Windows.Controls;
using OptimizationCore;
using OptimizationUI.Solvers;

namespace OptimizationUI.Controls
{
    public partial class ShortestPathParamsControl : UserControl, IGraphParams
    {
        public ShortestPathParamsControl() => InitializeComponent();

        public Graph LoadGraph()
        {
            var rows = AdjMatrixBox
                .Text.Split('\n')
                .Select(r => r.Split(',').Select(double.Parse).ToArray())
                .ToArray();
            int n = rows.Length;
            var g = new Graph(n);
            for (int i = 0; i < n; i++)
            for (int j = i + 1; j < n; j++)
                if (rows[i][j] > 0)
                    g.Edges.Add((i, j));
            return g;
        }

        public IGraphSolver CreateSolver(CancellationToken token)
        {
            var p = new OptimizerParameters();
            int n = int.Parse(VerticesBox.Text);
            p.Params["PermutationLength"] = n;
            p.Params["SwarmSize"] = 30;
            p.Params["MaxIterations"] = 200;
            p.Algorithm = "PSO"; // or expose choice
            return new TspSolver(p, token);
        }
    }
}
