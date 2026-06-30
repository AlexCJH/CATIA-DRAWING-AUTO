using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CatiaAutoDrawing.Core;
using CatiaAutoDrawing.Logging;
using CatiaAutoDrawing.Utils;

namespace CatiaAutoDrawing.DimensionGenerator;

/// <summary>
/// Role: Detects color based dimension targets and runs first drawing dimension experiments.
/// </summary>
public sealed class DimensionGenerator : IDimensionGenerator
{
    private const string DimensionTargetSetName = "GS_DIMENSION_TARGET";
    private const string FrontViewName = "FRONT_VIEW";
    private const string RedColorGroup = "RED";
    private const string SurfaceDistanceDimensionName = "DIM_RED_SURFACE_DISTANCE_01";

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
        _logger.Info($"Dimension target detection scope: {DimensionTargetSetName}");
        _logger.Info("STEP 6A geometry type detection started.");

        try
        {
            var collectionResult = CollectDimensionTargets(sourceDocument, logDiagnostics: true);
            if (!collectionResult.IsSuccess || collectionResult.Value is null)
            {
                return Result.Failure(collectionResult.ErrorMessage ?? "STEP 6A color based dimension target detection failed.");
            }

            var targetContext = collectionResult.Value;
            _logger.Info($"STEP 6A summary: candidates={targetContext.Candidates.Count}, colorConfirmed={targetContext.ColorConfirmedCount}, geometryConfirmed={targetContext.GeometryConfirmedCount}");
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

    public Result GenerateColorBasedSurfaceDistanceDimension(object drawingDocument, object sourceDocument)
    {
        _logger.Info("STEP 6B color based distance dimension generation started.");
        _logger.Info("RED surface dimension target search started.");

        try
        {
            var collectionResult = CollectDimensionTargets(sourceDocument, logDiagnostics: false);
            if (!collectionResult.IsSuccess || collectionResult.Value is null)
            {
                return Result.Failure(collectionResult.ErrorMessage ?? "STEP 6B dimension target collection failed.");
            }

            var context = collectionResult.Value;
            var redSurfaceTargets = context.Candidates
                .Where(candidate => string.Equals(candidate.ColorGroup, RedColorGroup, StringComparison.OrdinalIgnoreCase))
                .Where(candidate => IsSurfaceLike(candidate.GeometryType, candidate.CandidateType, candidate.CandidateName))
                .Take(2)
                .ToList();

            foreach (var candidate in redSurfaceTargets)
            {
                _logger.Info($"RED surface dimension target found: {candidate.CandidateName}");
            }

            _logger.Info($"RED surface dimension target count: {redSurfaceTargets.Count}");

            if (redSurfaceTargets.Count < 2)
            {
                const string message = "RED surface dimension target count is less than 2. STEP 6B skipped.";
                _logger.Warning(message);
                return Result.Failure(message);
            }

            var frontView = FindFrontView(drawingDocument);
            if (frontView is null)
            {
                const string message = "FRONT_VIEW not found. STEP 6B skipped.";
                _logger.Warning(message);
                return Result.Failure(message);
            }

            _logger.Info("FRONT_VIEW acquired for dimension generation.");

            _logger.Info("Dimension reference 1 creation started.");
            var reference1 = InvokeComMethod(context.Part, "CreateReferenceFromObject", redSurfaceTargets[0].Candidate);
            if (reference1 is null)
            {
                const string message = "Dimension reference 1 creation failed.";
                _logger.Warning(message);
                return Result.Failure(message);
            }

            _logger.Info("Dimension reference 1 creation succeeded.");

            _logger.Info("Dimension reference 2 creation started.");
            var reference2 = InvokeComMethod(context.Part, "CreateReferenceFromObject", redSurfaceTargets[1].Candidate);
            if (reference2 is null)
            {
                const string message = "Dimension reference 2 creation failed.";
                _logger.Warning(message);
                return Result.Failure(message);
            }

            _logger.Info("Dimension reference 2 creation succeeded.");
            _logger.Info("Surface-to-surface distance dimension API experiment started.");

            const string dimensionMethod = "DrawingDimensions.Add2(distanceType=0, geometryElements[2], pickPoints[4], lineRepresentation=0)";
            _logger.Info($"Dimension creation method: {dimensionMethod}");

            var dimension = TryCreateSurfaceDistanceDimension(frontView, reference1, reference2);
            if (dimension is null)
            {
                const string message = "STEP 6B dimension generation failed, but drawing generation will continue.";
                _logger.Warning(message);
                return Result.Failure(message);
            }

            _logger.Info("Surface-to-surface distance dimension generated successfully.");

            try
            {
                SetComProperty(dimension, "Name", SurfaceDistanceDimensionName);
                _logger.Info($"Dimension name set: {SurfaceDistanceDimensionName}");
            }
            catch (Exception ex)
            {
                LogWarningWithException("Dimension name set failed.", ex);
            }

            var sheet = GetFirstSheet(drawingDocument);
            if (sheet is not null)
            {
                TryInvokeComMethod(sheet, "Update");
            }

            _logger.Info("STEP 6B color based distance dimension generation completed.");
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogWarningWithException("STEP 6B dimension generation failed, but drawing generation will continue.", ex);
            return Result.Failure("STEP 6B dimension generation failed, but drawing generation will continue.");
        }
    }

    private Result<DimensionTargetContext> CollectDimensionTargets(object sourceDocument, bool logDiagnostics)
    {
        var part = TryGetComProperty(sourceDocument, "Part");
        if (part is null)
        {
            const string message = "STEP 6A requires a CATPart source document.";
            _logger.Warning(message);
            return Result<DimensionTargetContext>.Failure(message);
        }

        var selection = TryGetComProperty(sourceDocument, "Selection");
        var hybridBodies = TryGetComProperty(part, "HybridBodies");
        var dimensionTargetSet = hybridBodies is null ? null : FindHybridBodyByName(hybridBodies, DimensionTargetSetName);
        if (dimensionTargetSet is null)
        {
            const string message = "GS_DIMENSION_TARGET not found. STEP 6A skipped.";
            _logger.Warning(message);
            return Result<DimensionTargetContext>.Failure(message);
        }

        _logger.Info("GS_DIMENSION_TARGET found.");

        var rawCandidates = new List<CandidateSource>();
        CollectHybridShapeCandidates(dimensionTargetSet, DimensionTargetSetName, rawCandidates);
        if (rawCandidates.Count == 0)
        {
            const string message = "No dimension target candidates found in GS_DIMENSION_TARGET.";
            _logger.Warning(message);
            return Result<DimensionTargetContext>.Failure(message);
        }

        var spaWorkbench = TryGetSpaWorkbench(sourceDocument);
        var candidates = new List<CandidateInfo>();
        var colorConfirmedCount = 0;
        var geometryConfirmedCount = 0;

        foreach (var rawCandidate in rawCandidates)
        {
            _logger.Info("Dimension target candidate found.");

            var candidate = rawCandidate.Candidate;
            var candidateType = candidate.GetType().FullName ?? candidate.GetType().Name;
            var candidateName = Convert.ToString(TryGetComProperty(candidate, "Name")) ?? string.Empty;
            var isHybridShape = candidateType.Contains("HybridShape", StringComparison.OrdinalIgnoreCase);

            if (logDiagnostics)
            {
                _logger.Info($"Candidate diagnostic started: {candidateName}");
                _logger.Info($"Candidate name: {candidateName}");
                _logger.Info($"Candidate COM type: {candidateType}");
                _logger.Info($"Candidate CATIA name: {candidateName}");
                _logger.Info($"Candidate search path: {rawCandidate.SearchPath}");
                _logger.Info($"Candidate automation type: {candidateType}");
                _logger.Info($"Candidate HybridShape: {isHybridShape}");
                _logger.Info($"Candidate type flags: {BuildTypeFlags(candidateType, candidateName)}");
            }
            else
            {
                _logger.Info($"Candidate name: {candidateName}");
                _logger.Info($"Candidate COM type: {candidateType}");
                _logger.Info($"Candidate CATIA name: {candidateName}");
            }

            _logger.Info("Candidate color read attempt started.");

            var colorReadSucceeded = TryReadCandidateColor(selection, candidate, out var red, out var green, out var blue);
            var colorGroup = colorReadSucceeded ? GetColorGroup(red, green, blue) : "UNKNOWN";
            if (colorReadSucceeded)
            {
                colorConfirmedCount++;
                _logger.Info($"Candidate color RGB: R={red}, G={green}, B={blue}");
                _logger.Info($"Candidate color group: {colorGroup}");
            }

            var geometryType = DetectGeometryType(part, spaWorkbench, rawCandidate, candidateType, candidateName, logDiagnostics);
            if (string.Equals(geometryType, "Unknown", StringComparison.OrdinalIgnoreCase))
            {
                _logger.Warning(logDiagnostics
                    ? "Geometry type could not be determined after probes."
                    : "Candidate geometry type could not be determined.");
            }
            else
            {
                geometryConfirmedCount++;
            }

            if (logDiagnostics)
            {
                _logger.Info($"Candidate geometry type inferred: {geometryType}");
            }

            _logger.Info($"Candidate geometry type: {geometryType}");

            candidates.Add(new CandidateInfo(candidate, rawCandidate.SearchPath, candidateName, candidateType, colorGroup, geometryType));
        }

        return Result<DimensionTargetContext>.Success(new DimensionTargetContext(part, candidates, colorConfirmedCount, geometryConfirmedCount));
    }

    private string DetectGeometryType(
        object part,
        object? spaWorkbench,
        CandidateSource candidateSource,
        string candidateType,
        string candidateName,
        bool logDiagnostics)
    {
        if (logDiagnostics)
        {
            _logger.Info($"Candidate diagnostic started: {candidateName}");
            _logger.Info("Candidate reference creation started.");
        }

        object? reference = null;
        try
        {
            reference = InvokeComMethod(part, "CreateReferenceFromObject", candidateSource.Candidate);
        }
        catch (Exception ex)
        {
            if (logDiagnostics)
            {
                LogWarningWithException($"Candidate reference creation failed: {candidateName}", ex);
            }
        }

        if (reference is null)
        {
            if (logDiagnostics)
            {
                _logger.Warning($"Candidate reference creation failed: {candidateName}");
            }

            return InferGeometryTypeFromMetadata(candidateType, candidateName, new GeometryProbeResult());
        }

        if (logDiagnostics)
        {
            _logger.Info("Candidate reference creation succeeded.");
        }

        if (spaWorkbench is null)
        {
            if (logDiagnostics)
            {
                _logger.Warning("Candidate measurable extraction failed: SPAWorkbench is not available.");
            }

            return InferGeometryTypeFromMetadata(candidateType, candidateName, new GeometryProbeResult());
        }

        if (logDiagnostics)
        {
            _logger.Info("Candidate measurable extraction started.");
        }

        object? measurable = null;
        try
        {
            measurable = InvokeComMethod(spaWorkbench, "GetMeasurable", reference);
        }
        catch (Exception ex)
        {
            if (logDiagnostics)
            {
                LogWarningWithException($"Candidate measurable extraction failed: {candidateName}", ex);
            }
        }

        if (measurable is null)
        {
            if (logDiagnostics)
            {
                _logger.Warning($"Candidate measurable extraction failed: {candidateName}");
            }

            return InferGeometryTypeFromMetadata(candidateType, candidateName, new GeometryProbeResult());
        }

        if (logDiagnostics)
        {
            _logger.Info("Candidate measurable extraction succeeded.");
            _logger.Info($"Candidate measurable type: {measurable.GetType().FullName ?? measurable.GetType().Name}");
        }

        var probeResult = ProbeGeometryMethods(measurable, logDiagnostics);
        return InferGeometryTypeFromMetadata(candidateType, candidateName, probeResult);
    }

    private GeometryProbeResult ProbeGeometryMethods(object measurable, bool logDiagnostics)
    {
        var result = new GeometryProbeResult
        {
            PlaneSuccess = TryProbePlane(measurable),
            DirectionSuccess = TryProbeDirection(measurable),
            PointSuccess = TryProbePoint(measurable),
            RadiusSuccess = TryProbeRadius(measurable)
        };

        if (logDiagnostics)
        {
            _logger.Info($"Geometry probe GetPlane: {(result.PlaneSuccess ? "success" : "fail")}");
            _logger.Info($"Geometry probe GetDirection: {(result.DirectionSuccess ? "success" : "fail")}");
            _logger.Info($"Geometry probe GetPoint: {(result.PointSuccess ? "success" : "fail")}");
            _logger.Info($"Geometry probe GetRadius: {(result.RadiusSuccess ? "success" : "fail")}");
        }

        return result;
    }

    private object? TryCreateSurfaceDistanceDimension(object frontView, object reference1, object reference2)
    {
        var dimensions = TryGetComProperty(frontView, "Dimensions");
        if (dimensions is null)
        {
            _logger.Warning("STEP 6B dimension generation failed, but drawing generation will continue.");
            _logger.Warning("Root cause: FRONT_VIEW dimensions collection does not exist.");
            return null;
        }

        try
        {
            var geometryElements = new object[] { reference1, reference2 };
            var pickPoints = new object[] { 50.0, 50.0, 140.0, 140.0 };
            const int distanceDimensionType = 0;
            const int lineRepresentation = 0;

            return InvokeComMethod(
                dimensions,
                "Add2",
                distanceDimensionType,
                geometryElements,
                pickPoints,
                lineRepresentation);
        }
        catch (Exception ex)
        {
            LogWarningWithException("STEP 6B dimension generation failed, but drawing generation will continue.", ex);
            return null;
        }
    }

    private static bool IsSurfaceLike(string geometryType, string candidateType, string candidateName)
    {
        if (geometryType.Contains("Surface", StringComparison.OrdinalIgnoreCase) ||
            geometryType.Contains("Plane", StringComparison.OrdinalIgnoreCase) ||
            geometryType.Contains("Face", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var combined = $"{candidateType} {candidateName}";
        return combined.Contains("Surface", StringComparison.OrdinalIgnoreCase) ||
               combined.Contains("Face", StringComparison.OrdinalIgnoreCase) ||
               combined.Contains("Plane", StringComparison.OrdinalIgnoreCase);
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

    private static string InferGeometryTypeFromMetadata(string candidateType, string candidateName, GeometryProbeResult probeResult)
    {
        if (probeResult.PlaneSuccess)
        {
            if (candidateName.Contains("FACE", StringComparison.OrdinalIgnoreCase) ||
                candidateType.Contains("Surface", StringComparison.OrdinalIgnoreCase))
            {
                return "Surface";
            }

            return "Plane";
        }

        if (probeResult.RadiusSuccess)
        {
            if (candidateType.Contains("Cylinder", StringComparison.OrdinalIgnoreCase))
            {
                return "Cylinder";
            }

            return "CircleOrCylinder";
        }

        if (probeResult.PointSuccess)
        {
            return "Point";
        }

        if (probeResult.DirectionSuccess)
        {
            if (candidateType.Contains("Edge", StringComparison.OrdinalIgnoreCase) ||
                candidateName.Contains("EDGE", StringComparison.OrdinalIgnoreCase))
            {
                return "Edge";
            }

            return "LineOrEdge";
        }

        var combined = $"{candidateType} {candidateName}";
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

        if (combined.Contains("Plane", StringComparison.OrdinalIgnoreCase))
        {
            return "Plane";
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
            $"Surface={(candidateType.Contains("Surface", StringComparison.OrdinalIgnoreCase) || candidateName.Contains("SURFACE", StringComparison.OrdinalIgnoreCase) || candidateName.Contains("FACE", StringComparison.OrdinalIgnoreCase))}"
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

    private static void CollectHybridShapeCandidates(object hybridBody, string searchPath, List<CandidateSource> candidates)
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
                    candidates.Add(new CandidateSource(candidate, $"{searchPath}/{candidateName}"));
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

    private static object? FindFrontView(object drawingDocument)
    {
        var sheet = GetFirstSheet(drawingDocument);
        if (sheet is null)
        {
            return null;
        }

        var views = TryGetComProperty(sheet, "Views");
        return views is null ? null : FindItemByName(views, FrontViewName);
    }

    private static object? GetFirstSheet(object drawingDocument)
    {
        var sheets = TryGetComProperty(drawingDocument, "Sheets");
        if (sheets is null)
        {
            return null;
        }

        return InvokeComMethod(sheets, "Item", 1);
    }

    private static object? FindItemByName(object collection, string itemName)
    {
        var countObject = TryGetComProperty(collection, "Count");
        if (countObject is null)
        {
            return null;
        }

        var count = Convert.ToInt32(countObject);
        for (var index = 1; index <= count; index++)
        {
            var item = InvokeComMethod(collection, "Item", index);
            if (item is null)
            {
                continue;
            }

            var name = Convert.ToString(TryGetComProperty(item, "Name"));
            if (string.Equals(name, itemName, StringComparison.OrdinalIgnoreCase))
            {
                return item;
            }
        }

        return null;
    }

    private bool TryProbePlane(object measurable)
    {
        try
        {
            dynamic measurableDynamic = measurable;
            object planeData = new object[9];
            measurableDynamic.GetPlane(ref planeData);
            return HasAnyNonNullValues(planeData);
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
            dynamic measurableDynamic = measurable;
            object directionData = new object[3];
            measurableDynamic.GetDirection(ref directionData);
            return HasAnyNonNullValues(directionData);
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
            dynamic measurableDynamic = measurable;
            object pointData = new object[3];
            measurableDynamic.GetPoint(ref pointData);
            return HasAnyNonNullValues(pointData);
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
            var radius = InvokeComMethod(measurable, "GetRadius");
            return radius is not null;
        }
        catch
        {
            return false;
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

    private static bool HasAnyNonNullValues(object arrayObject)
    {
        return arrayObject is Array array && array.Cast<object?>().Any(value => value is not null);
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

    private static void SetComProperty(object comObject, string propertyName, object? value)
    {
        comObject.GetType().InvokeMember(
            propertyName,
            BindingFlags.SetProperty,
            binder: null,
            target: comObject,
            args: new[] { value });
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

    private sealed record CandidateSource(object Candidate, string SearchPath);

    private sealed record CandidateInfo(
        object Candidate,
        string SearchPath,
        string CandidateName,
        string CandidateType,
        string ColorGroup,
        string GeometryType);

    private sealed record DimensionTargetContext(
        object Part,
        List<CandidateInfo> Candidates,
        int ColorConfirmedCount,
        int GeometryConfirmedCount);

    private sealed class GeometryProbeResult
    {
        public bool PlaneSuccess { get; init; }
        public bool DirectionSuccess { get; init; }
        public bool PointSuccess { get; init; }
        public bool RadiusSuccess { get; init; }
    }
}
