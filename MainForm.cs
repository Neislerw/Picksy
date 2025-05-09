using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Drawing2D;

namespace Picksy
{
    public partial class MainForm : Form
    {
        private List<List<string>>? batches;
        private List<string>? currentBatch;
        private int currentPairIndex;
        private List<string>? remainingPhotos;
        private List<string>? losers;
        private string? currentFolderPath;
        private int currentBatchIndex;
        private Stack<(string? Loser, bool KeptBoth)> history;
        private int batchSizeMinimum = 4; // Default
        private int batchTimingMaximum = 20; // Default in seconds
        private Dictionary<string, int> photoRotations; // Track rotation angle (degrees) per photo
        private bool showFullResolution = false; // Track full-resolution toggle

        public MainForm()
        {
            InitializeComponent();
            history = new Stack<(string? Loser, bool KeptBoth)>();
            photoRotations = new Dictionary<string, int>();
            // Ensure initial UI state is clean
            pictureBoxLeft.Visible = false;
            pictureBoxRight.Visible = false;
            rotateClockwiseButton.Visible = false;
            rotateCounterclockwiseButton.Visible = false;
            thumbnailPanel.Visible = false;
            deletePromptLabel.Visible = false;
            remainingLabel.Visible = false;
            instructionLabel.Visible = false;
            selectFolderButton.Visible = true;
            selectFolderButton.BringToFront(); // Ensure button is on top
            try
            {
                this.Icon = new Icon("Resources\\logo.ico");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading icon: {ex.Message}", "Picksy Error");
            }
            UpdateSelectFolderButtonPosition();
        }

