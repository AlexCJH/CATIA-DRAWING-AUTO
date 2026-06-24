using System;
using System.Reflection;
using CatiaAutoDrawing.Core;
using CatiaAutoDrawing.Logging;
using CatiaAutoDrawing.Utils;

namespace CatiaAutoDrawing.ViewGenerator;

/// <summary>
/// Role: Creates drawing views and calculates view placement.
/// TODO: Add Projection, Detail, and Section Views only in their own steps.
/// </summary>
public sealed class ViewGenerator : IViewGenerator
{
    private const string FrontViewName = "FRONT_VIEW";

    private readonly ILogger _logger;

    public ViewGenerator(ILogger logger)
    {
        _logger = logger;
    }

    public ViewLayoutPlan CreateLayoutPlan(object catiaDocument)
    {
        throw new NotImplementedException("View generation is not implemented yet.");
    }

    public Result GenerateFrontView(
        object drawingDocument,
        object sourceDocument,
        string frontViewDirection,
        string topDirection,
        string viewSide,
        int viewRotation)
    {
        _logger.Info("Front view generation started.");
        _logger.Info("Marker based view orientation started.");

        try
        {
            var orientationResult = ResolveMarkerBasedOrientation(sourceDocument);
            if (!orientationResult.IsSuccess)
            {
                return Result.Failure(orientationResult.ErrorMessage ?? "Marker based view orientation failed.");
            }

            var normalizedViewSide = NormalizeViewSide(viewSide);
            var normalizedViewRotation = NormalizeViewRotation(viewRotation);
            var frontNormalBeforeSide = orientationResult.Value.FrontVector.Normalize();
            var frontNormal = string.Equals(normalizedViewSide, "Opposite", StringComparison.OrdinalIgnoreCase)
                ? frontNormalBeforeSide.Reverse()
                : frontNormalBeforeSide;
            var baseUpSource = orientationResult.Value.TopVector.Normalize();
            var baseUp = baseUpSource.RemoveComponentAlong(frontNormal).Normalize();

            _logger.Info($"View side selected: {normalizedViewSide}");
            _logger.Info($"View rotation selected: {normalizedViewRotation}");
            _logger.Info($"Front normal before side correction: {frontNormalBeforeSide}");
            _logger.Info($"Front normal after side correction: {frontNormal}");
            _logger.Info($"Base up vector: {baseUp}");

            if (baseUp.IsZero)
            {
                const string message = "Base up vector is zero after projecting onto the front plane.";
                _logger.Error(message);
                return Result.Failure(message);
            }

            var baseRight = baseUp.Cross(frontNormal).Normalize();
            _logger.Info($"Base right vector: {baseRight}");

            if (baseRight.IsZero)
            {
                const string message = "Base right vector is zero after cross product.";
                _logger.Error(message);
                return Result.Failure(message);
            }

            var (viewUp, viewRight) = ApplyViewRotation(baseUp, baseRight, normalizedViewRotation);
            _logger.Info($"Rotated view up vector: {viewUp}");
            _logger.Info($"Rotated view right vector: {viewRight}");

            var sheets = GetComProperty(drawingDocument, "Sheets");
            if (sheets is null)
            {
                const string message = "Drawing sheets collection does not exist.";
                _logger.Error(message);
                return Result.Failure(message);
            }

            var firstSheet = InvokeComMethod(sheets, "Item", 1);
            if (firstSheet is null)
            {
                const string message = "First drawing sheet could not be acquired.";
                _logger.Error(message);
                return Result.Failure(message);
            }

            _logger.Info("First drawing sheet acquired.");

            var views = GetComProperty(firstSheet, "Views");
            if (views is null)
            {
                const string message = "Drawing views collection does not exist.";
                _logger.Error(message);
                return Result.Failure(message);
            }

            var frontView = InvokeComMethod(views, "Add", FrontViewName);
            if (frontView is null)
            {
                const string message = "Front view could not be created.";
                _logger.Error(message);
                return Result.Failure(message);
            }

            _logger.Info("Front view created.");

            SetComProperty(frontView, "Name", FrontViewName);
            _logger.Info($"Front view name set: {FrontViewName}");

            SetComProperty(frontView, "x", 200.0);
            SetComProperty(frontView, "y", 150.0);
            SetComProperty(frontView, "Scale", 1.0);
            _logger.Info("Front view positioned.");

            var generativeBehavior = GetComProperty(frontView, "GenerativeBehavior");
            if (generativeBehavior is not null)
            {
                SetComProperty(generativeBehavior, "Document", sourceDocument);
                InvokeComMethod(
                    generativeBehavior,
                    "DefineFrontView",
                    viewRight.X,
                    viewRight.Y,
                    viewRight.Z,
                    viewUp.X,
                    viewUp.Y,
                    viewUp.Z);
                _logger.Info($"DefineFrontView vector 1: {viewRight}");
                _logger.Info($"DefineFrontView vector 2: {viewUp}");
                _logger.Info("Front view orientation applied from markers.");
                InvokeComMethod(generativeBehavior, "Update");
            }
            else
            {
                _logger.Warning("Front view generative behavior does not exist. Empty front view may be saved.");
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            var rootCause = ExceptionUtils.GetRootCause(ex);
            var comErrorCode = ExceptionUtils.GetComErrorCode(ex);
            var message = $"Front view generation failed: {ex.Message}";

            _logger.Error(ex, message);
            _logger.Error($"Root cause: {rootCause.Message}");
            if (!string.IsNullOrWhiteSpace(comErrorCode))
            {
                _logger.Error($"Root COM error: {comErrorCode}");
            }

            return Result.Failure(message);
        }
    }

    private Result<MarkerOrientation> ResolveMarkerBasedOrientation(object sourceDocument)
    {
        var part = TryGetComProperty(sourceDocument, "Part");
        if (part is null)
        {
            const string message = "Marker based orientation requires a CATPart document.";
            _logger.Error(message);
            return Result<MarkerOrientation>.Failure(message);
        }

        var hybridBodies = TryGetComProperty(part, "HybridBodies");
        var drawingInfoGeoSet = hybridBodies is null ? null : FindHybridBodyByName(hybridBodies, "GS_DRAWING_INFO");
        if (drawingInfoGeoSet is null)
        {
            const string message = "GS_DRAWING_INFO not found.";
            _logger.Error(message);
            return Result<MarkerOrientation>.Failure(message);
        }

        var mainViewPlane = FindMarkerByName(drawingInfoGeoSet, "MAIN_VIEW_PLANE");
        if (mainViewPlane is null)
        {
            const string message = "MAIN_VIEW_PLANE not found.";
            _logger.Error(message);
            return Result<MarkerOrientation>.Failure(message);
        }

        _logger.Info("Required marker found: MAIN_VIEW_PLANE");
        LogMarkerComInfo("MAIN_VIEW_PLANE", mainViewPlane);

        var topDirection = FindMarkerByName(drawingInfoGeoSet, "TOP_DIRECTION");
        if (topDirection is null)
        {
            const string message = "TOP_DIRECTION not found.";
            _logger.Error(message);
            return Result<MarkerOrientation>.Failure(message);
        }

        _logger.Info("Required marker found: TOP_DIRECTION");
        LogMarkerComInfo("TOP_DIRECTION", topDirection);

        var planeReference = InvokeComMethod(part, "CreateReferenceFromObject", mainViewPlane);
        var topReference = InvokeComMethod(part, "CreateReferenceFromObject", topDirection);
        var spaWorkbench = InvokeComMethod(sourceDocument, "GetWorkbench", "SPAWorkbench");

        if (planeReference is null || topReference is null || spaWorkbench is null)
        {
            const string message = "Failed to create marker references or SPAWorkbench.";
            _logger.Error(message);
            return Result<MarkerOrientation>.Failure(message);
        }

        var planeMeasurable = InvokeComMethod(spaWorkbench, "GetMeasurable", planeReference);
        var topMeasurable = InvokeComMethod(spaWorkbench, "GetMeasurable", topReference);

        if (planeMeasurable is null)
        {
            const string message = "Failed to extract plane normal vector.";
            _logger.Error(message);
            return Result<MarkerOrientation>.Failure(message);
        }

        if (topMeasurable is null)
        {
            const string message = "Failed to extract top direction vector.";
            _logger.Error(message);
            return Result<MarkerOrientation>.Failure(message);
        }

        if (!TryExtractPlaneNormal(planeMeasurable, out var planeNormal))
        {
            const string message = "Failed to extract plane normal vector.";
            _logger.Error(message);
            return Result<MarkerOrientation>.Failure(message);
        }

        if (!TryExtractDirection(topMeasurable, out var topVector))
        {
            const string message = "Failed to extract top direction vector.";
            _logger.Error(message);
            return Result<MarkerOrientation>.Failure(message);
        }

        planeNormal = planeNormal.Normalize();
        topVector = topVector.Normalize();

        _logger.Info($"MAIN_VIEW_PLANE normal vector: {planeNormal}");
        _logger.Info($"TOP_DIRECTION vector: {topVector}");

        if (planeNormal.IsParallelTo(topVector))
        {
            const string message = "TOP_DIRECTION is parallel to MAIN_VIEW_PLANE normal.";
            _logger.Error(message);
            return Result<MarkerOrientation>.Failure(message);
        }

        if (!planeNormal.IsPerpendicularTo(topVector))
        {
            const string message = "TOP_DIRECTION is not parallel to MAIN_VIEW_PLANE.";
            _logger.Error(message);
            return Result<MarkerOrientation>.Failure(message);
        }

        _logger.Info("Front view orientation source: GS_DRAWING_INFO");
        return Result<MarkerOrientation>.Success(new MarkerOrientation(planeNormal, topVector));
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

    private static void SetComProperty(object comObject, string propertyName, object value)
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

    private static object? InvokeComMethodWithSingleArgument(
        object comObject,
        string methodName,
        object argument)
    {
        return comObject.GetType().InvokeMember(
            methodName,
            BindingFlags.InvokeMethod,
            binder: null,
            target: comObject,
            args: new[] { argument });
    }

    private static object? InvokeComMethodWithByRefSingleArgument(
        object comObject,
        string methodName,
        object argument)
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
        }

        return null;
    }

