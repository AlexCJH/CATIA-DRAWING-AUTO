using System.Windows.Forms;

namespace CatiaAutoDrawing.UserPrompt;

/// <summary>
/// Role: Shows blocking WinForms prompts for manual CAD interaction checkpoints.
/// </summary>
public sealed class MessageBoxUserPromptService : IUserPromptService
{
    private readonly IWin32Window? _owner;

    public MessageBoxUserPromptService(IWin32Window? owner)
    {
        _owner = owner;
    }

    public bool ConfirmManualDimensionInput()
    {
        const string message = "CATIA 도면 View 생성이 완료되었습니다.\nCATIA에서 FRONT_VIEW에 수동 치수를 1개 이상 기입한 뒤 [확인]을 누르세요.\n확인 후 프로그램이 수동 치수 객체를 진단합니다.";
        const string title = "Manual Dimension Diagnostics";

        var result = MessageBox.Show(
            _owner,
            message,
            title,
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Information);

        return result == DialogResult.OK;
    }
}
