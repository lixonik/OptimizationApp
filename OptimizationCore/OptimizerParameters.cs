using System.Collections.Generic;

namespace OptimizationCore
{
    public class OptimizerParameters
    {
        public string Algorithm { get; set; } = "";
        public Dictionary<string, double> Params { get; set; } = new();
        public int MaxIterations { get; set; }
    }
}
