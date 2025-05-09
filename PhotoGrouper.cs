using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MetadataExtractor; // For EXIF data
using MetadataExtractor.Formats.Exif;

namespace Picksy
{
    public class PhotoGrouper
    {
        private readonly int batchSizeMinimum;
        private readonly int batchTimingMaximum;
        private readonly bool includeSubfolders;
        private readonly string batchSelectionMethod;

        public PhotoGrouper(int batchSizeMinimum, int batchTimingMaximum, bool includeSubfolders, string batchSelectionMethod)
        {
            this.batchSizeMinimum = batchSizeMinimum;
            this.batchTimingMaximum = batchTimingMaximum;
            this.includeSubfolders = includeSubfolders;
            this.batchSelectionMethod = batchSelectionMethod;
        }

        public List<List<string>> GroupPhotos(string folderPath)
        {
            var batches = new List<List<string>>();
            var photoFiles = GetPhotoFiles(folderPath);
            if (photoFiles.Count < batchSizeMinimum)
            {
                return batches;
            }

            try
            {
                switch (batchSelectionMethod)
                {
                    case "By Name":
                        batches = GroupByName(photoFiles);
                        break;
                    case "By Date Created":
                        batches = GroupByDateCreated(photoFiles);
                        break;
                    case "By Date Modified":
                        batches = GroupByDateModified(photoFiles);
                        break;
                    default:
                        throw new ArgumentException($"Unknown batch selection method: {batchSelectionMethod}");
                }

                // Log batch details for debugging
                LogBatches(batches);
            }
            catch (Exception ex)
            {
                File.WriteAllText("picksy_grouper.log", $"Error grouping photos: {ex}\nFolder: {folderPath}\nTimestamp: {DateTime.Now}");
                return batches;
            }

            return batches.Where(b => b.Count >= batchSizeMinimum).ToList();
        }

        private List<string> GetPhotoFiles(string folderPath)
        {
            var imageExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var searchOption = includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            return System.IO.Directory.GetFiles(folderPath, "*.*", searchOption)
                .Where(f => imageExtensions.Contains(Path.GetExtension(f).ToLower()))
                .Where(f => !f.Contains(Path.DirectorySeparatorChar + "_delete" + Path.DirectorySeparatorChar))
                .ToList();
        }

        private List<List<string>> GroupByName(List<string> photoFiles)
        {
            var batches = new List<List<string>>();
            var sortedFiles = photoFiles.OrderBy(f => GetPhotoTimestamp(f)).ToList();
            var currentBatch = new List<string>();
            string? lastPrefix = null;
            DateTime? lastTime = null;

            foreach (var file in sortedFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                // Extract prefix (e.g., "IMG_001" -> "IMG")
                string prefix = GetFilenamePrefix(fileName);
                DateTime currentTime = GetPhotoTimestamp(file);

                if (currentBatch.Count == 0)
                {
                    currentBatch.Add(file);
                    lastPrefix = prefix;
                    lastTime = currentTime;
                    continue;
                }

                // Check if the file belongs to the current batch
                bool samePrefix = prefix == lastPrefix;
                bool withinTime = lastTime.HasValue && (currentTime - lastTime.Value).TotalSeconds <= batchTimingMaximum;

                if (samePrefix && withinTime)
                {
                    currentBatch.Add(file);
                }
                else
                {
                    if (currentBatch.Count >= batchSizeMinimum)
                    {
                        batches.Add(currentBatch);
                    }
                    currentBatch = new List<string> { file };
                    lastPrefix = prefix;
                }
                lastTime = currentTime;
            }

            if (currentBatch.Count >= batchSizeMinimum)
            {
                batches.Add(currentBatch);
            }

            return batches;
        }

