using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MD = MetadataExtractor; // Alias to avoid ambiguity with System.IO.Directory

namespace Picksy
{
    public class PhotoGrouper
    {
        private readonly int _batchSizeMinimum;
        private readonly int _batchTimingMaximum;
        private readonly bool _includeSubfolders;
        private readonly string _batchSelectionMethod;

        public PhotoGrouper(int batchSizeMinimum, int batchTimingMaximum, bool includeSubfolders, string batchSelectionMethod)
        {
            _batchSizeMinimum = batchSizeMinimum;
            _batchTimingMaximum = batchTimingMaximum;
            _includeSubfolders = includeSubfolders;
            _batchSelectionMethod = batchSelectionMethod;
        }

        public List<List<string>> GroupPhotos(string folderPath, List<string>? photos = null)
        {
            try
            {
                var imageExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var searchOption = _includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

                // Use provided photos or scan directory
                var photoFiles = photos != null
                    ? photos.Where(f => imageExtensions.Contains(Path.GetExtension(f).ToLower())).ToList()
                    : System.IO.Directory.GetFiles(folderPath, "*.*", searchOption)
                        .Where(f => imageExtensions.Contains(Path.GetExtension(f).ToLower()))
                        .Where(f => !f.Contains(Path.DirectorySeparatorChar + "_delete" + Path.DirectorySeparatorChar))
                        .ToList();

                var photoTimestamps = new List<(string Path, DateTime Timestamp, bool HasValidDateTaken, string MetadataSource)>();
                var logEntries = new List<string> { $"Processing {photoFiles.Count} photos at {DateTime.Now}" };
                foreach (var photo in photoFiles)
                {
                    try
                    {
                        DateTime timestamp;
                        bool hasValidDateTaken = false;
                        string metadataSource = "None";
                        var directories = MD.ImageMetadataReader.ReadMetadata(photo);
                        // Log all EXIF tags for debugging
                        foreach (var directory in directories)
                        {
                            var tags = directory.Tags.Select(t => $"{t.Name}: {t.Description}").ToList();
                            logEntries.Add($"Photo: {System.IO.Path.GetFileName(photo)}, Directory: {directory.Name}, Tags: {string.Join("; ", tags)}");
                            foreach (var tag in directory.Tags)
                            {
                                if (tag.Name.Contains("DateTimeOriginal") || tag.Name.Contains("Date/Time") || tag.Name.Contains("Date Taken") || tag.Name.Contains("Create Date"))
                                {
                                    if (TryParseExifDate(tag.Description, out var parsedDate))
                                    {
                                        timestamp = parsedDate;
                                        hasValidDateTaken = true;
                                        metadataSource = $"{directory.Name}: {tag.Name} ({tag.Description})";
                                        photoTimestamps.Add((photo, timestamp, hasValidDateTaken, metadataSource));
                                        logEntries.Add($"Photo: {System.IO.Path.GetFileName(photo)}, Date Taken: {timestamp:yyyy-MM-dd HH:mm:ss}, Source: {metadataSource}");
                                        break;
                                    }
                                    else
                                    {
                                        logEntries.Add($"Photo: {System.IO.Path.GetFileName(photo)}, Failed to parse date: {tag.Description}");
                                    }
                                }
                            }
                            if (hasValidDateTaken) break;
                        }
                        if (!hasValidDateTaken)
                        {
                            // Fallback to file creation time
                            timestamp = File.GetCreationTime(photo);
                            metadataSource = "File Creation Time";
                            photoTimestamps.Add((photo, timestamp, hasValidDateTaken, metadataSource));
                            logEntries.Add($"Photo: {System.IO.Path.GetFileName(photo)}, No valid date tag, using {metadataSource}: {timestamp:yyyy-MM-dd HH:mm:ss}");
                        }
                    }
                    catch (Exception ex)
                    {
                        logEntries.Add($"Photo: {System.IO.Path.GetFileName(photo)}, Error reading metadata: {ex.Message}, using File Creation Time");
                        photoTimestamps.Add((photo, File.GetCreationTime(photo), false, "File Creation Time (Error)"));
                    }
                }

                var groupedPhotos = new List<List<string>>();
                if (_batchSelectionMethod == "Auto")
                {
                    // Try filename-based batching first
                    var filenameBatches = GroupByFilenamePrefix(photoFiles, photoTimestamps);
                    if (filenameBatches.Any(b => b.Count >= _batchSizeMinimum))
                    {
                        groupedPhotos = filenameBatches.Where(b => b.Count >= _batchSizeMinimum).ToList();
                        logEntries.Add($"Auto: Using filename-based batching, formed {groupedPhotos.Count} valid batches");
                    }
                    else
                    {
                        // Fallback to metadata-based batching
                        var metadataBatches = GroupByMetadata(photoTimestamps);
                        groupedPhotos = metadataBatches.Where(b => b.Count >= _batchSizeMinimum).ToList();
                        logEntries.Add($"Auto: Filename-based batching failed, using metadata-based batching, formed {groupedPhotos.Count} valid batches");
                    }
                }
                else if (_batchSelectionMethod == "By Name")
                {
                    groupedPhotos = GroupByFilenamePrefix(photoFiles, photoTimestamps);
                }
                else if (_batchSelectionMethod == "By Date Taken")
                {
                    groupedPhotos = GroupByMetadata(photoTimestamps);
                }
                else
                {
                    // By Date Created or By Date Modified
                    var sortedPhotos = _batchSelectionMethod == "By Date Created"
                        ? photoTimestamps.OrderBy(pt => File.GetCreationTime(pt.Path)).ToList()
                        : photoTimestamps.OrderBy(pt => pt.Timestamp).ToList();

                    var currentBatch = new List<string>();
                    DateTime? lastTimestamp = null;

                    foreach (var (Path, Timestamp, _, _) in sortedPhotos)
                    {
                        if (lastTimestamp == null || (Timestamp - lastTimestamp.Value).TotalSeconds <= _batchTimingMaximum)
                        {
                            currentBatch.Add(Path);
                        }
                        else
                        {
                            if (currentBatch.Count >= _batchSizeMinimum)
                            {
                                groupedPhotos.Add(currentBatch);
                                logEntries.Add($"Formed batch with {currentBatch.Count} photos: {string.Join(", ", currentBatch.Select(p => System.IO.Path.GetFileName(p)))}");
                            }
                            else if (currentBatch.Count > 0)
                            {
                                logEntries.Add($"Discarded batch with {currentBatch.Count} photos (below minimum {_batchSizeMinimum}): {string.Join(", ", currentBatch.Select(p => System.IO.Path.GetFileName(p)))}");
                            }
                            currentBatch = new List<string> { Path };
                        }
                        lastTimestamp = Timestamp;
                    }

                    if (currentBatch.Count >= _batchSizeMinimum)
                    {
                        groupedPhotos.Add(currentBatch);
                        logEntries.Add($"Formed batch with {currentBatch.Count} photos: {string.Join(", ", currentBatch.Select(p => System.IO.Path.GetFileName(p)))}");
                    }
                    else if (currentBatch.Count > 0)
                    {
                        logEntries.Add($"Discarded batch with {currentBatch.Count} photos (below minimum {_batchSizeMinimum}): {string.Join(", ", currentBatch.Select(p => System.IO.Path.GetFileName(p)))}");
                    }
                }

                try
                {
                    logEntries.Insert(0, $"Grouped {photoTimestamps.Count} photos into {groupedPhotos.Count} batches at {DateTime.Now}\n" +
                                        $"Settings: BatchSizeMinimum={_batchSizeMinimum}, BatchTimingMaximum={_batchTimingMaximum}, IncludeSubfolders={_includeSubfolders}, Method={_batchSelectionMethod}");
                    File.WriteAllText("picksy_grouper.log", string.Join("\n", logEntries));
                }
                catch
                {
                    Console.WriteLine("Error writing to picksy_grouper.log");
                }

                return groupedPhotos;
            }
            catch (Exception ex)
            {
                try
                {
                    File.WriteAllText("picksy_grouper_error.log", $"Error grouping photos: {ex}\nFolder: {folderPath}\nTimestamp: {DateTime.Now}");
                }
                catch
                {
                    Console.WriteLine($"Error grouping photos: {ex}\nFolder: {folderPath}");
                }
                return new List<List<string>>();
            }
        }

