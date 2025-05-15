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
using System.Drawing.Imaging;
using System.Drawing.Text;

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
        private PrivateFontCollection fontCollection;
        private Dictionary<string, int> photoRotations;
        private bool showFullResolution = false;
        private int initialFileCount = 0;
        private int totalBatchPhotos = 0;
        private int deletedPhotosCount = 0;
        private bool isLoadingSession = false;
        private int initialBatchSize = 0;
        private HashSet<string> seenPhotos = new HashSet<string>();
        private HashSet<string> reseenPhotos = new HashSet<string>();

        private Panel? seenProgressContainer;
        private Panel? seenProgressBar;
        private Panel? reseenProgressContainer;
        private Panel? reseenProgressBar;

        private bool isHovering = false;
        private Color baseColor = Color.FromArgb(26, 26, 26);
        private Color hoverColor = Color.FromArgb(60, 60, 60);
        private int transitionSteps = 10;
        private int currentStep = 0;
        private Button? currentButton;

        private System.Windows.Forms.Timer feedbackTimer;
        private Panel leftFeedbackBar;
        private Panel rightFeedbackBar;
        private PictureBox? nonSelectedBox;
        private bool isAnimating;
        

        public MainForm()
        {
            InitializeComponent();
            history = new Stack<(string? Loser, bool KeptBoth)>();
            photoRotations = new Dictionary<string, int>();
            fontCollection = new PrivateFontCollection();

            // Load embedded font
            LoadEmbeddedFont();

            // Apply font to form and controls
            ApplyMontserratFont();

            batchSelectionMethodComboBox.Items.AddRange(new[] { "Auto", "By Name", "By Date Created", "By Date Modified", "By Date Taken" });
            batchSelectionMethodComboBox.SelectedIndex = 0; // Default to "Auto"
            batchTimingNumericUpDown.Value = 300;
            pictureBoxLeft.Visible = false;
            pictureBoxRight.Visible = false;
            saveAndQuitButton.Visible = false;
            thumbnailPanel.Visible = false;
            deletePromptLabel.Visible = false;
            remainingLabel.Visible = false;
            batchProgressLabel.Visible = false;
            selectFolderButton.Visible = true;
            settingsGroupBox.Visible = true;
            logoPictureBox.Visible = true;
            controlsPictureBox.Visible = false;
            selectFolderButton.BringToFront();
            
            try
            {
                using (var iconStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Picksy.Resources.logo.ico"))
                {
                    if (iconStream != null)
                        this.Icon = new Icon(iconStream);
                    else
                        throw new FileNotFoundException("Embedded resource logo.ico not found.");
                }

                using (var imageStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Picksy.Resources.logo.png"))
                {
                    if (imageStream != null)
                        logoPictureBox.Image = Image.FromStream(imageStream);
                    else
                        throw new FileNotFoundException("Embedded resource logo.png not found.");
                }

                using (var controlsStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Picksy.Resources.controls.png"))
                {
                    if (controlsStream != null)
                        controlsPictureBox.Image = Image.FromStream(controlsStream);
                    else
                        throw new FileNotFoundException("Embedded resource controls.png not found.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading icon, logo, or controls: {ex.Message}", "Picksy Error");
            }
            UpdateMainPageControlsPosition();

            InitializeCustomProgressBars();
            hoverTimer.Interval = 15;
            hoverTimer.Tick += HoverTimer_Tick;

            feedbackTimer = new System.Windows.Forms.Timer { Interval = 500 };
            feedbackTimer.Tick += FeedbackTimer_Tick;

            leftFeedbackBar = new Panel
            {
                Visible = false,
                BackColor = Color.Transparent
            };
            rightFeedbackBar = new Panel
            {
                Visible = false,
                BackColor = Color.Transparent
            };
            this.Controls.Add(leftFeedbackBar);
            this.Controls.Add(rightFeedbackBar);

            SetupButtonHover(selectFolderButton);
            SetupButtonHover(saveAndQuitButton);

            Console.WriteLine($"MainForm: Initialized with default batch selection method: {batchSelectionMethodComboBox.SelectedItem?.ToString() ?? "None"}");
        }
        private void LoadEmbeddedFont()
        {
            try
            {
                // Load the Montserrat SemiBold font from embedded resource
                using (var fontStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Picksy.Resources.Montserrat-SemiBold.ttf"))
                {
                    if (fontStream == null)
                    {
                        throw new FileNotFoundException("Embedded font resource 'Montserrat-SemiBold.ttf' not found.");
                    }

                    byte[] fontData = new byte[fontStream.Length];
                    fontStream.Read(fontData, 0, (int)fontStream.Length);
                    IntPtr fontPtr = System.Runtime.InteropServices.Marshal.AllocCoTaskMem(fontData.Length);
                    System.Runtime.InteropServices.Marshal.Copy(fontData, 0, fontPtr, fontData.Length);
                    fontCollection.AddMemoryFont(fontPtr, fontData.Length);
                    System.Runtime.InteropServices.Marshal.FreeCoTaskMem(fontPtr);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading embedded font: {ex.Message}. Falling back to default font.", "Picksy Error");
                try
                {
                    File.AppendAllText("picksy_error.log", $"[{DateTime.Now}] Error loading embedded font: {ex}\n");
                }
                catch
                {
                    Console.WriteLine($"Error loading embedded font: {ex}");
                }
            }
        }

        private void ApplyMontserratFont()
        {
            try
            {
                // Create Montserrat SemiBold font
                Font montserratSemiBold12 = new Font(fontCollection.Families[0], 12F, FontStyle.Regular);
                Font montserratSemiBold16 = new Font(fontCollection.Families[0], 16F, FontStyle.Regular);
                Font montserratSemiBold20 = new Font(fontCollection.Families[0], 20F, FontStyle.Bold);
                Font montserratSemiBold24 = new Font(fontCollection.Families[0], 24F, FontStyle.Bold);

                // Apply to form
                this.Font = montserratSemiBold12;

                // Apply to specific controls
                selectFolderButton.Font = montserratSemiBold12;
                saveAndQuitButton.Font = montserratSemiBold12;
                batchSelectionMethodComboBox.Font = montserratSemiBold12;
                batchSizeNumericUpDown.Font = montserratSemiBold12;
                batchTimingNumericUpDown.Font = montserratSemiBold12;
                includeSubfoldersCheckBox.Font = montserratSemiBold12;
                skipAnimationsCheckBox.Font = montserratSemiBold12;
                skipConfirmationCheckBox.Font = montserratSemiBold12;
                remainingLabel.Font = montserratSemiBold12;
                batchProgressLabel.Font = montserratSemiBold12;
                deletePromptLabel.Font = montserratSemiBold12;
                copyrightLabel.Font = montserratSemiBold12;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying embedded font: {ex.Message}. Using default font.", "Picksy Error");
                try
                {
                    File.AppendAllText("picksy_error.log", $"[{DateTime.Now}] Error applying embedded font: {ex}\n");
                }
                catch
                {
                    Console.WriteLine($"Error applying embedded font: {ex}");
                }
            }
        }
        private void SetupButtonHover(Button button)
        {
            button.BackColor = baseColor;
            button.UseVisualStyleBackColor = false;
            button.MouseEnter += (s, e) =>
            {
                if (currentButton != button)
                {
                    hoverTimer.Stop();
                    currentStep = 0;
                    currentButton = button;
                }
                isHovering = true;
                hoverTimer.Start();
            };
            button.MouseLeave += (s, e) =>
            {
                if (currentButton == button)
                {
                    isHovering = false;
                    currentStep = 0;
                    hoverTimer.Start();
                }
            };
        }

        private void HoverTimer_Tick(object? sender, EventArgs e)
        {
            if (sender is null || currentButton == null)
            {
                hoverTimer.Stop();
                return;
            }

            currentStep++;
            float t = currentStep / (float)transitionSteps;

            Color startColor = isHovering ? baseColor : hoverColor;
            Color endColor = isHovering ? hoverColor : baseColor;

            int r = (int)(startColor.R + (endColor.R - startColor.R) * t);
            int g = (int)(startColor.G + (endColor.G - startColor.G) * t);
            int b = (int)(startColor.B + (endColor.B - startColor.B) * t);

            currentButton.BackColor = Color.FromArgb(r, g, b);

            if (currentStep >= transitionSteps)
            {
                hoverTimer.Stop();
            }
        }

        private void FeedbackTimer_Tick(object? sender, EventArgs e)
        {
            if (sender is null)
                return;

            feedbackTimer.Stop();
            leftFeedbackBar.Visible = false;
            rightFeedbackBar.Visible = false;
            isAnimating = false;
            Console.WriteLine($"FeedbackTimer_Tick: Completed at {DateTime.Now.Ticks / 10000}ms, Calling UpdateTournamentUI");
            UpdateTournamentUI();
        }

        private void InitializeCustomProgressBars()
        {
            seenProgressContainer = new Panel
            {
                BackColor = Color.FromArgb(36, 36, 36),
                Size = new Size(560, 10),
                Location = new Point(220, 730),
                Visible = false
            };
            seenProgressBar = new Panel
            {
                BackColor = Color.FromArgb(150, 150, 150),
                Size = new Size(0, 10),
                Location = new Point(0, 0)
            };
            seenProgressContainer.Controls.Add(seenProgressBar);
            this.Controls.Add(seenProgressContainer);

            reseenProgressContainer = new Panel
            {
                BackColor = Color.FromArgb(150, 150, 150),
                Size = new Size(560, 10),
                Location = new Point(220, 730),
                Visible = false
            };
            reseenProgressBar = new Panel
            {
                BackColor = Color.FromArgb(6, 148, 148),
                Size = new Size(0, 10),
                Location = new Point(0, 0)
            };
            reseenProgressContainer.Controls.Add(reseenProgressBar);
            this.Controls.Add(reseenProgressContainer);
            reseenProgressContainer.BringToFront();
        }

        private int GetExifOrientation(Image? image)
        {
            if (image == null)
                return 1;

            try
            {
                if (image.PropertyIdList.Contains(0x0112))
                {
                    var prop = image.GetPropertyItem(0x0112);
                    if (prop != null && prop.Value.Length >= 2)
                    {
                        return BitConverter.ToInt16(prop.Value, 0);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading EXIF orientation: {ex.Message}");
            }
            return 1;
        }

        private int ExifOrientationToDegrees(int orientation)
        {
            switch (orientation)
            {
                case 1: return 0;
                case 3: return 180;
                case 6: return 90;
                case 8: return 270;
                default: return 0;
            }
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

            Console.WriteLine($"SelectFolderButton_Click: Using batch selection method: {batchSelectionMethod}");

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
                        var batchMethod = Enum.Parse<BatchSelectionMethod>(batchSelectionMethod.Replace(" ", ""));
                        var grouper = new PhotoGrouper(batchSizeMinimum, batchTimingMaximum, includeSubfolders, batchMethod, debugLogging: false);
                        batches = grouper.GroupPhotos(dialog.SelectedPath);
                        currentBatchIndex = 0;
                        totalBatchPhotos = 0;
                        deletedPhotosCount = 0;
                        if (batches?.Count > 0)
                        {
                            StartTournament(batches[0]);
                        }
                        else
                        {
                            MessageBox.Show($"No valid batches found with at least {batchSizeMinimum} photos.", "Picksy");
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
                bool includeSubfolders = state.TryGetValue("IncludeSubfolders", out var subfoldersElement) && subfoldersElement.ValueKind == JsonValueKind.True;
                string batchSelectionMethod = state.TryGetValue("BatchSelectionMethod", out var methodElement) && methodElement.GetString() != null ? methodElement.GetString()! : batchSelectionMethodComboBox.SelectedItem?.ToString() ?? "Auto";

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
                    : new List<List<string>>();
                currentBatchIndex = state.TryGetValue("CurrentBatchIndex", out var batchIndexElement) && batchIndexElement.TryGetInt32(out int batchIndex)
                    ? batchIndex
                    : 0;
                currentBatch = state.TryGetValue("CurrentBatch", out var currentBatchElement)
                    ? currentBatchElement.Deserialize<List<string>>()
                    : new List<string>();
                currentPairIndex = state.TryGetValue("CurrentPairIndex", out var pairIndexElement) && pairIndexElement.TryGetInt32(out int pairIndex)
                    ? pairIndex
                    : 0;
                remainingPhotos = state.TryGetValue("RemainingPhotos", out var remainingPhotosElement)
                    ? remainingPhotosElement.Deserialize<List<string>>()
                    : new List<string>();
                losers = state.TryGetValue("Losers", out var losersElement)
                    ? losersElement.Deserialize<List<string>>()
                    : new List<string>();
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

                string deleteFolder = Path.Combine(currentFolderPath, "_delete");
                var validFilesFilter = new Func<string, bool>(f => File.Exists(f) && !f.Contains(Path.DirectorySeparatorChar + "_delete" + Path.DirectorySeparatorChar));

                int originalRemainingCount = remainingPhotos?.Count ?? 0;
                remainingPhotos = remainingPhotos?.Where(validFilesFilter).ToList() ?? new List<string>();
                losers = losers?.Where(validFilesFilter).ToList() ?? new List<string>();
                deletedPhotosCount += originalRemainingCount - (remainingPhotos?.Count ?? 0);

                int originalCurrentBatchCount = currentBatch?.Count ?? 0;
                currentBatch = currentBatch?.Where(validFilesFilter).ToList() ?? new List<string>();
                deletedPhotosCount += originalCurrentBatchCount - (currentBatch?.Count ?? 0);

                int originalBatchPhotoCount = batches?.Sum(b => b.Count) ?? 0;
                batches = batches?.Skip(currentBatchIndex)
                                 .Select(batch => batch.Where(validFilesFilter).ToList())
                                 .Where(batch => batch.Count >= savedSettings.BatchSizeMinimum || (isLoadingSession && batch.Any(f => currentBatch.Contains(f))))
                                 .ToList() ?? new List<List<string>>();
                int newBatchPhotoCount = batches?.Sum(b => b.Count) ?? 0;
                deletedPhotosCount += originalBatchPhotoCount - newBatchPhotoCount - originalCurrentBatchCount;

                if (batches == null || batches.Count == 0 || currentBatchIndex >= batches.Count)
                {
                    currentBatchIndex = 0;
                }
                else
                {
                    currentBatchIndex = 0;
                }

                var validFiles = new HashSet<string>((remainingPhotos ?? new List<string>()).Concat(losers ?? new List<string>()).Concat(batches?.SelectMany(b => b) ?? new List<string>()));
                photoRotations = photoRotations?.Where(kvp => validFiles.Contains(kvp.Key)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, int>();

                bool isSessionCompleted = batches == null || batches.Count == 0 || currentBatch == null || currentBatch.Count == 0 || remainingPhotos == null || remainingPhotos.Count == 0 || losers == null || losers.Count == 0 || currentBatchIndex >= batches.Count;
                if (isSessionCompleted)
                {
                    batches = new List<List<string>>();
                    currentBatch = new List<string>();
                    remainingPhotos = new List<string>();
                    losers = new List<string>();
                    currentBatchIndex = 0;
                    currentPairIndex = 0;
                }
                else if (currentBatch != null && currentBatch.Count < savedSettings.BatchSizeMinimum && batches != null && batches.Count > 0)
                {
                    currentBatch = new List<string>(batches[0]);
                    remainingPhotos = new List<string>(currentBatch);
                    currentPairIndex = 0;
                }

                var imageExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var searchOption = savedSettings.IncludeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                var allPhotos = Directory.GetFiles(currentFolderPath, "*.*", searchOption)
                    .Where(f => imageExtensions.Contains(Path.GetExtension(f).ToLower()))
                    .Where(f => !f.Contains(Path.DirectorySeparatorChar + "_delete" + Path.DirectorySeparatorChar))
                    .ToList();

                var processedPhotos = new HashSet<string>((remainingPhotos ?? new List<string>()).Concat(losers ?? new List<string>()).Concat(batches?.SelectMany(b => b) ?? new List<string>()));
                var newPhotos = allPhotos.Where(f => !processedPhotos.Contains(f)).ToList();

                try
                {
                    File.AppendAllText("picksy_debug.log", $"\n[{DateTime.Now}] LoadSession Debug:\n");
                    File.AppendAllText("picksy_debug.log", $"Total photos in folder: {allPhotos.Count}\n");
                    File.AppendAllText("picksy_debug.log", $"Processed photos count: {processedPhotos.Count}\n");
                    File.AppendAllText("picksy_debug.log", $"New photos found: {newPhotos.Count}\n");
                    if (newPhotos.Count > 0)
                    {
                        File.AppendAllText("picksy_debug.log", "New photos list:\n");
                        foreach (var photo in newPhotos)
                        {
                            File.AppendAllText("picksy_debug.log", $"- {Path.GetFileName(photo)}\n");
                        }
                    }
                }
                catch
                {
                    Console.WriteLine($"Debug: Total photos in folder: {allPhotos.Count}");
                    Console.WriteLine($"Debug: Processed photos count: {processedPhotos.Count}");
                    Console.WriteLine($"Debug: New photos found: {newPhotos.Count}");
                }

                if (newPhotos.Count > 0)
                {
                    var batchMethod = Enum.Parse<BatchSelectionMethod>(savedSettings.BatchSelectionMethod.Replace(" ", ""));
                    var grouper = new PhotoGrouper(savedSettings.BatchSizeMinimum, savedSettings.BatchTimingMaximum, savedSettings.IncludeSubfolders, batchMethod, debugLogging: true);
                    var newBatches = grouper.GroupPhotos(currentFolderPath, newPhotos);
                    newBatches = newBatches.Where(b => b.Count >= savedSettings.BatchSizeMinimum).ToList();

                    try
                    {
                        File.AppendAllText("picksy_debug.log", $"\nBatch grouping results:\n");
                        File.AppendAllText("picksy_debug.log", $"Initial batches created: {newBatches.Count}\n");
                        File.AppendAllText("picksy_debug.log", $"Valid batches (after minimum size filter): {newBatches.Count}\n");
                        if (newBatches.Count == 0)
                        {
                            File.AppendAllText("picksy_debug.log", "No valid batches created. Possible reasons:\n");
                            File.AppendAllText("picksy_debug.log", $"- Minimum batch size: {savedSettings.BatchSizeMinimum}\n");
                            File.AppendAllText("picksy_debug.log", $"- Maximum timing: {savedSettings.BatchTimingMaximum} seconds\n");
                            File.AppendAllText("picksy_debug.log", $"- Batch selection method: {savedSettings.BatchSelectionMethod}\n");
                            File.AppendAllText("picksy_debug.log", $"- Include subfolders: {savedSettings.IncludeSubfolders}\n");
                        }
                    }
                    catch
                    {
                        Console.WriteLine($"Debug: Initial batches created: {newBatches.Count}");
                        Console.WriteLine($"Debug: Valid batches (after minimum size filter): {newBatches.Count}");
                    }
                    
                    if (newBatches.Any())
                    {
                        if (batches == null || batches.Count == 0)
                        {
                            batches = newBatches;
                            currentBatchIndex = 0;
                            currentBatch = new List<string>(batches[0]);
                            remainingPhotos = new List<string>(currentBatch);
                            currentPairIndex = 0;
                        }
                        else
                        {
                            batches.AddRange(newBatches);
                            if (currentBatch == null || currentBatch.Count < savedSettings.BatchSizeMinimum)
                            {
                                currentBatchIndex = batches.Count - newBatches.Count;
                                currentBatch = new List<string>(batches[currentBatchIndex]);
                                remainingPhotos = new List<string>(currentBatch);
                                currentPairIndex = 0;
                            }
                        }
                        totalBatchPhotos += newBatches.Sum(b => b.Count);
                        initialFileCount += newPhotos.Count;
                    }
                    try
                    {
                        File.AppendAllText("picksy_debug.log", $"[{DateTime.Now}] LoadSession: Found {newPhotos.Count} new photos, grouped into {newBatches.Count} valid batches.\n");
                    }
                    catch
                    {
                        Console.WriteLine($"Debug: Found {newPhotos.Count} new photos, grouped into {newBatches.Count} valid batches.");
                    }
                }

                totalBatchPhotos = (batches?.Sum(b => b.Count) ?? 0) + (currentBatch?.Count ?? 0);

                if (batches != null && batches.Count > 0 && currentBatch != null && currentBatch.Count >= savedSettings.BatchSizeMinimum)
                {
                    isLoadingSession = true;
                    try
                    {
                        selectFolderButton.Visible = false;
                        settingsGroupBox.Visible = false;
                        logoPictureBox.Visible = false;
                        StartTournament(currentBatch);
                    }
                    finally
                    {
                        isLoadingSession = false;
                    }
                }
                else
                {
                    MessageBox.Show("No valid batches found to process.", "Picksy");
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
            initialBatchSize = batch.Count;
            seenPhotos.Clear();
            reseenPhotos.Clear();
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
            selectFolderButton.Visible = false;
            settingsGroupBox.Visible = false;
            logoPictureBox.Visible = false;
            thumbnailPanel.Visible = false;
            deletePromptLabel.Visible = false;
            pictureBoxLeft.Visible = true;
            pictureBoxRight.Visible = true;
            saveAndQuitButton.Visible = true;
            remainingLabel.Visible = true;
            controlsPictureBox.Visible = true;
            batchProgressLabel.Visible = true;
            if (seenProgressContainer != null)
                seenProgressContainer.Visible = true;
            if (reseenProgressContainer != null)
                reseenProgressContainer.Visible = false;
            if (seenProgressBar != null)
                seenProgressBar.Width = 0;
            if (reseenProgressBar != null)
                reseenProgressBar.Width = 0;
            leftFeedbackBar.Visible = false;
            rightFeedbackBar.Visible = false;
            UpdateTournamentUI();
        }

        private void UpdateTournamentUI()
        {
            var startTime = DateTime.Now.Ticks / 10000;
            Console.WriteLine($"UpdateTournamentUI: Started at {startTime}ms");

            if (remainingPhotos == null)
            {
                ShowResults();
                return;
            }
            if (remainingPhotos.Count == 0)
            {
                if (!skipConfirmationCheckBox.Checked)
                {
                    MessageBox.Show("Tournament ended. No photos left.", "Picksy");
                }
                if (seenProgressBar != null && seenProgressContainer != null)
                    seenProgressBar.Width = seenProgressContainer.Width;
                if (reseenProgressContainer != null)
                    reseenProgressContainer.Visible = false;
                batchProgressLabel.Text = "Batch Progress: 100% Seen";
                ShowResults();
                return;
            }
            if (remainingPhotos.Count == 1)
            {
                if (!skipConfirmationCheckBox.Checked)
                {
                    MessageBox.Show($"Tournament ended. Winner: {Path.GetFileName(remainingPhotos[0])}", "Picksy");
                }
                if (seenProgressBar != null && seenProgressContainer != null)
                    seenProgressBar.Width = seenProgressContainer.Width;
                if (reseenProgressContainer != null)
                    reseenProgressContainer.Visible = false;
                batchProgressLabel.Text = "Batch Progress: 100% Seen";
                ShowResults();
                return;
            }
            if (currentPairIndex + 1 >= remainingPhotos.Count)
            {
                remainingPhotos = Shuffle(remainingPhotos);
                currentPairIndex = 0;
            }

            bool allSeen = seenPhotos.Count == initialBatchSize;
            if (remainingPhotos.Count > currentPairIndex)
            {
                seenPhotos.Add(remainingPhotos[currentPairIndex]);
                if (allSeen && reseenProgressContainer != null)
                {
                    reseenProgressContainer.Visible = true;
                    reseenPhotos.Add(remainingPhotos[currentPairIndex]);
                }
            }
            if (remainingPhotos.Count > currentPairIndex + 1)
            {
                seenPhotos.Add(remainingPhotos[currentPairIndex + 1]);
                if (allSeen && reseenProgressContainer != null)
                {
                    reseenProgressContainer.Visible = true;
                    reseenPhotos.Add(remainingPhotos[currentPairIndex + 1]);
                }
            }

            pictureBoxLeft.Image?.Dispose();
            pictureBoxRight.Image?.Dispose();
            pictureBoxLeft.Image = null;
            pictureBoxRight.Image = null;
            Image? leftImage = null;
            Image? rightImage = null;
            try
            {
                var loadStart = DateTime.Now.Ticks / 10000;
                if (remainingPhotos.Count > currentPairIndex)
                    leftImage = Image.FromFile(remainingPhotos[currentPairIndex]);
                if (remainingPhotos.Count > currentPairIndex + 1)
                    rightImage = Image.FromFile(remainingPhotos[currentPairIndex + 1]);
                Console.WriteLine($"UpdateTournamentUI: Image loading took {(DateTime.Now.Ticks / 10000) - loadStart}ms");

                int leftExifRotation = remainingPhotos.Count > currentPairIndex ? GetExifOrientation(leftImage) : 1;
                int rightExifRotation = remainingPhotos.Count > currentPairIndex + 1 ? GetExifOrientation(rightImage) : 1;
                int leftManualRotation = remainingPhotos.Count > currentPairIndex ? photoRotations[remainingPhotos[currentPairIndex]] : 0;
                int rightManualRotation = remainingPhotos.Count > currentPairIndex + 1 ? photoRotations[remainingPhotos[currentPairIndex + 1]] : 0;

                int leftExifDegrees = ExifOrientationToDegrees(leftExifRotation);
                int rightExifDegrees = ExifOrientationToDegrees(rightExifRotation);

                int leftTotalRotation = (leftExifDegrees + leftManualRotation) % 360;
                int rightTotalRotation = (rightExifDegrees + rightManualRotation) % 360;

                if (showFullResolution)
                {
                    if (leftImage != null)
                        pictureBoxLeft.Image = RotateImage(leftImage, leftTotalRotation);
                    if (rightImage != null)
                        pictureBoxRight.Image = RotateImage(rightImage, rightTotalRotation);
                }
                else
                {
                    if (leftImage != null)
                        pictureBoxLeft.Image = CreateThumbnail(leftImage, leftTotalRotation);
                    if (rightImage != null)
                        pictureBoxRight.Image = CreateThumbnail(rightImage, rightTotalRotation);
                }
                if (remainingPhotos.Count > currentPairIndex)
                    toolTipLeft.SetToolTip(pictureBoxLeft, Path.GetFileName(remainingPhotos[currentPairIndex]));
                if (remainingPhotos.Count > currentPairIndex + 1)
                    toolTipRight.SetToolTip(pictureBoxRight, Path.GetFileName(remainingPhotos[currentPairIndex + 1]));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading images: {ex.Message}. Skipping this pair.", "Picksy Error");
                if (remainingPhotos.Count > 1)
                {
                    if (remainingPhotos.Count > currentPairIndex)
                        seenPhotos.Remove(remainingPhotos[currentPairIndex]);
                    if (remainingPhotos.Count > currentPairIndex + 1)
                        seenPhotos.Remove(remainingPhotos[currentPairIndex + 1]);
                    if (allSeen)
                    {
                        if (remainingPhotos.Count > currentPairIndex)
                            reseenPhotos.Remove(remainingPhotos[currentPairIndex]);
                        if (remainingPhotos.Count > currentPairIndex + 1)
                            reseenPhotos.Remove(remainingPhotos[currentPairIndex + 1]);
                    }
                    if (remainingPhotos.Count > currentPairIndex + 1)
                        remainingPhotos.RemoveAt(currentPairIndex + 1);
                    if (remainingPhotos.Count > currentPairIndex)
                        remainingPhotos.RemoveAt(currentPairIndex);
                }
                else if (remainingPhotos.Count == 1)
                {
                    remainingPhotos.Clear();
                }
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
            int batchesRemaining = batches != null && batches.Count > currentBatchIndex ? batches.Count - currentBatchIndex - 1 : 0;
            remainingLabel.Text = $"Photos remaining: {remainingPhotos.Count} | Batches remaining: {batchesRemaining}";

            int seenPercentage = initialBatchSize > 0 ? Math.Min((seenPhotos.Count * 100) / initialBatchSize, 100) : 100;
            int seenWidth = (int)(seenPercentage / 100.0 * (seenProgressContainer?.Width ?? 1));
            if (seenProgressBar != null && seenProgressContainer != null)
                seenProgressBar.Width = Math.Min(seenWidth, seenProgressContainer.Width);

            if (seenPhotos.Count >= initialBatchSize && remainingPhotos.Count > 0)
            {
                if (reseenProgressContainer != null)
                    reseenProgressContainer.Visible = true;
                int reseenPercentage = remainingPhotos.Count > 0 ? (reseenPhotos.Count * 100) / remainingPhotos.Count : 0;
                int reseenWidth = (int)(reseenPercentage / 100.0 * (reseenProgressContainer?.Width ?? 1));
                if (reseenProgressBar != null && reseenProgressContainer != null)
                    reseenProgressBar.Width = Math.Min(reseenWidth, reseenProgressContainer.Width);
                batchProgressLabel.Text = $"{reseenPercentage}% Re-Viewed" + 
                                         (reseenPercentage > 99 ? " (Press Enter to Keep All)" : "");
            }
            else
            {
                if (reseenProgressContainer != null)
                    reseenProgressContainer.Visible = false;
                if (reseenProgressBar != null)
                    reseenProgressBar.Width = 0;
                batchProgressLabel.Text = $"Batch Progress: {seenPercentage}% Viewed";
            }

            UpdatePictureBoxSizes();
            Console.WriteLine($"UpdateTournamentUI: Completed at {(DateTime.Now.Ticks / 10000) - startTime}ms");
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
            int availableHeight = ClientSize.Height - menuStrip.Height - remainingLabel.Height - batchProgressLabel.Height - saveAndQuitButton.Height - copyrightLabel.Height - controlsPictureBox.Height - (seenProgressContainer?.Height ?? 0) - (reseenProgressContainer?.Height ?? 0) - 100;
            int availableWidth = (ClientSize.Width - 40) / 2;

            pictureBoxLeft.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxRight.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxLeft.Size = new Size(availableWidth, availableHeight);
            pictureBoxRight.Size = new Size(availableWidth, availableHeight);
            pictureBoxLeft.Location = new Point(20, menuStrip.Height + 20);
            pictureBoxRight.Location = new Point(ClientSize.Width - availableWidth - 20, menuStrip.Height + 20);

            int barHeight = 10;
            leftFeedbackBar.Size = new Size(availableWidth, barHeight);
            leftFeedbackBar.Location = new Point(20, pictureBoxLeft.Bottom);
            rightFeedbackBar.Size = new Size(availableWidth, barHeight);
            rightFeedbackBar.Location = new Point(ClientSize.Width - availableWidth - 20, pictureBoxRight.Bottom);

            saveAndQuitButton.Location = new Point((ClientSize.Width - saveAndQuitButton.Width) / 2, pictureBoxLeft.Bottom + barHeight + 10);
            controlsPictureBox.Location = new Point((ClientSize.Width - controlsPictureBox.Width) / 2, saveAndQuitButton.Bottom + 15);

            int progressBarX = (ClientSize.Width - (seenProgressContainer?.Width ?? 560)) / 2;
            if (seenProgressContainer != null)
                seenProgressContainer.Location = new Point(progressBarX, controlsPictureBox.Bottom + 25);
            if (reseenProgressContainer != null)
                reseenProgressContainer.Location = new Point(progressBarX, controlsPictureBox.Bottom + 25);
            batchProgressLabel.Location = new Point(progressBarX, controlsPictureBox.Bottom + 2);
            remainingLabel.Location = new Point((ClientSize.Width - remainingLabel.Width) / 2, (seenProgressContainer?.Bottom ?? controlsPictureBox.Bottom) + 15);

            copyrightLabel.Location = new Point((ClientSize.Width - copyrightLabel.Width) / 2, remainingLabel.Bottom + 15);
            thumbnailPanel.Location = new Point(20, menuStrip.Height + 20);
            deletePromptLabel.Location = new Point(20, thumbnailPanel.Bottom + 20);
        }

        private void UpdateMainPageControlsPosition()
        {
            int totalHeight = logoPictureBox.Height + 30 + settingsGroupBox.Height + 30 + selectFolderButton.Height + copyrightLabel.Height + 30;
            int startY = (ClientSize.Height - totalHeight) / 2;

            int x = (ClientSize.Width - logoPictureBox.Width) / 2;
            logoPictureBox.Location = new Point(x, startY);

            startY += logoPictureBox.Height + 30;
            x = (ClientSize.Width - settingsGroupBox.Width) / 2;
            settingsGroupBox.Location = new Point(x, startY);

            startY += settingsGroupBox.Height + 30;
            x = (ClientSize.Width - selectFolderButton.Width) / 2;
            selectFolderButton.Location = new Point(x, startY);

            copyrightLabel.Location = new Point((ClientSize.Width - copyrightLabel.Width) / 2, ClientSize.Height - copyrightLabel.Height - 20);
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
            Console.WriteLine($"PictureBoxLeft_Click: Triggered at {DateTime.Now.Ticks / 10000}ms");
            SelectPhoto(true);
        }

        private void PictureBoxRight_Click(object sender, EventArgs e)
        {
            Console.WriteLine($"PictureBoxRight_Click: Triggered at {DateTime.Now.Ticks / 10000}ms");
            SelectPhoto(false);
        }

        private void SaveAndQuitButton_Click(object sender, EventArgs e)
        {
            SaveStateToFile(true);
            Application.Exit();
        }

        private void SaveStateToFile(bool showConfirmation)
        {
            if (currentFolderPath == null)
            {
                if (showConfirmation)
                {
                    MessageBox.Show("No folder selected. Cannot save state.", "Picksy Error");
                }
                return;
            }

            try
            {
                string deleteFolder = Path.Combine(currentFolderPath, "_delete");
                var validFilesFilter = new Func<string, bool>(f => File.Exists(f) && !f.Contains(Path.DirectorySeparatorChar + "_delete" + Path.DirectorySeparatorChar));

                var filteredBatches = batches?.Select(batch => batch.Where(validFilesFilter).ToList())
                                             .Where(batch => batch.Any())
                                             .ToList() ?? new List<List<string>>();
                var filteredCurrentBatch = currentBatch?.Where(validFilesFilter).ToList() ?? new List<string>();
                var filteredRemainingPhotos = remainingPhotos?.Where(validFilesFilter).ToList() ?? new List<string>();
                var filteredLosers = losers?.Where(validFilesFilter).ToList() ?? new List<string>();

                var validFiles = new HashSet<string>(filteredRemainingPhotos.Concat(filteredLosers).Concat(filteredBatches.SelectMany(b => b)));
                var filteredPhotoRotations = photoRotations?.Where(kvp => validFiles.Contains(kvp.Key)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, int>();

                var state = new
                {
                    CurrentFolderPath = currentFolderPath,
                    Batches = filteredBatches,
                    CurrentBatchIndex = currentBatchIndex,
                    CurrentBatch = filteredCurrentBatch,
                    CurrentPairIndex = currentPairIndex,
                    RemainingPhotos = filteredRemainingPhotos,
                    Losers = filteredLosers,
                    PhotoRotations = filteredPhotoRotations,
                    TotalBatchPhotos = totalBatchPhotos,
                    DeletedPhotosCount = deletedPhotosCount,
                    InitialFileCount = initialFileCount,
                    BatchSizeMinimum = (int)batchSizeNumericUpDown.Value,
                    BatchTimingMaximum = (int)batchTimingNumericUpDown.Value,
                    IncludeSubfolders = includeSubfoldersCheckBox.Checked,
                    BatchSelectionMethod = batchSelectionMethodComboBox.SelectedItem?.ToString() ?? "Auto"
                };
                string stateFilePath = Path.Combine(currentFolderPath, "picksy_state.json");
                File.WriteAllText(stateFilePath, JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
                if (showConfirmation)
                {
                    MessageBox.Show($"State saved to {stateFilePath}.", "Picksy");
                }
            }
            catch (Exception ex)
            {
                if (showConfirmation)
                {
                    MessageBox.Show($"Error saving state: {ex.Message}", "Picksy Error");
                }
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (currentBatch != null && pictureBoxLeft.Visible)
            {
                if (keyData == Keys.Left)
                {
                    Console.WriteLine($"ProcessCmdKey: Left key triggered at {DateTime.Now.Ticks / 10000}ms");
                    SelectPhoto(true);
                    return true;
                }
                else if (keyData == Keys.Right)
                {
                    Console.WriteLine($"ProcessCmdKey: Right key triggered at {DateTime.Now.Ticks / 10000}ms");
                    SelectPhoto(false);
                    return true;
                }
                else if (keyData == Keys.Up)
                {
                    Console.WriteLine($"ProcessCmdKey: Up key triggered at {DateTime.Now.Ticks / 10000}ms");
                    KeepBothPhotos();
                    return true;
                }
                else if (keyData == Keys.Down)
                {
                    UndoLastAction();
                    return true;
                }
                else if (keyData == Keys.Enter)
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
                else if (keyData == Keys.Space)
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
            if (remainingPhotos == null || isAnimating)
            {
                Console.WriteLine($"SelectPhoto: Blocked - remainingPhotos={(remainingPhotos == null ? "null" : remainingPhotos.Count.ToString())}, isAnimating={isAnimating}");
                return;
            }

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

            if (!skipAnimationsCheckBox.Checked)
            {
                isAnimating = true;
                nonSelectedBox = leftSelected ? pictureBoxRight : pictureBoxLeft;
                if (nonSelectedBox != null)
                {
                    nonSelectedBox.Image?.Dispose();
                    nonSelectedBox.Image = null;
                }

                if (leftSelected)
                {
                    leftFeedbackBar.BackColor = Color.Green;
                    leftFeedbackBar.Visible = true;
                }
                else
                {
                    rightFeedbackBar.BackColor = Color.Green;
                    rightFeedbackBar.Visible = true;
                }

                Console.WriteLine($"SelectPhoto: Showing green bar at {DateTime.Now.Ticks / 10000}ms, leftSelected={leftSelected}, leftImage={(pictureBoxLeft.Image == null ? "null" : "present")}, rightImage={(pictureBoxRight.Image == null ? "null" : "present")}");
                feedbackTimer.Start();
            }
            else
            {
                UpdateTournamentUI();
            }
        }

        private void KeepBothPhotos()
        {
            if (remainingPhotos == null || currentPairIndex + 1 >= remainingPhotos.Count || isAnimating)
            {
                Console.WriteLine($"KeepBothPhotos: Blocked - remainingPhotos={(remainingPhotos == null ? "null" : remainingPhotos.Count.ToString())}, isAnimating={isAnimating}");
                return;
            }

            bool allSeen = seenPhotos.Count == initialBatchSize;
            seenPhotos.Add(remainingPhotos[currentPairIndex]);
            seenPhotos.Add(remainingPhotos[currentPairIndex + 1]);
            if (allSeen)
            {
                reseenPhotos.Add(remainingPhotos[currentPairIndex]);
                reseenPhotos.Add(remainingPhotos[currentPairIndex + 1]);
            }
            history.Push((null, true));
            currentPairIndex += 2;

            if (!skipAnimationsCheckBox.Checked)
            {
                isAnimating = true;
                leftFeedbackBar.BackColor = Color.Green;
                leftFeedbackBar.Visible = true;
                rightFeedbackBar.BackColor = Color.Green;
                rightFeedbackBar.Visible = true;
                nonSelectedBox = null;

                Console.WriteLine($"KeepBothPhotos: Showing green bars at {DateTime.Now.Ticks / 10000}ms, leftImage={(pictureBoxLeft.Image == null ? "null" : "present")}, rightImage={(pictureBoxRight.Image == null ? "null" : "present")}");
                feedbackTimer.Start();
            }
            else
            {
                UpdateTournamentUI();
            }
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
                if (seenPhotos.Count >= initialBatchSize && remainingPhotos.Count >= 2)
                {
                    reseenPhotos.Remove(remainingPhotos[currentPairIndex]);
                    if (reseenProgressContainer != null)
                        reseenProgressContainer.Visible = false;
                    reseenPhotos.Remove(remainingPhotos[currentPairIndex + 1]);
                }
                seenPhotos.Remove(remainingPhotos[currentPairIndex]);
                seenPhotos.Remove(remainingPhotos[currentPairIndex + 1]);
            }
            else if (lastAction.Loser != null)
            {
                losers?.Remove(lastAction.Loser);
                remainingPhotos.Insert(currentPairIndex + 1, lastAction.Loser);
                if (seenPhotos.Contains(remainingPhotos[currentPairIndex]))
                {
                    seenPhotos.Remove(remainingPhotos[currentPairIndex]);
                }
                if (seenPhotos.Contains(lastAction.Loser))
                {
                    seenPhotos.Remove(lastAction.Loser);
                }
                if (reseenPhotos.Contains(remainingPhotos[currentPairIndex]))
                {
                    if (reseenProgressContainer != null)
                        reseenProgressContainer.Visible = false;
                    reseenPhotos.Remove(remainingPhotos[currentPairIndex]);
                }
                if (reseenPhotos.Contains(lastAction.Loser))
                {
                    if (reseenProgressContainer != null)
                        reseenProgressContainer.Visible = false;
                    reseenPhotos.Remove(lastAction.Loser);
                }
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
            saveAndQuitButton.Visible = false;
            remainingLabel.Visible = false;
            batchProgressLabel.Visible = false;
            if (seenProgressContainer != null)
                seenProgressContainer.Visible = false;
            if (reseenProgressContainer != null)
                reseenProgressContainer.Visible = false;
            leftFeedbackBar.Visible = false;
            rightFeedbackBar.Visible = false;
            controlsPictureBox.Visible = true;

            ClearThumbnails();
            foreach (var loser in losers)
            {
                try
                {
                    var pictureBox = new PictureBox
                    {
                        Size = new Size(120, 120),
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
                // Save state before showing completion screen
                SaveStateToFile(false);

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
                string titleMessage = "No more batches remain!";
                string statsMessage = $"You just processed {batches?.Count ?? 0} batches, containing {totalBatchPhotos} photos, " +
                                     $"eliminating {deletedPhotosCount} of them and saving {sizeMessage}!";

                using (var form = new Form
                {
                    Text = "Picksy - Session Complete",
                    Size = new Size(700, 550),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    BackColor = Color.FromArgb(26, 26, 26),
                    ForeColor = Color.White,
                    Font = new Font("Montserrat SemiBold", 12F, FontStyle.Regular)
                })
                {
                    var titleLabel = new Label
                    {
                        Text = titleMessage,
                        Location = new Point(20, 60),
                        Size = new Size(660, 60),
                        TextAlign = ContentAlignment.TopCenter,
                        BackColor = Color.Transparent,
                        ForeColor = Color.White,
                        Font = new Font("Montserrat SemiBold", 22F, FontStyle.Bold)
                    };

                    var statsLabel = new Label
                    {
                        Text = statsMessage,
                        Location = new Point(20, 160),
                        Size = new Size(660, 100),
                        TextAlign = ContentAlignment.TopCenter,
                        BackColor = Color.Transparent,
                        ForeColor = Color.White,
                        Font = new Font("Montserrat SemiBold", 16F, FontStyle.Regular)
                    };

                    var thanksLabel = new Label
                    {
                        Text = "Thanks for Using Picksy!",
                        Location = new Point(20, 260),
                        Size = new Size(660, 50),
                        TextAlign = ContentAlignment.MiddleCenter,
                        BackColor = Color.Transparent,
                        ForeColor = Color.White,
                        Font = new Font("Montserrat SemiBold", 24F, FontStyle.Bold)
                    };

                    bool saveStateExists = currentFolderPath != null && File.Exists(Path.Combine(currentFolderPath, "picksy_state.json"));
                    CheckBox? createSaveStateCheckBox = null;
                    if (!saveStateExists)
                    {
                        createSaveStateCheckBox = new CheckBox
                        {
                            Text = "Create savestate for future use",
                            Location = new Point((form.Width - 300) / 2, 430),
                            Size = new Size(300, 30),
                            TextAlign = ContentAlignment.MiddleLeft,
                            BackColor = Color.Transparent,
                            ForeColor = Color.White,
                            Font = new Font("Montserrat SemiBold", 12F, FontStyle.Regular),
                            Checked = true
                        };
                        form.Controls.Add(createSaveStateCheckBox);
                    }

                    var coffeeButton = new Button
                    {
                        Text = "Support Picksy",
                        Size = new Size(160, 60),
                        Location = new Point(90, 350),
                        BackColor = baseColor,
                        ForeColor = Color.White,
                        Font = new Font("Montserrat SemiBold", 12F, FontStyle.Regular),
                        UseVisualStyleBackColor = false
                    };
                    SetupButtonHover(coffeeButton);
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
                        Size = new Size(160, 60),
                        Location = new Point(270, 350),
                        BackColor = baseColor,
                        ForeColor = Color.White,
                        Font = new Font("Montserrat SemiBold", 12F, FontStyle.Regular),
                        UseVisualStyleBackColor = false
                    };
                    SetupButtonHover(shareButton);
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
                        Size = new Size(160, 60),
                        Location = new Point(450, 350),
                        BackColor = baseColor,
                        ForeColor = Color.White,
                        Font = new Font("Montserrat SemiBold", 12F, FontStyle.Regular),
                        UseVisualStyleBackColor = false
                    };
                    SetupButtonHover(closeButton);
                    closeButton.Click += (s, e) =>
                    {
                        if (saveStateExists || (createSaveStateCheckBox != null && createSaveStateCheckBox.Checked))
                        {
                            SaveStateToFile(false);
                        }
                        if (saveStateExists)
                        {
                            SaveStateToFile(true);
                            ResetUI();
                        }
                        form.Close();
                    };

                    form.Controls.Add(titleLabel);
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
            selectFolderButton.Visible = true;
            settingsGroupBox.Visible = true;
            logoPictureBox.Visible = true;
            controlsPictureBox.Visible = false;
            selectFolderButton.BringToFront();
            saveAndQuitButton.Visible = false;
            thumbnailPanel.Visible = false;
            deletePromptLabel.Visible = false;
            remainingLabel.Visible = false;
            batchProgressLabel.Visible = false;
            if (seenProgressContainer != null)
                seenProgressContainer.Visible = false;
            if (reseenProgressContainer != null)
                reseenProgressContainer.Visible = false;
            leftFeedbackBar.Visible = false;
            rightFeedbackBar.Visible = false;
            currentBatch = null;
            remainingPhotos = null;
            losers = null;
            history.Clear();
            seenPhotos.Clear();
            reseenPhotos.Clear();
            initialBatchSize = 0;
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