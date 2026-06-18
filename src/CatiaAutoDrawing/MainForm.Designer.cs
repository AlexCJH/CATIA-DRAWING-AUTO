using System.Windows.Forms;

namespace CatiaAutoDrawing;

/// <summary>
/// Role: WinForms control layout for the initial MVP harness.
/// TODO: Keep controls minimal until each workflow step is implemented.
/// </summary>
partial class MainForm
{
    private Button checkConnectionButton = null!;
    private Button readActiveDocumentButton = null!;
    private Button runDrawingButton = null!;
    private CheckBox enablePdfExportCheckBox = null!;
    private TextBox logTextBox = null!;
    private Label connectionStatusLabel = null!;
    private Label activeDocumentNameLabel = null!;
    private Label activeDocumentTypeLabel = null!;

    private void InitializeComponent()
    {
        checkConnectionButton = new Button();
        readActiveDocumentButton = new Button();
        runDrawingButton = new Button();
        enablePdfExportCheckBox = new CheckBox();
        logTextBox = new TextBox();
        connectionStatusLabel = new Label();
        activeDocumentNameLabel = new Label();
        activeDocumentTypeLabel = new Label();

        SuspendLayout();

        checkConnectionButton.Location = new System.Drawing.Point(16, 16);
        checkConnectionButton.Name = "checkConnectionButton";
        checkConnectionButton.Size = new System.Drawing.Size(160, 32);
        checkConnectionButton.TabIndex = 0;
        checkConnectionButton.Text = "CATIA 연결 확인";
        checkConnectionButton.UseVisualStyleBackColor = true;
        checkConnectionButton.Click += CheckConnectionButton_Click;

        readActiveDocumentButton.Location = new System.Drawing.Point(184, 16);
        readActiveDocumentButton.Name = "readActiveDocumentButton";
        readActiveDocumentButton.Size = new System.Drawing.Size(160, 32);
        readActiveDocumentButton.TabIndex = 1;
        readActiveDocumentButton.Text = "활성 문서 읽기";
        readActiveDocumentButton.UseVisualStyleBackColor = true;
        readActiveDocumentButton.Click += ReadActiveDocumentButton_Click;

        runDrawingButton.Enabled = false;
        runDrawingButton.Location = new System.Drawing.Point(352, 16);
        runDrawingButton.Name = "runDrawingButton";
        runDrawingButton.Size = new System.Drawing.Size(160, 32);
        runDrawingButton.TabIndex = 2;
        runDrawingButton.Text = "도면 생성 실행";
        runDrawingButton.UseVisualStyleBackColor = true;
        runDrawingButton.Click += RunDrawingButton_Click;

        enablePdfExportCheckBox.AutoSize = true;
        enablePdfExportCheckBox.Enabled = false;
        enablePdfExportCheckBox.Location = new System.Drawing.Point(528, 23);
        enablePdfExportCheckBox.Name = "enablePdfExportCheckBox";
        enablePdfExportCheckBox.Size = new System.Drawing.Size(74, 19);
        enablePdfExportCheckBox.TabIndex = 3;
        enablePdfExportCheckBox.Text = "PDF 출력";
        enablePdfExportCheckBox.UseVisualStyleBackColor = true;

        connectionStatusLabel.AutoSize = true;
        connectionStatusLabel.Location = new System.Drawing.Point(16, 68);
        connectionStatusLabel.Name = "connectionStatusLabel";
        connectionStatusLabel.Size = new System.Drawing.Size(142, 15);
        connectionStatusLabel.TabIndex = 4;
        connectionStatusLabel.Text = "CATIA connection: -";

        activeDocumentNameLabel.AutoSize = true;
        activeDocumentNameLabel.Location = new System.Drawing.Point(16, 96);
        activeDocumentNameLabel.Name = "activeDocumentNameLabel";
        activeDocumentNameLabel.Size = new System.Drawing.Size(110, 15);
        activeDocumentNameLabel.TabIndex = 5;
        activeDocumentNameLabel.Text = "Active document: -";

        activeDocumentTypeLabel.AutoSize = true;
        activeDocumentTypeLabel.Location = new System.Drawing.Point(16, 124);
        activeDocumentTypeLabel.Name = "activeDocumentTypeLabel";
        activeDocumentTypeLabel.Size = new System.Drawing.Size(101, 15);
        activeDocumentTypeLabel.TabIndex = 6;
        activeDocumentTypeLabel.Text = "Document type: -";

        logTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        logTextBox.Location = new System.Drawing.Point(16, 160);
        logTextBox.Multiline = true;
        logTextBox.Name = "logTextBox";
        logTextBox.ReadOnly = true;
        logTextBox.ScrollBars = ScrollBars.Vertical;
        logTextBox.Size = new System.Drawing.Size(656, 288);
        logTextBox.TabIndex = 7;

        AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new System.Drawing.Size(688, 464);
        Controls.Add(logTextBox);
        Controls.Add(activeDocumentTypeLabel);
        Controls.Add(activeDocumentNameLabel);
        Controls.Add(connectionStatusLabel);
        Controls.Add(enablePdfExportCheckBox);
        Controls.Add(runDrawingButton);
        Controls.Add(readActiveDocumentButton);
        Controls.Add(checkConnectionButton);
        MinimumSize = new System.Drawing.Size(704, 503);
        Name = "MainForm";
        Text = "CATIA V5 R35 자동도면 생성기";
        ResumeLayout(false);
        PerformLayout();
    }
}
