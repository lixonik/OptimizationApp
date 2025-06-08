using System.Threading;
using OptimizationUI.Solvers;

namespace OptimizationUI.Controls
{
    public interface IGraphParams
    {
        Graph LoadGraph();
        IGraphSolver CreateSolver(CancellationToken token);
    }
}
