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
    private Button inspectModelButton = null!;
    private Button runDrawingButton = null!;
    private ComboBox drawingSizeComboBox = null!;
    private Label drawingSizeLabel = null!;
    private ComboBox frontViewDirectionComboBox = null!;
    private Label frontViewDirectionLabel = null!;
    private ComboBox topDirectionComboBox = null!;
    private Label topDirectionLabel = null!;
    private CheckBox enablePdfExportCheckBox = null!;
    private TextBox logTextBox = null!;
    private Label connectionStatusLabel = null!;
    private Label activeDocumentNameLabel = null!;
    private Label activeDocumentTypeLabel = null!;
    private Label modelInspectionStatusLabel = null!;

    private void InitializeComponent()
    {
        checkConnectionButton = new Button();
        readActiveDocumentButton = new Button();
        inspectModelButton = new Button();
        runDrawingButton = new Button();
        drawingSizeComboBox = new ComboBox();
        drawingSizeLabel = new Label();
        frontViewDirectionComboBox = new ComboBox();
        frontViewDirectionLabel = new Label();
        topDirectionComboBox = new ComboBox();
        topDirectionLabel = new Label();
        enablePdfExportCheckBox = new CheckBox();
        logTextBox = new TextBox();
        connectionStatusLabel = new Label();
        activeDocumentNameLabel = new Label();
        activeDocumentTypeLabel = new Label();
        modelInspectionStatusLabel = new Label();

        SuspendLayout();

        checkConnectionButton.Location = new System.Drawing.Point(16, 16);
        checkConnectionButton.Name = "checkConnectionButton";
        checkConnectionButton.Size = new System.Drawing.Size(160, 32);
        checkConnectionButton.TabIndex = 0;
        checkConnectionButton.Text = "CATIA connection";
        checkConnectionButton.UseVisualStyleBackColor = true;
        checkConnectionButton.Click += CheckConnectionButton_Click;

        readActiveDocumentButton.Location = new System.Drawing.Point(184, 16);
        readActiveDocumentButton.Name = "readActiveDocumentButton";
        readActiveDocumentButton.Size = new System.Drawing.Size(160, 32);
        readActiveDocumentButton.TabIndex = 1;
        readActiveDocumentButton.Text = "Read active document";
        readActiveDocumentButton.UseVisualStyleBackColor = true;
        readActiveDocumentButton.Click += ReadActiveDocumentButton_Click;

        inspectModelButton.Location = new System.Drawing.Point(352, 16);
        inspectModelButton.Name = "inspectModelButton";
        inspectModelButton.Size = new System.Drawing.Size(160, 32);
        inspectModelButton.TabIndex = 2;
        inspectModelButton.Text = "Inspect model";
        inspectModelButton.UseVisualStyleBackColor = true;
        inspectModelButton.Click += InspectModelButton_Click;

        runDrawingButton.Location = new System.Drawing.Point(520, 16);
        runDrawingButton.Name = "runDrawingButton";
        runDrawingButton.Size = new System.Drawing.Size(160, 32);
        runDrawingButton.TabIndex = 3;
        runDrawingButton.Text = "Run drawing";
        runDrawingButton.UseVisualStyleBackColor = true;
        runDrawingButton.Click += RunDrawingButton_Click;

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

        modelInspectionStatusLabel.AutoSize = true;
        modelInspectionStatusLabel.Location = new System.Drawing.Point(16, 152);
        modelInspectionStatusLabel.Name = "modelInspectionStatusLabel";
        modelInspectionStatusLabel.Size = new System.Drawing.Size(112, 15);
        modelInspectionStatusLabel.TabIndex = 7;
        modelInspectionStatusLabel.Text = "Model inspection: -";

        drawingSizeLabel.AutoSize = true;
        drawingSizeLabel.Location = new System.Drawing.Point(16, 176);
        drawingSizeLabel.Name = "drawingSizeLabel";
        drawingSizeLabel.Size = new System.Drawing.Size(75, 15);
        drawingSizeLabel.TabIndex = 8;
        drawingSizeLabel.Text = "Drawing size";

        drawingSizeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        drawingSizeComboBox.FormattingEnabled = true;
        drawingSizeComboBox.Items.AddRange(new object[] { "A4", "A3", "A2", "A1" });
        drawingSizeComboBox.Location = new System.Drawing.Point(104, 172);
        drawingSizeComboBox.Name = "drawingSizeComboBox";
        drawingSizeComboBox.Size = new System.Drawing.Size(88, 23);
        drawingSizeComboBox.TabIndex = 9;

        frontViewDirectionLabel.AutoSize = true;
        frontViewDirectionLabel.Location = new System.Drawing.Point(216, 176);
        frontViewDirectionLabel.Name = "frontViewDirectionLabel";
        frontViewDirectionLabel.Size = new System.Drawing.Size(83, 15);
        frontViewDirectionLabel.TabIndex = 10;
        frontViewDirectionLabel.Text = "Front direction";

        frontViewDirectionComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        frontViewDirectionComboBox.FormattingEnabled = true;
        frontViewDirectionComboBox.Items.AddRange(new object[] { "+X", "-X", "+Y", "-Y", "+Z", "-Z" });
        frontViewDirectionComboBox.Location = new System.Drawing.Point(312, 172);
        frontViewDirectionComboBox.Name = "frontViewDirectionComboBox";
        frontViewDirectionComboBox.Size = new System.Drawing.Size(72, 23);
        frontViewDirectionComboBox.TabIndex = 11;

        topDirectionLabel.AutoSize = true;
        topDirectionLabel.Location = new System.Drawing.Point(408, 176);
        topDirectionLabel.Name = "topDirectionLabel";
        topDirectionLabel.Size = new System.Drawing.Size(75, 15);
        topDirectionLabel.TabIndex = 12;
        topDirectionLabel.Text = "Top direction";

        topDirectionComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        topDirectionComboBox.FormattingEnabled = true;
        topDirectionComboBox.Items.AddRange(new object[] { "+X", "-X", "+Y", "-Y", "+Z", "-Z" });
        topDirectionComboBox.Location = new System.Drawing.Point(496, 172);
        topDirectionComboBox.Name = "topDirectionComboBox";
        topDirectionComboBox.Size = new System.Drawing.Size(72, 23);
        topDirectionComboBox.TabIndex = 13;

        enablePdfExportCheckBox.AutoSize = true;
        enablePdfExportCheckBox.Enabled = false;
        enablePdfExportCheckBox.Location = new System.Drawing.Point(592, 174);
        enablePdfExportCheckBox.Name = "enablePdfExportCheckBox";
        enablePdfExportCheckBox.Size = new System.Drawing.Size(82, 19);
        enablePdfExportCheckBox.TabIndex = 14;
        enablePdfExportCheckBox.Text = "PDF export";
        enablePdfExportCheckBox.UseVisualStyleBackColor = true;

        logTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        logTextBox.Location = new System.Drawing.Point(16, 208);
        logTextBox.Multiline = true;
        logTextBox.Name = "logTextBox";
        logTextBox.ReadOnly = true;
        logTextBox.ScrollBars = ScrollBars.Vertical;
        logTextBox.Size = new System.Drawing.Size(664, 240);
        logTextBox.TabIndex = 15;

        AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new System.Drawing.Size(696, 464);
        Controls.Add(logTextBox);
        Controls.Add(enablePdfExportCheckBox);
        Controls.Add(topDirectionComboBox);
        Controls.Add(topDirectionLabel);
        Controls.Add(frontViewDirectionComboBox);
        Controls.Add(frontViewDirectionLabel);
        Controls.Add(drawingSizeComboBox);
        Controls.Add(drawingSizeLabel);
        Controls.Add(modelInspectionStatusLabel);
        Controls.Add(activeDocumentTypeLabel);
        Controls.Add(activeDocumentNameLabel);
        Controls.Add(connectionStatusLabel);
        Controls.Add(runDrawingButton);
        Controls.Add(inspectModelButton);
        Controls.Add(readActiveDocumentButton);
        Controls.Add(checkConnectionButton);
        MinimumSize = new System.Drawing.Size(712, 503);
        Name = "MainForm";
        Text = "CATIA V5 R35 Auto Drawing Generator";
        ResumeLayout(false);
        PerformLayout();
    }
}
