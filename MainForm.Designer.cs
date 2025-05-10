namespace Picksy
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.selectLocalFolderButton = new System.Windows.Forms.Button();
            this.selectPhoneFolderButton = new System.Windows.Forms.Button();
            this.pictureBoxLeft = new System.Windows.Forms.PictureBox();
            this.pictureBoxRight = new System.Windows.Forms.PictureBox();
            this.remainingLabel = new System.Windows.Forms.Label();
            this.instructionLabel = new System.Windows.Forms.Label();
            this.thumbnailPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.rotateClockwiseButton = new System.Windows.Forms.Button();
            this.rotateCounterclockwiseButton = new System.Windows.Forms.Button();
            this.deletePromptLabel = new System.Windows.Forms.Label();
            this.settingsGroupBox = new System.Windows.Forms.GroupBox();
            this.batchSelectionMethodComboBox = new System.Windows.Forms.ComboBox();
            this.batchSelectionMethodLabel = new System.Windows.Forms.Label();
            this.skipConfirmationCheckBox = new System.Windows.Forms.CheckBox();
            this.batchTimingDescriptionLabel = new System.Windows.Forms.Label();
            this.batchSizeDescriptionLabel = new System.Windows.Forms.Label();
            this.includeSubfoldersCheckBox = new System.Windows.Forms.CheckBox();
            this.batchTimingNumericUpDown = new System.Windows.Forms.NumericUpDown();
            this.batchTimingLabel = new System.Windows.Forms.Label();
            this.batchSizeNumericUpDown = new System.Windows.Forms.NumericUpDown();
            this.batchSizeLabel = new System.Windows.Forms.Label();
            this.settingsHeaderLabel = new System.Windows.Forms.Label();
            this.logoPictureBox = new System.Windows.Forms.PictureBox();
            this.toolTipLeft = new System.Windows.Forms.ToolTip(this.components);
            this.toolTipRight = new System.Windows.Forms.ToolTip(this.components);
            this.saveAndQuitButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxLeft)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxRight)).BeginInit();
            this.thumbnailPanel.SuspendLayout();
            this.menuStrip.SuspendLayout();
            this.settingsGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.batchTimingNumericUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.batchSizeNumericUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.logoPictureBox)).BeginInit();
            this.SuspendLayout();

            this.selectLocalFolderButton.Location = new System.Drawing.Point(325, 450);
            this.selectLocalFolderButton.Name = "selectLocalFolderButton";
            this.selectLocalFolderButton.Size = new System.Drawing.Size(150, 30);
            this.selectLocalFolderButton.TabIndex = 0;
            this.selectLocalFolderButton.Text = "Select Local Folder";
            this.selectLocalFolderButton.UseVisualStyleBackColor = true;
            this.selectLocalFolderButton.Click += new System.EventHandler(this.SelectLocalFolderButton_Click);

            this.selectPhoneFolderButton.Location = new System.Drawing.Point(325, 490);
            this.selectPhoneFolderButton.Name = "selectPhoneFolderButton";
            this.selectPhoneFolderButton.Size = new System.Drawing.Size(150, 30);
            this.selectPhoneFolderButton.TabIndex = 1;
            this.selectPhoneFolderButton.Text = "Select Phone Folder (MTP)";
            this.selectPhoneFolderButton.UseVisualStyleBackColor = true;
            this.selectPhoneFolderButton.Click += new System.EventHandler(this.SelectPhoneFolderButton_Click);

            this.pictureBoxLeft.Location = new System.Drawing.Point(10, 34);
            this.pictureBoxLeft.Name = "pictureBoxLeft";
            this.pictureBoxLeft.Size = new System.Drawing.Size(380, 400);
            this.pictureBoxLeft.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBoxLeft.TabIndex = 2;
            this.pictureBoxLeft.TabStop = false;
            this.pictureBoxLeft.Click += new System.EventHandler(this.PictureBoxLeft_Click);

            this.pictureBoxRight.Location = new System.Drawing.Point(410, 34);
            this.pictureBoxRight.Name = "pictureBoxRight";
            this.pictureBoxRight.Size = new System.Drawing.Size(380, 400);
            this.pictureBoxRight.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBoxRight.TabIndex = 3;
            this.pictureBoxRight.TabStop = false;
            this.pictureBoxRight.Click += new System.EventHandler(this.PictureBoxRight_Click);

            this.remainingLabel.Location = new System.Drawing.Point(10, 540);
            this.remainingLabel.Name = "remainingLabel";
            this.remainingLabel.Size = new System.Drawing.Size(760, 30);
            this.remainingLabel.TabIndex = 4;
            this.remainingLabel.Text = "Photos remaining: 0";
            this.remainingLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            this.instructionLabel.Location = new System.Drawing.Point(10, 570);
            this.instructionLabel.Name = "instructionLabel";
            this.instructionLabel.Size = new System.Drawing.Size(760, 60);
            this.instructionLabel.TabIndex = 5;
            this.instructionLabel.Text = "Click or use Left/Right to select a photo, Up to keep both, Down to undo, Q/E to rotate, W to toggle full resolution, Del to delete batch, Space to keep all.";
            this.instructionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            this.thumbnailPanel.AutoScroll = true;
            this.thumbnailPanel.Location = new System.Drawing.Point(20, 70);
            this.thumbnailPanel.Name = "thumbnailPanel";
            this.thumbnailPanel.Size = new System.Drawing.Size(760, 400);
            this.thumbnailPanel.TabIndex = 6;
            this.thumbnailPanel.Visible = false;

            this.menuStrip.Location = new System.Drawing.Point(0, 0);
            this.menuStrip.Name = "menuStrip";
            this.menuStrip.Size = new System.Drawing.Size(800, 24);
            this.menuStrip.TabIndex = 8;
            this.menuStrip.Text = "menuStrip";

            this.rotateClockwiseButton.Location = new System.Drawing.Point(10, 444);
            this.rotateClockwiseButton.Name = "rotateClockwiseButton";
            this.rotateClockwiseButton.Size = new System.Drawing.Size(100, 30);
            this.rotateClockwiseButton.TabIndex = 9;
            this.rotateClockwiseButton.Text = "Rotate CW";
            this.rotateClockwiseButton.UseVisualStyleBackColor = true;
            this.rotateClockwiseButton.Visible = false;
            this.rotateClockwiseButton.Click += new System.EventHandler(this.RotateClockwiseButton_Click);

            this.rotateCounterclockwiseButton.Location = new System.Drawing.Point(690, 444);
            this.rotateCounterclockwiseButton.Name = "rotateCounterclockwiseButton";
            this.rotateCounterclockwiseButton.Size = new System.Drawing.Size(100, 30);
            this.rotateCounterclockwiseButton.TabIndex = 10;
            this.rotateCounterclockwiseButton.Text = "Rotate CCW";
            this.rotateCounterclockwiseButton.UseVisualStyleBackColor = true;
            this.rotateCounterclockwiseButton.Visible = false;
            this.rotateCounterclockwiseButton.Click += new System.EventHandler(this.RotateCounterclockwiseButton_Click);

            this.deletePromptLabel.Location = new System.Drawing.Point(20, 480);
            this.deletePromptLabel.Name = "deletePromptLabel";
            this.deletePromptLabel.Size = new System.Drawing.Size(760, 30);
            this.deletePromptLabel.TabIndex = 11;
            this.deletePromptLabel.Text = "Move to delete folder? Enter to confirm or Esc to cancel batch";
            this.deletePromptLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.deletePromptLabel.Visible = false;

            this.settingsGroupBox.Controls.Add(this.batchSelectionMethodComboBox);
            this.settingsGroupBox.Controls.Add(this.batchSelectionMethodLabel);
            this.settingsGroupBox.Controls.Add(this.skipConfirmationCheckBox);
            this.settingsGroupBox.Controls.Add(this.batchTimingDescriptionLabel);
            this.settingsGroupBox.Controls.Add(this.batchSizeDescriptionLabel);
            this.settingsGroupBox.Controls.Add(this.includeSubfoldersCheckBox);
            this.settingsGroupBox.Controls.Add(this.batchTimingNumericUpDown);
            this.settingsGroupBox.Controls.Add(this.batchTimingLabel);
            this.settingsGroupBox.Controls.Add(this.batchSizeNumericUpDown);
            this.settingsGroupBox.Controls.Add(this.batchSizeLabel);
            this.settingsGroupBox.Controls.Add(this.settingsHeaderLabel);
            this.settingsGroupBox.Location = new System.Drawing.Point(175, 110);
            this.settingsGroupBox.Name = "settingsGroupBox";
            this.settingsGroupBox.Size = new System.Drawing.Size(450, 360);
            this.settingsGroupBox.TabIndex = 17;
            this.settingsGroupBox.TabStop = false;

            this.batchSelectionMethodComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.batchSelectionMethodComboBox.FormattingEnabled = true;
            this.batchSelectionMethodComboBox.Location = new System.Drawing.Point(300, 298);
            this.batchSelectionMethodComboBox.Name = "batchSelectionMethodComboBox";
            this.batchSelectionMethodComboBox.Size = new System.Drawing.Size(130, 21);
            this.batchSelectionMethodComboBox.TabIndex = 9;

            this.batchSelectionMethodLabel.AutoSize = true;
            this.batchSelectionMethodLabel.Location = new System.Drawing.Point(20, 300);
            this.batchSelectionMethodLabel.Name = "batchSelectionMethodLabel";
            this.batchSelectionMethodLabel.Size = new System.Drawing.Size(132, 13);
            this.batchSelectionMethodLabel.TabIndex = 8;
            this.batchSelectionMethodLabel.Text = "Batch Selection Method:";

            this.skipConfirmationCheckBox.AutoSize = true;
            this.skipConfirmationCheckBox.Location = new System.Drawing.Point(20, 260);
            this.skipConfirmationCheckBox.Name = "skipConfirmationCheckBox";
            this.skipConfirmationCheckBox.Size = new System.Drawing.Size(179, 17);
            this.skipConfirmationCheckBox.TabIndex = 7;
            this.skipConfirmationCheckBox.Text = "Skip Confirmation between batches";
            this.skipConfirmationCheckBox.UseVisualStyleBackColor = true;

            this.batchTimingDescriptionLabel.AutoSize = true;
            this.batchTimingDescriptionLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.batchTimingDescriptionLabel.Location = new System.Drawing.Point(20, 160);
            this.batchTimingDescriptionLabel.Name = "batchTimingDescriptionLabel";
            this.batchTimingDescriptionLabel.Size = new System.Drawing.Size(260, 39);
            this.batchTimingDescriptionLabel.TabIndex = 6;
            this.batchTimingDescriptionLabel.Text = "The maximum amount of seconds between photos to\r\nstill be considered part of the same batch";

            this.batchSizeDescriptionLabel.AutoSize = true;
            this.batchSizeDescriptionLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.batchSizeDescriptionLabel.Location = new System.Drawing.Point(20, 80);
            this.batchSizeDescriptionLabel.Name = "batchSizeDescriptionLabel";
            this.batchSizeDescriptionLabel.Size = new System.Drawing.Size(248, 26);
            this.batchSizeDescriptionLabel.TabIndex = 5;
            this.batchSizeDescriptionLabel.Text = "The minimum number of closely timed photos to be\r\nconsidered a Batch";

            this.includeSubfoldersCheckBox.AutoSize = true;
            this.includeSubfoldersCheckBox.Location = new System.Drawing.Point(20, 220);
            this.includeSubfoldersCheckBox.Name = "includeSubfoldersCheckBox";
            this.includeSubfoldersCheckBox.Size = new System.Drawing.Size(104, 17);
            this.includeSubfoldersCheckBox.TabIndex = 4;
            this.includeSubfoldersCheckBox.Text = "Include Subfolders";
            this.includeSubfoldersCheckBox.UseVisualStyleBackColor = true;

            this.batchTimingNumericUpDown.Location = new System.Drawing.Point(300, 138);
            this.batchTimingNumericUpDown.Maximum = 600;
            this.batchTimingNumericUpDown.Minimum = 1;
            this.batchTimingNumericUpDown.Name = "batchTimingNumericUpDown";
            this.batchTimingNumericUpDown.Size = new System.Drawing.Size(60, 20);
            this.batchTimingNumericUpDown.TabIndex = 3;
            this.batchTimingNumericUpDown.Value = 300;

            this.batchTimingLabel.AutoSize = true;
            this.batchTimingLabel.Location = new System.Drawing.Point(20, 140);
            this.batchTimingLabel.Name = "batchTimingLabel";
            this.batchTimingLabel.Size = new System.Drawing.Size(154, 13);
            this.batchTimingLabel.TabIndex = 2;
            this.batchTimingLabel.Text = "Batch Timing Maximum (1–600s):";

            this.batchSizeNumericUpDown.Location = new System.Drawing.Point(300, 58);
            this.batchSizeNumericUpDown.Maximum = 100;
            this.batchSizeNumericUpDown.Minimum = 2;
            this.batchSizeNumericUpDown.Name = "batchSizeNumericUpDown";
            this.batchSizeNumericUpDown.Size = new System.Drawing.Size(60, 20);
            this.batchSizeNumericUpDown.TabIndex = 1;
            this.batchSizeNumericUpDown.Value = 4;

            this.batchSizeLabel.AutoSize = true;
            this.batchSizeLabel.Location = new System.Drawing.Point(20, 60);
            this.batchSizeLabel.Name = "batchSizeLabel";
            this.batchSizeLabel.Size = new System.Drawing.Size(139, 13);
            this.batchSizeLabel.TabIndex = 0;
            this.batchSizeLabel.Text = "Batch Size Minimum (2–100):";

            this.settingsHeaderLabel.AutoSize = true;
            this.settingsHeaderLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.settingsHeaderLabel.Location = new System.Drawing.Point(20, 20);
            this.settingsHeaderLabel.Name = "settingsHeaderLabel";
            this.settingsHeaderLabel.Size = new System.Drawing.Size(76, 20);
            this.settingsHeaderLabel.TabIndex = 0;
            this.settingsHeaderLabel.Text = "Settings:";

            this.logoPictureBox.Location = new System.Drawing.Point(350, 50);
            this.logoPictureBox.Name = "logoPictureBox";
            this.logoPictureBox.Size = new System.Drawing.Size(300, 100);
            this.logoPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.logoPictureBox.TabIndex = 18;
            this.logoPictureBox.TabStop = false;

            this.toolTipLeft.AutoPopDelay = 5000;
            this.toolTipLeft.InitialDelay = 500;
            this.toolTipLeft.ReshowDelay = 100;

            this.toolTipRight.AutoPopDelay = 5000;
            this.toolTipRight.InitialDelay = 500;
            this.toolTipRight.ReshowDelay = 100;

            this.saveAndQuitButton.Location = new System.Drawing.Point(350, 444);
            this.saveAndQuitButton.Name = "saveAndQuitButton";
            this.saveAndQuitButton.Size = new System.Drawing.Size(100, 30);
            this.saveAndQuitButton.TabIndex = 19;
            this.saveAndQuitButton.Text = "Save and Quit";
            this.saveAndQuitButton.UseVisualStyleBackColor = true;
            this.saveAndQuitButton.Visible = false;
            this.saveAndQuitButton.Click += new System.EventHandler(this.SaveAndQuitButton_Click);

            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 600);
            this.Controls.Add(this.saveAndQuitButton);
            this.Controls.Add(this.logoPictureBox);
            this.Controls.Add(this.settingsGroupBox);
            this.Controls.Add(this.deletePromptLabel);
            this.Controls.Add(this.rotateCounterclockwiseButton);
            this.Controls.Add(this.rotateClockwiseButton);
            this.Controls.Add(this.thumbnailPanel);
            this.Controls.Add(this.instructionLabel);
            this.Controls.Add(this.remainingLabel);
            this.Controls.Add(this.pictureBoxRight);
            this.Controls.Add(this.pictureBoxLeft);
            this.Controls.Add(this.selectPhoneFolderButton);
            this.Controls.Add(this.selectLocalFolderButton);
            this.Controls.Add(this.menuStrip);
            this.MainMenuStrip = this.menuStrip;
            this.Text = "Picksy";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxLeft)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxRight)).EndInit();
            this.thumbnailPanel.ResumeLayout(false);
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.settingsGroupBox.ResumeLayout(false);
            this.settingsGroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.batchTimingNumericUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.batchSizeNumericUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.logoPictureBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Button selectLocalFolderButton;
        private System.Windows.Forms.Button selectPhoneFolderButton;
        private System.Windows.Forms.PictureBox pictureBoxLeft;
        private System.Windows.Forms.PictureBox pictureBoxRight;
        private System.Windows.Forms.Label remainingLabel;
        private System.Windows.Forms.Label instructionLabel;
        private System.Windows.Forms.FlowLayoutPanel thumbnailPanel;
        private System.Windows.Forms.MenuStrip menuStrip;
        private System.Windows.Forms.Button rotateClockwiseButton;
        private System.Windows.Forms.Button rotateCounterclockwiseButton;
        private System.Windows.Forms.Label deletePromptLabel;
        private System.Windows.Forms.GroupBox settingsGroupBox;
        private System.Windows.Forms.CheckBox skipConfirmationCheckBox;
        private System.Windows.Forms.Label batchTimingDescriptionLabel;
        private System.Windows.Forms.Label batchSizeDescriptionLabel;
        private System.Windows.Forms.CheckBox includeSubfoldersCheckBox;
        private System.Windows.Forms.NumericUpDown batchTimingNumericUpDown;
        private System.Windows.Forms.Label batchTimingLabel;
        private System.Windows.Forms.NumericUpDown batchSizeNumericUpDown;
        private System.Windows.Forms.Label batchSizeLabel;
        private System.Windows.Forms.Label settingsHeaderLabel;
        private System.Windows.Forms.PictureBox logoPictureBox;
        private System.Windows.Forms.ComboBox batchSelectionMethodComboBox;
        private System.Windows.Forms.Label batchSelectionMethodLabel;
        private System.Windows.Forms.ToolTip toolTipLeft;
        private System.Windows.Forms.ToolTip toolTipRight;
        private System.Windows.Forms.Button saveAndQuitButton;
    }
}