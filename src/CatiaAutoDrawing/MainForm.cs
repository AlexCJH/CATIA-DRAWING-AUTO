using System;
using System.Windows.Forms;
using CatiaAutoDrawing.CatiaConnection;
using CatiaAutoDrawing.DrawingGenerator;
using CatiaAutoDrawing.Logging;
using CatiaAutoDrawing.ModelInspector;

namespace CatiaAutoDrawing;

/// <summary>
/// Role: Minimal user interface for CATIA connection checks, active document display, and log output.
/// TODO: Connect CATIA connection check button to a verified COM implementation.
/// TODO: Keep ActiveDocument handling limited to service result display.
/// TODO: Keep drawing generation button disabled until MVP steps are complete.
/// Warning: Do not call CATIA COM API directly from this form.
/// </summary>
public partial class MainForm : Form
{
    private readonly ICatiaConnectionService _catiaConnectionService;
    private readonly IModelInspector _modelInspector;
    private readonly IDrawingGenerator _drawingGenerator;
    private readonly ILogger _logger;

    public MainForm()
    {
        InitializeComponent();

        _logger = new FileLogger("logs", AppendLog);
        _catiaConnectionService = new CatiaConnectionService(_logger);
        _modelInspector = new ModelInspector.ModelInspector(_logger);
        _drawingGenerator = new DrawingGenerator.DrawingGenerator(_logger);
    }

    private void CheckConnectionButton_Click(object? sender, EventArgs e)
    {
        _logger.Info("CATIA connection check requested.");
        var result = _catiaConnectionService.CheckConnection();

        if (result.IsSuccess)
        {
            connectionStatusLabel.Text = "CATIA connection: Connected";
            _logger.Info("CATIA connection status displayed as connected.");
            return;
        }

        var message = result.ErrorMessage ?? "CATIA connection failed.";
        connectionStatusLabel.Text = $"CATIA connection: Failed - {message}";
        _logger.Warning(message);
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
        _logger.Info("Drawing generation requested.");

        var result = _drawingGenerator.Generate(new DrawingGenerationContext
        {
            OutputFolder = "output",
            EnablePdfExport = false
        });

        if (!result.IsSuccess)
        {
            _logger.Warning(result.ErrorMessage ?? "Drawing generation failed.");
        }
    }

    private void InspectModelButton_Click(object? sender, EventArgs e)
    {
        var result = _modelInspector.InspectActiveDocument();

        if (result.IsSuccess && result.Value is not null)
        {
            modelInspectionStatusLabel.Text =
                $"Model inspection: GS={FormatFound(result.Value.HasRequiredGeometrySet)}, " +
                $"MAIN_VIEW_PLANE={FormatFound(result.Value.HasMainViewPlane)}, " +
                $"TOP_DIRECTION={FormatFound(result.Value.HasTopDirection)}";
            return;
        }

        modelInspectionStatusLabel.Text = $"Model inspection: Failed - {result.ErrorMessage}";
    }

    private static string FormatFound(bool found) => found ? "Found" : "Missing";

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
