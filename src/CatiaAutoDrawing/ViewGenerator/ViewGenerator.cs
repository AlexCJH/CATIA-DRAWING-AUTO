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

    public Result GenerateFrontView(object drawingDocument, object sourceDocument)
    {
        _logger.Info("Front view generation started.");

        try
        {
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
                InvokeComMethod(generativeBehavior, "DefineFrontView", 1.0, 0.0, 0.0, 0.0, 0.0, 1.0);
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

    private static object? GetComProperty(object comObject, string propertyName)
    {
        return comObject.GetType().InvokeMember(
            propertyName,
            BindingFlags.GetProperty,
            binder: null,
            target: comObject,
            args: null);
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
}
