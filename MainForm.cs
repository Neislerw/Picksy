#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text.Json;
using System.Diagnostics;
using System.Web;

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
        private Dictionary<string, int> photoRotations;
        private bool showFullResolution = false;
        private int initialFileCount = 0;
        private int totalBatchPhotos = 0;
        private int deletedPhotosCount = 0;
        private bool isLoadingSession = false;

        public MainForm()
        {
            InitializeComponent();
            history = new Stack<(string? Loser, bool KeptBoth)>();
            photoRotations = new Dictionary<string, int>();
            batchSelectionMethodComboBox.Items.AddRange(new[] { "By Name", "By Date Created", "By Date Modified" });
            batchSelectionMethodComboBox.SelectedIndex = 0;
            batchTimingNumericUpDown.Value = 300;
            pictureBoxLeft.Visible = false;
            pictureBoxRight.Visible = false;
            rotateClockwiseButton.Visible = false;
            rotateCounterclockwiseButton.Visible = false;
            saveAndQuitButton.Visible = false;
            thumbnailPanel.Visible = false;
            deletePromptLabel.Visible = false;
            remainingLabel.Visible = false;
            instructionLabel.Visible = false;
            selectLocalFolderButton.Visible = true;
            selectPhoneFolderButton.Visible = true;
            settingsGroupBox.Visible = true;
            logoPictureBox.Visible = true;
            selectLocalFolderButton.BringToFront();
            selectPhoneFolderButton.BringToFront();
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

        private void SelectLocalFolderButton_Click(object sender, EventArgs e)
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
                dialog.Description = "Select a local folder containing photos";
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

                        string stateFilePath = Path.Combine(currentFolderPath, "picksy_state.json");
                        if (File.Exists(stateFilePath))
                        {
                            bool loadState = PromptLoadSavedState(stateFilePath, out var savedSettings);
                            if (loadState && savedSettings.HasValue)
                            {
                                batchSizeNumericUpDown.Value = savedSettings.Value.BatchSizeMinimum;
                                batchTimingNumericUpDown.Value = savedSettings.Value.BatchTimingMaximum;
                                includeSubfoldersCheckBox.Checked = savedSettings.Value.IncludeSubfolders;
                                batchSelectionMethodComboBox.SelectedItem = savedSettings.Value.BatchSelectionMethod;
                                LoadSession(stateFilePath, savedSettings.Value);
                                return;
                            }
                        }

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

        private void SelectPhoneFolderButton_Click(object sender, EventArgs e)
        {
            using (var mtpForm = new MTPDeviceForm())
            {
                mtpForm.ShowDialog(this);
            }
        }

        private bool PromptLoadSavedState(string stateFilePath, out (int BatchSizeMinimum, int BatchTimingMaximum, bool IncludeSubfolders, string BatchSelectionMethod)? savedSettings)
        {
            savedSettings = null;
            bool isSessionCompleted = false;
            try
            {
                string json = File.ReadAllText(stateFilePath);
                var state = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)
                    ?? throw new InvalidOperationException("Failed to deserialize saved session.");

                if (!state.TryGetValue("CurrentFolderPath", out var folderPath) || folderPath.GetString() == null)
                {
                    throw new InvalidOperationException("Missing CurrentFolderPath in saved session.");
                }
                int batchSizeMinimum = state.TryGetValue("BatchSizeMinimum", out var sizeElement) && sizeElement.TryGetInt32(out int size) ? size : (int)batchSizeNumericUpDown.Value;
                int batchTimingMaximum = state.TryGetValue("BatchTimingMaximum", out var timingElement) && timingElement.TryGetInt32(out int timing) ? timing : (int)batchTimingNumericUpDown.Value;
                bool includeSubfolders = state.TryGetValue("IncludeSubfolders", out var subfoldersElement) && subfoldersElement.ValueKind == JsonValueKind.True ? true : false;
                string batchSelectionMethod = state.TryGetValue("BatchSelectionMethod", out var methodElement) && methodElement.GetString() != null ? methodElement.GetString()! : batchSelectionMethodComboBox.SelectedItem?.ToString() ?? "By Name";

                if (state.TryGetValue("Batches", out var batchesElement) && batchesElement.Deserialize<List<List<string>>>() is List<List<string>> savedBatches)
                {
                    int currentBatchIndex = state.TryGetValue("CurrentBatchIndex", out var batchIndexElement) && batchIndexElement.TryGetInt32(out int batchIndex) ? batchIndex : 0;
                    isSessionCompleted = savedBatches == null || currentBatchIndex + 1 >= savedBatches.Count;
                }

                savedSettings = (batchSizeMinimum, batchTimingMaximum, includeSubfolders, batchSelectionMethod);

                string statusMessage = isSessionCompleted ? "Session completed, but new photos can be processed." : "Session in progress.";
                string message = $"A saved Picksy session was found in the selected folder.\n\n" +
                                $"Status: {statusMessage}\n\n" +
                                $"Saved Settings:\n" +
                                $"- Batch Size Minimum: {savedSettings.Value.BatchSizeMinimum}\n" +
                                $"- Batch Timing Maximum: {savedSettings.Value.BatchTimingMaximum} seconds\n" +
                                $"- Include Subfolders: {(savedSettings.Value.IncludeSubfolders ? "Yes" : "No")}\n" +
                                $"- Batch Selection Method: {savedSettings.Value.BatchSelectionMethod}\n\n" +
                                $"Would you like to load this session? Click 'Yes' to load, or 'No' to start a new session with current settings.";
                var result = MessageBox.Show(message, "Load Saved Session?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                return result == DialogResult.Yes;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading saved session: {ex.Message}. Starting a new session.", "Picksy Error");
                try
                {
                    File.WriteAllText("picksy_error.log", $"Error reading saved session: {ex}\nFile: {stateFilePath}\nTimestamp: {DateTime.Now}");
                }
                catch
                {
                    Console.WriteLine($"Error reading saved session: {ex}");
                }
                return false;
            }
        }

        // Replace only the LoadSession method in MainForm.cs; keep the rest unchanged
