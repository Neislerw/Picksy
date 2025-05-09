using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

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

        public MainForm()
        {
            InitializeComponent();
            history = new Stack<(string? Loser, bool KeptBoth)>();
            try
            {
                this.Icon = new Icon("Resources\\logo.ico");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading icon: {ex.Message}", "Picksy Error");
            }
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
            currentPairIndex = 0;
            selectFolderButton.Visible = false;
            thumbnailPanel.Visible = false;
            deleteButton.Visible = false;
            cancelButton.Visible = false;
            pictureBoxLeft.Visible = true;
            pictureBoxRight.Visible = true;
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

            // Load two photos
            pictureBoxLeft.Image?.Dispose();
            pictureBoxRight.Image?.Dispose();
            pictureBoxLeft.Image = null;
            pictureBoxRight.Image = null;
            try
            {
                pictureBoxLeft.Image = Image.FromFile(remainingPhotos[currentPairIndex]);
                pictureBoxRight.Image = Image.FromFile(remainingPhotos[currentPairIndex + 1]);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading images: {ex.Message}. Skipping this pair.", "Picksy Error");
                remainingPhotos.RemoveAt(currentPairIndex + 1);
                remainingPhotos.RemoveAt(currentPairIndex);
                UpdateTournamentUI();
                return;
            }
            remainingLabel.Text = $"Photos remaining: {remainingPhotos.Count}";
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

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (currentBatch != null)
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
                        Size = new System.Drawing.Size(100, 100),
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
            deleteButton.Visible = true;
            cancelButton.Visible = true;
        }

        private void DeleteButton_Click(object sender, EventArgs e)
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

        private void CancelButton_Click(object sender, EventArgs e)
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
        }

        private void MoveToNextBatch()
        {
            thumbnailPanel.Visible = false;
            deleteButton.Visible = false;
            cancelButton.Visible = false;
            ResetUI();

            if (batches != null && currentBatchIndex + 1 < batches.Count)
            {
                currentBatchIndex++;
                StartTournament(batches[currentBatchIndex]);
            }
            else
            {
                MessageBox.Show("All batches completed.", "Picksy");
            }
        }

        private void ResetUI()
        {
            pictureBoxLeft.Image?.Dispose();
            pictureBoxRight.Image?.Dispose();
            pictureBoxLeft.Image = null;
            pictureBoxRight.Image = null;
            remainingLabel.Text = "";
            selectFolderButton.Visible = true;
            currentBatch = null;
            remainingPhotos = null;
            losers = null;
            history.Clear();
            // Preserve batches and currentFolderPath for next batch
            ClearThumbnails();
        }
    }
}