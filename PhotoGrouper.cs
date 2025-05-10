using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MetadataExtractor;

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

                var photoTimestamps = new List<(string Path, DateTime Timestamp)>();
                foreach (var photo in photoFiles)
                {
                    try
                    {
                        var directories = ImageMetadataReader.ReadMetadata(photo);
                        DateTime? timestamp = null;
                        foreach (var directory in directories)
                        {
                            foreach (var tag in directory.Tags)
                            {
                                if (tag.Name.Contains("Date/Time Original") || tag.Name.Contains("Date/Time"))
                                {
                                    if (DateTime.TryParse(tag.Description, out var parsedDate))
                                    {
                                        timestamp = parsedDate;
                                        break;
                                    }
                                }
                            }
                            if (timestamp.HasValue) break;
                        }
                        timestamp ??= System.IO.File.GetLastWriteTime(photo);
                        photoTimestamps.Add((photo, timestamp.Value));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error reading metadata for {photo}: {ex.Message}");
                        photoTimestamps.Add((photo, System.IO.File.GetLastWriteTime(photo)));
                    }
                }

                var groupedPhotos = new List<List<string>>();
                if (_batchSelectionMethod == "By Name")
                {
                    // Group by prefix, then split by timing threshold
                    var prefixGroups = photoTimestamps
                        .GroupBy(pt => GetFilenamePrefix(pt.Path))
                        .Select(g => g.OrderBy(pt => pt.Timestamp).ToList())
                        .ToList();

                    foreach (var prefixGroup in prefixGroups)
                    {
                        var currentBatch = new List<string>();
                        DateTime? lastTimestamp = null;

                        foreach (var (path, timestamp) in prefixGroup)
                        {
                            if (lastTimestamp == null || (timestamp - lastTimestamp.Value).TotalSeconds <= _batchTimingMaximum)
                            {
                                currentBatch.Add(path);
                            }
                            else
                            {
                                if (currentBatch.Count >= _batchSizeMinimum)
                                {
                                    groupedPhotos.Add(currentBatch);
                                }
                                currentBatch = new List<string> { path };
                            }
                            lastTimestamp = timestamp;
                        }

                        if (currentBatch.Count >= _batchSizeMinimum)
                        {
                            groupedPhotos.Add(currentBatch);
                        }
                    }
                }
                else
                {
                    // Existing logic for other methods
                    var sortedPhotos = _batchSelectionMethod == "By Date Created"
                        ? photoTimestamps.OrderBy(pt => System.IO.File.GetCreationTime(pt.Path)).ToList()
                        : photoTimestamps.OrderBy(pt => pt.Timestamp).ToList();

                    var currentBatch = new List<string>();
                    DateTime? lastTimestamp = null;

                    foreach (var (path, timestamp) in sortedPhotos)
                    {
                        if (lastTimestamp == null || (timestamp - lastTimestamp.Value).TotalSeconds <= _batchTimingMaximum)
                        {
                            currentBatch.Add(path);
                        }
                        else
                        {
                            if (currentBatch.Count >= _batchSizeMinimum)
                            {
                                groupedPhotos.Add(currentBatch);
                            }
                            currentBatch = new List<string> { path };
                        }
                        lastTimestamp = timestamp;
                    }

                    if (currentBatch.Count >= _batchSizeMinimum)
                    {
                        groupedPhotos.Add(currentBatch);
                    }
                }

                try
                {
                    File.WriteAllText("picksy_grouper.log", $"Grouped {photoTimestamps.Count} photos into {groupedPhotos.Count} batches at {DateTime.Now}\n" +
                        $"Settings: BatchSizeMinimum={_batchSizeMinimum}, BatchTimingMaximum={_batchTimingMaximum}, IncludeSubfolders={_includeSubfolders}, Method={_batchSelectionMethod}\n" +
                        string.Join("\n", groupedPhotos.Select((g, i) => $"Batch {i + 1}: {string.Join(", ", g.Select(Path.GetFileName))}")));
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

        private string GetFilenamePrefix(string path)
        {
            string fileName = Path.GetFileNameWithoutExtension(path);
            // Assuming filename is a timestamp like "20241015_120000"
            // Extract the date part (e.g., "20241015") as the prefix
            if (fileName.Length >= 8 && fileName.Contains("_"))
            {
                int underscoreIndex = fileName.IndexOf('_');
                if (underscoreIndex >= 8) // Ensure there's a date-like part before underscore
                {
                    return fileName.Substring(0, 8); // e.g., "20241015"
                }
            }
            return fileName; // Fallback to full name if no valid timestamp format
        }
    }
}