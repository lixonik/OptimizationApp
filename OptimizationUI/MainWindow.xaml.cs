using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.WPF;
using Microsoft.Win32;
using OptimizationCore;
using Expr = NCalc.Expression;

namespace OptimizationUI
{
    public partial class MainWindow : Window
    {
        private OptimizerParameters _optParams = new();
        private Func<double[], double> _objective = null!;
        private (double Min, double Max)[] _bounds = Array.Empty<(double, double)>();
        private readonly ObservableCollection<double> _progressValues = new();
        private readonly List<double[]> _solutions = new();
        public ISeries[] SeriesCollection { get; set; }

        private readonly Dictionary<string, List<ParamItem>> _algoParams = new()
        {
            ["PSO"] = new()
            {
                new ParamItem("MaxIterations", 100),
                new ParamItem("SwarmSize", 30),
                new ParamItem("Inertia", 0.7),
                new ParamItem("C1", 1.5),
                new ParamItem("C2", 1.5),
            },
            ["SA"] = new()
            {
                new ParamItem("MaxIterations", 1000),
                new ParamItem("TempStart", 100),
                new ParamItem("TempEnd", 1),
                new ParamItem("CoolingRate", 0.95),
                new ParamItem("InnerIter", 10),
            },
            ["DE"] = new()
            {
                new ParamItem("MaxIterations", 200),
                new ParamItem("PopSize", 50),
                new ParamItem("F", 0.8),
                new ParamItem("CR", 0.9),
            },
        };

        public MainWindow()
        {
            InitializeComponent();

            AlgoCombo.ItemsSource = _algoParams.Keys;
            AlgoCombo.SelectedIndex = 0;

            ParamsGrid.ItemsSource = _algoParams[AlgoCombo.Text];
            BoundsGrid.ItemsSource = new List<BoundItem>
            {
                new() { Min = -5, Max = 5 },
            };

            SeriesCollection = new ISeries[]
            {
                new LineSeries<double>
                {
                    Name = "Best Value",
                    Values = _progressValues,
                    GeometrySize = 4,
                },
            };
            DataContext = this;
        }

        private void AlgoCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AlgoCombo.SelectedItem is string algo)
                ParamsGrid.ItemsSource = _algoParams[algo];
        }

        private void PrepareSettings()
        {
            _optParams = new OptimizerParameters
            {
                Algorithm = AlgoCombo.Text,
                MaxIterations = (int)
                    (
                        (ParamItem)
                            ParamsGrid.Items.Cast<ParamItem>().First(p => p.Name == "MaxIterations")
                    ).Value,
            };

            _optParams.Params.Clear();
            foreach (ParamItem pi in ParamsGrid.Items.OfType<ParamItem>())
                _optParams.Params[pi.Name] = pi.Value;

            _bounds = BoundsGrid.Items.OfType<BoundItem>().Select(b => (b.Min, b.Max)).ToArray();

            var expr = new Expr(FuncBox.Text);
            bool maximize = MaximizeBox.IsChecked == true;

            _objective = x =>
            {
                for (int i = 0; i < x.Length; i++)
                    expr.Parameters[$"x{i}"] = x[i];
                double value = Convert.ToDouble(expr.Evaluate());
                return maximize ? -value : value;
            };
        }

        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool maximize = MaximizeBox.IsChecked == true;

                PrepareSettings();
                _progressValues.Clear();
                _solutions.Clear();
                Log("Start");

                IOptimizer opt = _optParams.Algorithm switch
                {
                    "PSO" => new PSOOptimizer(),
                    "SA" => new SAOptimizer(),
                    "DE" => new DEOptimizer(),
                    _ => throw new(),
                };

                var result = await opt.OptimizeAsync(
                    _objective,
                    _bounds,
                    _optParams,
                    (iter, bestVal) =>
                        Dispatcher.Invoke(() =>
                        {
                            double shownVal = maximize ? -bestVal : bestVal;
                            _progressValues.Add(shownVal);
                            Log($"Iter {iter}: {shownVal:F4}");
                        })
                );

                _solutions.Add(result.BestSolution);
                double finalValue = maximize ? -result.BestValue : result.BestValue;
                Log($"Done! Best = {finalValue:F4}");
            }
            catch (Exception ex)
            {
                Log($"Error: {ex.Message}");
            }
        }

        private void Stop_Click(object sender, RoutedEventArgs e) =>
            Log("Stop pressed (не поддерживается).");

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog { Filter = "CSV|*.csv|JSON|*.json" };
            if (dlg.ShowDialog() != true)
                return;

            if (dlg.FileName.EndsWith(".csv"))
            {
                using var sw = new StreamWriter(dlg.FileName);
                sw.WriteLine("Iteration,BestValue");
                for (int i = 0; i < _progressValues.Count; i++)
                    sw.WriteLine($"{i + 1},{_progressValues[i]}");
            }
            else
            {
                var maximize = MaximizeBox.IsChecked == true;
                var exportObj = new
                {
                    Progress = _progressValues.ToArray(),
                    Best = maximize ? _solutions.LastOrDefault() : _solutions.LastOrDefault(),
                    BestValue = maximize
                        ? -_progressValues.LastOrDefault()
                        : _progressValues.LastOrDefault(),
                };
                File.WriteAllText(
                    dlg.FileName,
                    JsonSerializer.Serialize(
                        exportObj,
                        options: new JsonSerializerOptions { WriteIndented = true }
                    )
                );
            }
            Log($"Exported to {dlg.FileName}");
        }

        private void Log(string msg) =>
            Dispatcher.Invoke(() => LogBox.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {msg}"));
    }

    public class ParamItem
    {
        public string Name { get; }
        public double Value { get; set; }

        public ParamItem(string name, double value)
        {
            Name = name;
            Value = value;
        }
    }

    public class BoundItem
    {
        public double Min { get; set; }
        public double Max { get; set; }
    }
}
