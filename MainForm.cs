#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text.Json;

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
        private Dictionary<string, int> photoRotations; // Track rotation angle (degrees) per photo
        private bool showFullResolution = false; // Track full-resolution toggle
        private int initialFileCount = 0; // Total files in folder at start
        private int totalBatchPhotos = 0; // Total photos in all batches
        private int deletedPhotosCount = 0; // Total photos moved to _delete
        private bool isLoadingSession = false; // Flag to indicate session loading

        public MainForm()
        {
            InitializeComponent();
            history = new Stack<(string? Loser, bool KeptBoth)>();
            photoRotations = new Dictionary<string, int>();
            // Initialize Batch Selection Method ComboBox
            batchSelectionMethodComboBox.Items.AddRange(new[] { "By Name", "By Date Created", "By Date Modified" });
            batchSelectionMethodComboBox.SelectedIndex = 0; // Default to "By Name"
            // Set default batch timing to 300 seconds
            batchTimingNumericUpDown.Value = 300;
            // Ensure initial UI state is clean
            pictureBoxLeft.Visible = false;
            pictureBoxRight.Visible = false;
            rotateClockwiseButton.Visible = false;
            rotateCounterclockwiseButton.Visible = false;
            saveAndQuitButton.Visible = false;
            thumbnailPanel.Visible = false;
            deletePromptLabel.Visible = false;
            remainingLabel.Visible = false;
            instructionLabel.Visible = false;
            selectFolderButton.Visible = true;
            loadSessionButton.Visible = true;
            settingsGroupBox.Visible = true;
            logoPictureBox.Visible = true;
            selectFolderButton.BringToFront(); // Ensure buttons are on top
            loadSessionButton.BringToFront();
            try
            {
                this.Icon = new Icon("Resources\\logo.ico");
                logoPictureBox.Image = Image.FromFile("Resources\\logo.png");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading icon or logo: {ex.Message}", "Picksy Error");
            }
            UpdateMainPageControlsPosition();
        }

        private void SelectFolderButton_Click(object sender, EventArgs e)
        {
            int batchSizeMinimum = (int)batchSizeNumericUpDown.Value;
            int batchTimingMaximum = (int)batchTimingNumericUpDown.Value;
            bool includeSubfolders = includeSubfoldersCheckBox.Checked;
            string? batchSelectionMethod = batchSelectionMethodComboBox.SelectedItem?.ToString();

            if (batchSizeMinimum < 2 || batchSizeMinimum > 100)
            {
                MessageBox.Show("Batch Size Minimum must be between 2 and 100.", "Invalid Input");
                return;
            }
            if (batchTimingMaximum < 1 || batchTimingMaximum > 600)
            {
                MessageBox.Show("Batch Timing Maximum must be between 1 and 600 seconds.", "Invalid Input");
                return;
            }
            if (string.IsNullOrEmpty(batchSelectionMethod))
            {
                MessageBox.Show("Please select a batch selection method.", "Invalid Input");
                return;
            }

            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select a folder containing photos";
                dialog.ShowNewFolderButton = false;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        currentFolderPath = dialog.SelectedPath;
                        if (string.IsNullOrEmpty(currentFolderPath) || !Directory.Exists(currentFolderPath))
                        {
                            throw new InvalidOperationException($"Invalid or inaccessible folder selected. Path: {currentFolderPath}");
                        }

                        // Count initial image files
                        var imageExtensions = new[] { ".jpg", ".jpeg", ".png" };
                        var searchOption = includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                        initialFileCount = Directory.GetFiles(currentFolderPath, "*.*", searchOption)
                            .Where(f => imageExtensions.Contains(Path.GetExtension(f).ToLower()))
                            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "_delete" + Path.DirectorySeparatorChar))
                            .Count();
                        var grouper = new PhotoGrouper(batchSizeMinimum, batchTimingMaximum, includeSubfolders, batchSelectionMethod);
                        batches = grouper.GroupPhotos(dialog.SelectedPath);
                        currentBatchIndex = 0;
                        totalBatchPhotos = 0;
                        deletedPhotosCount = 0;
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
                        try
                        {
                            File.WriteAllText("picksy_error.log", $"Error details: {ex}\nAttempted path: {dialog.SelectedPath}\nTimestamp: {DateTime.Now}");
                        }
                        catch
                        {
                            Console.WriteLine($"Error details: {ex}\nAttempted path: {dialog.SelectedPath}");
                        }
                    }
                }
            }
        }

        private void LoadSessionButton_Click(object sender, EventArgs e)
        {
            if (!File.Exists("picksy_state.json"))
            {
                MessageBox.Show("No saved session found at picksy_state.json.", "Picksy");
                return;
            }

            try
            {
                string json = File.ReadAllText("picksy_state.json");
                var state = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json) 
                    ?? throw new InvalidOperationException("Failed to deserialize saved session.");

                // Restore state with null checks and default values
                currentFolderPath = state.TryGetValue("CurrentFolderPath", out var folderPath) 
                    ? folderPath.GetString() 
                    : throw new InvalidOperationException("Missing CurrentFolderPath in saved session.");
                if (string.IsNullOrEmpty(currentFolderPath) || !Directory.Exists(currentFolderPath))
                {
                    throw new InvalidOperationException("Saved folder path is invalid or inaccessible.");
                }

                batches = state.TryGetValue("Batches", out var batchesElement)
                    ? batchesElement.Deserialize<List<List<string>>>()
                    : throw new InvalidOperationException("Missing Batches in saved session.");
                
                currentBatchIndex = state.TryGetValue("CurrentBatchIndex", out var batchIndexElement) && batchIndexElement.TryGetInt32(out int batchIndex)
                    ? batchIndex
                    : throw new InvalidOperationException("Invalid or missing CurrentBatchIndex in saved session.");
                
                currentBatch = state.TryGetValue("CurrentBatch", out var currentBatchElement)
                    ? currentBatchElement.Deserialize<List<string>>()
                    : throw new InvalidOperationException("Missing CurrentBatch in saved session.");
                
                currentPairIndex = state.TryGetValue("CurrentPairIndex", out var pairIndexElement) && pairIndexElement.TryGetInt32(out int pairIndex)
                    ? pairIndex
                    : throw new InvalidOperationException("Invalid or missing CurrentPairIndex in saved session.");
                
                remainingPhotos = state.TryGetValue("RemainingPhotos", out var remainingPhotosElement)
                    ? remainingPhotosElement.Deserialize<List<string>>()
                    : throw new InvalidOperationException("Missing RemainingPhotos in saved session.");
                
                losers = state.TryGetValue("Losers", out var losersElement)
                    ? losersElement.Deserialize<List<string>>()
                    : throw new InvalidOperationException("Missing Losers in saved session.");
                
                photoRotations = state.TryGetValue("PhotoRotations", out var rotationsElement)
                    ? rotationsElement.Deserialize<Dictionary<string, int>>() ?? new Dictionary<string, int>()
                    : new Dictionary<string, int>();
                
                totalBatchPhotos = state.TryGetValue("TotalBatchPhotos", out var totalPhotosElement) && totalPhotosElement.TryGetInt32(out int totalPhotos)
                    ? totalPhotos
                    : 0;
                
                deletedPhotosCount = state.TryGetValue("DeletedPhotosCount", out var deletedCountElement) && deletedCountElement.TryGetInt32(out int deletedCount)
                    ? deletedCount
                    : 0;
                
                initialFileCount = state.TryGetValue("InitialFileCount", out var initialCountElement) && initialCountElement.TryGetInt32(out int initialCount)
                    ? initialCount
                    : 0;

                // Validate state
                if (batches == null || currentBatch == null || remainingPhotos == null || losers == null ||
                    currentBatchIndex < 0 || currentBatchIndex >= batches.Count ||
                    !remainingPhotos.All(f => File.Exists(f)) || !losers.All(f => File.Exists(f)))
                {
                    throw new InvalidOperationException("Invalid or corrupted saved session.");
                }

                // Resume tournament
                isLoadingSession = true; // Set flag for session loading
                try
                {
                    selectFolderButton.Visible = false;
                    loadSessionButton.Visible = false;
                    settingsGroupBox.Visible = false;
                    logoPictureBox.Visible = false;
                    StartTournament(currentBatch);
                }
                finally
                {
                    isLoadingSession = false; // Reset flag
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading session: {ex.Message}", "Picksy Error");
                try
                {
                    File.WriteAllText("picksy_error.log", $"Error loading session: {ex}\nTimestamp: {DateTime.Now}");
                }
                catch
                {
                    Console.WriteLine($"Error loading session: {ex}");
                }
            }
        }

        private void StartTournament(List<string> batch)
        {
            // Allow batches with at least 2 photos when loading a session (partially completed batch)
            int minimumBatchSize = isLoadingSession ? 2 : (int)batchSizeNumericUpDown.Value;
            if (batch == null || batch.Count < minimumBatchSize)
            {
                MessageBox.Show($"Invalid batch. At least {minimumBatchSize} photos required.", "Picksy");
                ResetUI();
                return;
            }

            currentBatch = new List<string>(batch);
            if (remainingPhotos == null) // Only set if not loaded from state
            {
                remainingPhotos = new List<string>(batch);
            }
            if (losers == null) // Only set if not loaded from state
            {
                losers = new List<string>();
            }
            history.Clear();
            // Initialize rotations for all batch photos, preserving existing ones from state
            foreach (var photo in batch)
            {
                if (!photoRotations.ContainsKey(photo))
                {
                    photoRotations[photo] = 0; // Initialize rotation to 0 degrees
                }
            }
            if (!isLoadingSession)
            {
                totalBatchPhotos += batch.Count; // Track total photos in batches, only for new sessions
            }
            showFullResolution = false;
            selectFolderButton.Visible = false;
            loadSessionButton.Visible = false;
            settingsGroupBox.Visible = false;
            logoPictureBox.Visible = false;
            thumbnailPanel.Visible = false;
            deletePromptLabel.Visible = false;
            pictureBoxLeft.Visible = true;
            pictureBoxRight.Visible = true;
            rotateClockwiseButton.Visible = true;
            rotateCounterclockwiseButton.Visible = true;
            saveAndQuitButton.Visible = true;
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
                // Set tooltips with filenames
                toolTipLeft.SetToolTip(pictureBoxLeft, Path.GetFileName(remainingPhotos[currentPairIndex]));
                toolTipRight.SetToolTip(pictureBoxRight, Path.GetFileName(remainingPhotos[currentPairIndex + 1]));
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
            // Update remaining photos and batches display
            int batchesRemaining = batches != null ? batches.Count - currentBatchIndex - 1 : 0;
            remainingLabel.Text = $"Photos remaining: {remainingPhotos.Count} | Batches remaining: {batchesRemaining}";
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
            int availableHeight = ClientSize.Height - menuStrip.Height - remainingLabel.Height - instructionLabel.Height - rotateClockwiseButton.Height - saveAndQuitButton.Height - 40;
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
            saveAndQuitButton.Location = new Point((ClientSize.Width - saveAndQuitButton.Width) / 2, pictureBoxLeft.Bottom + 5);
            remainingLabel.Location = new Point(10, saveAndQuitButton.Bottom + 5);
            instructionLabel.Location = new Point(10, remainingLabel.Bottom + 5);
            thumbnailPanel.Location = new Point(20, menuStrip.Height + 10);
            deletePromptLabel.Location = new Point(20, thumbnailPanel.Bottom + 10);
        }

        private void UpdateMainPageControlsPosition()
        {
            // Center the logo, settings group box, and buttons vertically
            int totalHeight = logoPictureBox.Height + 10 + settingsGroupBox.Height + 10 + selectFolderButton.Height + 10 + loadSessionButton.Height;
            int startY = (ClientSize.Height - totalHeight) / 2;

            // Logo PictureBox
            int x = (ClientSize.Width - logoPictureBox.Width) / 2;
            logoPictureBox.Location = new Point(x, startY);

            // Settings GroupBox
            startY += logoPictureBox.Height + 10;
            x = (ClientSize.Width - settingsGroupBox.Width) / 2;
            settingsGroupBox.Location = new Point(x, startY);

            // Select Folder Button
            startY += settingsGroupBox.Height + 10;
            x = (ClientSize.Width - selectFolderButton.Width) / 2;
            selectFolderButton.Location = new Point(x, startY);

            // Load Session Button
            startY += selectFolderButton.Height + 10;
            x = (ClientSize.Width - loadSessionButton.Width) / 2;
            loadSessionButton.Location = new Point(x, startY);
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
                UpdateMainPageControlsPosition();
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

        private void SaveAndQuitButton_Click(object sender, EventArgs e)
        {
            try
            {
                var state = new
                {
                    CurrentFolderPath = currentFolderPath,
                    Batches = batches,
                    CurrentBatchIndex = currentBatchIndex,
                    CurrentBatch = currentBatch,
                    CurrentPairIndex = currentPairIndex,
                    RemainingPhotos = remainingPhotos,
                    Losers = losers,
                    PhotoRotations = photoRotations,
                    TotalBatchPhotos = totalBatchPhotos,
                    DeletedPhotosCount = deletedPhotosCount,
                    InitialFileCount = initialFileCount
                };
                File.WriteAllText("picksy_state.json", JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
                MessageBox.Show("State saved to picksy_state.json. Exiting.", "Picksy");
                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving state: {ex.Message}", "Picksy Error");
            }
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
                else if (keyData == Keys.Delete)
                {
                    if (currentBatch == null) return true;
                    var result = MessageBox.Show("Delete all photos in this batch?", "Confirm Batch Deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.Yes)
                    {
                        DeleteCurrentBatch();
                    }
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
                MoveToNextBatch();
                return;
            }

            if (skipConfirmationCheckBox.Checked)
            {
                MoveToDeleteFolder();
                return;
            }

            // Hide tournament UI
            pictureBoxLeft.Image?.Dispose();
            pictureBoxRight.Image?.Dispose();
            pictureBoxLeft.Image = null;
            pictureBoxRight.Image = null;
            pictureBoxLeft.Visible = false;
            pictureBoxRight.Visible = false;
            rotateClockwiseButton.Visible = false;
            rotateCounterclockwiseButton.Visible = false;
            saveAndQuitButton.Visible = false;
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

        private void DeleteCurrentBatch()
        {
            if (currentBatch == null || currentFolderPath == null) return;

            try
            {
                // Dispose current images to release file handles
                pictureBoxLeft.Image?.Dispose();
                pictureBoxRight.Image?.Dispose();
                pictureBoxLeft.Image = null;
                pictureBoxRight.Image = null;

                string deleteFolder = Path.Combine(currentFolderPath, "_delete");
                Directory.CreateDirectory(deleteFolder);

                foreach (var photo in currentBatch)
                {
                    string fileName = Path.GetFileName(photo);
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
                    File.Move(photo, destPath);
                }

                // Track deleted photos
                deletedPhotosCount += currentBatch.Count;

                // Clear current batch and advance
                currentBatch = null;
                remainingPhotos = null;
                losers = null;
                MoveToNextBatch();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error moving files: {ex.Message}", "Picksy Error");
            }
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

                // Track deleted photos
                deletedPhotosCount += losers.Count;

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
                string sizeMessage = deleteFolderSizeMB > 1024
                    ? $"{deleteFolderSizeMB / 1024.0:F1} GB"
                    : $"{deleteFolderSizeMB:F2} MB";
                string message = $"No more batches remain!\n\n" +
                                $"You just processed {batches?.Count ?? 0} batches, containing {totalBatchPhotos} photos, " +
                                $"eliminating {deletedPhotosCount} of them and saving {sizeMessage}!\n\n" +
                                $"When you are ready, erase the '_delete' folder to permanently remove those files";
                using (var form = new Form
                {
                    Text = "Picksy",
                    Size = new Size(400, 200),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false
                })
                {
                    var label = new Label
                    {
                        Text = message,
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleCenter
                    };
                    form.Controls.Add(label);
                    form.ShowDialog(this);
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
            loadSessionButton.Visible = true;
            settingsGroupBox.Visible = true;
            logoPictureBox.Visible = true;
            selectFolderButton.BringToFront();
            loadSessionButton.BringToFront();
            rotateClockwiseButton.Visible = false;
            rotateCounterclockwiseButton.Visible = false;
            saveAndQuitButton.Visible = false;
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
            UpdateMainPageControlsPosition();
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
    }
}