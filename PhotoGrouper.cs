using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MD = MetadataExtractor;

namespace Picksy
{
    public enum BatchSelectionMethod
    {
        Auto,
        ByName,
        ByDateTaken,
        ByDateCreated,
        ByDateModified
    }

    public class PhotoGrouper
    {
        private readonly int _batchSizeMinimum;
        private readonly int _batchTimingMaximum;
        private readonly bool _includeSubfolders;
        private readonly BatchSelectionMethod _batchSelectionMethod;
        private readonly bool _debugLogging;

        public PhotoGrouper(int batchSizeMinimum, int batchTimingMaximum, bool includeSubfolders, BatchSelectionMethod batchSelectionMethod, bool debugLogging = false)
        {
            if (batchSizeMinimum < 1) throw new ArgumentException("Batch size minimum must be at least 1.", nameof(batchSizeMinimum));
            if (batchTimingMaximum < 1) throw new ArgumentException("Batch timing maximum must be at least 1 second.", nameof(batchTimingMaximum));
            _batchSizeMinimum = batchSizeMinimum;
            _batchTimingMaximum = batchTimingMaximum;
            _includeSubfolders = includeSubfolders;
            _batchSelectionMethod = batchSelectionMethod;
            _debugLogging = debugLogging;
        }

        public List<List<string>> GroupPhotos(string folderPath, List<string>? photos = null)
        {
            try
            {
                var imageExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var searchOption = _includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

                var photoFiles = photos != null
                    ? photos.Where(f => imageExtensions.Contains(Path.GetExtension(f).ToLower())).ToList()
                    : Directory.GetFiles(folderPath, "*.*", searchOption)
                        .Where(f => imageExtensions.Contains(Path.GetExtension(f).ToLower()))
                        .Where(f => !f.Contains(Path.DirectorySeparatorChar + "_delete" + Path.DirectorySeparatorChar))
                        .Where(f => !Path.GetFileName(f).StartsWith("._"))
                        .ToList();

                var photoTimestamps = new List<(string Path, DateTime Timestamp, bool HasValidDateTaken, string MetadataSource, DateTime CreationTime, DateTime ModifiedTime)>();
                var logEntries = new List<string> { $"Processing {photoFiles.Count} photos at {DateTime.Now}" };

                foreach (var photo in photoFiles)
                {
                    try
                    {
                        DateTime timestamp;
                        bool hasValidDateTaken = false;
                        string metadataSource = "None";
                        var directories = MD.ImageMetadataReader.ReadMetadata(photo);
                        if (_debugLogging)
                        {
                            foreach (var directory in directories)
                            {
                                var tags = directory.Tags.Select(t => $"{t.Name}: {t.Description}").ToList();
                                logEntries.Add($"Photo: {Path.GetFileName(photo)}, Directory: {directory.Name}, Tags: {string.Join("; ", tags)}");
                            }
                        }
                        foreach (var directory in directories)
                        {
                            foreach (var tag in directory.Tags)
                            {
                                if (tag.Name.Contains("DateTimeOriginal") || tag.Name.Contains("Date/Time") || tag.Name.Contains("Date Taken") || tag.Name.Contains("Create Date"))
                                {
                                    if (TryParseExifDate(tag.Description, out var parsedDate))
                                    {
                                        timestamp = parsedDate;
                                        hasValidDateTaken = true;
                                        metadataSource = $"{directory.Name}: {tag.Name} ({tag.Description})";
                                        photoTimestamps.Add((photo, timestamp, hasValidDateTaken, metadataSource, File.GetCreationTime(photo), File.GetLastWriteTime(photo)));
                                        logEntries.Add($"Photo: {Path.GetFileName(photo)}, Date Taken: {timestamp:yyyy-MM-dd HH:mm:ss}, Source: {metadataSource}");
                                        break;
                                    }
                                    else
                                    {
                                        logEntries.Add($"Photo: {Path.GetFileName(photo)}, Failed to parse date: {tag.Description}");
                                    }
                                }
                            }
                            if (hasValidDateTaken) break;
                        }
                        if (!hasValidDateTaken)
                        {
                            timestamp = File.GetCreationTime(photo);
                            metadataSource = "File Creation Time";
                            photoTimestamps.Add((photo, timestamp, hasValidDateTaken, metadataSource, timestamp, File.GetLastWriteTime(photo)));
                            logEntries.Add($"Photo: {Path.GetFileName(photo)}, No valid date tag, using {metadataSource}: {timestamp:yyyy-MM-dd HH:mm:ss}");
                        }
                    }
                    catch (Exception ex)
                    {
                        var timestamp = File.GetCreationTime(photo);
                        photoTimestamps.Add((photo, timestamp, false, "File Creation Time (Error)", timestamp, File.GetLastWriteTime(photo)));
                        logEntries.Add($"Photo: {Path.GetFileName(photo)}, Error reading metadata: {ex.Message}, using File Creation Time: {timestamp:yyyy-MM-dd HH:mm:ss}");
                    }
                }

                var groupedPhotos = new List<List<string>>();
                var discardedCount = 0;
                int batchNumber = 0;

                if (_batchSelectionMethod == BatchSelectionMethod.Auto)
                {
                    var filenameBatches = GroupByFilenamePrefix(photoFiles, photoTimestamps, ref logEntries, ref discardedCount, ref batchNumber, "FilenamePrefix");
                    var metadataBatches = GroupByMetadata(photoTimestamps, ref logEntries, ref discardedCount, ref batchNumber, "Metadata");
                    groupedPhotos = filenameBatches.Concat(metadataBatches)
                        .GroupBy(b => b.OrderBy(p => p).Aggregate((a, b) => a + b))
                        .Select(g => g.First())
                        .Where(b => b.Count >= _batchSizeMinimum)
                        .ToList();
                    logEntries.Add($"Auto: Combined {filenameBatches.Count} filename-based and {metadataBatches.Count} metadata-based batches, formed {groupedPhotos.Count} valid batches");
                }
                else if (_batchSelectionMethod == BatchSelectionMethod.ByName)
                {
                    groupedPhotos = GroupByFilenamePrefix(photoFiles, photoTimestamps, ref logEntries, ref discardedCount, ref batchNumber, "FilenamePrefix");
                }
                else if (_batchSelectionMethod == BatchSelectionMethod.ByDateTaken)
                {
                    groupedPhotos = GroupByMetadata(photoTimestamps, ref logEntries, ref discardedCount, ref batchNumber, "Metadata");
                }
                else
                {
                    var sortedPhotos = _batchSelectionMethod == BatchSelectionMethod.ByDateCreated
                        ? photoTimestamps.OrderBy(pt => pt.CreationTime).ToList()
                        : photoTimestamps.OrderBy(pt => pt.ModifiedTime).ToList();
                    groupedPhotos = GroupByTimestamp(sortedPhotos, pt => pt.Item2, ref logEntries, ref discardedCount, ref batchNumber, "Timestamp");
                }

                try
                {
                    logEntries.Insert(0, $"Grouped {photoTimestamps.Count} photos into {groupedPhotos.Count} batches at {DateTime.Now}\n" +
                                        $"Settings: BatchSizeMinimum={_batchSizeMinimum}, BatchTimingMaximum={_batchTimingMaximum}, IncludeSubfolders={_includeSubfolders}, Method={_batchSelectionMethod}\n" +
                                        $"Summary: {photoTimestamps.Count} photos processed, {groupedPhotos.Count} batches formed, {discardedCount} photos discarded");
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

        private List<List<string>> GroupByFilenamePrefix(List<string> photoFiles, List<(string Path, DateTime Timestamp, bool HasValidDateTaken, string MetadataSource, DateTime CreationTime, DateTime ModifiedTime)> photoTimestamps, ref List<string> logEntries, ref int discardedCount, ref int batchNumber, string method)
        {
            var groupedPhotos = new List<List<string>>();
            var prefixGroups = photoTimestamps
                .GroupBy(pt => GetFilenamePrefix(pt.Path))
                .Select(g => g.OrderBy(pt => pt.Item2).ToList())
                .ToList();

            foreach (var prefixGroup in prefixGroups)
            {
                var batches = GroupByTimestamp(prefixGroup, pt => pt.Item2, ref logEntries, ref discardedCount, ref batchNumber, method);
                groupedPhotos.AddRange(batches);
            }

            return groupedPhotos;
        }

        private List<List<string>> GroupByMetadata(List<(string Path, DateTime Timestamp, bool HasValidDateTaken, string MetadataSource, DateTime CreationTime, DateTime ModifiedTime)> photoTimestamps, ref List<string> logEntries, ref int discardedCount, ref int batchNumber, string method)
        {
            var sortedPhotos = photoTimestamps
                .Where(pt => pt.HasValidDateTaken)
                .OrderBy(pt => pt.Item2)
                .ToList();
            return GroupByTimestamp(sortedPhotos, pt => pt.Item2, ref logEntries, ref discardedCount, ref batchNumber, method);
        }

        private List<List<string>> GroupByTimestamp(List<(string Path, DateTime Timestamp, bool HasValidDateTaken, string MetadataSource, DateTime CreationTime, DateTime ModifiedTime)> photos, Func<(string, DateTime, bool, string, DateTime, DateTime), DateTime> timestampSelector, ref List<string> logEntries, ref int discardedCount, ref int batchNumber, string method)
        {
            var groupedPhotos = new List<List<string>>();
            var currentBatch = new List<string>();
            DateTime? lastTimestamp = null;

            foreach (var photo in photos)
            {
                var timestamp = timestampSelector(photo);
                if (lastTimestamp == null || (timestamp - lastTimestamp.Value).TotalSeconds <= _batchTimingMaximum)
                {
                    currentBatch.Add(photo.Path);
                }
                else
                {
                    if (currentBatch.Count >= _batchSizeMinimum)
                    {
                        batchNumber++;
                        groupedPhotos.Add(currentBatch);
                        logEntries.Add($"Batch {batchNumber} formed with {currentBatch.Count} photos using {method}: {string.Join(", ", currentBatch.Select(p => Path.GetFileName(p)))}");
                    }
                    else if (currentBatch.Count > 0)
                    {
                        batchNumber++;
                        discardedCount += currentBatch.Count;
                        logEntries.Add($"Batch {batchNumber} discarded with {currentBatch.Count} photos using {method} (below minimum {_batchSizeMinimum}): {string.Join(", ", currentBatch.Select(p => Path.GetFileName(p)))}");
                    }
                    currentBatch = new List<string> { photo.Path };
                }
                lastTimestamp = timestamp;
            }

            if (currentBatch.Count >= _batchSizeMinimum)
            {
                batchNumber++;
                groupedPhotos.Add(currentBatch);
                logEntries.Add($"Batch {batchNumber} formed with {currentBatch.Count} photos using {method}: {string.Join(", ", currentBatch.Select(p => Path.GetFileName(p)))}");
            }
            else if (currentBatch.Count > 0)
            {
                batchNumber++;
                discardedCount += currentBatch.Count;
                logEntries.Add($"Batch {batchNumber} discarded with {currentBatch.Count} photos using {method} (below minimum {_batchSizeMinimum}): {string.Join(", ", currentBatch.Select(p => Path.GetFileName(p)))}");
            }

            return groupedPhotos;
        }

        private bool TryParseExifDate(string? dateString, out DateTime date)
        {
            date = default;
            if (string.IsNullOrEmpty(dateString))
                return false;

            if (_debugLogging)
                Console.WriteLine($"Parsing date string: {dateString}");

            string[] formats = {
                "yyyy:MM:dd HH:mm:ss",
                "yyyy-MM-dd HH:mm:ss",
                "MM/dd/yyyy HH:mm:ss",
                "yyyy:MM:dd HH:mm:ss.fff",
                "yyyy-MM-dd'T'HH:mm:ss",
                "yyyy-MM-dd'T'HH:mm:ssZ",
                "yyyy-MM-dd'T'HH:mm:sszzz"
            };
            return DateTime.TryParseExact(dateString, formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out date) ||
                   DateTime.TryParse(dateString, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out date);
        }

        private string GetFilenamePrefix(string path)
        {
            string fileName = Path.GetFileNameWithoutExtension(path);
            string[] datePatterns = {
                @"^(?<date>\d{8})_\d{6}",
                @"^IMG_(?<date>\d{8})_\d{6}",
                @"^(?<date>\d{4}-\d{2}-\d{2})_\d{6}"
            };
            foreach (var pattern in datePatterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(fileName, pattern);
                if (match.Success)
                {
                    string datePart = match.Groups["date"].Value;
                    if (DateTime.TryParseExact(datePart.Replace("-", ""), "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _))
                        return datePart;
                }
            }
            return fileName;
        }
    }
}