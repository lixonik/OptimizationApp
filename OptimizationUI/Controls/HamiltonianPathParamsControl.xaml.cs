using System.Linq;
using System.Threading;
using System.Windows.Controls;
using OptimizationCore;
using OptimizationUI.Solvers;

namespace OptimizationUI.Controls
{
    public partial class HamiltonianPathParamsControl : UserControl, IGraphParams
    {
        public HamiltonianPathParamsControl() => InitializeComponent();

        public Graph LoadGraph()
        {
            var mat = AdjMatrixBox
                .Text.Split('\n')
                .Select(r => r.Split(',').Select(double.Parse).ToArray())
                .ToArray();
            int n = mat.Length;
            var g = new Graph(n);
            for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                if (mat[i][j] > 0)
                    g.Edges.Add((i, j));
            return g;
        }

        public IGraphSolver CreateSolver(CancellationToken token)
        {
            var p = new OptimizerParameters();
            int n = int.Parse(VerticesBox.Text);
            p.Params["PermutationLength"] = n;
            p.Params["SwarmSize"] = 40;
            p.Params["MaxIterations"] = 200;
            p.Algorithm = "DE";
            return new HamiltonianPathSolver(p, token);
        }
    }
}
