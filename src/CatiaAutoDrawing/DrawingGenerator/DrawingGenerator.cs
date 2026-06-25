using System;
using System.IO;
using System.Runtime.InteropServices;
using CatiaAutoDrawing.Core;
using CatiaAutoDrawing.Logging;
using CatiaAutoDrawing.Utils;
using CatiaAutoDrawing.ViewGenerator;

namespace CatiaAutoDrawing.DrawingGenerator;

/// <summary>
/// Role: Coordinates the template open and SaveAs drawing flow.
/// TODO: Add ViewGenerator, DimensionGenerator, and TitleBlockWriter calls only in their own steps.
/// </summary>
public sealed class DrawingGenerator : IDrawingGenerator
{
    private const string CatiaApplicationProgId = "CATIA.Application";

    private readonly ILogger _logger;
    private readonly IViewGenerator _viewGenerator;

    public DrawingGenerator(ILogger logger)
        : this(logger, new ViewGenerator.ViewGenerator(logger))
    {
    }

    public DrawingGenerator(ILogger logger, IViewGenerator viewGenerator)
    {
        _logger = logger;
        _viewGenerator = viewGenerator;
    }

    public Result<string> Generate(DrawingGenerationContext context)
    {
        _logger.Info("Drawing generation requested.");

        object? catiaApplication = null;
        object? activeDocument = null;
        object? documents = null;
        object? drawingDocument = null;

        try
        {
            catiaApplication = GetRunningCatiaApplication();
            activeDocument = GetComProperty(catiaApplication, "ActiveDocument");

            if (activeDocument is null)
            {
                const string message = "CATIA ActiveDocument does not exist.";
                _logger.Error(message);
                return Result<string>.Failure(message);
            }

            var activeDocumentName = Convert.ToString(GetComProperty(activeDocument, "Name")) ?? string.Empty;
            _logger.Info($"Active document: {activeDocumentName}");

            if (!IsSupportedActiveDocument(activeDocumentName))
            {
                var message = $"Active document is not a CATPart or CATProduct: {activeDocumentName}";
                _logger.Warning(message);
                return Result<string>.Failure(message);
            }

            var drawingSize = NormalizeDrawingSize(context.DrawingSize);
            _logger.Info($"Selected drawing size: {drawingSize}");

            if (!context.DrawingTemplates.TryGetValue(drawingSize, out var configuredTemplatePath) ||
                string.IsNullOrWhiteSpace(configuredTemplatePath))
            {
                var message = $"Drawing template is not configured for size: {drawingSize}";
                _logger.Error(message);
                return Result<string>.Failure(message);
            }

            var templatePath = Path.GetFullPath(ResolveRepositoryPath(configuredTemplatePath));
            _logger.Info($"Template path resolved: {GetDisplayPath(templatePath)}");

            if (!File.Exists(templatePath))
            {
                var message = $"Drawing template not found: {GetDisplayPath(templatePath)}";
                _logger.Error(message);
                return Result<string>.Failure(message);
            }

            documents = GetComProperty(catiaApplication, "Documents");
            if (documents is null)
            {
                const string message = "CATIA Documents collection does not exist.";
                _logger.Error(message);
                return Result<string>.Failure(message);
            }

            LogTemplateOpenDiagnostics(templatePath);

            _logger.Info("Opening drawing template...");
            try
            {
                drawingDocument = InvokeComMethod(documents, "Open", templatePath);
            }
            catch
            {
                LogTemplateOpenFailureHints();
                throw;
            }

            if (drawingDocument is null)
            {
                const string message = "Drawing template could not be opened.";
                _logger.Error(message);
                return Result<string>.Failure(message);
            }

            _logger.Info("Drawing template opened.");

            var frontViewResult = _viewGenerator.GenerateFrontView(
                drawingDocument,
                activeDocument,
                context.ViewSide,
                context.ViewRotation);

            Result projectionViewResult = Result.Success();
            if (frontViewResult.IsSuccess)
            {
                _logger.Info("STEP 4 succeeded.");
                projectionViewResult = _viewGenerator.GenerateProjectionViews(drawingDocument, activeDocument);
            }
            var outputFolder = ResolveOutputFolder(context.OutputFolder);
            Directory.CreateDirectory(outputFolder);

            var safeActiveName = SanitizeFileName(Path.GetFileNameWithoutExtension(activeDocumentName));
            var drawingFileName = $"{safeActiveName}_{drawingSize}.CATDrawing";
            var drawingPath = GetAvailableDrawingPath(Path.Combine(outputFolder, drawingFileName));

            _logger.Info($"Saving drawing as: {GetDisplayPath(drawingPath)}");
            InvokeComMethod(drawingDocument, "SaveAs", drawingPath);

            _logger.Info("Drawing template copy saved.");

            if (!frontViewResult.IsSuccess)
            {
                var message = frontViewResult.ErrorMessage ?? "Front view generation failed.";
                _logger.Warning("STEP 4 failed, but drawing template copy was saved.");
                return Result<string>.Failure(message);
            }

            if (!projectionViewResult.IsSuccess)
            {
                var message = projectionViewResult.ErrorMessage ?? "Projection view generation failed.";
                _logger.Warning("STEP 5A failed, but drawing template copy was saved.");
                return Result<string>.Failure(message);
            }

            _logger.Info("Projection view step completed.");

            return Result<string>.Success(drawingPath);
        }
        catch (Exception ex)
        {
            var rootCause = ExceptionUtils.GetRootCause(ex);
            var comErrorCode = ExceptionUtils.GetComErrorCode(ex);
            var message = $"Drawing generation failed: {ex.Message}";

            _logger.Error(ex, message);
            _logger.Error($"Root cause: {rootCause.Message}");
            if (!string.IsNullOrWhiteSpace(comErrorCode))
            {
                _logger.Error($"Root COM error: {comErrorCode}");
            }

            return Result<string>.Failure(message);
        }
        finally
        {
            ReleaseComObject(drawingDocument);
            ReleaseComObject(documents);
            ReleaseComObject(activeDocument);
            ReleaseComObject(catiaApplication);
        }
    }