private void LoadSession(string stateFilePath, (int BatchSizeMinimum, int BatchTimingMaximum, bool IncludeSubfolders, string BatchSelectionMethod) savedSettings)
{
    try
    {
        string json = File.ReadAllText(stateFilePath);
        var state = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json) 
            ?? throw new InvalidOperationException("Failed to deserialize saved session.");

        currentFolderPath = state.TryGetValue("CurrentFolderPath", out var folderPath) 
            ? folderPath.GetString() 
            : throw new InvalidOperationException("Missing CurrentFolderPath in saved session.");
        if (string.IsNullOrEmpty(currentFolderPath) || !Directory.Exists(currentFolderPath))
        {
            throw new InvalidOperationException("Saved folder path is invalid or inaccessible.");
        }

        batches = state.TryGetValue("Batches", out var batchesElement)
            ? batchesElement.Deserialize<List<List<string>>>()
            : null;
        currentBatchIndex = state.TryGetValue("CurrentBatchIndex", out var batchIndexElement) && batchIndexElement.TryGetInt32(out int batchIndex)
            ? batchIndex
            : 0;
        currentBatch = state.TryGetValue("CurrentBatch", out var currentBatchElement)
            ? currentBatchElement.Deserialize<List<string>>()
            : null;
        currentPairIndex = state.TryGetValue("CurrentPairIndex", out var pairIndexElement) && pairIndexElement.TryGetInt32(out int pairIndex)
            ? pairIndex
            : 0;
        remainingPhotos = state.TryGetValue("RemainingPhotos", out var remainingPhotosElement)
            ? remainingPhotosElement.Deserialize<List<string>>()
            : null;
        losers = state.TryGetValue("Losers", out var losersElement)
            ? losersElement.Deserialize<List<string>>()
            : null;
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

        bool isSessionCompleted = batches == null || currentBatch == null || remainingPhotos == null || losers == null || currentBatchIndex + 1 >= batches.Count;
        if (isSessionCompleted)
        {
            batches = new List<List<string>>();
            currentBatch = new List<string>();
            remainingPhotos = new List<string>();
            losers = new List<string>();
            currentBatchIndex = 0;
            currentPairIndex = 0;
        }
        else
        {
            string deleteFolder = Path.Combine(currentFolderPath, "_delete");
            remainingPhotos = remainingPhotos != null
                ? remainingPhotos.Where(f => File.Exists(f) && !f.Contains(Path.DirectorySeparatorChar + "_delete" + Path.DirectorySeparatorChar)).ToList()
                : new List<string>();
            losers = losers != null
                ? losers.Where(f => File.Exists(f) && !f.Contains(Path.DirectorySeparatorChar + "_delete" + Path.DirectorySeparatorChar)).ToList()
                : new List<string>();

            if (batches != null && batches.Any(batch => batch.Any(f => !File.Exists(f) || f.Contains(Path.DirectorySeparatorChar + "_delete" + Path.DirectorySeparatorChar))))
            {
                batches = batches.Where(batch => batch.All(f => File.Exists(f) && !f.Contains(Path.DirectorySeparatorChar + "_delete" + Path.DirectorySeparatorChar))).ToList();
                currentBatchIndex = Math.Min(currentBatchIndex, batches.Count - 1);
                if (currentBatchIndex < 0) currentBatchIndex = 0;
                if (batches.Count > 0)
                {
                    currentBatch = new List<string>(batches[currentBatchIndex]);
                    remainingPhotos = new List<string>(currentBatch);
                }
                else
                {
                    currentBatch = new List<string>();
                    remainingPhotos = new List<string>();
                    currentBatchIndex = 0;
                }
            }
            else if (batches == null)
            {
                batches = new List<List<string>>();
                currentBatch = new List<string>();
                remainingPhotos = new List<string>();
                currentBatchIndex = 0;
            }
        }

        var imageExtensions = new[] { ".jpg", ".jpeg", ".png" };
        var searchOption = savedSettings.IncludeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var allPhotos = Directory.GetFiles(currentFolderPath, "*.*", searchOption)
            .Where(f => imageExtensions.Contains(Path.GetExtension(f).ToLower()))
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "_delete" + Path.DirectorySeparatorChar))
            .ToList();
        var processedPhotos = new HashSet<string>(remainingPhotos.Concat(losers));
        var newPhotos = allPhotos.Where(f => !processedPhotos.Contains(f)).ToList();

        if (newPhotos.Count > 0)
        {
            var grouper = new PhotoGrouper(savedSettings.BatchSizeMinimum, savedSettings.BatchTimingMaximum, savedSettings.IncludeSubfolders, savedSettings.BatchSelectionMethod);
            var newBatches = grouper.GroupPhotos(currentFolderPath, newPhotos);
            if (batches == null || batches.Count == 0)
            {
                batches = newBatches;
                currentBatchIndex = 0;
            }
            else
            {
                batches.AddRange(newBatches);
            }
            totalBatchPhotos += newPhotos.Count;
            initialFileCount += newPhotos.Count;
        }

        if (batches != null && batches.Count > 0)
        {
            isLoadingSession = true;
            try
            {
                selectLocalFolderButton.Visible = false;
                selectPhoneFolderButton.Visible = false;
                settingsGroupBox.Visible = false;
                logoPictureBox.Visible = false;
                StartTournament(batches[currentBatchIndex]);
            }
            finally
            {
                isLoadingSession = false;
            }
        }
        else
        {
            MessageBox.Show("No new photos found to process.", "Picksy");
            ResetUI();
        }
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Error loading session: {ex.Message}", "Picksy Error");
        try
        {
            File.WriteAllText("picksy_error.log", $"Error loading session: {ex}\nFile: {stateFilePath}\nTimestamp: {DateTime.Now}");
        }
        catch
        {
            Console.WriteLine($"Error loading session: {ex}");
        }
        ResetUI();
    }
}
        private void StartTournament(List<string> batch)
        {
            int minimumBatchSize = isLoadingSession ? 2 : (int)batchSizeNumericUpDown.Value;
            if (batch == null || batch.Count < minimumBatchSize)
            {
                MessageBox.Show($"Invalid batch. At least {minimumBatchSize} photos required.", "Picksy");
                ResetUI();
                return;
            }

            currentBatch = new List<string>(batch);
            if (remainingPhotos == null || remainingPhotos.Count == 0)
            {
                remainingPhotos = new List<string>(batch);
            }
            if (losers == null)
            {
                losers = new List<string>();
            }
            history.Clear();
            foreach (var photo in batch)
            {
                if (!photoRotations.ContainsKey(photo))
                {
                    photoRotations[photo] = 0;
                }
            }
            if (!isLoadingSession)
            {
                totalBatchPhotos += batch.Count;
            }
            showFullResolution = false;
            selectLocalFolderButton.Visible = false;
            selectPhoneFolderButton.Visible = false;
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
                if (!skipConfirmationCheckBox.Checked)
                {
                    MessageBox.Show("Tournament ended. No photos left.", "Picksy");
                }
                ShowResults();
                return;
            }
            if (remainingPhotos.Count == 1)
            {
                if (!skipConfirmationCheckBox.Checked)
                {
                    MessageBox.Show($"Tournament ended. Winner: {Path.GetFileName(remainingPhotos[0])}", "Picksy");
                }
                ShowResults();
                return;
            }
            if (currentPairIndex + 1 >= remainingPhotos.Count)
            {
                remainingPhotos = Shuffle(remainingPhotos);
                currentPairIndex = 0;
            }

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
                    pictureBoxLeft.Image = CreateThumbnail(leftImage, leftRotation);
                    pictureBoxRight.Image = CreateThumbnail(rightImage, rightRotation);
                }
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
                leftImage?.Dispose();
                rightImage?.Dispose();
            }
            int batchesRemaining = (batches != null && batches.Count > currentBatchIndex) ? batches.Count - currentBatchIndex - 1 : 0;
            remainingLabel.Text = $"Photos remaining: {remainingPhotos.Count} | Batches remaining: {batchesRemaining}";
            UpdatePictureBoxSizes();
        }

        private Image CreateThumbnail(Image image, int rotation)
        {
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
            if (angle == 0) return new Bitmap(image);
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
            int availableHeight = ClientSize.Height - menuStrip.Height - remainingLabel.Height - instructionLabel.Height - rotateClockwiseButton.Height - saveAndQuitButton.Height - 40;
            int availableWidth = (ClientSize.Width - 30) / 2;
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
            int totalHeight = logoPictureBox.Height + 10 + settingsGroupBox.Height + 10 + selectLocalFolderButton.Height + 10 + selectPhoneFolderButton.Height;
            int startY = (ClientSize.Height - totalHeight) / 2;
            int x = (ClientSize.Width - logoPictureBox.Width) / 2;
            logoPictureBox.Location = new Point(x, startY);
            startY += logoPictureBox.Height + 10;
            x = (ClientSize.Width - settingsGroupBox.Width) / 2;
            settingsGroupBox.Location = new Point(x, startY);
            startY += settingsGroupBox.Height + 10;
            x = (ClientSize.Width - selectLocalFolderButton.Width) / 2;
            selectLocalFolderButton.Location = new Point(x, startY);
            startY += selectLocalFolderButton.Height + 10;
            x = (ClientSize.Width - selectPhoneFolderButton.Width) / 2;
            selectPhoneFolderButton.Location = new Point(x, startY);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (remainingPhotos != null && pictureBoxLeft.Visible)
            {
                UpdatePictureBoxSizes();
            }
            else if (selectLocalFolderButton.Visible)
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
            if (currentFolderPath == null)
            {
                MessageBox.Show("No folder selected. Cannot save state.", "Picksy Error");
                return;
            }

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
                    InitialFileCount = initialFileCount,
                    BatchSizeMinimum = (int)batchSizeNumericUpDown.Value,
                    BatchTimingMaximum = (int)batchTimingNumericUpDown.Value,
                    IncludeSubfolders = includeSubfoldersCheckBox.Checked,
                    BatchSelectionMethod = batchSelectionMethodComboBox.SelectedItem?.ToString() ?? "By Name"
                };
                string stateFilePath = Path.Combine(currentFolderPath, "picksy_state.json");
                File.WriteAllText(stateFilePath, JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
                MessageBox.Show($"State saved to {stateFilePath}. Exiting.", "Picksy");
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
            UpdateTournamentUI();
        }

        private void KeepBothPhotos()
        {
            if (remainingPhotos == null) return;
            history.Push((null, true));
            currentPairIndex += 2;
            UpdateTournamentUI();
        }

        private void EndTournament()
        {
            if (remainingPhotos == null) return;
            if (!skipConfirmationCheckBox.Checked)
            {
                MessageBox.Show($"Tournament ended. Keeping {remainingPhotos.Count} remaining photos.", "Picksy");
            }
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

                deletedPhotosCount += currentBatch.Count;
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
                ClearThumbnails();
                string deleteFolder = Path.Combine(currentFolderPath, "_delete");
                Directory.CreateDirectory(deleteFolder);

                foreach (var loser in losers)
                {
                    string fileName = Path.GetFileName(loser);
                    string destPath = Path.Combine(deleteFolder, fileName);
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
                        deleteFolderSizeMB = totalBytes / (1024.0 * 1024.0);
                    }
                }
                string sizeMessage = deleteFolderSizeMB > 1024
                    ? $"{deleteFolderSizeMB / 1024.0:F1} GB"
                    : $"{deleteFolderSizeMB:F2} MB";
                string statsMessage = $"No more batches remain!\n\n" +
                                     $"You just processed {batches?.Count ?? 0} batches, containing {totalBatchPhotos} photos, " +
                                     $"eliminating {deletedPhotosCount} of them and saving {sizeMessage}!";

                using (var form = new Form
                {
                    Text = "Picksy - Session Complete",
                    Size = new Size(400, 300),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false
                })
                {
                    var statsLabel = new Label
                    {
                        Text = statsMessage,
                        Location = new Point(20, 20),
                        Size = new Size(340, 100),
                        TextAlign = ContentAlignment.TopCenter
                    };
                    var thanksLabel = new Label
                    {
                        Text = "Thanks for Using Picksy!",
                        Location = new Point(20, 220),
                        Size = new Size(340, 20),
                        TextAlign = ContentAlignment.MiddleCenter,
                        Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Bold)
                    };
                    var coffeeButton = new Button
                    {
                        Text = "Support Picksy",
                        Size = new Size(100, 30),
                        Location = new Point(50, 170)
                    };
                    coffeeButton.Click += (s, e) =>
                    {
                        try
                        {
                            Process.Start(new ProcessStartInfo("https://buymeacoffee.com/neislerw") { UseShellExecute = true });
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error opening Support Picksy: {ex.Message}", "Picksy Error");
                        }
                    };
                    var shareButton = new Button
                    {
                        Text = "Share Picksy",
                        Size = new Size(100, 30),
                        Location = new Point(150, 170)
                    };
                    shareButton.Click += (s, e) =>
                    {
                        try
                        {
                            string postText = $"I just used Picksy to clean up my photos! Processed {batches?.Count ?? 0} batches, {totalBatchPhotos} photos, eliminated {deletedPhotosCount}, and saved {sizeMessage}! Check it out at www.github.com/neislerw/picksy #Picksy #PhotoCleanup";
                            string encodedText = HttpUtility.UrlEncode(postText);
                            string xUrl = $"https://x.com/intent/tweet?text={encodedText}";
                            Process.Start(new ProcessStartInfo(xUrl) { UseShellExecute = true });
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error sharing to X: {ex.Message}", "Picksy Error");
                        }
                    };
                    var closeButton = new Button
                    {
                        Text = "Close",
                        Size = new Size(100, 30),
                        Location = new Point(250, 170)
                    };
                    closeButton.Click += (s, e) => form.Close();
                    form.Controls.Add(statsLabel);
                    form.Controls.Add(thanksLabel);
                    form.Controls.Add(coffeeButton);
                    form.Controls.Add(shareButton);
                    form.Controls.Add(closeButton);
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
            selectLocalFolderButton.Visible = true;
            selectPhoneFolderButton.Visible = true;
            settingsGroupBox.Visible = true;
            logoPictureBox.Visible = true;
            selectLocalFolderButton.BringToFront();
            selectPhoneFolderButton.BringToFront();
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
            deletePromptLabel.Visible = false;
        }
    }
}