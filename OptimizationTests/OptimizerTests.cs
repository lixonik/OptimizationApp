using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OptimizationCore;
using Xunit;

namespace OptimizationTests
{
    public class OptimizerTests
    {
        private static (double[] BestSolution, double BestValue) RunOptimizer(
            IOptimizer optimizer,
            Func<double[], double> objective,
            (double Min, double Max)[] bounds,
            OptimizerParameters parameters
        )
        {
            var task = optimizer.OptimizeAsync(objective, bounds, parameters, null);
            task.Wait();
            return task.Result;
        }

        [Theory]
        [InlineData(2)]
        [InlineData(5)]
        public void PSO_Finds_Minimum_Of_SumOfSquares(int dimension)
        {
            Func<double[], double> obj = x => x.Select(v => v * v).Sum();
            var bounds = Enumerable.Repeat((Min: -5.0, Max: 5.0), dimension).ToArray();

            var parameters = new OptimizerParameters { MaxIterations = 50 };
            parameters.Params["SwarmSize"] = 20;
            parameters.Params["Inertia"] = 0.5;
            parameters.Params["C1"] = 1.0;
            parameters.Params["C2"] = 1.0;

            var result = RunOptimizer(new PSOOptimizer(), obj, bounds, parameters);

            Assert.True(
                result.BestValue < 1e-2,
                $"PSO did not converge sufficiently: {result.BestValue}"
            );
            Assert.All(result.BestSolution, v => Assert.InRange(v, -0.1, 0.1));
        }

