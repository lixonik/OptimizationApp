using System;
using System.Windows;

namespace OptimizationUI
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка при старте:\n{ex.GetType().Name}: {ex.Message}\n\n{ex.StackTrace}",
                    "Fatal startup error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                Shutdown(-1);
            }
        }
    }
}
