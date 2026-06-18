using System;
using System.Runtime.InteropServices;
using CatiaAutoDrawing.Core;
using CatiaAutoDrawing.Logging;

namespace CatiaAutoDrawing.ModelInspector;

/// <summary>
/// Role: Inspects CATPart/CATProduct drawing markers such as GS_DRAWING_INFO.
/// TODO: Expand marker search rules only after CATIA V5 R35 sample model validation.
/// </summary>
public sealed class ModelInspector : IModelInspector
{
    private const string CatiaApplicationProgId = "CATIA.Application";
    private const string RequiredGeoSetName = "GS_DRAWING_INFO";
    private const string MainViewPlaneName = "MAIN_VIEW_PLANE";
    private const string TopDirectionName = "TOP_DIRECTION";

    private readonly ILogger _logger;

    public ModelInspector(ILogger logger)
    {
        _logger = logger;
    }

    public Result<InspectionReport> InspectActiveDocument()
    {
        object? catiaApplication = null;
        object? activeDocument = null;

        try
        {
            catiaApplication = GetRunningCatiaApplication();
            activeDocument = GetComProperty(catiaApplication, "ActiveDocument");

            if (activeDocument is null)
            {
                const string message = "CATIA ActiveDocument does not exist.";
                _logger.Error(message);
                return Result<InspectionReport>.Failure(message);
            }

            return Inspect(activeDocument);
        }
        catch (COMException ex)
        {
            var message = $"Model inspection failed. COM error: 0x{ex.ErrorCode:X8}";
            _logger.Error(message);
            return Result<InspectionReport>.Failure(message);
        }
        catch (Exception ex)
        {
            var message = $"Model inspection failed: {ex.Message}";
            _logger.Error(message);
            return Result<InspectionReport>.Failure(message);
        }
        finally
        {
            ReleaseComObject(activeDocument);
            ReleaseComObject(catiaApplication);
        }
    }

    public Result<InspectionReport> Inspect(object catiaDocument)
    {
        _logger.Info("Model inspection started.");

        var report = new InspectionReport();
        var documentName = Convert.ToString(GetComProperty(catiaDocument, "Name")) ?? string.Empty;

        if (!documentName.EndsWith(".CATPart", StringComparison.OrdinalIgnoreCase))
        {
            var message = $"Active document is not a CATPart: {documentName}";
            report.Messages.Add(message);
            _logger.Warning(message);
            return Result<InspectionReport>.Failure(message);
        }

        var part = GetComProperty(catiaDocument, "Part");
        var hybridBodies = part is null ? null : GetComProperty(part, "HybridBodies");
        var requiredGeoSet = hybridBodies is null ? null : FindHybridBodyByName(hybridBodies, RequiredGeoSetName);

        if (requiredGeoSet is null)
        {
            var message = $"Required geometrical set not found: {RequiredGeoSetName}";
            report.Messages.Add(message);
            _logger.Warning(message);
            return Result<InspectionReport>.Failure(message);
        }

        report.HasRequiredGeometrySet = true;
        report.Markers.Add(new DrawingMarkerInfo(RequiredGeoSetName, "GeometricalSet", true));
        _logger.Info($"Required geometrical set found: {RequiredGeoSetName}");

        report.HasMainViewPlane = FindMarkerByName(requiredGeoSet, MainViewPlaneName);
        LogMarkerResult(report, MainViewPlaneName, report.HasMainViewPlane);

        report.HasTopDirection = FindMarkerByName(requiredGeoSet, TopDirectionName);
        LogMarkerResult(report, TopDirectionName, report.HasTopDirection);

        if (!report.HasMainViewPlane || !report.HasTopDirection)
        {
            return Result<InspectionReport>.Failure("Required model markers are missing.");
        }

        _logger.Info("Model inspection succeeded.");
        return Result<InspectionReport>.Success(report);
    }

    private void LogMarkerResult(InspectionReport report, string markerName, bool found)
    {
        if (found)
        {
            report.Markers.Add(new DrawingMarkerInfo(markerName, "Marker", true));
            _logger.Info($"Required marker found: {markerName}");
            return;
        }

        var message = $"Required marker not found: {markerName}";
        report.Messages.Add(message);
        _logger.Warning(message);
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

    private static object? FindHybridBodyByName(object hybridBodies, string targetName)
    {
        var count = Convert.ToInt32(GetComProperty(hybridBodies, "Count"));

        for (var index = 1; index <= count; index++)
        {
            var hybridBody = InvokeComMethod(hybridBodies, "Item", index);
            if (hybridBody is null)
            {
                continue;
            }

            var name = Convert.ToString(GetComProperty(hybridBody, "Name"));

            if (string.Equals(name, targetName, StringComparison.OrdinalIgnoreCase))
            {
                return hybridBody;
            }

            var childHybridBodies = TryGetComProperty(hybridBody, "HybridBodies");
            if (childHybridBodies is not null)
            {
                var found = FindHybridBodyByName(childHybridBodies, targetName);
                if (found is not null)
                {
                    return found;
                }
            }

            ReleaseComObject(hybridBody);
        }

        return null;
    }

    private static bool FindMarkerByName(object hybridBody, string markerName)
    {
        if (ContainsNamedItem(TryGetComProperty(hybridBody, "HybridShapes"), markerName))
        {
            return true;
        }

        var childHybridBodies = TryGetComProperty(hybridBody, "HybridBodies");
        if (childHybridBodies is null)
        {
            return false;
        }

        var count = Convert.ToInt32(GetComProperty(childHybridBodies, "Count"));
        for (var index = 1; index <= count; index++)
        {
            var childHybridBody = InvokeComMethod(childHybridBodies, "Item", index);
            if (childHybridBody is null)
            {
                continue;
            }

            if (FindMarkerByName(childHybridBody, markerName))
            {
                ReleaseComObject(childHybridBody);
                return true;
            }

            ReleaseComObject(childHybridBody);
        }

        return false;
    }

    private static bool ContainsNamedItem(object? collection, string itemName)
    {
        if (collection is null)
        {
            return false;
        }

        var count = Convert.ToInt32(GetComProperty(collection, "Count"));
        for (var index = 1; index <= count; index++)
        {
            var item = InvokeComMethod(collection, "Item", index);
            if (item is null)
            {
                continue;
            }

            var name = Convert.ToString(GetComProperty(item, "Name"));
            ReleaseComObject(item);

            if (string.Equals(name, itemName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
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

    private static object? TryGetComProperty(object comObject, string propertyName)
    {
        try
        {
            return GetComProperty(comObject, propertyName);
        }
        catch (COMException)
        {
            return null;
        }
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
