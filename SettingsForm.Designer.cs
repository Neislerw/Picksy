namespace Picksy
{
    partial class SettingsForm
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
            this.batchSizeLabel = new System.Windows.Forms.Label();
            this.batchSizeNumericUpDown = new System.Windows.Forms.NumericUpDown();
            this.batchTimingLabel = new System.Windows.Forms.Label();
            this.batchTimingNumericUpDown = new System.Windows.Forms.NumericUpDown();
            this.saveButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.includeSubfoldersCheckBox = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.batchSizeNumericUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.batchTimingNumericUpDown)).BeginInit();
            this.SuspendLayout();
            // 
            // batchSizeLabel
            // 
            this.batchSizeLabel.AutoSize = true;
            this.batchSizeLabel.Location = new System.Drawing.Point(20, 20);
            this.batchSizeLabel.Name = "batchSizeLabel";
            this.batchSizeLabel.Size = new System.Drawing.Size(139, 13);
            this.batchSizeLabel.TabIndex = 0;
            this.batchSizeLabel.Text = "Batch Size Minimum (2–100):";
            // 
            // batchSizeNumericUpDown
            // 
            this.batchSizeNumericUpDown.Location = new System.Drawing.Point(165, 18);
            this.batchSizeNumericUpDown.Maximum = 100;
            this.batchSizeNumericUpDown.Minimum = 2;
            this.batchSizeNumericUpDown.Name = "batchSizeNumericUpDown";
            this.batchSizeNumericUpDown.Size = new System.Drawing.Size(60, 20);
            this.batchSizeNumericUpDown.TabIndex = 1;
            this.batchSizeNumericUpDown.Value = 4;
            // 
            // batchTimingLabel
            // 
            this.batchTimingLabel.AutoSize = true;
            this.batchTimingLabel.Location = new System.Drawing.Point(20, 50);
            this.batchTimingLabel.Name = "batchTimingLabel";
            this.batchTimingLabel.Size = new System.Drawing.Size(154, 13);
            this.batchTimingLabel.TabIndex = 2;
            this.batchTimingLabel.Text = "Batch Timing Maximum (1–600s):";
            // 
            // batchTimingNumericUpDown
            // 
            this.batchTimingNumericUpDown.Location = new System.Drawing.Point(165, 48);
            this.batchTimingNumericUpDown.Maximum = 600;
            this.batchTimingNumericUpDown.Minimum = 1;
            this.batchTimingNumericUpDown.Name = "batchTimingNumericUpDown";
            this.batchTimingNumericUpDown.Size = new System.Drawing.Size(60, 20);
            this.batchTimingNumericUpDown.TabIndex = 3;
            this.batchTimingNumericUpDown.Value = 20;
            // 
            // saveButton
            // 
            this.saveButton.Location = new System.Drawing.Point(70, 120);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(75, 23);
            this.saveButton.TabIndex = 5;
            this.saveButton.Text = "Save";
            this.saveButton.UseVisualStyleBackColor = true;
            this.saveButton.Click += new System.EventHandler(this.SaveButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.Location = new System.Drawing.Point(150, 120);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 6;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // includeSubfoldersCheckBox
            // 
            this.includeSubfoldersCheckBox.AutoSize = true;
            this.includeSubfoldersCheckBox.Location = new System.Drawing.Point(20, 80);
            this.includeSubfoldersCheckBox.Name = "includeSubfoldersCheckBox";
            this.includeSubfoldersCheckBox.Size = new System.Drawing.Size(104, 17);
            this.includeSubfoldersCheckBox.TabIndex = 4;
            this.includeSubfoldersCheckBox.Text = "Include Subfolders";
            this.includeSubfoldersCheckBox.UseVisualStyleBackColor = true;
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(244, 163);
            this.Controls.Add(this.includeSubfoldersCheckBox);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.saveButton);
            this.Controls.Add(this.batchTimingNumericUpDown);
            this.Controls.Add(this.batchTimingLabel);
            this.Controls.Add(this.batchSizeNumericUpDown);
            this.Controls.Add(this.batchSizeLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsForm";
            this.Text = "Picksy Settings";
            ((System.ComponentModel.ISupportInitialize)(this.batchSizeNumericUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.batchTimingNumericUpDown)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label batchSizeLabel;
        private System.Windows.Forms.NumericUpDown batchSizeNumericUpDown;
        private System.Windows.Forms.Label batchTimingLabel;
        private System.Windows.Forms.NumericUpDown batchTimingNumericUpDown;
        private System.Windows.Forms.Button saveButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.CheckBox includeSubfoldersCheckBox;
    }
}