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
    private const string TopViewName = "TOP_VIEW";
    private const string RightViewName = "RIGHT_VIEW";
    private const double FrontViewX = 200.0;
    private const double FrontViewY = 150.0;
    private const double ProjectionViewOffsetX = 180.0;
    private const double ProjectionViewOffsetY = 120.0;
    private const int CatRightProjectionViewType = 0;
    private const int CatTopProjectionViewType = 2;

    private readonly ILogger _logger;
    private ViewBasis? _lastFrontViewBasis;

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
            var frontNormalForOrthographicViews = viewRight.Cross(viewUp).Normalize();
            if (frontNormalForOrthographicViews.IsZero)
            {
                const string message = "Front normal vector is zero after viewRight/viewUp cross product.";
                _logger.Error(message);
                return Result.Failure(message);
            }

            _lastFrontViewBasis = new ViewBasis(viewRight, viewUp, frontNormalForOrthographicViews);
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

            SetComProperty(frontView, "x", FrontViewX);
            SetComProperty(frontView, "y", FrontViewY);
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
            _logger.Warning($"Root cause: {rootCause.Message}");
            if (!string.IsNullOrWhiteSpace(comErrorCode))
            {
                _logger.Warning($"Root COM error: {comErrorCode}");
            }

            return Result.Failure(message);
        }
    }

    public Result GenerateProjectionViews(object drawingDocument, object sourceDocument)
    {
        _logger.Info("Projection view generation started.");
        _logger.Info("STEP 5A CATIA API Projection View experiment started.");
        _logger.Info("Stable independent generative view fallback is enabled.");

        try
        {
            if (!_lastFrontViewBasis.HasValue)
            {
                const string message = "Front view orientation basis is not available for orthographic views.";
                _logger.Error(message);
                return Result.Failure(message);
            }

            var firstSheet = GetFirstSheet(drawingDocument);
            if (firstSheet is null)
            {
                const string message = "First drawing sheet could not be acquired for projection views.";
                _logger.Error(message);
                return Result.Failure(message);
            }

            var views = GetViews(firstSheet);
            if (views is null)
            {
                const string message = "Drawing views collection does not exist for projection views.";
                _logger.Error(message);
                return Result.Failure(message);
            }

            var frontView = FindDrawingViewByName(views, FrontViewName);
            if (frontView is null)
            {
                const string message = "FRONT_VIEW could not be acquired for projection views.";
                _logger.Error(message);
                return Result.Failure(message);
            }

            _logger.Info("FRONT_VIEW acquired for CATIA API projection.");

            var basis = _lastFrontViewBasis.Value;
            var frontScale = Convert.ToDouble(GetComProperty(frontView, "Scale") ?? 1.0);
            var apiAttemptResult = TryGenerateProjectionViewsByCatiaApi(firstSheet, views, sourceDocument, frontView, frontScale);

            if (apiAttemptResult.Outcome == ProjectionAttemptOutcome.ApiSuccessConfirmed)
            {
                _logger.Info("STEP 5A succeeded using CATIA API Projection View.");
                return Result.Success();
            }

            if (apiAttemptResult.Outcome == ProjectionAttemptOutcome.ApiCandidateNeedsManualVerification)
            {
                _logger.Warning("STEP 5A generated candidate views; manual verification required.");
                return Result.Success();
            }

            _logger.Warning("CATIA API projection view generation failed.");
            if (!string.IsNullOrWhiteSpace(apiAttemptResult.RootCauseMessage))
            {
                _logger.Warning($"API failure root cause: {apiAttemptResult.RootCauseMessage}");
            }

            if (!string.IsNullOrWhiteSpace(apiAttemptResult.ComErrorCode))
            {
                _logger.Warning($"API failure COM error: {apiAttemptResult.ComErrorCode}");
            }

            _logger.Warning("Falling back to independent generative views.");

            var fallbackNames = PrepareFallbackViewNames(views);
            var fallbackResult = GenerateIndependentProjectionViewsFallback(
                firstSheet,
                views,
                sourceDocument,
                basis,
                frontScale,
                fallbackNames.TopViewName,
                fallbackNames.RightViewName);

            if (fallbackResult.IsSuccess)
            {
                _logger.Info("STEP 5A completed using independent generative fallback.");
                return Result.Success();
            }

            _logger.Error("STEP 5A failed. CATIA API projection and fallback both failed.");
            return Result.Failure(fallbackResult.ErrorMessage ?? apiAttemptResult.ErrorMessage ?? "STEP 5A failed.");
        }
        catch (Exception ex)
        {
            var rootCause = ExceptionUtils.GetRootCause(ex);
            var comErrorCode = ExceptionUtils.GetComErrorCode(ex);
            var message = $"Projection view generation failed: {ex.Message}";

            _logger.Error(ex, message);
            _logger.Error($"Root cause: {rootCause.Message}");
            if (!string.IsNullOrWhiteSpace(comErrorCode))
            {
                _logger.Error($"Root COM error: {comErrorCode}");
            }

            return Result.Failure(message);
        }
    }

    private ApiProjectionAttemptResult TryGenerateProjectionViewsByCatiaApi(
        object sheet,
        object views,
        object sourceDocument,
        object frontView,
        double frontScale)
    {
        _logger.Info("Attempting CATIA API projection views from FRONT_VIEW.");

        try
        {
            var frontGenerativeBehavior = GetComProperty(frontView, "GenerativeBehavior");
            if (frontGenerativeBehavior is null)
            {
                return ApiProjectionAttemptResult.Failed("FRONT_VIEW generative behavior does not exist.");
            }

            _logger.Info($"FRONT_VIEW generative behavior type: {frontGenerativeBehavior.GetType().FullName ?? frontGenerativeBehavior.GetType().Name}");
            _logger.Info("FRONT_VIEW update before API projection started.");
            InvokeComMethod(frontGenerativeBehavior, "Update");
            _logger.Info("FRONT_VIEW update before API projection completed.");
            _logger.Info("Sheet update before API projection started.");
            InvokeComMethod(sheet, "Update");
            _logger.Info("Sheet update before API projection completed.");

            var topResult = TryGenerateTopProjectionViewByCatiaApi(sheet, views, sourceDocument, frontGenerativeBehavior, frontScale);
            if (!topResult.IsSuccess)
            {
                return ApiProjectionAttemptResult.Failed(topResult.ErrorMessage, topResult.RootCauseMessage, topResult.ComErrorCode);
            }

            var rightResult = TryGenerateRightProjectionViewByCatiaApi(sheet, views, sourceDocument, frontGenerativeBehavior, frontScale);
            if (!rightResult.IsSuccess)
            {
                return ApiProjectionAttemptResult.Failed(rightResult.ErrorMessage, rightResult.RootCauseMessage, rightResult.ComErrorCode);
            }

            if (topResult.Assessment == ProjectionViewAssessmentStatus.ManualVerificationRequired ||
                rightResult.Assessment == ProjectionViewAssessmentStatus.ManualVerificationRequired)
            {
                return ApiProjectionAttemptResult.CandidateNeedsManualVerification("CATIA API generated candidate views; manual verification required.");
            }

            return ApiProjectionAttemptResult.SuccessConfirmed();
        }
        catch (Exception ex)
        {
            var rootCause = ExceptionUtils.GetRootCause(ex);
            var comErrorCode = ExceptionUtils.GetComErrorCode(ex);

            return ApiProjectionAttemptResult.Failed(
                $"CATIA API projection view generation failed: {ex.Message}",
                rootCause.Message,
                comErrorCode);
        }
    }

    private ApiProjectionViewResult TryGenerateTopProjectionViewByCatiaApi(
        object sheet,
        object views,
        object sourceDocument,
        object frontGenerativeBehavior,
        double scale)
    {
        _logger.Info("CATIA API TOP_VIEW generation started.");
        _logger.Info("CATIA API TOP_VIEW creation method: DrawingViews.Add -> GenerativeBehavior.Document -> DefineProjectionView -> Name/Position/Scale -> Update");
        _logger.Info($"CATIA API projection type candidate for TOP_VIEW: {CatTopProjectionViewType}");

        return TryGenerateProjectionViewByCatiaApi(
            sheet,
            views,
            sourceDocument,
            frontGenerativeBehavior,
            TopViewName,
            FrontViewX,
            FrontViewY + ProjectionViewOffsetY,
            scale,
            CatTopProjectionViewType);
    }

    private ApiProjectionViewResult TryGenerateRightProjectionViewByCatiaApi(
        object sheet,
        object views,
        object sourceDocument,
        object frontGenerativeBehavior,
        double scale)
    {
        _logger.Info("CATIA API RIGHT_VIEW generation started.");
        _logger.Info("CATIA API RIGHT_VIEW creation method: DrawingViews.Add -> GenerativeBehavior.Document -> DefineProjectionView -> Name/Position/Scale -> Update");
        _logger.Info($"CATIA API projection type candidate for RIGHT_VIEW: {CatRightProjectionViewType}");

        return TryGenerateProjectionViewByCatiaApi(
            sheet,
            views,
            sourceDocument,
            frontGenerativeBehavior,
            RightViewName,
            FrontViewX + ProjectionViewOffsetX,
            FrontViewY,
            scale,
            CatRightProjectionViewType);
    }

    private ApiProjectionViewResult TryGenerateProjectionViewByCatiaApi(
        object sheet,
        object views,
        object sourceDocument,
        object frontGenerativeBehavior,
        string viewName,
        double x,
        double y,
        double scale,
        int projectionTypeCandidate)
    {
        try
        {
            var projectionView = InvokeComMethod(views, "Add", viewName);
            if (projectionView is null)
            {
                return ApiProjectionViewResult.Failed($"CATIA API {viewName} could not be created.");
            }

            var projectionGenerativeBehavior = GetComProperty(projectionView, "GenerativeBehavior");
            if (projectionGenerativeBehavior is null)
            {
                return ApiProjectionViewResult.Failed($"CATIA API {viewName} generative behavior does not exist.");
            }

            SetComProperty(projectionGenerativeBehavior, "Document", sourceDocument);
            InvokeComMethod(
                projectionGenerativeBehavior,
                "DefineProjectionView",
                frontGenerativeBehavior,
                projectionTypeCandidate);

            SetComProperty(projectionView, "Name", viewName);
            SetComProperty(projectionView, "x", x);
            SetComProperty(projectionView, "y", y);
            SetComProperty(projectionView, "Scale", scale);

            _logger.Info($"CATIA API {viewName} update started.");
            TryInvokeOptionalComMethod(projectionView, "Update", $"CATIA API {viewName} DrawingView.Update");
            InvokeComMethod(projectionGenerativeBehavior, "Update");
            InvokeComMethod(sheet, "Update");
            _logger.Info($"CATIA API {viewName} update completed.");

            var assessment = AssessApiProjectionView(projectionView, viewName);
            return assessment switch
            {
                ProjectionViewAssessmentStatus.Confirmed => ApiProjectionViewResult.SuccessConfirmed(),
                ProjectionViewAssessmentStatus.ManualVerificationRequired => ApiProjectionViewResult.CandidateNeedsManualVerification(),
                ProjectionViewAssessmentStatus.FailedEmpty => ApiProjectionViewResult.Failed($"CATIA API {viewName} appears to be empty after update."),
                _ => ApiProjectionViewResult.Failed($"CATIA API {viewName} assessment failed.")
            };
        }
        catch (Exception ex)
        {
            var rootCause = ExceptionUtils.GetRootCause(ex);
            var comErrorCode = ExceptionUtils.GetComErrorCode(ex);
            return ApiProjectionViewResult.Failed(
                $"CATIA API {viewName} generation failed: {ex.Message}",
                rootCause.Message,
                comErrorCode);
        }
    }

    private ProjectionViewAssessmentStatus AssessApiProjectionView(object projectionView, string viewName)
    {
        if (!TryGetDrawingViewSize(projectionView, out var size))
        {
            _logger.Warning($"CATIA API {viewName} size validation could not be confirmed.");
            _logger.Warning($"CATIA API {viewName} generated candidate view; manual verification required.");
            return ProjectionViewAssessmentStatus.ManualVerificationRequired;
        }

        var width = Math.Abs(size[1] - size[0]);
        var height = Math.Abs(size[3] - size[2]);
        _logger.Info($"CATIA API {viewName} size: {FormatArray(size)}");

        if (width < 0.000001 || height < 0.000001)
        {
            _logger.Error("Projection view appears to be empty after update.");
            _logger.Error("Projection view generation did not create visible geometry.");
            return ProjectionViewAssessmentStatus.FailedEmpty;
        }

        _logger.Info($"CATIA API {viewName} generated successfully.");
        return ProjectionViewAssessmentStatus.Confirmed;
    }

    private Result GenerateIndependentProjectionViewsFallback(
        object sheet,
        object views,
        object sourceDocument,
        ViewBasis frontBasis,
        double scale,
        string topViewName,
        string rightViewName)
    {
        _logger.Info("Independent generative fallback started.");

        var topResult = GenerateIndependentTopView(sheet, views, sourceDocument, frontBasis, scale, topViewName);
        if (!topResult.IsSuccess)
        {
            return topResult;
        }

        var rightResult = GenerateIndependentRightView(sheet, views, sourceDocument, frontBasis, scale, rightViewName);
        if (!rightResult.IsSuccess)
        {
            return rightResult;
        }

        _logger.Info("Independent generative fallback succeeded.");
        return Result.Success();
    }

    private FallbackViewNames PrepareFallbackViewNames(object views)
    {
        var topFallbackName = TopViewName;
        var rightFallbackName = RightViewName;

        if (FindDrawingViewByName(views, TopViewName) is not null)
        {
            if (!TryRenameView(views, TopViewName, "TOP_VIEW_API_FAILED"))
            {
                topFallbackName = "TOP_VIEW_FALLBACK";
            }
        }

        if (FindDrawingViewByName(views, RightViewName) is not null)
        {
            if (!TryRenameView(views, RightViewName, "RIGHT_VIEW_API_FAILED"))
            {
                rightFallbackName = "RIGHT_VIEW_FALLBACK";
            }
        }

        return new FallbackViewNames(topFallbackName, rightFallbackName);
    }

    private bool TryRenameView(object views, string currentName, string newName)
    {
        try
        {
            var view = FindDrawingViewByName(views, currentName);
            if (view is null)
            {
                return true;
            }

            SetComProperty(view, "Name", newName);
            _logger.Warning($"Renamed API candidate view {currentName} to {newName} before fallback generation.");
            return true;
        }
        catch (Exception ex)
        {
            var rootCause = ExceptionUtils.GetRootCause(ex);
            var comErrorCode = ExceptionUtils.GetComErrorCode(ex);

            _logger.Warning($"Failed to rename {currentName} before fallback generation.");
            _logger.Warning($"Fallback rename root cause: {rootCause.Message}");
            if (!string.IsNullOrWhiteSpace(comErrorCode))
            {
                _logger.Warning($"Fallback rename COM error: {comErrorCode}");
            }

            return false;
        }
    }

    private void TryInvokeOptionalComMethod(object comObject, string methodName, string displayName)
    {
        try
        {
            InvokeComMethod(comObject, methodName);
        }
        catch (Exception ex)
        {
            var rootCause = ExceptionUtils.GetRootCause(ex);
            var comErrorCode = ExceptionUtils.GetComErrorCode(ex);

            _logger.Warning($"{displayName} could not be confirmed.");
            _logger.Warning($"Optional update root cause: {rootCause.Message}");
            if (!string.IsNullOrWhiteSpace(comErrorCode))
            {
                _logger.Warning($"Optional update COM error: {comErrorCode}");
            }
        }
    }

    private Result GenerateIndependentTopView(
        object sheet,
        object views,
        object sourceDocument,
        ViewBasis frontBasis,
        double scale,
        string viewName)
    {
        return CreateGenerativeView(
            sheet,
            views,
            sourceDocument,
            viewName,
            frontBasis.FrontRight,
            frontBasis.FrontNormal.Reverse(),
            FrontViewX,
            FrontViewY + ProjectionViewOffsetY,
            scale);
    }

    private Result GenerateIndependentTopView(
        object sheet,
        object views,
        object sourceDocument,
        ViewBasis frontBasis,
        double scale)
    {
        return GenerateIndependentTopView(sheet, views, sourceDocument, frontBasis, scale, TopViewName);
    }

    private Result GenerateIndependentRightView(
        object sheet,
        object views,
        object sourceDocument,
        ViewBasis frontBasis,
        double scale,
        string viewName)
    {
        return CreateGenerativeView(
            sheet,
            views,
            sourceDocument,
            viewName,
            frontBasis.FrontNormal.Reverse(),
            frontBasis.FrontUp,
            FrontViewX + ProjectionViewOffsetX,
            FrontViewY,
            scale);
    }

    private Result GenerateIndependentRightView(
        object sheet,
        object views,
        object sourceDocument,
        ViewBasis frontBasis,
        double scale)
    {
        return GenerateIndependentRightView(sheet, views, sourceDocument, frontBasis, scale, RightViewName);
    }

    private void LogIndependentViewTarget(string viewName)
    {
        if (viewName.StartsWith(TopViewName, StringComparison.OrdinalIgnoreCase))
        {
            _logger.Info("TOP_VIEW target side: Front view upper side.");
            _logger.Info("TOP_VIEW expected normal: frontUp");
            return;
        }

        if (viewName.StartsWith(RightViewName, StringComparison.OrdinalIgnoreCase))
        {
            _logger.Info("RIGHT_VIEW target side: Front view right side.");
            _logger.Info("RIGHT_VIEW expected normal: frontRight");
        }
    }

    private Result CreateGenerativeView(
        object sheet,
        object views,
        object sourceDocument,
        string viewName,
        DirectionVector vector1,
        DirectionVector vector2,
        double x,
        double y,
        double scale)
    {
        LogIndependentViewTarget(viewName);
        _logger.Info($"Creating {viewName} as independent generative view.");
        _logger.Info($"{viewName} vector 1: {vector1}");
        _logger.Info($"{viewName} vector 2: {vector2}");

        try
        {
            var drawingView = InvokeComMethod(views, "Add", viewName);
            if (drawingView is null)
            {
                var message = $"{viewName} could not be created.";
                _logger.Error(message);
                return Result.Failure(message);
            }

            SetComProperty(drawingView, "Name", viewName);
            SetComProperty(drawingView, "x", x);
            SetComProperty(drawingView, "y", y);
            SetComProperty(drawingView, "Scale", scale);

            var generativeBehavior = GetComProperty(drawingView, "GenerativeBehavior");
            if (generativeBehavior is null)
            {
                var message = $"{viewName} generative behavior does not exist.";
                _logger.Error(message);
                return Result.Failure(message);
            }

            SetComProperty(generativeBehavior, "Document", sourceDocument);
            InvokeComMethod(
                generativeBehavior,
                "DefineFrontView",
                vector1.X,
                vector1.Y,
                vector1.Z,
                vector2.X,
                vector2.Y,
                vector2.Z);

            InvokeComMethod(generativeBehavior, "Update");
            _logger.Info($"{viewName} update completed.");

            _logger.Info("Sheet update started.");
            InvokeComMethod(sheet, "Update");
            _logger.Info("Sheet update completed.");

            TryValidateProjectionViewHasGeometry(drawingView, viewName);

            return Result.Success();
        }
        catch (Exception ex)
        {
            var rootCause = ExceptionUtils.GetRootCause(ex);
            var comErrorCode = ExceptionUtils.GetComErrorCode(ex);
            var message = $"{viewName} generation failed: {ex.Message}";

            _logger.Error(ex, message);
            _logger.Warning($"Root cause: {rootCause.Message}");
            if (!string.IsNullOrWhiteSpace(comErrorCode))
            {
                _logger.Warning($"Root COM error: {comErrorCode}");
            }

            return Result.Failure(message);
        }
    }

    private bool TryValidateProjectionViewHasGeometry(object projectionView, string displayName)
    {
        if (!TryGetDrawingViewSize(projectionView, out var size))
        {
            _logger.Warning("Projection view size validation could not be confirmed.");
            _logger.Warning("Skipping size validation for independent generative view experiment.");
            return true;
        }

        var width = Math.Abs(size[1] - size[0]);
        var height = Math.Abs(size[3] - size[2]);
        _logger.Info($"{displayName} projection view size: {FormatArray(size)}");

        if (width < 0.000001 || height < 0.000001)
        {
            _logger.Warning("Projection view size validation could not be confirmed.");
            _logger.Warning("Skipping size validation for independent generative view experiment.");
            return true;
        }

        return true;
    }

    private bool TryGetDrawingViewSize(object drawingView, out double[] size)
    {
        size = Array.Empty<double>();
        var args = new object[] { 0.0, 0.0, 0.0, 0.0 };
        var modifiers = new ParameterModifier(4);
        for (var index = 0; index < 4; index++)
        {
            modifiers[index] = true;
        }

        try
        {
            drawingView.GetType().InvokeMember(
                "Size",
                BindingFlags.InvokeMethod,
                binder: null,
                target: drawingView,
                args: args,
                modifiers: new[] { modifiers },
                culture: null,
                namedParameters: null);

            size = new[]
            {
                Convert.ToDouble(args[0]),
                Convert.ToDouble(args[1]),
                Convert.ToDouble(args[2]),
                Convert.ToDouble(args[3])
            };

            return true;
        }
        catch (Exception ex)
        {
            var rootCause = ExceptionUtils.GetRootCause(ex);
            var comErrorCode = ExceptionUtils.GetComErrorCode(ex);

            _logger.Warning("Projection view size validation could not be confirmed.");
            _logger.Warning($"Root cause: {rootCause.Message}");
            if (!string.IsNullOrWhiteSpace(comErrorCode))
            {
                _logger.Warning($"Root COM error: {comErrorCode}");
            }

            return false;
        }
    }

    private static object? GetFirstSheet(object drawingDocument)
    {
        var sheets = GetComProperty(drawingDocument, "Sheets");
        return sheets is null ? null : InvokeComMethod(sheets, "Item", 1);
    }

    private static object? GetViews(object sheet)
    {
        return GetComProperty(sheet, "Views");
    }

    private static object? FindDrawingViewByName(object views, string viewName)
    {
        var count = Convert.ToInt32(GetComProperty(views, "Count"));
        for (var index = 1; index <= count; index++)
        {
            var view = InvokeComMethod(views, "Item", index);
            if (view is null)
            {
                continue;
            }

            var name = Convert.ToString(GetComProperty(view, "Name"));
            if (string.Equals(name, viewName, StringComparison.OrdinalIgnoreCase))
            {
                return view;
            }
        }

        return null;
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
            _logger.Warning($"Root cause: {rootCause.Message}");
            if (!string.IsNullOrWhiteSpace(comErrorCode))
            {
                _logger.Warning($"Root COM error: {comErrorCode}");
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
            _logger.Warning($"Root cause: {rootCause.Message}");
            if (!string.IsNullOrWhiteSpace(comErrorCode))
            {
                _logger.Warning($"Root COM error: {comErrorCode}");
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
            _logger.Warning($"Root cause: {rootCause.Message}");
            if (!string.IsNullOrWhiteSpace(comErrorCode))
            {
                _logger.Warning($"Root COM error: {comErrorCode}");
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
            _logger.Warning($"Root cause: {rootCause.Message}");
            if (!string.IsNullOrWhiteSpace(comErrorCode))
            {
                _logger.Warning($"Root COM error: {comErrorCode}");
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
                _logger.Warning($"Root cause: {rootCause.Message}");
                if (!string.IsNullOrWhiteSpace(comErrorCode))
                {
                    _logger.Warning($"Root COM error: {comErrorCode}");
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

    private enum ProjectionAttemptOutcome
    {
        ApiSuccessConfirmed,
        ApiCandidateNeedsManualVerification,
        ApiFailed
    }

    private enum ProjectionViewAssessmentStatus
    {
        Confirmed,
        ManualVerificationRequired,
        FailedEmpty
    }

    private readonly struct ApiProjectionAttemptResult
    {
        private ApiProjectionAttemptResult(
            ProjectionAttemptOutcome outcome,
            string? errorMessage,
            string? rootCauseMessage,
            string? comErrorCode)
        {
            Outcome = outcome;
            ErrorMessage = errorMessage;
            RootCauseMessage = rootCauseMessage;
            ComErrorCode = comErrorCode;
        }

        public ProjectionAttemptOutcome Outcome { get; }
        public string? ErrorMessage { get; }
        public string? RootCauseMessage { get; }
        public string? ComErrorCode { get; }

        public static ApiProjectionAttemptResult SuccessConfirmed()
            => new(ProjectionAttemptOutcome.ApiSuccessConfirmed, null, null, null);

        public static ApiProjectionAttemptResult CandidateNeedsManualVerification(string? message)
            => new(ProjectionAttemptOutcome.ApiCandidateNeedsManualVerification, message, null, null);

        public static ApiProjectionAttemptResult Failed(string? message, string? rootCauseMessage = null, string? comErrorCode = null)
            => new(ProjectionAttemptOutcome.ApiFailed, message, rootCauseMessage, comErrorCode);
    }

    private readonly struct ApiProjectionViewResult
    {
        private ApiProjectionViewResult(
            bool isSuccess,
            ProjectionViewAssessmentStatus assessment,
            string? errorMessage,
            string? rootCauseMessage,
            string? comErrorCode)
        {
            IsSuccess = isSuccess;
            Assessment = assessment;
            ErrorMessage = errorMessage;
            RootCauseMessage = rootCauseMessage;
            ComErrorCode = comErrorCode;
        }

        public bool IsSuccess { get; }
        public ProjectionViewAssessmentStatus Assessment { get; }
        public string? ErrorMessage { get; }
        public string? RootCauseMessage { get; }
        public string? ComErrorCode { get; }

        public static ApiProjectionViewResult SuccessConfirmed()
            => new(true, ProjectionViewAssessmentStatus.Confirmed, null, null, null);

        public static ApiProjectionViewResult CandidateNeedsManualVerification()
            => new(true, ProjectionViewAssessmentStatus.ManualVerificationRequired, null, null, null);

        public static ApiProjectionViewResult Failed(string? message, string? rootCauseMessage = null, string? comErrorCode = null)
            => new(false, ProjectionViewAssessmentStatus.FailedEmpty, message, rootCauseMessage, comErrorCode);
    }

    private readonly struct FallbackViewNames
    {
        public FallbackViewNames(string topViewName, string rightViewName)
        {
            TopViewName = topViewName;
            RightViewName = rightViewName;
        }

        public string TopViewName { get; }
        public string RightViewName { get; }
    }
    private readonly struct ViewBasis
    {
        public ViewBasis(DirectionVector frontRight, DirectionVector frontUp, DirectionVector frontNormal)
        {
            FrontRight = frontRight;
            FrontUp = frontUp;
            FrontNormal = frontNormal;
        }

        public DirectionVector FrontRight { get; }
        public DirectionVector FrontUp { get; }
        public DirectionVector FrontNormal { get; }
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

