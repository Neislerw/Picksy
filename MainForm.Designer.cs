namespace Picksy;

partial class MainForm
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
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
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
        this.selectFolderButton = new System.Windows.Forms.Button();
        this.pictureBoxLeft = new System.Windows.Forms.PictureBox();
        this.pictureBoxRight = new System.Windows.Forms.PictureBox();
        this.remainingLabel = new System.Windows.Forms.Label();
        this.instructionLabel = new System.Windows.Forms.Label();
        this.thumbnailPanel = new System.Windows.Forms.FlowLayoutPanel();
        this.menuStrip = new System.Windows.Forms.MenuStrip();
        this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        this.rotateClockwiseButton = new System.Windows.Forms.Button();
        this.rotateCounterclockwiseButton = new System.Windows.Forms.Button();
        this.deletePromptLabel = new System.Windows.Forms.Label();
        ((System.ComponentModel.ISupportInitialize)(this.pictureBoxLeft)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.pictureBoxRight)).BeginInit();
        this.thumbnailPanel.SuspendLayout();
        this.menuStrip.SuspendLayout();
        this.SuspendLayout();
        // 
        // selectFolderButton
        // 
        this.selectFolderButton.Location = new System.Drawing.Point(325, 285);
        this.selectFolderButton.Name = "selectFolderButton";
        this.selectFolderButton.Size = new System.Drawing.Size(150, 30);
        this.selectFolderButton.TabIndex = 0;
        this.selectFolderButton.Text = "Select Image Folder";
        this.selectFolderButton.UseVisualStyleBackColor = true;
        this.selectFolderButton.Click += new System.EventHandler(this.SelectFolderButton_Click);
        // 
        // pictureBoxLeft
        // 
        this.pictureBoxLeft.Location = new System.Drawing.Point(10, 34);
        this.pictureBoxLeft.Name = "pictureBoxLeft";
        this.pictureBoxLeft.Size = new System.Drawing.Size(380, 400);
        this.pictureBoxLeft.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
        this.pictureBoxLeft.TabIndex = 1;
        this.pictureBoxLeft.TabStop = false;
        this.pictureBoxLeft.Click += new System.EventHandler(this.PictureBoxLeft_Click);
        // 
        // pictureBoxRight
        // 
        this.pictureBoxRight.Location = new System.Drawing.Point(410, 34);
        this.pictureBoxRight.Name = "pictureBoxRight";
        this.pictureBoxRight.Size = new System.Drawing.Size(380, 400);
        this.pictureBoxRight.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
        this.pictureBoxRight.TabIndex = 2;
        this.pictureBoxRight.TabStop = false;
        this.pictureBoxRight.Click += new System.EventHandler(this.PictureBoxRight_Click);
        // 
        // remainingLabel
        // 
        this.remainingLabel.Location = new System.Drawing.Point(10, 540);
        this.remainingLabel.Name = "remainingLabel";
        this.remainingLabel.Size = new System.Drawing.Size(760, 30);
        this.remainingLabel.TabIndex = 3;
        this.remainingLabel.Text = "Photos remaining: 0";
        this.remainingLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        // 
        // instructionLabel
        // 
        this.instructionLabel.Location = new System.Drawing.Point(10, 570);
        this.instructionLabel.Name = "instructionLabel";
        this.instructionLabel.Size = new System.Drawing.Size(760, 60);
        this.instructionLabel.TabIndex = 4;
        this.instructionLabel.Text = "Click or use Left/Right arrow to select a photo, Up to keep both, Down to undo, Q/E to rotate, W to toggle full resolution, Space to keep all remaining.";
        this.instructionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        // 
        // thumbnailPanel
        // 
        this.thumbnailPanel.AutoScroll = true;
        this.thumbnailPanel.Location = new System.Drawing.Point(20, 70);
        this.thumbnailPanel.Name = "thumbnailPanel";
        this.thumbnailPanel.Size = new System.Drawing.Size(760, 400);
        this.thumbnailPanel.TabIndex = 5;
        this.thumbnailPanel.Visible = false;
        // 
        // menuStrip
        // 
        this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.settingsToolStripMenuItem});
        this.menuStrip.Location = new System.Drawing.Point(0, 0);
        this.menuStrip.Name = "menuStrip";
        this.menuStrip.Size = new System.Drawing.Size(800, 24);
        this.menuStrip.TabIndex = 8;
        this.menuStrip.Text = "menuStrip";
        // 
        // settingsToolStripMenuItem
        // 
        this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
        this.settingsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
        this.settingsToolStripMenuItem.Text = "Settings";
        this.settingsToolStripMenuItem.Click += new System.EventHandler(this.SettingsToolStripMenuItem_Click);
        // 
        // rotateClockwiseButton
        // 
        this.rotateClockwiseButton.Location = new System.Drawing.Point(10, 444);
        this.rotateClockwiseButton.Name = "rotateClockwiseButton";
        this.rotateClockwiseButton.Size = new System.Drawing.Size(100, 30);
        this.rotateClockwiseButton.TabIndex = 9;
        this.rotateClockwiseButton.Text = "Rotate CW";
        this.rotateClockwiseButton.UseVisualStyleBackColor = true;
        this.rotateClockwiseButton.Visible = false;
        this.rotateClockwiseButton.Click += new System.EventHandler(this.RotateClockwiseButton_Click);
        // 
        // rotateCounterclockwiseButton
        // 
        this.rotateCounterclockwiseButton.Location = new System.Drawing.Point(690, 444);
        this.rotateCounterclockwiseButton.Name = "rotateCounterclockwiseButton";
        this.rotateCounterclockwiseButton.Size = new System.Drawing.Size(100, 30);
        this.rotateCounterclockwiseButton.TabIndex = 10;
        this.rotateCounterclockwiseButton.Text = "Rotate CCW";
        this.rotateCounterclockwiseButton.UseVisualStyleBackColor = true;
        this.rotateCounterclockwiseButton.Visible = false;
        this.rotateCounterclockwiseButton.Click += new System.EventHandler(this.RotateCounterclockwiseButton_Click);
        // 
        // deletePromptLabel
        // 
        this.deletePromptLabel.Location = new System.Drawing.Point(20, 480);
        this.deletePromptLabel.Name = "deletePromptLabel";
        this.deletePromptLabel.Size = new System.Drawing.Size(760, 30);
        this.deletePromptLabel.TabIndex = 11;
        this.deletePromptLabel.Text = "Move to delete folder? Enter to confirm or Esc to cancel batch";
        this.deletePromptLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        this.deletePromptLabel.Visible = false;
        // 
        // MainForm
        // 
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(800, 600);
        this.Controls.Add(this.deletePromptLabel);
        this.Controls.Add(this.rotateCounterclockwiseButton);
        this.Controls.Add(this.rotateClockwiseButton);
        this.Controls.Add(this.thumbnailPanel);
        this.Controls.Add(this.instructionLabel);
        this.Controls.Add(this.remainingLabel);
        this.Controls.Add(this.pictureBoxRight);
        this.Controls.Add(this.pictureBoxLeft);
        this.Controls.Add(this.selectFolderButton);
        this.Controls.Add(this.menuStrip);
        this.MainMenuStrip = this.menuStrip;
        this.Text = "Picksy";
        ((System.ComponentModel.ISupportInitialize)(this.pictureBoxLeft)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.pictureBoxRight)).EndInit();
        this.thumbnailPanel.ResumeLayout(false);
        this.menuStrip.ResumeLayout(false);
        this.menuStrip.PerformLayout();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private System.Windows.Forms.Button selectFolderButton;
    private System.Windows.Forms.PictureBox pictureBoxLeft;
    private System.Windows.Forms.PictureBox pictureBoxRight;
    private System.Windows.Forms.Label remainingLabel;
    private System.Windows.Forms.Label instructionLabel;
    private System.Windows.Forms.FlowLayoutPanel thumbnailPanel;
    private System.Windows.Forms.MenuStrip menuStrip;
    private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
    private System.Windows.Forms.Button rotateClockwiseButton;
    private System.Windows.Forms.Button rotateCounterclockwiseButton;
    private System.Windows.Forms.Label deletePromptLabel;
}