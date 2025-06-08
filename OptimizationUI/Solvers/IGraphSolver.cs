using System;
using System.Threading;

namespace OptimizationUI.Solvers
{
    public delegate void ProgressHandler(int step, GraphState state);

    public interface IGraphSolver
    {
        event ProgressHandler Progress;
        void Solve(Graph graph);
    }
}