        [Theory]
        [InlineData(2)]
        public void DE_Finds_Minimum_Of_SumOfSquares(int dimension)
        {
            Func<double[], double> obj = x => x.Select(v => v * v).Sum();
            var bounds = Enumerable.Repeat((Min: -5.0, Max: 5.0), dimension).ToArray();

            var parameters = new OptimizerParameters { MaxIterations = 30 };
            parameters.Params["PopSize"] = 30;
            parameters.Params["F"] = 0.7;
            parameters.Params["CR"] = 0.9;

            var result = RunOptimizer(new DEOptimizer(), obj, bounds, parameters);

            Assert.True(
                result.BestValue < 1e-2,
                $"DE did not converge sufficiently: {result.BestValue}"
            );
            Assert.All(result.BestSolution, v => Assert.InRange(v, -0.2, 0.2));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        public void SA_Finds_Minimum_Of_SumOfSquares(int dimension)
        {
            Func<double[], double> obj = x => x.Select(v => v * v).Sum();
            var bounds = Enumerable.Repeat((Min: -5.0, Max: 5.0), dimension).ToArray();

            var parameters = new OptimizerParameters { MaxIterations = 100 };
            parameters.Params["TempStart"] = 100.0;
            parameters.Params["TempEnd"] = 1.0;
            parameters.Params["CoolingRate"] = 0.9;
            parameters.Params["InnerIter"] = 5;

            var result = RunOptimizer(new SAOptimizer(), obj, bounds, parameters);

            Assert.All(
                result.BestSolution,
                (v, i) => Assert.InRange(v, bounds[i].Min, bounds[i].Max)
            );
            double expected = obj(result.BestSolution);
            Assert.Equal(expected, result.BestValue, precision: 8);
        }

        [Fact]
        public void PSO_ProgressCallback_Is_Called_For_Each_Iteration()
        {
            Func<double[], double> obj = x => x[0] * x[0];
            var bounds = new[] { (Min: -1.0, Max: 1.0) };

            var parameters = new OptimizerParameters { MaxIterations = 10 };
            parameters.Params["SwarmSize"] = 5;
            parameters.Params["Inertia"] = 0.5;
            parameters.Params["C1"] = 1.0;
            parameters.Params["C2"] = 1.0;

            int callCount = 0;
            void Progress(int iter, double val) => callCount++;

            var task = new PSOOptimizer().OptimizeAsync(obj, bounds, parameters, Progress);
            task.Wait();

            Assert.Equal(10, callCount);
        }

        [Fact]
        public void DE_ProgressCallback_Is_Called_For_Each_Generation()
        {
            Func<double[], double> obj = x => x[0] * x[0];
            var bounds = new[] { (Min: -1.0, Max: 1.0) };

            var parameters = new OptimizerParameters { MaxIterations = 8 };
            parameters.Params["PopSize"] = 10;
            parameters.Params["F"] = 0.8;
            parameters.Params["CR"] = 0.9;

            int callCount = 0;
            void Progress(int gen, double val) => callCount++;

            var task = new DEOptimizer().OptimizeAsync(obj, bounds, parameters, Progress);
            task.Wait();

            Assert.Equal(8, callCount);
        }

        [Fact]
        public void SA_ProgressCallback_Is_Called_At_Least_Once()
        {
            Func<double[], double> obj = x => x[0] * x[0];
            var bounds = new[] { (Min: -1.0, Max: 1.0) };

            var parameters = new OptimizerParameters { MaxIterations = 5 };
            parameters.Params["TempStart"] = 50.0;
            parameters.Params["TempEnd"] = 0.1;
            parameters.Params["CoolingRate"] = 0.5;
            parameters.Params["InnerIter"] = 2;

            int callCount = 0;
            void Progress(int iter, double val) => callCount++;

            var task = new SAOptimizer().OptimizeAsync(obj, bounds, parameters, Progress);
            task.Wait();

            Assert.True(callCount > 0, "SA progress callback was not called");
        }

        // Edge-case tests

        [Fact]
        public void ZeroDimension_Returns_EmptySolution()
        {
            var bounds = Array.Empty<(double Min, double Max)>();
            var parameters = new OptimizerParameters { MaxIterations = 10 };
            parameters.Params["SwarmSize"] = 10;
            parameters.Params["Inertia"] = 0.5;
            parameters.Params["C1"] = 1.0;
            parameters.Params["C2"] = 1.0;
            var result = RunOptimizer(new PSOOptimizer(), x => 0.0, bounds, parameters);
            Assert.Empty(result.BestSolution);
            Assert.Equal(0.0, result.BestValue);
        }

        [Theory]
        [InlineData(typeof(PSOOptimizer))]
        [InlineData(typeof(DEOptimizer))]
        [InlineData(typeof(SAOptimizer))]
        public void Respects_Bounds_When_MinEqualsMax(Type optimizerType)
        {
            var bounds = new[] { (Min: 3.14, Max: 3.14), (Min: -1.0, Max: -1.0) };
            var parameters = new OptimizerParameters { MaxIterations = 5 };
            if (optimizerType == typeof(PSOOptimizer))
            {
                parameters.Params["SwarmSize"] = 5;
                parameters.Params["Inertia"] = 0.5;
                parameters.Params["C1"] = 1.0;
                parameters.Params["C2"] = 1.0;
            }
            else if (optimizerType == typeof(DEOptimizer))
            {
                parameters.Params["PopSize"] = 5;
                parameters.Params["F"] = 0.7;
                parameters.Params["CR"] = 0.9;
            }
            else if (optimizerType == typeof(SAOptimizer))
            {
                parameters.Params["TempStart"] = 10.0;
                parameters.Params["TempEnd"] = 1.0;
                parameters.Params["CoolingRate"] = 0.9;
                parameters.Params["InnerIter"] = 1;
            }
            var optimizer = (IOptimizer)Activator.CreateInstance(optimizerType);
            var (sol, val) = RunOptimizer(optimizer, x => 42.0, bounds, parameters);
            for (int i = 0; i < sol.Length; i++)
            {
                Assert.Equal(bounds[i].Min, sol[i]);
            }
            Assert.Equal(42.0, val);
        }

        [Fact]
        public void InvalidParameters_DoesNotThrow()
        {
            var bounds = new[] { (Min: -1.0, Max: 1.0) };
            var parameters = new OptimizerParameters { MaxIterations = -1 };
            parameters.Params["PopSize"] = 5;
            parameters.Params["F"] = 0.7;
            parameters.Params["CR"] = 0.9;
            var result = RunOptimizer(new DEOptimizer(), x => 0.0, bounds, parameters);
            Assert.NotNull(result.BestSolution);
        }

        [Fact]
        public void OptimizeAsync_Propagates_Exception_From_Objective()
        {
            var bounds = new[] { (Min: -1.0, Max: 1.0) };
            var parameters = new OptimizerParameters { MaxIterations = 5 };
            parameters.Params["SwarmSize"] = 5;
            parameters.Params["Inertia"] = 0.5;
            parameters.Params["C1"] = 1.0;
            parameters.Params["C2"] = 1.0;
            Func<double[], double> badObj = x => throw new InvalidOperationException("fail");
            var ex = Assert.Throws<AggregateException>(
                () => RunOptimizer(new PSOOptimizer(), badObj, bounds, parameters)
            );
            Assert.Contains(ex.InnerExceptions, e => e is InvalidOperationException);
        }
    }
}
