using System;
using System.Runtime.InteropServices;
using CatiaAutoDrawing.Core;
using CatiaAutoDrawing.Logging;

namespace CatiaAutoDrawing.CatiaConnection;

/// <summary>
/// Role: Connects to a running CATIA Application and reads ActiveDocument metadata.
/// TODO: Return an error when ActiveDocument does not exist.
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
            _logger.Error(result.ErrorMessage ?? "CATIA connection failed.");
            return result;
        }

        _logger.Info("CATIA connection check succeeded.");
        return Result.Success();
    }

    public Result<CatiaDocumentInfo> GetActiveDocumentInfo()
    {
        _logger.Info("ActiveDocument read started.");
        return Result<CatiaDocumentInfo>.Failure("TODO: ActiveDocument read is not implemented yet.");
    }

    private Result TryGetRunningCatiaApplication()
    {
        try
        {
            var clsidResult = CLSIDFromProgID(CatiaApplicationProgId, out var clsid);
            if (clsidResult < 0)
            {
                return Result.Failure($"CATIA COM ProgID not found: {CatiaApplicationProgId}");
            }

            GetActiveObject(ref clsid, IntPtr.Zero, out var catiaApplication);
            if (catiaApplication is null)
            {
                return Result.Failure("CATIA V5 is not running.");
            }

            if (Marshal.IsComObject(catiaApplication))
            {
                Marshal.ReleaseComObject(catiaApplication);
            }

            return Result.Success();
        }
        catch (COMException ex)
        {
            return Result.Failure($"CATIA V5 is not running or cannot be accessed. COM error: 0x{ex.ErrorCode:X8}");
        }
        catch (Exception ex)
        {
            return Result.Failure($"CATIA connection check failed: {ex.Message}");
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
