namespace CatiaAutoDrawing.Config;

/// <summary>
/// Role: Strongly typed model for config/appsettings.json.
/// TODO: Add validation rules for required paths and marker names.
/// </summary>
public sealed class AppSettings
{
    public AppSettings()
    {
        DrawingTemplates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["A4"] = "templates/STD_A4_TEMPLATE.CATDrawing",
            ["A3"] = "templates/STD_A3_TEMPLATE.CATDrawing",
            ["A2"] = "templates/STD_A2_TEMPLATE.CATDrawing",
            ["A1"] = "templates/STD_A1_TEMPLATE.CATDrawing"
        };
    }

    public string CatiaVersion { get; set; } = "V5R35";
    public string DefaultTemplatePath { get; set; } = "templates/STD_A3_TEMPLATE.CATDrawing";
    public string DefaultOutputFolder { get; set; } = "output";
    public string DefaultLogFolder { get; set; } = "logs";
    public string ProjectionMethod { get; set; } = "ThirdAngle";
    public string DefaultSheetSize { get; set; } = "A3";
    public double DefaultScale { get; set; } = 1.0;
    public string RequiredGeoSetName { get; set; } = "GS_DRAWING_INFO";
    public string MainViewPlaneName { get; set; } = "MAIN_VIEW_PLANE";
    public string TopDirectionName { get; set; } = "TOP_DIRECTION";
    public string MatchingFacePrefix { get; set; } = "MATCHING_FACE_";
    public string AssemblyFacePrefix { get; set; } = "ASSEMBLY_FACE_";
    public string SectionPlanePrefix { get; set; } = "SECTION_";
    public string DetailAreaPrefix { get; set; } = "DETAIL_";
    public bool EnablePdfExport { get; set; }
    public Dictionary<string, string> DrawingTemplates { get; set; }
}
