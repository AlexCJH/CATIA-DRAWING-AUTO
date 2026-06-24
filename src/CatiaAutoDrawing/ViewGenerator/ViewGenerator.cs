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
        string topDirection)
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

            var frontVector = orientationResult.Value.FrontVector;
            var topVector = orientationResult.Value.TopVector;

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
                    frontVector.X,
                    frontVector.Y,
                    frontVector.Z,
                    topVector.X,
                    topVector.Y,
                    topVector.Z);
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

        var topDirection = FindMarkerByName(drawingInfoGeoSet, "TOP_DIRECTION");
        if (topDirection is null)
        {
            const string message = "TOP_DIRECTION not found.";
            _logger.Error(message);
            return Result<MarkerOrientation>.Failure(message);
        }

        _logger.Info("Required marker found: TOP_DIRECTION");

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

    private static bool TryExtractPlaneNormal(object measurable, out DirectionVector normal)
    {
        normal = default;

        try
        {
            var planeData = new double[9];
            InvokeComMethod(measurable, "GetPlane", planeData);

            var firstDirection = new DirectionVector(planeData[3], planeData[4], planeData[5]);
            var secondDirection = new DirectionVector(planeData[6], planeData[7], planeData[8]);
            normal = firstDirection.Cross(secondDirection).Normalize();
            return !normal.IsZero;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryExtractDirection(object measurable, out DirectionVector direction)
    {
        direction = default;

        try
        {
            var directionData = new double[3];
            InvokeComMethod(measurable, "GetDirection", directionData);

            direction = new DirectionVector(directionData[0], directionData[1], directionData[2]).Normalize();
            return !direction.IsZero;
        }
        catch
        {
            return false;
        }
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

