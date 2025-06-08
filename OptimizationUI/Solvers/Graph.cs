using System.Collections.Generic;

namespace OptimizationUI.Solvers
{
    public class Graph
    {
        public int VerticesCount { get; }
        public List<(int, int)> Edges { get; } = new();

        public Graph(int n)
        {
            VerticesCount = n;
        }
    }
}
