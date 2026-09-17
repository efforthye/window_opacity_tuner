using WindowOpacityTuner.Core;
using WindowOpacityTuner.Localization;
using WindowOpacityTuner.UI;

namespace WindowOpacityTuner;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.SetCompatibleTextRenderingDefault(false);

        AppSettings settings = AppSettings.Load();
        Loc.Current = settings.Language;

        Application.ThreadException += (_, e) => ShowFatal(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => ShowFatal(e.ExceptionObject as Exception);

        Application.Run(new MainForm(settings));
    }

    private static void ShowFatal(Exception exception)
    {
        MessageBox.Show(
            exception?.ToString() ?? "Unknown error",
            "Window Opacity Tuner",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }
}