    private static object? FindMarkerByName(object hybridBody, string markerName)
    {
        var marker = FindNamedItem(TryGetComProperty(hybridBody, "HybridShapes"), markerName);
        if (marker is not null)
        {
            return marker;
        }

        var childHybridBodies = TryGetComProperty(hybridBody, "HybridBodies");
        if (childHybridBodies is null)
        {
            return null;
        }

        var count = Convert.ToInt32(GetComProperty(childHybridBodies, "Count"));
        for (var index = 1; index <= count; index++)
        {
            var childHybridBody = InvokeComMethod(childHybridBodies, "Item", index);
            if (childHybridBody is null)
            {
                continue;
            }

            marker = FindMarkerByName(childHybridBody, markerName);
            if (marker is not null)
            {
                return marker;
            }
        }

        return null;
    }

    private static object? FindNamedItem(object? collection, string itemName)
    {
        if (collection is null)
        {
            return null;
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
            if (string.Equals(name, itemName, StringComparison.OrdinalIgnoreCase))
            {
                return item;
            }
        }

        return null;
    }

    private void LogMarkerComInfo(string markerLabel, object marker)
    {
        var markerType = marker.GetType().FullName ?? marker.GetType().Name;
        var markerName = Convert.ToString(TryGetComProperty(marker, "Name")) ?? string.Empty;

        _logger.Info($"{markerLabel} marker type: {markerType}");
        _logger.Info($"{markerLabel} marker name: {markerName}");
    }

