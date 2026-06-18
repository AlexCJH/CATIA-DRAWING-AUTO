using System;
using System.Windows.Forms;
using CatiaAutoDrawing.CatiaConnection;
using CatiaAutoDrawing.Logging;

namespace CatiaAutoDrawing;

/// <summary>
/// Role: Minimal user interface for CATIA connection checks, active document display, and log output.
/// TODO: Connect CATIA connection check button to a verified COM implementation.
/// TODO: Connect ActiveDocument read feature after CATIA connection service is implemented.
/// TODO: Keep drawing generation button disabled until MVP steps are complete.
/// Warning: Do not call CATIA COM API directly from this form.
/// </summary>
public partial class MainForm : Form
{
    private readonly ICatiaConnectionService _catiaConnectionService;
    private readonly ILogger _logger;

    public MainForm()
    {
        InitializeComponent();

        _logger = new FileLogger("logs", AppendLog);
        _catiaConnectionService = new CatiaConnectionService(_logger);
    }

    private void CheckConnectionButton_Click(object? sender, EventArgs e)
    {
        _logger.Info("CATIA connection check requested.");
        var result = _catiaConnectionService.CheckConnection();
        connectionStatusLabel.Text = result.IsSuccess ? "CATIA connection: Ready" : "CATIA connection: Not implemented";

        if (!result.IsSuccess)
        {
            _logger.Warning(result.ErrorMessage ?? "CATIA connection check is not available.");
        }
    }

    private void ReadActiveDocumentButton_Click(object? sender, EventArgs e)
    {
        _logger.Info("Active document read requested.");
        var result = _catiaConnectionService.GetActiveDocumentInfo();

        if (!result.IsSuccess || result.Value is null)
        {
            activeDocumentNameLabel.Text = "Active document: -";
            activeDocumentTypeLabel.Text = "Document type: -";
            _logger.Warning(result.ErrorMessage ?? "Active document read is not available.");
            return;
        }

        activeDocumentNameLabel.Text = $"Active document: {result.Value.Name}";
        activeDocumentTypeLabel.Text = $"Document type: {result.Value.DocumentType}";
        _logger.Info($"Active document found: {result.Value.Name}");
    }

    private void RunDrawingButton_Click(object? sender, EventArgs e)
    {
        _logger.Warning("Drawing generation is disabled in the initial architecture phase.");
    }

    private void AppendLog(string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => AppendLog(message));
            return;
        }

        logTextBox.AppendText(message + Environment.NewLine);
    }
}