    private static object GetRunningCatiaApplication()
    {
        var clsidResult = CLSIDFromProgID(CatiaApplicationProgId, out var clsid);
        if (clsidResult < 0)
        {
            throw new COMException($"CATIA COM ProgID not found: {CatiaApplicationProgId}", clsidResult);
        }

        GetActiveObject(ref clsid, IntPtr.Zero, out var catiaApplication);
        return catiaApplication;
    }

    private static bool IsSupportedActiveDocument(string activeDocumentName)
    {
        return activeDocumentName.EndsWith(".CATPart", StringComparison.OrdinalIgnoreCase) ||
               activeDocumentName.EndsWith(".CATProduct", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeDrawingSize(string drawingSize)
    {
        return string.IsNullOrWhiteSpace(drawingSize)
            ? "A3"
            : drawingSize.Trim().ToUpperInvariant();
    }

    private static string ResolveOutputFolder(string outputFolder)
    {
        var configuredFolder = string.IsNullOrWhiteSpace(outputFolder) ? "output" : outputFolder;
        return ResolveRepositoryPath(configuredFolder);
    }

    private static string ResolveRepositoryPath(string path)
    {
        var configuredPath = string.IsNullOrWhiteSpace(path) ? "." : path;
        if (Path.IsPathRooted(configuredPath))
        {
            return configuredPath;
        }

        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "CatiaAutoDrawing.sln")))
            {
                return Path.Combine(current.FullName, configuredPath);
            }

            current = current.Parent;
        }

        return Path.GetFullPath(configuredPath);
    }

    private void LogTemplateOpenDiagnostics(string templatePath)
    {
        var fileInfo = new FileInfo(templatePath);

        _logger.Info($"Template display path: {GetDisplayPath(templatePath)}");
        _logger.Info($"Template absolute path: {templatePath}");
        _logger.Info($"Template file exists: {fileInfo.Exists}");
        _logger.Info($"Template file size: {(fileInfo.Exists ? fileInfo.Length : 0)} bytes");
        _logger.Info($"Template read-only: {fileInfo.Exists && fileInfo.IsReadOnly}");
        _logger.Info($"Current directory: {Directory.GetCurrentDirectory()}");
        _logger.Info($"CATIA Documents.Open argument: {templatePath}");
    }

    private void LogTemplateOpenFailureHints()
    {
        _logger.Warning("CATIA Documents.Open failed. Check whether the template opens manually in CATIA.");
        _logger.Warning("If the path contains Korean characters, spaces, or special characters, test with a simple path such as C:\\CatiaAutoDrawingTemplates.");
        _logger.Warning("Check whether the template file is already open, locked, or saved in an incompatible CATIA version.");
    }

    private static string GetAvailableDrawingPath(string requestedPath)
    {
        if (!File.Exists(requestedPath))
        {
            return requestedPath;
        }

        var folder = Path.GetDirectoryName(requestedPath) ?? Directory.GetCurrentDirectory();
        var fileName = Path.GetFileNameWithoutExtension(requestedPath);
        return Path.Combine(folder, $"{fileName}_{DateTime.Now:yyyyMMddHHmmss}.CATDrawing");
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var chars = fileName.ToCharArray();
        for (var index = 0; index < chars.Length; index++)
        {
            if (Array.IndexOf(invalidChars, chars[index]) >= 0)
            {
                chars[index] = '_';
            }
        }

        var sanitized = new string(chars).Trim();
        return string.IsNullOrWhiteSpace(sanitized) ? "CATIA_DOCUMENT" : sanitized;
    }

    private static string GetDisplayPath(string path)
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var fullPath = Path.GetFullPath(path);

        return Path.GetRelativePath(currentDirectory, fullPath);
    }

    private static object? GetComProperty(object comObject, string propertyName)
    {
        return comObject.GetType().InvokeMember(
            propertyName,
            System.Reflection.BindingFlags.GetProperty,
            binder: null,
            target: comObject,
            args: null);
    }

    private static object? InvokeComMethod(object comObject, string methodName, params object[] args)
    {
        return comObject.GetType().InvokeMember(
            methodName,
            System.Reflection.BindingFlags.InvokeMethod,
            binder: null,
            target: comObject,
            args: args);
    }

    private static void ReleaseComObject(object? comObject)
    {
        if (comObject is not null && Marshal.IsComObject(comObject))
        {
            Marshal.ReleaseComObject(comObject);
        }
    }

    [DllImport("ole32.dll", CharSet = CharSet.Unicode)]
    private static extern int CLSIDFromProgID(string progId, out Guid clsid);

    [DllImport("oleaut32.dll", PreserveSig = false)]
    private static extern void GetActiveObject(
        ref Guid rclsid,
        IntPtr pvReserved,
        [MarshalAs(UnmanagedType.IUnknown)] out object ppunk);
}