    private bool TryExtractPlaneNormal(object measurable, out DirectionVector normal)
    {
        normal = default;

        _logger.Info("Extracting MAIN_VIEW_PLANE measurable data...");
        _logger.Info($"MAIN_VIEW_PLANE measurable type: {measurable.GetType().FullName ?? measurable.GetType().Name}");

        if (!TryGetPlaneDataWithDynamicRef(measurable, out var planeData))
        {
            return false;
        }

        var firstDirection = new DirectionVector(planeData[3], planeData[4], planeData[5]);
        var secondDirection = new DirectionVector(planeData[6], planeData[7], planeData[8]);
        var normalCandidate = firstDirection.Cross(secondDirection);

        _logger.Info($"Plane first direction: {firstDirection}");
        _logger.Info($"Plane second direction: {secondDirection}");
        _logger.Info($"Plane normal candidate: {normalCandidate}");

        if (normalCandidate.IsZero)
        {
            _logger.Error("Plane normal vector is zero after cross product.");
            return false;
        }

        normal = normalCandidate.Normalize();
        return true;
    }

    private bool TryExtractDirection(object measurable, out DirectionVector direction)
    {
        direction = default;

        _logger.Info("Extracting TOP_DIRECTION measurable data...");
        _logger.Info($"TOP_DIRECTION measurable type: {measurable.GetType().FullName ?? measurable.GetType().Name}");

        if (!TryGetDirectionDataWithDynamicRef(measurable, out var directionData))
        {
            return false;
        }

        var directionCandidate = new DirectionVector(directionData[0], directionData[1], directionData[2]);
        _logger.Info($"Top direction candidate: {directionCandidate}");

        if (directionCandidate.IsZero)
        {
            _logger.Error("Top direction vector is zero.");
            return false;
        }

        direction = directionCandidate.Normalize();
        return true;
    }