        private List<List<string>> GroupByFilenamePrefix(List<string> photoFiles, List<(string Path, DateTime Timestamp, bool HasValidDateTaken, string MetadataSource)> photoTimestamps)
        {
            var groupedPhotos = new List<List<string>>();
            var prefixGroups = photoTimestamps
                .GroupBy(pt => GetFilenamePrefix(pt.Path))
                .Select(g => g.OrderBy(pt => pt.Timestamp).ToList())
                .ToList();

            foreach (var prefixGroup in prefixGroups)
            {
                var currentBatch = new List<string>();
                DateTime? lastTimestamp = null;

                foreach (var (Path, Timestamp, _, _) in prefixGroup)
                {
                    if (lastTimestamp == null || (Timestamp - lastTimestamp.Value).TotalSeconds <= _batchTimingMaximum)
                    {
                        currentBatch.Add(Path);
                    }
                    else
                    {
                        if (currentBatch.Count >= _batchSizeMinimum)
                        {
                            groupedPhotos.Add(currentBatch);
                        }
                        currentBatch = new List<string> { Path };
                    }
                    lastTimestamp = Timestamp;
                }

                if (currentBatch.Count >= _batchSizeMinimum)
                {
                    groupedPhotos.Add(currentBatch);
                }
            }

            return groupedPhotos;
        }

