using System.Linq;
using System.Threading;
using System.Windows.Controls;
using OptimizationCore;
using OptimizationUI.Solvers;

namespace OptimizationUI.Controls
{
    public partial class EulerPathParamsControl : UserControl, IGraphParams
    {
        public EulerPathParamsControl() => InitializeComponent();

        public Graph LoadGraph()
        {
            var mat = AdjMatrixBox
                .Text.Split('\n')
                .Select(r => r.Split(',').Select(double.Parse).ToArray())
                .ToArray();
            var g = new Graph(mat.Length);
            for (int i = 0; i < mat.Length; i++)
            for (int j = 0; j < mat.Length; j++)
                if (mat[i][j] > 0)
                    g.Edges.Add((i, j));
            return g;
        }

        public IGraphSolver CreateSolver(CancellationToken t)
        {
            var p = new OptimizerParameters();
            int n = int.Parse(VerticesBox.Text);
            p.Params["PermutationLength"] = n;
            p.Params["SwarmSize"] = 50;
            p.Params["MaxIterations"] = 200;
            p.Algorithm = "DE";
            return new EulerPathSolver(p, t);
        }
    }
}