    private bool TryGetPlaneDataWithDynamicRef(object measurable, out double[] planeData)
    {
        planeData = Array.Empty<double>();

        if (TryGetPlaneDataWithDynamicRefObjectArray(measurable, out planeData))
        {
            return true;
        }

        if (TryGetPlaneDataWithDynamicRefDoubleArray(measurable, out planeData))
        {
            return true;
        }

        return false;
    }

    private bool TryGetPlaneDataWithDynamicRefObjectArray(object measurable, out double[] planeData)
    {
        planeData = Array.Empty<double>();

        try
        {
            _logger.Info("Calling Measurable.GetPlane with dynamic ref object array...");
            dynamic measurableDynamic = measurable;
            object planeDataObject = new object[9];

            measurableDynamic.GetPlane(ref planeDataObject);

            _logger.Info($"GetPlane dynamic object array returned type: {planeDataObject?.GetType().FullName ?? "<null>"}");
            return TryConvertComArrayToDoubleArray(planeDataObject, 9, "Plane", out planeData);
        }
        catch (Exception ex)
        {
            var rootCause = ExceptionUtils.GetRootCause(ex);
            var comErrorCode = ExceptionUtils.GetComErrorCode(ex);

            _logger.Error(ex, "Measurable.GetPlane failed with dynamic ref object array.");
            _logger.Error($"Root cause: {rootCause.Message}");
            if (!string.IsNullOrWhiteSpace(comErrorCode))
            {
                _logger.Error($"Root COM error: {comErrorCode}");
            }

            return false;
        }
    }

    private bool TryGetPlaneDataWithDynamicRefDoubleArray(object measurable, out double[] planeData)
    {
        planeData = Array.Empty<double>();

        try
        {
            _logger.Info("Calling Measurable.GetPlane with dynamic ref double array...");
            dynamic measurableDynamic = measurable;
            object planeDataObject = new double[9];

            measurableDynamic.GetPlane(ref planeDataObject);

            _logger.Info($"GetPlane dynamic double array returned type: {planeDataObject?.GetType().FullName ?? "<null>"}");
            return TryConvertComArrayToDoubleArray(planeDataObject, 9, "Plane", out planeData);
        }
        catch (Exception ex)
        {
            var rootCause = ExceptionUtils.GetRootCause(ex);
            var comErrorCode = ExceptionUtils.GetComErrorCode(ex);

            _logger.Error(ex, "Measurable.GetPlane failed with dynamic ref double array.");
            _logger.Error($"Root cause: {rootCause.Message}");
            if (!string.IsNullOrWhiteSpace(comErrorCode))
            {
                _logger.Error($"Root COM error: {comErrorCode}");
            }

            return false;
        }
    }

    private bool TryGetDirectionDataWithDynamicRef(object measurable, out double[] directionData)
    {
        directionData = Array.Empty<double>();

        if (TryGetDirectionDataWithDynamicRefObjectArray(measurable, out directionData))
        {
            return true;
        }

        if (TryGetDirectionDataWithDynamicRefDoubleArray(measurable, out directionData))
        {
            return true;
        }

        return false;
    }