        private List<List<string>> GroupByMetadata(List<(string Path, DateTime Timestamp, bool HasValidDateTaken, string MetadataSource)> photoTimestamps)
        {
            var sortedPhotos = photoTimestamps
                .Where(pt => pt.HasValidDateTaken)
                .OrderBy(pt => pt.Timestamp)
                .ToList();

            var groupedPhotos = new List<List<string>>();
            var currentBatch = new List<string>();
            DateTime? lastTimestamp = null;

            foreach (var (Path, Timestamp, _, MetadataSource) in sortedPhotos)
            {
                if (lastTimestamp == null || (Timestamp - lastTimestamp.Value).TotalSeconds <= _batchTimingMaximum)
                {
                    currentBatch.Add(Path);
                }
                else
                {
                    if (currentBatch.Count >= _batchSizeMinimum)
                    {
                        groupedPhotos.Add(currentBatch);
                    }
                    currentBatch = new List<string> { Path };
                }
                lastTimestamp = Timestamp;
            }

            if (currentBatch.Count >= _batchSizeMinimum)
            {
                groupedPhotos.Add(currentBatch);
            }

            return groupedPhotos;
        }

        private bool TryParseExifDate(string? dateString, out DateTime date)
        {
            date = default;
            if (string.IsNullOrEmpty(dateString))
                return false;

            // Log raw date string for debugging
            Console.WriteLine($"Parsing date string: {dateString}");

            // Try standard EXIF format
            if (DateTime.TryParseExact(dateString, "yyyy:MM:dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out date))
                return true;

            // Try additional formats
            string[] formats = {
                "yyyy-MM-dd HH:mm:ss",
                "MM/dd/yyyy HH:mm:ss",
                "yyyy:MM:dd HH:mm:ss.fff"
            };
            return DateTime.TryParseExact(dateString, formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out date) ||
                   DateTime.TryParse(dateString, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out date);
        }

        private string GetFilenamePrefix(string path)
        {
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            // Check for date-structured filename (e.g., 20200101_074051)
            if (fileName.Length >= 8 && fileName.Contains("_"))
            {
                int underscoreIndex = fileName.IndexOf('_');
                if (underscoreIndex >= 8 && DateTime.TryParseExact(fileName.Substring(0, 8), "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _))
                {
                    return fileName.Substring(0, 8); // e.g., "20200101"
                }
            }
            return fileName; // Fallback to full name
        }
    }
}