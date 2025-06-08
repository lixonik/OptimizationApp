using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.WPF;
using Microsoft.Win32;
using OptimizationCore;
using OptimizationUI.Controls;
using OptimizationUI.Solvers;
using Expr = NCalc.Expression;

namespace OptimizationUI
{
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<double> _progressValues = new();
        public ISeries[] SeriesCollection { get; set; }

        private OptimizerParameters _optParams = new();
        private Func<double[], double> _objective = null!;
        private (double Min, double Max)[] _bounds = Array.Empty<(double, double)>();
        private CancellationTokenSource _cts = new();

        // контрол для текущей задачи графовой вкладки
        private IGraphParams? _currentGraphParams;

        // алгоритмы математической вкладки
        private readonly Dictionary<string, List<ParamItem>> _algoParams = new()
        {
            ["PSO"] = new()
            {
                new("MaxIterations", 100),
                new("SwarmSize", 30),
                new("Inertia", 0.7),
                new("C1", 1.5),
                new("C2", 1.5),
            },
            ["SA"] = new()
            {
                new("MaxIterations", 1000),
                new("TempStart", 100),
                new("TempEnd", 1),
                new("CoolingRate", 0.95),
                new("InnerIter", 10),
            },
            ["DE"] = new()
            {
                new("MaxIterations", 200),
                new("PopSize", 50),
                new("F", 0.8),
                new("CR", 0.9),
            },
        };

        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;

            SeriesCollection = new ISeries[]
            {
                new LineSeries<double>
                {
                    Name = "Best Value",
                    Values = _progressValues,
                    GeometrySize = 4,
                },
            };

            InitMathTab();
            // try
            // {
            InitGraphTab();
            // }
            // catch (Exception ex)
            // {
            //     MessageBox.Show(
            //         $"InitGraphTab threw:\n{ex.GetType().Name}: {ex.Message}\n\n{ex.StackTrace}",
            //         "InitGraphTab Error",
            //         MessageBoxButton.OK,
            //         MessageBoxImage.Error
            //     );
            // }
        }

        private void InitMathTab()
        {
            // алгоритмы
            AlgoCombo.ItemsSource = _algoParams.Keys;
            AlgoCombo.SelectedIndex = 0;

            // параметры алгоритма
            ParamsGrid.ItemsSource = _algoParams[AlgoCombo.Text];

            // bounds по умолчанию
            BoundsGrid.ItemsSource = new List<BoundItem>
            {
                new() { Min = -5, Max = 5 },
            };
        }

        private void InitGraphTab()
        {
            // список задач
            GraphTaskCombo.ItemsSource = new[]
            {
                "Поиск кратчайшего пути",
                "Задача о раскраске",
                "Нахождение Эйлерова пути",
                "Нахождение Эйлерова цикла",
                "Нахождение Гамильтонова пути",
                "Нахождение Гамильтонова цикла",
            };
            GraphTaskCombo.SelectionChanged += GraphTaskCombo_SelectionChanged;
            GraphTaskCombo.SelectedIndex = 0;
        }

        private void GraphTaskCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // подгружаем нужный UserControl в ContentControl GraphParamsHost
            _currentGraphParams = GraphTaskCombo.SelectedIndex switch
            {
                0 => new ShortestPathParamsControl(), // нужно реализовать
                1 => new ColoringParamsControl(),
                2 => new EulerPathParamsControl(),
                3 => new EulerCycleParamsControl(),
                4 => new HamiltonianPathParamsControl(),
                5 => new HamiltonianCycleParamsControl(),
                _ => null,
            };

            GraphParamsHost.Content = _currentGraphParams as UserControl;
        }

        private void AlgoCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AlgoCombo.SelectedItem is string algo)
                ParamsGrid.ItemsSource = _algoParams[algo];
        }

        private void PrepareMathSettings()
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
                try
                {
                    for (int i = 0; i < x.Length; i++)
                        expr.Parameters[$"x{i}"] = x[i];

                    double val = Convert.ToDouble(expr.Evaluate());
                    return maximize ? -val : val;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Ошибка в целевой функции: {ex.Message}",
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                    throw;
                }
            };
        }

        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            // Завершаем предыдущие графовые задачи
            _cts.Cancel();
            _cts = new CancellationTokenSource();

            _progressValues.Clear();

            if (MainTabControl.SelectedIndex == 0)
            {
                // — математическая вкладка
                PrepareMathSettings();
                Log("Start math optimization");

                IOptimizer opt = _optParams.Algorithm switch
                {
                    "PSO" => new PSOOptimizer(),
                    "SA" => new SAOptimizer(),
                    "DE" => new DEOptimizer(),
                    _ => throw new InvalidOperationException(),
                };

                var result = await opt.OptimizeAsync(
                    _objective,
                    _bounds,
                    _optParams,
                    (it, bestVal) =>
                    {
                        double shown = (MaximizeBox.IsChecked == true) ? -bestVal : bestVal;
                        Dispatcher.Invoke(() =>
                        {
                            _progressValues.Add(shown);
                            Log($"Iter {it}: {shown:F4}");
                        });
                    },
                    _cts.Token
                );

                Log(
                    $"Done! Best = {(MaximizeBox.IsChecked == true ? -result.BestValue : result.BestValue):F4}"
                );
            }
            else
            {
                // — графовая вкладка
                if (_currentGraphParams is null)
                {
                    MessageBox.Show("Не выбраны параметры графовой задачи");
                    return;
                }

                try
                {
                    var graph = _currentGraphParams.LoadGraph();
                    var solver = _currentGraphParams.CreateSolver(_cts.Token);
                    solver.Progress += (it, state) =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            // здесь можно обновлять визуализацию state
                            Log($"Graph iter {it}");
                        });
                    };
                    Log("Start graph task");
                    await Task.Run(() => solver.Solve(graph), _cts.Token);
                    Log("Graph task done");
                }
                catch (OperationCanceledException)
                {
                    Log("Graph task cancelled");
                }
                catch (Exception ex)
                {
                    Log($"Error: {ex.Message}");
                }
            }
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            _cts.Cancel();
            Log("Stop pressed, cancelling...");
        }

        private void Export_Click(object s, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "CSV|*.csv|JSON|*.json" };
            if (dlg.ShowDialog() != true)
                return;
            if (dlg.FileName.EndsWith(".csv"))
            {
                using var sw = new System.IO.StreamWriter(dlg.FileName);
                sw.WriteLine("Iter,Value");
                for (int i = 0; i < _progressValues.Count; i++)
                    sw.WriteLine($"{i + 1},{_progressValues[i]}");
            }
            else
            {
                bool max = MaximizeBox.IsChecked == true;
                var obj = new
                {
                    Progress = _progressValues.ToArray(),
                    Best = max ? _progressValues.Max() : _progressValues.Min(),
                };
                System.IO.File.WriteAllText(
                    dlg.FileName,
                    JsonSerializer.Serialize(
                        obj,
                        new JsonSerializerOptions { WriteIndented = true }
                    )
                );
            }
            Log($"Экспорт: {dlg.FileName}");
        }

        private void Log(string msg) =>
            Dispatcher.Invoke(() => LogBox.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {msg}"));
    }

    // вспомогательные классы
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
