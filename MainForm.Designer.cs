namespace Picksy
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.selectFolderButton = new System.Windows.Forms.Button();
            this.pictureBoxLeft = new System.Windows.Forms.PictureBox();
            this.pictureBoxRight = new System.Windows.Forms.PictureBox();
            this.remainingLabel = new System.Windows.Forms.Label();
            this.thumbnailPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.deletePromptLabel = new System.Windows.Forms.Label();
            this.settingsGroupBox = new System.Windows.Forms.GroupBox();
            this.skipAnimationsCheckBox = new System.Windows.Forms.CheckBox();
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
            this.controlsPictureBox = new System.Windows.Forms.PictureBox();
            this.toolTipLeft = new System.Windows.Forms.ToolTip(this.components);
            this.toolTipRight = new System.Windows.Forms.ToolTip(this.components);
            this.saveAndQuitButton = new System.Windows.Forms.Button();
            this.copyrightLabel = new System.Windows.Forms.Label();
            this.batchProgressLabel = new System.Windows.Forms.Label();
            this.hoverTimer = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxLeft)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxRight)).BeginInit();
            this.thumbnailPanel.SuspendLayout();
            this.menuStrip.SuspendLayout();
            this.settingsGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.batchTimingNumericUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.batchSizeNumericUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.logoPictureBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.controlsPictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // selectFolderButton
            // 
            this.selectFolderButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(26)))), ((int)(((byte)(26)))));
            this.selectFolderButton.Font = new System.Drawing.Font("Montserrat SemiBold", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.selectFolderButton.ForeColor = System.Drawing.Color.White;
            this.selectFolderButton.Location = new System.Drawing.Point(390, 600);
            this.selectFolderButton.Name = "selectFolderButton";
            this.selectFolderButton.Size = new System.Drawing.Size(220, 40);
            this.selectFolderButton.TabIndex = 0;
            this.selectFolderButton.Text = "Select Image Folder";
            this.selectFolderButton.UseVisualStyleBackColor = false;
            this.selectFolderButton.Visible = true;
            this.selectFolderButton.Click += new System.EventHandler(this.SelectFolderButton_Click);
            // 
            // pictureBoxLeft
            // 
            this.pictureBoxLeft.BackColor = System.Drawing.Color.Transparent;
            this.pictureBoxLeft.Location = new System.Drawing.Point(20, 40);
            this.pictureBoxLeft.Name = "pictureBoxLeft";
            this.pictureBoxLeft.Size = new System.Drawing.Size(400, 420);
            this.pictureBoxLeft.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBoxLeft.TabIndex = 1;
            this.pictureBoxLeft.TabStop = false;
            this.pictureBoxLeft.Visible = true;
            this.pictureBoxLeft.Click += new System.EventHandler(this.PictureBoxLeft_Click);
            // 
            // pictureBoxRight
            // 
            this.pictureBoxRight.BackColor = System.Drawing.Color.Transparent;
            this.pictureBoxRight.Location = new System.Drawing.Point(580, 40);
            this.pictureBoxRight.Name = "pictureBoxRight";
            this.pictureBoxRight.Size = new System.Drawing.Size(400, 420);
            this.pictureBoxRight.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBoxRight.TabIndex = 2;
            this.pictureBoxRight.TabStop = false;
            this.pictureBoxRight.Visible = true;
            this.pictureBoxRight.Click += new System.EventHandler(this.PictureBoxRight_Click);
            // 
            // remainingLabel
            // 
            this.remainingLabel.Font = new System.Drawing.Font("Montserrat SemiBold", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.remainingLabel.ForeColor = System.Drawing.Color.White;
            this.remainingLabel.Location = new System.Drawing.Point(20, 650);
            this.remainingLabel.Name = "remainingLabel";
            this.remainingLabel.Size = new System.Drawing.Size(960, 40);
            this.remainingLabel.TabIndex = 3;
            this.remainingLabel.Text = "Photos remaining: 0";
            this.remainingLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.remainingLabel.Visible = true;
            // 
            // thumbnailPanel
            // 
            this.thumbnailPanel.AutoScroll = true;
            this.thumbnailPanel.BackColor = System.Drawing.Color.Transparent;
            this.thumbnailPanel.Location = new System.Drawing.Point(20, 480);
            this.thumbnailPanel.Name = "thumbnailPanel";
            this.thumbnailPanel.Size = new System.Drawing.Size(960, 100);
            this.thumbnailPanel.TabIndex = 5;
            this.thumbnailPanel.Visible = true;
            // 
            // menuStrip
            // 
            this.menuStrip.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(26)))), ((int)(((byte)(26)))));
            this.menuStrip.Font = new System.Drawing.Font("Montserrat SemiBold", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.menuStrip.ForeColor = System.Drawing.Color.White;
            this.menuStrip.Location = new System.Drawing.Point(0, 0);
            this.menuStrip.Name = "menuStrip";
            this.menuStrip.Size = new System.Drawing.Size(1000, 28);
            this.menuStrip.TabIndex = 8;
            this.menuStrip.Text = "menuStrip";
            this.menuStrip.Visible = true;
            // 
            // deletePromptLabel
            // 
            this.deletePromptLabel.Font = new System.Drawing.Font("Montserrat SemiBold", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.deletePromptLabel.ForeColor = System.Drawing.Color.White;
            this.deletePromptLabel.Location = new System.Drawing.Point(20, 600);
            this.deletePromptLabel.Name = "deletePromptLabel";
            this.deletePromptLabel.Size = new System.Drawing.Size(960, 40);
            this.deletePromptLabel.TabIndex = 11;
            this.deletePromptLabel.Text = "Move to delete folder? Enter to confirm or Esc to cancel batch";
            this.deletePromptLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.deletePromptLabel.Visible = false;
            // 
            // settingsGroupBox
            // 
            this.settingsGroupBox.BackColor = System.Drawing.Color.Transparent;
            this.settingsGroupBox.Controls.Add(this.skipAnimationsCheckBox);
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
            this.settingsGroupBox.Font = new System.Drawing.Font("Montserrat SemiBold", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.settingsGroupBox.ForeColor = System.Drawing.Color.White;
            this.settingsGroupBox.Location = new System.Drawing.Point(220, 120);
            this.settingsGroupBox.Name = "settingsGroupBox";
            this.settingsGroupBox.Size = new System.Drawing.Size(560, 440);
            this.settingsGroupBox.TabIndex = 17;
            this.settingsGroupBox.TabStop = false;
            this.settingsGroupBox.Visible = true;
            // 
            // skipAnimationsCheckBox
            // 
            this.skipAnimationsCheckBox.AutoSize = true;
            this.skipAnimationsCheckBox.Font = new System.Drawing.Font("Montserrat SemiBold", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.skipAnimationsCheckBox.ForeColor = System.Drawing.Color.White;
            this.skipAnimationsCheckBox.Location = new System.Drawing.Point(30, 265);
            this.skipAnimationsCheckBox.Name = "skipAnimationsCheckBox";
            this.skipAnimationsCheckBox.Size = new System.Drawing.Size(200, 26);
            this.skipAnimationsCheckBox.TabIndex = 6;
            this.skipAnimationsCheckBox.Text = "Skip Animations";
            this.skipAnimationsCheckBox.UseVisualStyleBackColor = true;
            // 
            // batchSelectionMethodComboBox
            // 
            this.batchSelectionMethodComboBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(26)))), ((int)(((byte)(26)))));
            this.batchSelectionMethodComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.batchSelectionMethodComboBox.Font = new System.Drawing.Font("Montserrat SemiBold", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.batchSelectionMethodComboBox.ForeColor = System.Drawing.Color.White;
            this.batchSelectionMethodComboBox.FormattingEnabled = true;
            this.batchSelectionMethodComboBox.Location = new System.Drawing.Point(330, 365);
            this.batchSelectionMethodComboBox.Name = "batchSelectionMethodComboBox";
            this.batchSelectionMethodComboBox.Size = new System.Drawing.Size(200, 28);
            this.batchSelectionMethodComboBox.TabIndex = 8;
            // 
            // batchSelectionMethodLabel
            // 
            this.batchSelectionMethodLabel.AutoSize = true;
            this.batchSelectionMethodLabel.Font = new System.Drawing.Font("Montserrat SemiBold", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.batchSelectionMethodLabel.ForeColor = System.Drawing.Color.White;
            this.batchSelectionMethodLabel.Location = new System.Drawing.Point(30, 365);
            this.batchSelectionMethodLabel.Name = "batchSelectionMethodLabel";
            this.batchSelectionMethodLabel.Size = new System.Drawing.Size(200, 26);
            this.batchSelectionMethodLabel.TabIndex = 8;
            this.batchSelectionMethodLabel.Text = "Batch Selection Method:";
            // 
            // skipConfirmationCheckBox
            // 
            this.skipConfirmationCheckBox.AutoSize = true;
            this.skipConfirmationCheckBox.Checked = true;
            this.skipConfirmationCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.skipConfirmationCheckBox.Font = new System.Drawing.Font("Montserrat SemiBold", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.skipConfirmationCheckBox.ForeColor = System.Drawing.Color.White;
            this.skipConfirmationCheckBox.Location = new System.Drawing.Point(30, 290);
            this.skipConfirmationCheckBox.Name = "skipConfirmationCheckBox";
            this.skipConfirmationCheckBox.Size = new System.Drawing.Size(300, 26);
            this.skipConfirmationCheckBox.TabIndex = 7;
            this.skipConfirmationCheckBox.Text = "Skip Confirmation between batches";
            this.skipConfirmationCheckBox.UseVisualStyleBackColor = true;
            // 
            // batchTimingDescriptionLabel
            // 
            this.batchTimingDescriptionLabel.AutoSize = true;
            this.batchTimingDescriptionLabel.Font = new System.Drawing.Font("Montserrat SemiBold", 9F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.batchTimingDescriptionLabel.ForeColor = System.Drawing.Color.Gray;
            this.batchTimingDescriptionLabel.Location = new System.Drawing.Point(30, 180);
            this.batchTimingDescriptionLabel.Name = "batchTimingDescriptionLabel";
            this.batchTimingDescriptionLabel.Size = new System.Drawing.Size(400, 42);
            this.batchTimingDescriptionLabel.TabIndex = 6;
            this.batchTimingDescriptionLabel.Text = "The maximum amount of seconds between photos to\r\nstill be considered part of the same batch";
            // 
            // batchSizeDescriptionLabel
            // 
            this.batchSizeDescriptionLabel.AutoSize = true;
            this.batchSizeDescriptionLabel.Font = new System.Drawing.Font("Montserrat SemiBold", 9F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.batchSizeDescriptionLabel.ForeColor = System.Drawing.Color.Gray;
            this.batchSizeDescriptionLabel.Location = new System.Drawing.Point(30, 100);
            this.batchSizeDescriptionLabel.Name = "batchSizeDescriptionLabel";
            this.batchSizeDescriptionLabel.Size = new System.Drawing.Size(400, 28);
            this.batchSizeDescriptionLabel.TabIndex = 5;
            this.batchSizeDescriptionLabel.Text = "The minimum number of closely timed photos to be\r\nconsidered a Batch";
            // 
            // includeSubfoldersCheckBox
            // 
            this.includeSubfoldersCheckBox.AutoSize = true;
            this.includeSubfoldersCheckBox.Font = new System.Drawing.Font("Montserrat SemiBold", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.includeSubfoldersCheckBox.ForeColor = System.Drawing.Color.White;
            this.includeSubfoldersCheckBox.Location = new System.Drawing.Point(30, 240);
            this.includeSubfoldersCheckBox.Name = "includeSubfoldersCheckBox";
            this.includeSubfoldersCheckBox.Size = new System.Drawing.Size(200, 26);
            this.includeSubfoldersCheckBox.TabIndex = 5;
            this.includeSubfoldersCheckBox.Text = "Include Subfolders";
            this.includeSubfoldersCheckBox.UseVisualStyleBackColor = true;
            // 
            // batchTimingNumericUpDown
            // 
            this.batchTimingNumericUpDown.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(26)))), ((int)(((byte)(26)))));
            this.batchTimingNumericUpDown.Font = new System.Drawing.Font("Montserrat SemiBold", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.batchTimingNumericUpDown.ForeColor = System.Drawing.Color.White;
            this.batchTimingNumericUpDown.Location = new System.Drawing.Point(450, 148);
            this.batchTimingNumericUpDown.Maximum = new decimal(new int[] { 600, 0, 0, 0 });
            this.batchTimingNumericUpDown.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.batchTimingNumericUpDown.Name = "batchTimingNumericUpDown";
            this.batchTimingNumericUpDown.Size = new System.Drawing.Size(80, 28);
            this.batchTimingNumericUpDown.TabIndex = 3;
            this.batchTimingNumericUpDown.Value = new decimal(new int[] { 300, 0, 0, 0 });
            // 
            // batchTimingLabel
            // 
            this.batchTimingLabel.AutoSize = true;
            this.batchTimingLabel.Font = new System.Drawing.Font("Montserrat SemiBold", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.batchTimingLabel.ForeColor = System.Drawing.Color.White;
            this.batchTimingLabel.Location = new System.Drawing.Point(30, 150);
            this.batchTimingLabel.Name = "batchTimingLabel";
            this.batchTimingLabel.Size = new System.Drawing.Size(250, 26);
            this.batchTimingLabel.TabIndex = 2;
            this.batchTimingLabel.Text = "Batch Timing Maximum (1–600s):";
            // 
            // batchSizeNumericUpDown
            // 
            this.batchSizeNumericUpDown.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(26)))), ((int)(((byte)(26)))));
            this.batchSizeNumericUpDown.Font = new System.Drawing.Font("Montserrat SemiBold", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.batchSizeNumericUpDown.ForeColor = System.Drawing.Color.White;
            this.batchSizeNumericUpDown.Location = new System.Drawing.Point(450, 68);
            this.batchSizeNumericUpDown.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.batchSizeNumericUpDown.Minimum = new decimal(new int[] { 2, 0, 0, 0 });
            this.batchSizeNumericUpDown.Name = "batchSizeNumericUpDown";
            this.batchSizeNumericUpDown.Size = new System.Drawing.Size(80, 28);
            this.batchSizeNumericUpDown.TabIndex = 1;
            this.batchSizeNumericUpDown.Value = new decimal(new int[] { 2, 0, 0, 0 });
            // 
            // batchSizeLabel
            // 
            this.batchSizeLabel.AutoSize = true;
            this.batchSizeLabel.Font = new System.Drawing.Font("Montserrat SemiBold", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.batchSizeLabel.ForeColor = System.Drawing.Color.White;
            this.batchSizeLabel.Location = new System.Drawing.Point(30, 70);
            this.batchSizeLabel.Name = "batchSizeLabel";
            this.batchSizeLabel.Size = new System.Drawing.Size(250, 26);
            this.batchSizeLabel.TabIndex = 0;
            this.batchSizeLabel.Text = "Batch Size Minimum (2–100):";
            // 
            // settingsHeaderLabel
            // 
            this.settingsHeaderLabel.AutoSize = true;
            this.settingsHeaderLabel.Font = new System.Drawing.Font("Montserrat SemiBold", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.settingsHeaderLabel.ForeColor = System.Drawing.Color.White;
            this.settingsHeaderLabel.Location = new System.Drawing.Point(30, 30);
            this.settingsHeaderLabel.Name = "settingsHeaderLabel";
            this.settingsHeaderLabel.Size = new System.Drawing.Size(100, 34);
            this.settingsHeaderLabel.TabIndex = 0;
            this.settingsHeaderLabel.Text = "Settings:";
            // 
            // logoPictureBox
            // 
            this.logoPictureBox.BackColor = System.Drawing.Color.Transparent;
            this.logoPictureBox.Location = new System.Drawing.Point(275, 80);
            this.logoPictureBox.Name = "logoPictureBox";
            this.logoPictureBox.Size = new System.Drawing.Size(450, 140);
            this.logoPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.logoPictureBox.TabIndex = 18;
            this.logoPictureBox.TabStop = false;
            // 
            // controlsPictureBox
            // 
            this.controlsPictureBox.BackColor = System.Drawing.Color.Transparent;
            this.controlsPictureBox.Location = new System.Drawing.Point(75, 520);
            this.controlsPictureBox.Name = "controlsPictureBox";
            this.controlsPictureBox.Size = new System.Drawing.Size(850, 200);
            this.controlsPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.controlsPictureBox.TabIndex = 22;
            this.controlsPictureBox.TabStop = false;
            // 
            // toolTipLeft
            // 
            this.toolTipLeft.AutoPopDelay = 5000;
            this.toolTipLeft.InitialDelay = 500;
            this.toolTipLeft.ReshowDelay = 100;
            // 
            // toolTipRight
            // 
            this.toolTipRight.AutoPopDelay = 5000;
            this.toolTipRight.InitialDelay = 500;
            this.toolTipRight.ReshowDelay = 100;
            // 
            // saveAndQuitButton
            // 
            this.saveAndQuitButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(26)))), ((int)(((byte)(26)))));
            this.saveAndQuitButton.Font = new System.Drawing.Font("Montserrat SemiBold", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.saveAndQuitButton.ForeColor = System.Drawing.Color.White;
            this.saveAndQuitButton.Location = new System.Drawing.Point(390, 700);
            this.saveAndQuitButton.Name = "saveAndQuitButton";
            this.saveAndQuitButton.Size = new System.Drawing.Size(220, 40);
            this.saveAndQuitButton.TabIndex = 0;
            this.saveAndQuitButton.Text = "Save and Quit";
            this.saveAndQuitButton.UseVisualStyleBackColor = false;
            this.saveAndQuitButton.Visible = true;
            this.saveAndQuitButton.Click += new System.EventHandler(this.SaveAndQuitButton_Click);
            // 
            // copyrightLabel
            // 
            this.copyrightLabel.AutoSize = true;
            this.copyrightLabel.Font = new System.Drawing.Font("Montserrat SemiBold", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.copyrightLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(56)))), ((int)(((byte)(56)))), ((int)(((byte)(56)))));
            this.copyrightLabel.Location = new System.Drawing.Point(425, 770);
            this.copyrightLabel.Name = "copyrightLabel";
            this.copyrightLabel.Size = new System.Drawing.Size(150, 16);
            this.copyrightLabel.TabIndex = 20;
            this.copyrightLabel.Text = "\u00A9 2025 W.A. Neisler";
            this.copyrightLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // batchProgressLabel
            // 
            this.batchProgressLabel.AutoSize = false;
            this.batchProgressLabel.Font = new System.Drawing.Font("Montserrat SemiBold", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.batchProgressLabel.ForeColor = System.Drawing.Color.Gray;
            this.batchProgressLabel.Location = new System.Drawing.Point(220, 730);
            this.batchProgressLabel.Name = "batchProgressLabel";
            this.batchProgressLabel.Size = new System.Drawing.Size(560, 16);
            this.batchProgressLabel.TabIndex = 21;
            this.batchProgressLabel.Text = "Batch Progress: 0% Seen";
            this.batchProgressLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.batchProgressLabel.Visible = false;
            // 
            // hoverTimer
            // 
            this.hoverTimer.Interval = 15;
            this.hoverTimer.Tick += new System.EventHandler(this.HoverTimer_Tick);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(18)))), ((int)(((byte)(18)))), ((int)(((byte)(18)))));
            this.ClientSize = new System.Drawing.Size(1000, 760);
            this.Controls.Add(this.saveAndQuitButton);
            this.Controls.Add(this.remainingLabel);
            this.Controls.Add(this.deletePromptLabel);
            this.Controls.Add(this.settingsGroupBox);
            this.Controls.Add(this.thumbnailPanel);
            this.Controls.Add(this.pictureBoxRight);
            this.Controls.Add(this.pictureBoxLeft);
            this.Controls.Add(this.selectFolderButton);
            this.Controls.Add(this.menuStrip);
            this.MainMenuStrip = this.menuStrip;
            this.Name = "MainForm";
            this.Text = "Picksy";
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
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
            ((System.ComponentModel.ISupportInitialize)(this.controlsPictureBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button selectFolderButton;
        private System.Windows.Forms.PictureBox pictureBoxLeft;
        private System.Windows.Forms.PictureBox pictureBoxRight;
        private System.Windows.Forms.Label remainingLabel;
        private System.Windows.Forms.FlowLayoutPanel thumbnailPanel;
        private System.Windows.Forms.MenuStrip menuStrip;
        private System.Windows.Forms.Label deletePromptLabel;
        private System.Windows.Forms.GroupBox settingsGroupBox;
        private System.Windows.Forms.CheckBox skipAnimationsCheckBox;
        private System.Windows.Forms.ComboBox batchSelectionMethodComboBox;
        private System.Windows.Forms.Label batchSelectionMethodLabel;
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
        private System.Windows.Forms.PictureBox controlsPictureBox;
        private System.Windows.Forms.ToolTip toolTipLeft;
        private System.Windows.Forms.ToolTip toolTipRight;
        private System.Windows.Forms.Button saveAndQuitButton;
        private System.Windows.Forms.Label copyrightLabel;
        private System.Windows.Forms.Label batchProgressLabel;
        private System.Windows.Forms.Timer hoverTimer;
    }
}