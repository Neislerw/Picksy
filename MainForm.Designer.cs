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
        this.deleteButton = new System.Windows.Forms.Button();
        this.cancelButton = new System.Windows.Forms.Button();
        this.menuStrip = new System.Windows.Forms.MenuStrip();
        this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        ((System.ComponentModel.ISupportInitialize)(this.pictureBoxLeft)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.pictureBoxRight)).BeginInit();
        this.thumbnailPanel.SuspendLayout();
        this.menuStrip.SuspendLayout();
        this.SuspendLayout();
        // 
        // selectFolderButton
        // 
        this.selectFolderButton.Location = new System.Drawing.Point(20, 30);
        this.selectFolderButton.Name = "selectFolderButton";
        this.selectFolderButton.Size = new System.Drawing.Size(150, 30);
        this.selectFolderButton.TabIndex = 0;
        this.selectFolderButton.Text = "Select Image Folder";
        this.selectFolderButton.UseVisualStyleBackColor = true;
        this.selectFolderButton.Click += new System.EventHandler(this.SelectFolderButton_Click);
        // 
        // pictureBoxLeft
        // 
        this.pictureBoxLeft.Location = new System.Drawing.Point(20, 70);
        this.pictureBoxLeft.Name = "pictureBoxLeft";
        this.pictureBoxLeft.Size = new System.Drawing.Size(380, 400);
        this.pictureBoxLeft.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
        this.pictureBoxLeft.TabIndex = 1;
        this.pictureBoxLeft.TabStop = false;
        this.pictureBoxLeft.Click += new System.EventHandler(this.PictureBoxLeft_Click);
        // 
        // pictureBoxRight
        // 
        this.pictureBoxRight.Location = new System.Drawing.Point(400, 70);
        this.pictureBoxRight.Name = "pictureBoxRight";
        this.pictureBoxRight.Size = new System.Drawing.Size(380, 400);
        this.pictureBoxRight.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
        this.pictureBoxRight.TabIndex = 2;
        this.pictureBoxRight.TabStop = false;
        this.pictureBoxRight.Click += new System.EventHandler(this.PictureBoxRight_Click);
        // 
        // remainingLabel
        // 
        this.remainingLabel.Location = new System.Drawing.Point(20, 480);
        this.remainingLabel.Name = "remainingLabel";
        this.remainingLabel.Size = new System.Drawing.Size(760, 30);
        this.remainingLabel.TabIndex = 3;
        this.remainingLabel.Text = "Photos remaining: 0";
        this.remainingLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        // 
        // instructionLabel
        // 
        this.instructionLabel.Location = new System.Drawing.Point(20, 510);
        this.instructionLabel.Name = "instructionLabel";
        this.instructionLabel.Size = new System.Drawing.Size(760, 60);
        this.instructionLabel.TabIndex = 4;
        this.instructionLabel.Text = "Click or use Left/Right arrow to select a photo, Up to keep both, Down to undo, Space to keep all remaining.";
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
        // deleteButton
        // 
        this.deleteButton.Location = new System.Drawing.Point(200, 480);
        this.deleteButton.Name = "deleteButton";
        this.deleteButton.Size = new System.Drawing.Size(150, 30);
        this.deleteButton.TabIndex = 6;
        this.deleteButton.Text = "Delete";
        this.deleteButton.UseVisualStyleBackColor = true;
        this.deleteButton.Visible = false;
        this.deleteButton.Click += new System.EventHandler(this.DeleteButton_Click);
        // 
        // cancelButton
        // 
        this.cancelButton.Location = new System.Drawing.Point(450, 480);
        this.cancelButton.Name = "cancelButton";
        this.cancelButton.Size = new System.Drawing.Size(150, 30);
        this.cancelButton.TabIndex = 7;
        this.cancelButton.Text = "Cancel";
        this.cancelButton.UseVisualStyleBackColor = true;
        this.cancelButton.Visible = false;
        this.cancelButton.Click += new System.EventHandler(this.CancelButton_Click);
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
        // MainForm
        // 
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(800, 600);
        this.Controls.Add(this.cancelButton);
        this.Controls.Add(this.deleteButton);
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
    private System.Windows.Forms.Button deleteButton;
    private System.Windows.Forms.Button cancelButton;
    private System.Windows.Forms.MenuStrip menuStrip;
    private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
}