        private List<List<string>> GroupByDateCreated(List<string> photoFiles)
        {
            var batches = new List<List<string>>();
            var sortedFiles = photoFiles.OrderBy(f => File.GetCreationTime(f)).ToList();
            var currentBatch = new List<string>();
            DateTime? lastTime = null;

            foreach (var file in sortedFiles)
            {
                DateTime currentTime = File.GetCreationTime(file);

                if (currentBatch.Count == 0)
                {
                    currentBatch.Add(file);
                    lastTime = currentTime;
                    continue;
                }

                if (lastTime.HasValue && (currentTime - lastTime.Value).TotalSeconds <= batchTimingMaximum)
                {
                    currentBatch.Add(file);
                }
                else
                {
                    if (currentBatch.Count >= batchSizeMinimum)
                    {
                        batches.Add(currentBatch);
                    }
                    currentBatch = new List<string> { file };
                }
                lastTime = currentTime;
            }

            if (currentBatch.Count >= batchSizeMinimum)
            {
                batches.Add(currentBatch);
            }

            return batches;
        }

        private List<List<string>> GroupByDateModified(List<string> photoFiles)
        {
            var batches = new List<List<string>>();
            var sortedFiles = photoFiles.OrderBy(f => File.GetLastWriteTime(f)).ToList();
            var currentBatch = new List<string>();
            DateTime? lastTime = null;

            foreach (var file in sortedFiles)
            {
                DateTime currentTime = File.GetLastWriteTime(file);

                if (currentBatch.Count == 0)
                {
                    currentBatch.Add(file);
                    lastTime = currentTime;
                    continue;
                }

                if (lastTime.HasValue && (currentTime - lastTime.Value).TotalSeconds <= batchTimingMaximum)
                {
                    currentBatch.Add(file);
                }
                else
                {
                    if (currentBatch.Count >= batchSizeMinimum)
                    {
                        batches.Add(currentBatch);
                    }
                    currentBatch = new List<string> { file };
                }
                lastTime = currentTime;
            }

            if (currentBatch.Count >= batchSizeMinimum)
            {
                batches.Add(currentBatch);
            }

            return batches;
        }

        private string GetFilenamePrefix(string fileName)
        {
            // Extract prefix up to the last underscore or number
            int lastUnderscore = fileName.LastIndexOf('_');
            if (lastUnderscore > 0)
            {
                return fileName.Substring(0, lastUnderscore);
            }
            // If no underscore, take non-numeric part
            int firstDigit = fileName.IndexOfAny("0123456789".ToCharArray());
            if (firstDigit > 0)
            {
                return fileName.Substring(0, firstDigit);
            }
            return fileName;
        }

        private DateTime GetPhotoTimestamp(string filePath)
        {
            try
            {
                var directories = ImageMetadataReader.ReadMetadata(filePath);
                var subIfdDirectory = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
                if (subIfdDirectory != null)
                {
                    var dateTime = subIfdDirectory.GetDateTime(ExifDirectoryBase.TagDateTimeOriginal);
                    return dateTime; // Non-nullable, exception if invalid
                }
            }
            catch
            {
                // Fallback to file creation time if EXIF fails
            }
            return File.GetCreationTime(filePath);
        }

        private void LogBatches(List<List<string>> batches)
        {
            try
            {
                using (var writer = new StreamWriter("picksy_grouper.log", false))
                {
                    writer.WriteLine($"Batch grouping log - Timestamp: {DateTime.Now}");
                    writer.WriteLine($"Batch Selection Method: {batchSelectionMethod}");
                    writer.WriteLine($"Batch Size Minimum: {batchSizeMinimum}");
                    writer.WriteLine($"Batch Timing Maximum: {batchTimingMaximum} seconds");
                    writer.WriteLine($"Include Subfolders: {includeSubfolders}");
                    writer.WriteLine();

                    for (int i = 0; i < batches.Count; i++)
                    {
                        writer.WriteLine($"Batch {i + 1} ({batches[i].Count} photos):");
                        foreach (var file in batches[i])
                        {
                            var timestamp = GetPhotoTimestamp(file);
                            writer.WriteLine($"  {Path.GetFileName(file)} (Timestamp: {timestamp})");
                        }
                        writer.WriteLine();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing batch log: {ex}");
            }
        }
    }
}