    private bool TryGetDirectionDataWithDynamicRefObjectArray(object measurable, out double[] directionData)
    {
        directionData = Array.Empty<double>();

        try
        {
            _logger.Info("Calling Measurable.GetDirection with dynamic ref object array...");
            dynamic measurableDynamic = measurable;
            object directionDataObject = new object[3];

            measurableDynamic.GetDirection(ref directionDataObject);

            _logger.Info($"GetDirection dynamic object array returned type: {directionDataObject?.GetType().FullName ?? "<null>"}");
            return TryConvertComArrayToDoubleArray(directionDataObject, 3, "Direction", out directionData);
        }
        catch (Exception ex)
        {
            var rootCause = ExceptionUtils.GetRootCause(ex);
            var comErrorCode = ExceptionUtils.GetComErrorCode(ex);

            _logger.Error(ex, "Measurable.GetDirection failed with dynamic ref object array.");
            _logger.Error($"Root cause: {rootCause.Message}");
            if (!string.IsNullOrWhiteSpace(comErrorCode))
            {
                _logger.Error($"Root COM error: {comErrorCode}");
            }

            return false;
        }
    }

    private bool TryGetDirectionDataWithDynamicRefDoubleArray(object measurable, out double[] directionData)
    {
        directionData = Array.Empty<double>();

        try
        {
            _logger.Info("Calling Measurable.GetDirection with dynamic ref double array...");
            dynamic measurableDynamic = measurable;
            object directionDataObject = new double[3];

            measurableDynamic.GetDirection(ref directionDataObject);

            _logger.Info($"GetDirection dynamic double array returned type: {directionDataObject?.GetType().FullName ?? "<null>"}");
            return TryConvertComArrayToDoubleArray(directionDataObject, 3, "Direction", out directionData);
        }
        catch (Exception ex)
        {
            var rootCause = ExceptionUtils.GetRootCause(ex);
            var comErrorCode = ExceptionUtils.GetComErrorCode(ex);

            _logger.Error(ex, "Measurable.GetDirection failed with dynamic ref double array.");
            _logger.Error($"Root cause: {rootCause.Message}");
            if (!string.IsNullOrWhiteSpace(comErrorCode))
            {
                _logger.Error($"Root COM error: {comErrorCode}");
            }

            return false;
        }
    }

    private bool TryConvertComArrayToDoubleArray(object? arrayObject, int expectedLength, string label, out double[] values)
    {
        values = Array.Empty<double>();

        if (arrayObject is null)
        {
            _logger.Error($"{label} data object is null.");
            return false;
        }

        var rawValues = new object?[expectedLength];
        if (arrayObject is double[] doubleValues)
        {
            if (doubleValues.Length < expectedLength)
            {
                _logger.Error($"{label} data length is shorter than expected: {doubleValues.Length} < {expectedLength}");
                return false;
            }

            for (var index = 0; index < expectedLength; index++)
            {
                rawValues[index] = doubleValues[index];
            }
        }
        else if (arrayObject is object[] objectValues)
        {
            if (objectValues.Length < expectedLength)
            {
                _logger.Error($"{label} data length is shorter than expected: {objectValues.Length} < {expectedLength}");
                return false;
            }

            for (var index = 0; index < expectedLength; index++)
            {
                rawValues[index] = objectValues[index];
            }
        }
        else if (arrayObject is Array arrayValues)
        {
            if (arrayValues.Length < expectedLength)
            {
                _logger.Error($"{label} data length is shorter than expected: {arrayValues.Length} < {expectedLength}");
                return false;
            }

            for (var index = 0; index < expectedLength; index++)
            {
                rawValues[index] = arrayValues.GetValue(index);
            }
        }
        else
        {
            _logger.Error($"{label} data has unsupported type: {arrayObject.GetType().FullName}");
            return false;
        }

        _logger.Info($"{label} raw data: {FormatArray(rawValues)}");
        var convertedValues = new double[expectedLength];

        for (var index = 0; index < expectedLength; index++)
        {
            var value = rawValues[index];
            if (value is null)
            {
                _logger.Error($"{label} raw data index {index} is null.");
                return false;
            }

            try
            {
                convertedValues[index] = Convert.ToDouble(value);
            }
            catch (Exception ex)
            {
                var rootCause = ExceptionUtils.GetRootCause(ex);
                var comErrorCode = ExceptionUtils.GetComErrorCode(ex);

                _logger.Error(ex, $"Failed to convert {label} raw data index {index}: {value} ({value.GetType().FullName})");
                _logger.Error($"Root cause: {rootCause.Message}");
                if (!string.IsNullOrWhiteSpace(comErrorCode))
                {
                    _logger.Error($"Root COM error: {comErrorCode}");
                }

                return false;
            }
        }

        values = convertedValues;
        _logger.Info($"{label} converted data: {FormatArray(values)}");
        return true;
    }

