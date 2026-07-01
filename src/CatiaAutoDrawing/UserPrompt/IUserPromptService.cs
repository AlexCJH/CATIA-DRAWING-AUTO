namespace CatiaAutoDrawing.UserPrompt;

/// <summary>
/// Role: Handles user-facing confirmations without mixing UI concerns into CATIA workflow services.
/// </summary>
public interface IUserPromptService
{
    bool ConfirmManualDimensionInput();
}