        private void SelectFolderButton_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        currentFolderPath = dialog.SelectedPath;
                        var grouper = new PhotoGrouper(batchSizeMinimum, batchTimingMaximum);
                        batches = grouper.GroupPhotos(dialog.SelectedPath);
                        currentBatchIndex = 0;
                        if (batches.Count > 0)
                        {
                            StartTournament(batches[0]);
                        }
                        else
                        {
                            MessageBox.Show("No valid batches found with at least " + batchSizeMinimum + " photos.", "Picksy");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error scanning folder: {ex.Message}", "Picksy Error");
                    }
                }
            }
        }

        private void SettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var settingsForm = new SettingsForm(batchSizeMinimum, batchTimingMaximum))
            {
                if (settingsForm.ShowDialog() == DialogResult.OK)
                {
                    batchSizeMinimum = settingsForm.GetBatchSizeMinimum();
                    batchTimingMaximum = settingsForm.GetBatchTimingMaximum();
                }
            }
        }

        private void StartTournament(List<string> batch)
        {
            if (batch == null || batch.Count < batchSizeMinimum)
            {
                MessageBox.Show("Invalid batch. At least " + batchSizeMinimum + " photos required.", "Picksy");
                ResetUI();
                return;
            }

            currentBatch = new List<string>(batch);
            remainingPhotos = new List<string>(batch);
            losers = new List<string>();
            history.Clear();
            photoRotations.Clear();
            foreach (var photo in batch)
            {
                photoRotations[photo] = 0; // Initialize rotation to 0 degrees
            }
            currentPairIndex = 0;
            showFullResolution = false;
            selectFolderButton.Visible = false;
            thumbnailPanel.Visible = false;
            deletePromptLabel.Visible = false;
            pictureBoxLeft.Visible = true;
            pictureBoxRight.Visible = true;
            rotateClockwiseButton.Visible = true;
            rotateCounterclockwiseButton.Visible = true;
            remainingLabel.Visible = true;
            instructionLabel.Visible = true;
            UpdateTournamentUI();
        }

        private void UpdateTournamentUI()
        {
            if (remainingPhotos == null || remainingPhotos.Count == 0)
            {
                MessageBox.Show("Tournament ended. No photos left.", "Picksy");
                ShowResults();
                return;
            }
            if (remainingPhotos.Count == 1)
            {
                MessageBox.Show($"Tournament ended. Winner: {Path.GetFileName(remainingPhotos[0])}", "Picksy");
                ShowResults();
                return;
            }
            if (currentPairIndex + 1 >= remainingPhotos.Count)
            {
                // Shuffle remaining photos for next round
                remainingPhotos = Shuffle(remainingPhotos);
                currentPairIndex = 0;
            }

            // Load two photos with rotation
            pictureBoxLeft.Image?.Dispose();
            pictureBoxRight.Image?.Dispose();
            pictureBoxLeft.Image = null;
            pictureBoxRight.Image = null;
            Image? leftImage = null;
            Image? rightImage = null;
            try
            {
                leftImage = Image.FromFile(remainingPhotos[currentPairIndex]);
                rightImage = Image.FromFile(remainingPhotos[currentPairIndex + 1]);
                int leftRotation = photoRotations[remainingPhotos[currentPairIndex]];
                int rightRotation = photoRotations[remainingPhotos[currentPairIndex + 1]];
                if (showFullResolution)
                {
                    pictureBoxLeft.Image = RotateImage(leftImage, leftRotation);
                    pictureBoxRight.Image = RotateImage(rightImage, rightRotation);
                }
                else
                {
                    // Load thumbnails for performance
                    pictureBoxLeft.Image = CreateThumbnail(leftImage, leftRotation);
                    pictureBoxRight.Image = CreateThumbnail(rightImage, rightRotation);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading images: {ex.Message}. Skipping this pair.", "Picksy Error");
                remainingPhotos.RemoveAt(currentPairIndex + 1);
                remainingPhotos.RemoveAt(currentPairIndex);
                leftImage?.Dispose();
                rightImage?.Dispose();
                UpdateTournamentUI();
                return;
            }
            finally
            {
                // Dispose original images after use
                leftImage?.Dispose();
                rightImage?.Dispose();
            }
            remainingLabel.Text = $"Photos remaining: {remainingPhotos.Count}";
            UpdatePictureBoxSizes();
        }

        private Image CreateThumbnail(Image image, int rotation)
        {
            // Create a thumbnail scaled to fit within 400x400, preserving aspect ratio
            int maxSize = 400;
            float ratio = Math.Min((float)maxSize / image.Width, (float)maxSize / image.Height);
            int newWidth = (int)(image.Width * ratio);
            int newHeight = (int)(image.Height * ratio);
            var thumbnail = new Bitmap(newWidth, newHeight);
            try
            {
                using (var g = Graphics.FromImage(thumbnail))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.Clear(Color.Transparent);
                    g.DrawImage(image, 0, 0, newWidth, newHeight);
                }
                return RotateImage(thumbnail, rotation);
            }
            catch
            {
                thumbnail.Dispose();
                throw;
            }
        }

        private Image RotateImage(Image image, int angle)
        {
            if (angle == 0) return new Bitmap(image); // Return a copy to ensure disposal
            // Calculate new size for rotated image
            double radians = Math.Abs(angle * Math.PI / 180);
            double sin = Math.Sin(radians);
            double cos = Math.Cos(radians);
            int newWidth = (int)(image.Width * cos + image.Height * sin);
            int newHeight = (int)(image.Width * sin + image.Height * cos);
            var rotated = new Bitmap(newWidth, newHeight);
            try
            {
                using (var g = Graphics.FromImage(rotated))
                {
                    g.Clear(Color.Transparent);
                    g.TranslateTransform(newWidth / 2f, newHeight / 2f);
                    g.RotateTransform(angle);
                    g.TranslateTransform(-image.Width / 2f, -image.Height / 2f);
                    g.DrawImage(image, 0, 0);
                }
                return rotated;
            }
            catch
            {
                rotated.Dispose();
                throw;
            }
        }

        private void UpdatePictureBoxSizes()
        {
            // Calculate available space, accounting for menu strip, buttons, and labels
            int availableHeight = ClientSize.Height - menuStrip.Height - remainingLabel.Height - instructionLabel.Height - rotateClockwiseButton.Height - 30;
            int availableWidth = (ClientSize.Width - 30) / 2; // Split for two images with padding

            // Set PictureBox size to fit images without cropping
            pictureBoxLeft.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxRight.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxLeft.Size = new Size(availableWidth, availableHeight);
            pictureBoxRight.Size = new Size(availableWidth, availableHeight);
            pictureBoxLeft.Location = new Point(10, menuStrip.Height + 10);
            pictureBoxRight.Location = new Point(ClientSize.Width - availableWidth - 10, menuStrip.Height + 10);
            rotateClockwiseButton.Location = new Point(10, pictureBoxLeft.Bottom + 5);
            rotateCounterclockwiseButton.Location = new Point(ClientSize.Width - rotateCounterclockwiseButton.Width - 10, pictureBoxLeft.Bottom + 5);
            remainingLabel.Location = new Point(10, ClientSize.Height - instructionLabel.Height - remainingLabel.Height - 10);
            instructionLabel.Location = new Point(10, ClientSize.Height - instructionLabel.Height - 5);
            thumbnailPanel.Location = new Point(20, menuStrip.Height + 10);
            deletePromptLabel.Location = new Point(20, thumbnailPanel.Bottom + 10);
        }

        private void UpdateSelectFolderButtonPosition()
        {
            // Center the select folder button
            int x = (ClientSize.Width - selectFolderButton.Width) / 2;
            int y = (ClientSize.Height - selectFolderButton.Height) / 2;
            selectFolderButton.Location = new Point(x, y);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (remainingPhotos != null && pictureBoxLeft.Visible)
            {
                UpdatePictureBoxSizes();
            }
            else if (selectFolderButton.Visible)
            {
                UpdateSelectFolderButtonPosition();
            }
        }

        private List<string> Shuffle(List<string> list)
        {
            var rng = new Random();
            var result = new List<string>(list);
            int n = result.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                var value = result[k];
                result[k] = result[n];
                result[n] = value;
            }
            return result;
        }

        private void PictureBoxLeft_Click(object sender, EventArgs e)
        {
            SelectPhoto(true);
        }

        private void PictureBoxRight_Click(object sender, EventArgs e)
        {
            SelectPhoto(false);
        }

        private void RotateClockwiseButton_Click(object sender, EventArgs e)
        {
            if (currentBatch == null) return;
            foreach (var photo in currentBatch)
            {
                photoRotations[photo] = (photoRotations[photo] + 90) % 360;
            }
            UpdateTournamentUI();
        }

        private void RotateCounterclockwiseButton_Click(object sender, EventArgs e)
        {
            if (currentBatch == null) return;
            foreach (var photo in currentBatch)
            {
                photoRotations[photo] = (photoRotations[photo] - 90 + 360) % 360;
            }
            UpdateTournamentUI();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (currentBatch != null && pictureBoxLeft.Visible)
            {
                if (keyData == Keys.Left)
                {
                    SelectPhoto(true);
                    return true;
                }
                else if (keyData == Keys.Right)
                {
                    SelectPhoto(false);
                    return true;
                }
                else if (keyData == Keys.Up)
                {
                    KeepBothPhotos();
                    return true;
                }
                else if (keyData == Keys.Down)
                {
                    UndoLastAction();
                    return true;
                }
                else if (keyData == Keys.Space)
                {
                    EndTournament();
                    return true;
                }
                else if (keyData == Keys.Q)
                {
                    if (currentBatch == null) return true;
                    foreach (var photo in currentBatch)
                    {
                        photoRotations[photo] = (photoRotations[photo] - 90 + 360) % 360;
                    }
                    UpdateTournamentUI();
                    return true;
                }
                else if (keyData == Keys.E)
                {
                    if (currentBatch == null) return true;
                    foreach (var photo in currentBatch)
                    {
                        photoRotations[photo] = (photoRotations[photo] + 90) % 360;
                    }
                    UpdateTournamentUI();
                    return true;
                }
                else if (keyData == Keys.W)
                {
                    showFullResolution = !showFullResolution;
                    UpdateTournamentUI();
                    return true;
                }
            }
            else if (thumbnailPanel.Visible && losers != null)
            {
                if (keyData == Keys.Enter)
                {
                    MoveToDeleteFolder();
                    return true;
                }
                else if (keyData == Keys.Escape)
                {
                    CancelBatch();
                    return true;
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void SelectPhoto(bool leftSelected)
        {
            if (remainingPhotos == null) return;

            // Keep the winner, add loser to losers list
            int winnerIndex = leftSelected ? currentPairIndex : currentPairIndex + 1;
            int loserIndex = leftSelected ? currentPairIndex + 1 : currentPairIndex;
            string loser = remainingPhotos[loserIndex];
            losers?.Add(loser);
            remainingPhotos.RemoveAt(loserIndex);
            if (winnerIndex > loserIndex)
            {
                winnerIndex--;
            }
            history.Push((loser, false));
            currentPairIndex = winnerIndex;

            // Advance to next pair
            UpdateTournamentUI();
        }

        private void KeepBothPhotos()
        {
            if (remainingPhotos == null) return;

            // Keep both photos, advance to next pair
            history.Push((null, true));
            currentPairIndex += 2;
            UpdateTournamentUI();
        }

        private void EndTournament()
        {
            if (remainingPhotos == null) return;

            // End tournament, keep all remaining photos
            MessageBox.Show($"Tournament ended. Keeping {remainingPhotos.Count} remaining photos.", "Picksy");
            ShowResults();
        }

        private void UndoLastAction()
        {
            if (remainingPhotos == null || history.Count == 0)
            {
                MessageBox.Show("No actions to undo.", "Picksy");
                return;
            }

            var lastAction = history.Pop();
            if (lastAction.KeptBoth)
            {
                // Revert keeping both by moving back one pair
                if (currentPairIndex >= 2)
                {
                    currentPairIndex -= 2;
                }
                else
                {
                    currentPairIndex = 0;
                }
            }
            else if (lastAction.Loser != null)
            {
                // Restore the loser
                losers?.Remove(lastAction.Loser);
                remainingPhotos.Insert(currentPairIndex + 1, lastAction.Loser);
            }

            UpdateTournamentUI();
        }

        private void ShowResults()
        {
            if (losers == null || losers.Count == 0)
            {
                MessageBox.Show("No losing photos to display.", "Picksy");
                MoveToNextBatch();
                return;
            }

            // Hide tournament UI
            pictureBoxLeft.Image?.Dispose();
            pictureBoxRight.Image?.Dispose();
            pictureBoxLeft.Visible = false;
            pictureBoxRight.Visible = false;
            rotateClockwiseButton.Visible = false;
            rotateCounterclockwiseButton.Visible = false;
            remainingLabel.Visible = false;
            instructionLabel.Visible = false;

            // Show thumbnails
            ClearThumbnails();
            foreach (var loser in losers)
            {
                try
                {
                    var pictureBox = new PictureBox
                    {
                        Size = new Size(100, 100),
                        SizeMode = PictureBoxSizeMode.Zoom,
                        Image = Image.FromFile(loser)
                    };
                    thumbnailPanel.Controls.Add(pictureBox);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading thumbnail for {Path.GetFileName(loser)}: {ex.Message}", "Picksy Error");
                }
            }

            thumbnailPanel.Visible = true;
            deletePromptLabel.Visible = true;
        }

        private void MoveToDeleteFolder()
        {
            if (losers == null || currentFolderPath == null) return;

            try
            {
                // Dispose thumbnails to release file handles
                ClearThumbnails();

                string deleteFolder = Path.Combine(currentFolderPath, "_delete");
                Directory.CreateDirectory(deleteFolder);

                foreach (var loser in losers)
                {
                    string fileName = Path.GetFileName(loser);
                    string destPath = Path.Combine(deleteFolder, fileName);
                    // Handle duplicate filenames
                    int counter = 1;
                    while (File.Exists(destPath))
                    {
                        string baseName = Path.GetFileNameWithoutExtension(fileName);
                        string extension = Path.GetExtension(fileName);
                        destPath = Path.Combine(deleteFolder, $"{baseName}_{counter}{extension}");
                        counter++;
                    }
                    File.Move(loser, destPath);
                }

                MoveToNextBatch();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error moving files: {ex.Message}", "Picksy Error");
            }
        }

        private void CancelBatch()
        {
            // Dispose thumbnails to release file handles
            ClearThumbnails();
            MoveToNextBatch();
        }

        private void ClearThumbnails()
        {
            foreach (Control control in thumbnailPanel.Controls)
            {
                if (control is PictureBox pictureBox)
                {
                    pictureBox.Image?.Dispose();
                    pictureBox.Image = null;
                }
            }
            thumbnailPanel.Controls.Clear();
            // Reset visibility
            deletePromptLabel.Visible = false;
        }

        private void MoveToNextBatch()
        {
            thumbnailPanel.Visible = false;
            deletePromptLabel.Visible = false;
            ResetUI();

            if (batches != null && currentBatchIndex + 1 < batches.Count)
            {
                currentBatchIndex++;
                StartTournament(batches[currentBatchIndex]);
            }
            else
            {
                // Calculate size of _delete folder
                double deleteFolderSizeMB = 0;
                if (currentFolderPath != null)
                {
                    string deleteFolder = Path.Combine(currentFolderPath, "_delete");
                    if (Directory.Exists(deleteFolder))
                    {
                        long totalBytes = 0;
                        foreach (var file in Directory.GetFiles(deleteFolder, "*", SearchOption.AllDirectories))
                        {
                            totalBytes += new FileInfo(file).Length;
                        }
                        deleteFolderSizeMB = totalBytes / (1024.0 * 1024.0); // Convert bytes to MB
                    }
                }
                if (deleteFolderSizeMB > 1024)
                {
                    double deleteFolderSizeGB = deleteFolderSizeMB / 1024.0;
                    MessageBox.Show($"All batches completed. {deleteFolderSizeGB:F1} GB Saved!", "Picksy");
                }
                else
                {
                    MessageBox.Show($"All batches completed. {deleteFolderSizeMB:F2} MB Saved!", "Picksy");
                }
            }
        }

        private void ResetUI()
        {
            pictureBoxLeft.Image?.Dispose();
            pictureBoxRight.Image?.Dispose();
            pictureBoxLeft.Image = null;
            pictureBoxRight.Image = null;
            pictureBoxLeft.Visible = false;
            pictureBoxRight.Visible = false;
            remainingLabel.Text = "";
            selectFolderButton.Visible = true;
            selectFolderButton.BringToFront();
            rotateClockwiseButton.Visible = false;
            rotateCounterclockwiseButton.Visible = false;
            thumbnailPanel.Visible = false;
            deletePromptLabel.Visible = false;
            remainingLabel.Visible = false;
            instructionLabel.Visible = false;
            currentBatch = null;
            remainingPhotos = null;
            losers = null;
            history.Clear();
            // Preserve batches and currentFolderPath for next batch
            ClearThumbnails();
            UpdateSelectFolderButtonPosition();
        }
    }
}