    private static string NormalizeViewSide(string viewSide)
    {
        if (string.Equals(viewSide, "Normal", StringComparison.OrdinalIgnoreCase))
        {
            return "Normal";
        }

        if (string.Equals(viewSide, "Opposite", StringComparison.OrdinalIgnoreCase))
        {
            return "Opposite";
        }

        throw new InvalidOperationException($"Unsupported view side value: {viewSide}");
    }

    private static int NormalizeViewRotation(int viewRotation)
    {
        return viewRotation switch
        {
            0 or 90 or 180 or 270 => viewRotation,
            _ => throw new InvalidOperationException($"Unsupported view rotation value: {viewRotation}")
        };
    }

    private static (DirectionVector ViewUp, DirectionVector ViewRight) ApplyViewRotation(
        DirectionVector baseUp,
        DirectionVector baseRight,
        int viewRotation)
    {
        return viewRotation switch
        {
            0 => (baseUp, baseRight),
            90 => (baseRight, baseUp.Reverse()),
            180 => (baseUp.Reverse(), baseRight.Reverse()),
            270 => (baseRight.Reverse(), baseUp),
            _ => throw new InvalidOperationException($"Unsupported view rotation value: {viewRotation}")
        };
    }
    private static string FormatArray(object?[] values)
    {
        return string.Join(", ", Array.ConvertAll(values, value => value?.ToString() ?? "<null>"));
    }

    private static string FormatArray(double[] values)
    {
        return string.Join(", ", Array.ConvertAll(values, value => value.ToString("0.######")));
    }

    private readonly struct DirectionVector
    {
        public DirectionVector(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public double X { get; }
        public double Y { get; }
        public double Z { get; }
        public bool IsZero => Length < 0.000001;
        private double Length => Math.Sqrt((X * X) + (Y * Y) + (Z * Z));

        public static DirectionVector FromDirection(string direction)
        {
            return direction.Trim().ToUpperInvariant() switch
            {
                "+X" => new DirectionVector(1.0, 0.0, 0.0),
                "-X" => new DirectionVector(-1.0, 0.0, 0.0),
                "+Y" => new DirectionVector(0.0, 1.0, 0.0),
                "-Y" => new DirectionVector(0.0, -1.0, 0.0),
                "+Z" => new DirectionVector(0.0, 0.0, 1.0),
                "-Z" => new DirectionVector(0.0, 0.0, -1.0),
                _ => throw new ArgumentException($"Unsupported direction value: {direction}", nameof(direction))
            };
        }

        public bool IsParallelTo(DirectionVector other)
        {
            var cross = Cross(other);
            return cross.Length < 0.000001;
        }

        public bool IsPerpendicularTo(DirectionVector other)
        {
            return Math.Abs(Dot(other)) < 0.000001;
        }

        public DirectionVector Reverse()
        {
            return new DirectionVector(-X, -Y, -Z);
        }

        public DirectionVector RemoveComponentAlong(DirectionVector normal)
        {
            var normalizedNormal = normal.Normalize();
            var component = Dot(normalizedNormal);
            return new DirectionVector(
                X - (component * normalizedNormal.X),
                Y - (component * normalizedNormal.Y),
                Z - (component * normalizedNormal.Z));
        }

        public DirectionVector Normalize()
        {
            var length = Length;
            return length < 0.000001
                ? new DirectionVector(0.0, 0.0, 0.0)
                : new DirectionVector(X / length, Y / length, Z / length);
        }

        public DirectionVector Cross(DirectionVector other)
        {
            return new DirectionVector(
                (Y * other.Z) - (Z * other.Y),
                (Z * other.X) - (X * other.Z),
                (X * other.Y) - (Y * other.X));
        }

        private double Dot(DirectionVector other)
        {
            return (X * other.X) + (Y * other.Y) + (Z * other.Z);
        }

        public override string ToString()
        {
            return $"{X:0.######}, {Y:0.######}, {Z:0.######}";
        }
    }

    private readonly struct MarkerOrientation
    {
        public MarkerOrientation(DirectionVector frontVector, DirectionVector topVector)
        {
            FrontVector = frontVector;
            TopVector = topVector;
        }

        public DirectionVector FrontVector { get; }
        public DirectionVector TopVector { get; }
    }
}




