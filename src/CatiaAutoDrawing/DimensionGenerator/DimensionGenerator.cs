using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using CatiaAutoDrawing.Core;
using CatiaAutoDrawing.Logging;
using CatiaAutoDrawing.Utils;

namespace CatiaAutoDrawing.DimensionGenerator;

/// <summary>
/// Role: Detects candidate dimension targets and, in later steps, will create dimensions.
/// </summary>
public sealed class DimensionGenerator : IDimensionGenerator
{
    private readonly ILogger _logger;

    public DimensionGenerator(ILogger logger)
    {
        _logger = logger;
    }

    public DimensionPlan CreateDimensionPlan(object catiaDocument)
    {
        throw new NotImplementedException("Dimension generation is not implemented yet.");
    }

    public Result DetectColorBasedTargets(object sourceDocument)
    {
        _logger.Info("STEP 6A color based dimension target detection started.");
        _logger.Info("Dimension target detection scope: GS_DIMENSION_TARGET");
        _logger.Info("STEP 6A geometry type detection started.");

        try
        {
            var part = TryGetComProperty(sourceDocument, "Part");
            if (part is null)
            {
                const string message = "STEP 6A requires a CATPart source document.";
                _logger.Warning(message);
                return Result.Failure(message);
            }

            var selection = TryGetComProperty(sourceDocument, "Selection");
            var hybridBodies = TryGetComProperty(part, "HybridBodies");
            var dimensionTargetSet = hybridBodies is null ? null : FindHybridBodyByName(hybridBodies, "GS_DIMENSION_TARGET");
            if (dimensionTargetSet is null)
            {
                const string message = "GS_DIMENSION_TARGET not found. STEP 6A skipped.";
                _logger.Warning(message);
                return Result.Failure(message);
            }

            _logger.Info("GS_DIMENSION_TARGET found.");

            var candidates = new List<CandidateInfo>();
            CollectHybridShapeCandidates(dimensionTargetSet, "GS_DIMENSION_TARGET", candidates);
            if (candidates.Count == 0)
            {
                const string message = "No dimension target candidates found in GS_DIMENSION_TARGET.";
                _logger.Warning(message);
                _logger.Info("STEP 6A color based dimension target detection completed.");
                return Result.Failure(message);
            }

            var spaWorkbench = TryGetSpaWorkbench(sourceDocument);
            var detectedCount = 0;
            var colorConfirmedCount = 0;
            var geometryConfirmedCount = 0;

            foreach (var candidateInfo in candidates)
            {
                detectedCount++;
                _logger.Info("Dimension target candidate found.");

                var candidate = candidateInfo.Candidate;
                var candidateType = candidate.GetType().FullName ?? candidate.GetType().Name;
                var candidateName = Convert.ToString(TryGetComProperty(candidate, "Name")) ?? string.Empty;
                var isHybridShape = candidateType.Contains("HybridShape", StringComparison.OrdinalIgnoreCase);

                _logger.Info($"Candidate diagnostic started: {candidateName}");
                _logger.Info($"Candidate name: {candidateName}");
                _logger.Info($"Candidate COM type: {candidateType}");
                _logger.Info($"Candidate CATIA name: {candidateName}");
                _logger.Info($"Candidate search path: {candidateInfo.SearchPath}");
                _logger.Info($"Candidate automation type: {candidateType}");
                _logger.Info($"Candidate HybridShape: {isHybridShape}");
                _logger.Info($"Candidate type flags: {BuildTypeFlags(candidateType, candidateName)}");
                _logger.Info("Candidate color read attempt started.");

                if (TryReadCandidateColor(selection, candidate, out var red, out var green, out var blue))
                {
                    colorConfirmedCount++;
                    _logger.Info($"Candidate color RGB: R={red}, G={green}, B={blue}");
                    _logger.Info($"Candidate color group: {GetColorGroup(red, green, blue)}");
                }

                var geometryType = DetectGeometryType(part, spaWorkbench, candidateInfo, candidateType, candidateName);
                if (string.Equals(geometryType, "Unknown", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.Warning("Geometry type could not be determined after probes.");
                }
                else
                {
                    geometryConfirmedCount++;
                }

                _logger.Info($"Candidate geometry type inferred: {geometryType}");
                _logger.Info($"Candidate geometry type: {geometryType}");
            }

            _logger.Info($"STEP 6A summary: candidates={detectedCount}, colorConfirmed={colorConfirmedCount}, geometryConfirmed={geometryConfirmedCount}");
            _logger.Info("STEP 6A color based dimension target detection completed.");
            return Result.Success();
        }
        catch (Exception ex)
        {
            var rootCause = ExceptionUtils.GetRootCause(ex);
            var comErrorCode = ExceptionUtils.GetComErrorCode(ex);
            var message = $"STEP 6A color based dimension target detection failed: {ex.Message}";

            _logger.Warning(message);
            _logger.Warning($"Root cause: {rootCause.Message}");
            if (!string.IsNullOrWhiteSpace(comErrorCode))
            {
                _logger.Warning($"Root COM error: {comErrorCode}");
            }

            return Result.Failure(message);
        }
    }

    private string DetectGeometryType(object part, object? spaWorkbench, CandidateInfo candidateInfo, string candidateType, string candidateName)
    {
        _logger.Info($"Candidate diagnostic started: {candidateName}");
        _logger.Info("Candidate reference creation started.");

        object? reference;
        try
        {
            reference = InvokeComMethod(part, "CreateReferenceFromObject", candidateInfo.Candidate);
        }
        catch (Exception ex)
        {
            LogWarningWithException($"Candidate reference creation failed: {candidateName}", ex);
            return DetermineGeometryType(candidateType, candidateName, new GeometryProbeResult());
        }

        if (reference is null)
        {
            _logger.Warning($"Candidate reference creation failed: {candidateName}");
            return DetermineGeometryType(candidateType, candidateName, new GeometryProbeResult());
        }

        _logger.Info("Candidate reference creation succeeded.");

        if (spaWorkbench is null)
        {
            _logger.Warning("Candidate measurable extraction failed: SPAWorkbench is not available.");
            return DetermineGeometryType(candidateType, candidateName, new GeometryProbeResult());
        }

        _logger.Info("Candidate measurable extraction started.");

        object? measurable;
        try
        {
            measurable = InvokeComMethod(spaWorkbench, "GetMeasurable", reference);
        }
        catch (Exception ex)
        {
            LogWarningWithException($"Candidate measurable extraction failed: {candidateName}", ex);
            return DetermineGeometryType(candidateType, candidateName, new GeometryProbeResult());
        }

        if (measurable is null)
        {
            _logger.Warning($"Candidate measurable extraction failed: {candidateName}");
            return DetermineGeometryType(candidateType, candidateName, new GeometryProbeResult());
        }

        _logger.Info("Candidate measurable extraction succeeded.");
        _logger.Info($"Candidate measurable type: {measurable.GetType().FullName ?? measurable.GetType().Name}");

        var probeResult = ProbeGeometryMethods(measurable);
        return DetermineGeometryType(candidateType, candidateName, probeResult);
    }

    private GeometryProbeResult ProbeGeometryMethods(object measurable)
    {
        var result = new GeometryProbeResult();

        result.PlaneSuccess = TryProbePlane(measurable);
        _logger.Info($"Geometry probe GetPlane: {(result.PlaneSuccess ? "success" : "fail")}");

        result.DirectionSuccess = TryProbeDirection(measurable);
        _logger.Info($"Geometry probe GetDirection: {(result.DirectionSuccess ? "success" : "fail")}");

        result.PointSuccess = TryProbePoint(measurable);
        _logger.Info($"Geometry probe GetPoint: {(result.PointSuccess ? "success" : "fail")}");

        result.AxisSuccess = TryProbeAxis(measurable);
        _logger.Info($"Geometry probe GetAxis: {(result.AxisSuccess ? "success" : "fail")}");

        result.RadiusSuccess = TryProbeRadius(measurable);
        _logger.Info($"Geometry probe GetRadius: {(result.RadiusSuccess ? "success" : "fail")}");

        return result;
    }

    private bool TryProbePlane(object measurable)
    {
        try
        {
            var planeData = new object[9];
            InvokeComMethodWithByRefSingleArgument(measurable, "GetPlane", planeData);
            return HasAnyNonNullValue(planeData);
        }
        catch
        {
            return false;
        }
    }

    private bool TryProbeDirection(object measurable)
    {
        try
        {
            var directionData = new object[3];
            InvokeComMethodWithByRefSingleArgument(measurable, "GetDirection", directionData);
            return HasAnyNonNullValue(directionData);
        }
        catch
        {
            return false;
        }
    }

    private bool TryProbePoint(object measurable)
    {
        try
        {
            var pointData = new object[3];
            InvokeComMethodWithByRefSingleArgument(measurable, "GetPoint", pointData);
            return HasAnyNonNullValue(pointData);
        }
        catch
        {
            return false;
        }
    }

    private bool TryProbeAxis(object measurable)
    {
        try
        {
            var axisData = new object[3];
            InvokeComMethodWithByRefSingleArgument(measurable, "GetAxis", axisData);
            return HasAnyNonNullValue(axisData);
        }
        catch
        {
            return false;
        }
    }

    private bool TryProbeRadius(object measurable)
    {
        try
        {
            var rawRadius = InvokeComMethod(measurable, "GetRadius");
            if (rawRadius is null)
            {
                return false;
            }

            _ = Convert.ToDouble(rawRadius, CultureInfo.InvariantCulture);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool TryReadCandidateColor(object? selection, object candidate, out int red, out int green, out int blue)
    {
        red = 0;
        green = 0;
        blue = 0;

        if (selection is null)
        {
            _logger.Warning("Candidate color could not be read.");
            _logger.Warning("Root cause: Selection object does not exist.");
            return false;
        }

        try
        {
            TryInvokeComMethod(selection, "Clear");
            InvokeComMethod(selection, "Add", candidate);

            var visProperties = TryGetComProperty(selection, "VisProperties");
            if (visProperties is null)
            {
                _logger.Warning("Candidate color could not be read.");
                _logger.Warning("Root cause: Selection.VisProperties does not exist.");
                return false;
            }

            var args = new object[] { 0, 0, 0 };
            var modifiers = new ParameterModifier(3);
            modifiers[0] = true;
            modifiers[1] = true;
            modifiers[2] = true;

            visProperties.GetType().InvokeMember(
                "GetRealColor",
                BindingFlags.InvokeMethod,
                binder: null,
                target: visProperties,
                args: args,
                modifiers: new[] { modifiers },
                culture: null,
                namedParameters: null);

            red = Convert.ToInt32(args[0]);
            green = Convert.ToInt32(args[1]);
            blue = Convert.ToInt32(args[2]);
            return true;
        }
        catch (Exception ex)
        {
            var rootCause = ExceptionUtils.GetRootCause(ex);
            var comErrorCode = ExceptionUtils.GetComErrorCode(ex);

            _logger.Warning("Candidate color could not be read.");
            _logger.Warning($"Root cause: {rootCause.Message}");
            if (!string.IsNullOrWhiteSpace(comErrorCode))
            {
                _logger.Warning($"Root COM error: {comErrorCode}");
            }

            return false;
        }
        finally
        {
            TryInvokeComMethod(selection, "Clear");
        }
    }

    private static string DetermineGeometryType(string candidateType, string candidateName, GeometryProbeResult probeResult)
    {
        if (probeResult.PlaneSuccess)
        {
            if (candidateName.Contains("FACE", StringComparison.OrdinalIgnoreCase) ||
                candidateType.Contains("Surface", StringComparison.OrdinalIgnoreCase))
            {
                return "PlanarSurface";
            }

            return "Plane";
        }

        if (probeResult.RadiusSuccess)
        {
            if (candidateType.Contains("Cylinder", StringComparison.OrdinalIgnoreCase) ||
                candidateName.Contains("CYL", StringComparison.OrdinalIgnoreCase))
            {
                return "Cylinder";
            }

            return "CircleOrCylinder";
        }

        if (probeResult.PointSuccess)
        {
            return "Point";
        }

        if (probeResult.AxisSuccess || probeResult.DirectionSuccess)
        {
            if (candidateType.Contains("Edge", StringComparison.OrdinalIgnoreCase) ||
                candidateName.Contains("EDGE", StringComparison.OrdinalIgnoreCase))
            {
                return "Edge";
            }

            return "LineOrEdge";
        }

        var combined = $"{candidateType} {candidateName}";

        if (combined.Contains("Plane", StringComparison.OrdinalIgnoreCase))
        {
            return "Plane";
        }

        if (combined.Contains("Cylinder", StringComparison.OrdinalIgnoreCase))
        {
            return "Cylinder";
        }

        if (combined.Contains("Edge", StringComparison.OrdinalIgnoreCase))
        {
            return "Edge";
        }

        if (combined.Contains("Point", StringComparison.OrdinalIgnoreCase))
        {
            return "Point";
        }

        if (combined.Contains("Surface", StringComparison.OrdinalIgnoreCase) ||
            combined.Contains("Face", StringComparison.OrdinalIgnoreCase))
        {
            return "Surface";
        }

        if (combined.Contains("Line", StringComparison.OrdinalIgnoreCase))
        {
            return "Line";
        }

        return "Unknown";
    }

    private static string BuildTypeFlags(string candidateType, string candidateName)
    {
        var flags = new List<string>
        {
            $"HybridShape={candidateType.Contains("HybridShape", StringComparison.OrdinalIgnoreCase)}",
            $"Shape={candidateType.Contains("Shape", StringComparison.OrdinalIgnoreCase)}",
            $"Face={(candidateType.Contains("Face", StringComparison.OrdinalIgnoreCase) || candidateName.Contains("FACE", StringComparison.OrdinalIgnoreCase))}",
            $"Edge={(candidateType.Contains("Edge", StringComparison.OrdinalIgnoreCase) || candidateName.Contains("EDGE", StringComparison.OrdinalIgnoreCase))}",
            $"Point={(candidateType.Contains("Point", StringComparison.OrdinalIgnoreCase) || candidateName.Contains("POINT", StringComparison.OrdinalIgnoreCase))}",
            $"Plane={(candidateType.Contains("Plane", StringComparison.OrdinalIgnoreCase) || candidateName.Contains("PLANE", StringComparison.OrdinalIgnoreCase))}",
            $"Surface={(candidateType.Contains("Surface", StringComparison.OrdinalIgnoreCase) || candidateName.Contains("SURFACE", StringComparison.OrdinalIgnoreCase))}"
        };

        return string.Join(", ", flags);
    }

    private static string GetColorGroup(int red, int green, int blue)
    {
        if (red > 200 && green < 100 && blue < 100)
        {
            return "RED";
        }

        if (blue > 200 && red < 100 && green < 180)
        {
            return "BLUE";
        }

        if (red > 200 && green > 200 && blue < 120)
        {
            return "YELLOW";
        }

        if (green > 160 && red < 160 && blue < 160)
        {
            return "GREEN";
        }

        return "UNKNOWN";
    }

    private static void CollectHybridShapeCandidates(object hybridBody, string searchPath, List<CandidateInfo> candidates)
    {
        var hybridShapes = TryGetComProperty(hybridBody, "HybridShapes");
        if (hybridShapes is not null)
        {
            var count = Convert.ToInt32(GetComProperty(hybridShapes, "Count") ?? 0);
            for (var index = 1; index <= count; index++)
            {
                var candidate = InvokeComMethod(hybridShapes, "Item", index);
                if (candidate is not null)
                {
                    var candidateName = Convert.ToString(TryGetComProperty(candidate, "Name")) ?? $"Item{index}";
                    candidates.Add(new CandidateInfo(candidate, $"{searchPath}/{candidateName}"));
                }
            }
        }

        var childHybridBodies = TryGetComProperty(hybridBody, "HybridBodies");
        if (childHybridBodies is null)
        {
            return;
        }

        var childCount = Convert.ToInt32(GetComProperty(childHybridBodies, "Count") ?? 0);
        for (var index = 1; index <= childCount; index++)
        {
            var childHybridBody = InvokeComMethod(childHybridBodies, "Item", index);
            if (childHybridBody is not null)
            {
                var childName = Convert.ToString(TryGetComProperty(childHybridBody, "Name")) ?? $"HybridBody{index}";
                CollectHybridShapeCandidates(childHybridBody, $"{searchPath}/{childName}", candidates);
            }
        }
    }

    private static object? FindHybridBodyByName(object hybridBodies, string targetName)
    {
        var count = Convert.ToInt32(GetComProperty(hybridBodies, "Count") ?? 0);
        for (var index = 1; index <= count; index++)
        {
            var hybridBody = InvokeComMethod(hybridBodies, "Item", index);
            if (hybridBody is null)
            {
                continue;
            }

            var name = Convert.ToString(TryGetComProperty(hybridBody, "Name"));
            if (string.Equals(name, targetName, StringComparison.OrdinalIgnoreCase))
            {
                return hybridBody;
            }

            var childHybridBodies = TryGetComProperty(hybridBody, "HybridBodies");
            if (childHybridBodies is null)
            {
                continue;
            }

            var found = FindHybridBodyByName(childHybridBodies, targetName);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    private object? TryGetSpaWorkbench(object sourceDocument)
    {
        try
        {
            return InvokeComMethod(sourceDocument, "GetWorkbench", "SPAWorkbench");
        }
        catch (Exception ex)
        {
            LogWarningWithException("SPAWorkbench acquisition failed.", ex);
            return null;
        }
    }

    private void LogWarningWithException(string message, Exception ex)
    {
        var rootCause = ExceptionUtils.GetRootCause(ex);
        var comErrorCode = ExceptionUtils.GetComErrorCode(ex);

        _logger.Warning(message);
        _logger.Warning($"Root cause: {rootCause.Message}");
        if (!string.IsNullOrWhiteSpace(comErrorCode))
        {
            _logger.Warning($"Root COM error: {comErrorCode}");
        }
    }

    private static bool HasAnyNonNullValue(object[] values)
    {
        foreach (var value in values)
        {
            if (value is not null)
            {
                return true;
            }
        }

        return false;
    }

    private static object? TryGetComProperty(object comObject, string propertyName)
    {
        try
        {
            return GetComProperty(comObject, propertyName);
        }
        catch
        {
            return null;
        }
    }

    private static object? GetComProperty(object comObject, string propertyName)
    {
        return comObject.GetType().InvokeMember(
            propertyName,
            BindingFlags.GetProperty,
            binder: null,
            target: comObject,
            args: null);
    }

    private static object? InvokeComMethod(object comObject, string methodName, params object[] args)
    {
        return comObject.GetType().InvokeMember(
            methodName,
            BindingFlags.InvokeMethod,
            binder: null,
            target: comObject,
            args: args);
    }

    private static object? InvokeComMethodWithByRefSingleArgument(object comObject, string methodName, object argument)
    {
        var args = new[] { argument };
        var modifiers = new ParameterModifier(1);
        modifiers[0] = true;

        return comObject.GetType().InvokeMember(
            methodName,
            BindingFlags.InvokeMethod,
            binder: null,
            target: comObject,
            args: args,
            modifiers: new[] { modifiers },
            culture: null,
            namedParameters: null);
    }

    private static void TryInvokeComMethod(object comObject, string methodName, params object[] args)
    {
        try
        {
            InvokeComMethod(comObject, methodName, args);
        }
        catch
        {
        }
    }

    private sealed record CandidateInfo(object Candidate, string SearchPath);

    private sealed class GeometryProbeResult
    {
        public bool PlaneSuccess { get; set; }
        public bool DirectionSuccess { get; set; }
        public bool PointSuccess { get; set; }
        public bool AxisSuccess { get; set; }
        public bool RadiusSuccess { get; set; }
    }
}
