using System;
using System.Runtime.InteropServices;
using CatiaAutoDrawing.Core;
using CatiaAutoDrawing.Logging;

namespace CatiaAutoDrawing.CatiaConnection;

/// <summary>
/// Role: Connects to a running CATIA Application and reads ActiveDocument metadata.
/// TODO: Add more document metadata only when later MVP steps require it.
/// </summary>
public sealed class CatiaConnectionService : ICatiaConnectionService
{
    private const string CatiaApplicationProgId = "CATIA.Application";

    private readonly ILogger _logger;

    public CatiaConnectionService(ILogger logger)
    {
        _logger = logger;
    }

    public Result CheckConnection()
    {
        _logger.Info("CATIA connection check started.");

        var result = TryGetRunningCatiaApplication();

        if (!result.IsSuccess)
        {
            var message = result.ErrorMessage ?? "CATIA connection failed.";
            _logger.Error(message);
            return Result.Failure(message);
        }

        ReleaseComObject(result.Value);
        _logger.Info("CATIA connection check succeeded.");
        return Result.Success();
    }

    public Result<CatiaDocumentInfo> GetActiveDocumentInfo()
    {
        _logger.Info("ActiveDocument read started.");

        object? catiaApplication = null;
        object? activeDocument = null;

        try
        {
            var catiaResult = TryGetRunningCatiaApplication();
            if (!catiaResult.IsSuccess || catiaResult.Value is null)
            {
                var message = catiaResult.ErrorMessage ?? "CATIA V5 is not running.";
                _logger.Error(message);
                return Result<CatiaDocumentInfo>.Failure(message);
            }

            catiaApplication = catiaResult.Value;
            activeDocument = GetComProperty(catiaApplication, "ActiveDocument");

            if (activeDocument is null)
            {
                const string message = "CATIA ActiveDocument does not exist.";
                _logger.Error(message);
                return Result<CatiaDocumentInfo>.Failure(message);
            }

            var documentName = Convert.ToString(GetComProperty(activeDocument, "Name")) ?? string.Empty;

            if (string.IsNullOrWhiteSpace(documentName))
            {
                const string message = "CATIA ActiveDocument.Name is empty.";
                _logger.Error(message);
                return Result<CatiaDocumentInfo>.Failure(message);
            }

            var documentType = GetDocumentTypeFromName(documentName);

            _logger.Info($"Active document name: {documentName}");
            _logger.Info($"Document type: {documentType}");

            return Result<CatiaDocumentInfo>.Success(new CatiaDocumentInfo(documentName, documentType));
        }
        catch (COMException ex)
        {
            var message = $"ActiveDocument read failed. COM error: 0x{ex.ErrorCode:X8}";
            _logger.Error(message);
            return Result<CatiaDocumentInfo>.Failure(message);
        }
        catch (Exception ex)
        {
            var message = $"ActiveDocument read failed: {ex.Message}";
            _logger.Error(message);
            return Result<CatiaDocumentInfo>.Failure(message);
        }
        finally
        {
            ReleaseComObject(activeDocument);
            ReleaseComObject(catiaApplication);
        }
    }

    private Result<object> TryGetRunningCatiaApplication()
    {
        try
        {
            var clsidResult = CLSIDFromProgID(CatiaApplicationProgId, out var clsid);
            if (clsidResult < 0)
            {
                return Result<object>.Failure($"CATIA COM ProgID not found: {CatiaApplicationProgId}");
            }

            GetActiveObject(ref clsid, IntPtr.Zero, out var catiaApplication);
            if (catiaApplication is null)
            {
                return Result<object>.Failure("CATIA V5 is not running.");
            }

            return Result<object>.Success(catiaApplication);
        }
        catch (COMException ex)
        {
            return Result<object>.Failure($"CATIA V5 is not running or cannot be accessed. COM error: 0x{ex.ErrorCode:X8}");
        }
        catch (Exception ex)
        {
            return Result<object>.Failure($"CATIA connection check failed: {ex.Message}");
        }
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

    private static string GetDocumentTypeFromName(string documentName)
    {
        if (documentName.EndsWith(".CATPart", StringComparison.OrdinalIgnoreCase))
        {
            return "Part";
        }

        if (documentName.EndsWith(".CATProduct", StringComparison.OrdinalIgnoreCase))
        {
            return "Product";
        }

        if (documentName.EndsWith(".CATDrawing", StringComparison.OrdinalIgnoreCase))
        {
            return "Drawing";
        }

        return "Unknown";
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
