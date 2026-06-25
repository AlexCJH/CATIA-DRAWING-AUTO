using System;
using System.Collections.Generic;
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

            var candidates = new List<object>();
            CollectHybridShapeCandidates(dimensionTargetSet, candidates);
            if (candidates.Count == 0)
            {
                const string message = "No dimension target candidates found in GS_DIMENSION_TARGET.";
                _logger.Warning(message);
                _logger.Info("STEP 6A color based dimension target detection completed.");
                return Result.Failure(message);
            }

            var detectedCount = 0;
            var colorConfirmedCount = 0;
            var geometryConfirmedCount = 0;

            foreach (var candidate in candidates)
            {
                detectedCount++;
                _logger.Info("Dimension target candidate found.");

                var candidateType = candidate.GetType().FullName ?? candidate.GetType().Name;
                var candidateName = Convert.ToString(TryGetComProperty(candidate, "Name")) ?? string.Empty;

                _logger.Info($"Candidate name: {candidateName}");
                _logger.Info($"Candidate COM type: {candidateType}");
                _logger.Info($"Candidate CATIA name: {candidateName}");
                _logger.Info("Candidate color read attempt started.");

                if (TryReadCandidateColor(selection, candidate, out var red, out var green, out var blue))
                {
                    colorConfirmedCount++;
                    _logger.Info($"Candidate color RGB: R={red}, G={green}, B={blue}");
                    _logger.Info($"Candidate color group: {GetColorGroup(red, green, blue)}");
                }
                else
                {
                    _logger.Warning("Candidate color could not be read.");
                }

                var geometryType = DetermineGeometryType(candidateType, candidateName);
                if (string.Equals(geometryType, "Unknown", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.Warning("Candidate geometry type could not be determined.");
                }
                else
                {
                    geometryConfirmedCount++;
                }

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

    private static string DetermineGeometryType(string candidateType, string candidateName)
    {
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

        if (combined.Contains("Surface", StringComparison.OrdinalIgnoreCase))
        {
            return "Surface";
        }

        if (combined.Contains("Line", StringComparison.OrdinalIgnoreCase))
        {
            return "Line";
        }

        return "Unknown";
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

    private static void CollectHybridShapeCandidates(object hybridBody, List<object> candidates)
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
                    candidates.Add(candidate);
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
                CollectHybridShapeCandidates(childHybridBody, candidates);
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
}