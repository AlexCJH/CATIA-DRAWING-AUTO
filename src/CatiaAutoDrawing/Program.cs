using System;
using System.Windows.Forms;

namespace CatiaAutoDrawing;

/// <summary>
/// Role: Application entry point that starts MainForm.
/// TODO: Add global exception handling.
/// TODO: Add configuration loading failure handling.
/// </summary>
internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
