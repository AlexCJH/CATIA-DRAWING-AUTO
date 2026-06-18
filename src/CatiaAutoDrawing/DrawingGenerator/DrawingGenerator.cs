using System;
using System.IO;
using System.Runtime.InteropServices;
using CatiaAutoDrawing.Core;
using CatiaAutoDrawing.Logging;

namespace CatiaAutoDrawing.DrawingGenerator;

/// <summary>
/// Role: Coordinates the drawing document creation flow.
/// TODO: Add template opening after the template MVP step starts.
/// TODO: Add ViewGenerator, DimensionGenerator, and TitleBlockWriter calls only in their own steps.
/// </summary>
public sealed class DrawingGenerator : IDrawingGenerator
{
    private const string CatiaApplicationProgId = "CATIA.Application";

    private readonly ILogger _logger;

    public DrawingGenerator(ILogger logger)
    {
        _logger = logger;
    }

    public Result<string> Generate(DrawingGenerationContext context)
    {
        _logger.Info("Drawing document creation started.");

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
            if (!activeDocumentName.EndsWith(".CATPart", StringComparison.OrdinalIgnoreCase))
            {
                var message = $"Active document is not a CATPart: {activeDocumentName}";
                _logger.Warning(message);
                return Result<string>.Failure(message);
            }

            documents = GetComProperty(catiaApplication, "Documents");
            if (documents is null)
            {
                const string message = "CATIA Documents collection does not exist.";
                _logger.Error(message);
                return Result<string>.Failure(message);
            }

            drawingDocument = InvokeComMethod(documents, "Add", "Drawing");
            if (drawingDocument is null)
            {
                const string message = "New CATDrawing document could not be created.";
                _logger.Error(message);
                return Result<string>.Failure(message);
            }

            _logger.Info("New CATDrawing document created.");
            _logger.Warning("TODO: A3 sheet setup is skipped until CATIA V5 R35 sheet API is validated.");

            var outputFolder = ResolveOutputFolder(context.OutputFolder);
            Directory.CreateDirectory(outputFolder);

            var drawingFileName = Path.GetFileNameWithoutExtension(activeDocumentName) + ".CATDrawing";
            var drawingPath = GetAvailableDrawingPath(Path.Combine(outputFolder, drawingFileName));

            InvokeComMethod(drawingDocument, "SaveAs", drawingPath);

            _logger.Info($"Drawing saved: {Path.GetRelativePath(Directory.GetCurrentDirectory(), drawingPath)}");
            _logger.Info("Drawing generation step 3 succeeded.");

            return Result<string>.Success(drawingPath);
        }
        catch (COMException ex)
        {
            var message = $"Drawing generation failed. COM error: 0x{ex.ErrorCode:X8}";
            _logger.Error(message);
            return Result<string>.Failure(message);
        }
        catch (Exception ex)
        {
            var message = $"Drawing generation failed: {ex.Message}";
            _logger.Error(message);
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

    private static string ResolveOutputFolder(string outputFolder)
    {
        var configuredFolder = string.IsNullOrWhiteSpace(outputFolder) ? "output" : outputFolder;
        if (Path.IsPathRooted(configuredFolder))
        {
            return configuredFolder;
        }

        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "CatiaAutoDrawing.sln")))
            {
                return Path.Combine(current.FullName, configuredFolder);
            }

            current = current.Parent;
        }

        return Path.GetFullPath(configuredFolder);
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
