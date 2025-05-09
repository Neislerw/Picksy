using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Picksy
{
    public class PhotoGrouper
    {
        private readonly int TimeThresholdSeconds; // Max seconds between photos in a batch
        private readonly int MinBatchSize; // Minimum photos per batch
        private readonly bool IncludeSubfolders; // Whether to include subfolders

        public PhotoGrouper(int minBatchSize, int timeThresholdSeconds, bool includeSubfolders)
        {
            MinBatchSize = minBatchSize;
            TimeThresholdSeconds = timeThresholdSeconds;
            IncludeSubfolders = includeSubfolders;
        }

        public List<List<string>> GroupPhotos(string folderPath)
        {
            var batches = new List<List<string>>();
            var photos = new List<(string Path, DateTime? Timestamp)>();

            // Get all image files
            var imageExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var searchOption = IncludeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var files = System.IO.Directory.GetFiles(folderPath, "*.*", searchOption)
                .Where(f => imageExtensions.Contains(System.IO.Path.GetExtension(f).ToLower()))
                // Exclude files in the _delete folder
                .Where(f => !f.Contains(Path.DirectorySeparatorChar + "_delete" + Path.DirectorySeparatorChar))
                .ToList();

            // Extract timestamps from filenames
            foreach (var file in files)
            {
                try
                {
                    var fileName = System.IO.Path.GetFileNameWithoutExtension(file);
                    // Expect format: YYYYMMDD_HHMMSS_XXX (e.g., 20250122_171223_003)
                    if (fileName.Length >= 15 && fileName[8] == '_')
                    {
                        var dateTimeStr = fileName.Substring(0, 15); // YYYYMMDD_HHMMSS
                        if (DateTime.TryParseExact(dateTimeStr, "yyyyMMdd_HHmmss", null, System.Globalization.DateTimeStyles.None, out var dateTime))
                        {
                            photos.Add((file, dateTime));
                        }
                    }
                }
                catch
                {
                    // Skip files with errors
                }
            }

            // Sort photos by timestamp
            photos.Sort((a, b) =>
            {
                if (a.Timestamp is null && b.Timestamp is null) return 0;
                if (a.Timestamp is null) return -1;
                if (b.Timestamp is null) return 1;
                return a.Timestamp.Value.CompareTo(b.Timestamp.Value);
            });

            // Group photos
            var currentBatch = new List<string>();
            for (int i = 0; i < photos.Count; i++)
            {
                if (!photos[i].Timestamp.HasValue) continue;

                currentBatch.Add(photos[i].Path);

                // Check if next photo is within time threshold
                if (i < photos.Count - 1 && photos[i + 1].Timestamp.HasValue)
                {
                    var timeDiff = (photos[i + 1].Timestamp.Value - photos[i].Timestamp.Value).TotalSeconds;
                    if (timeDiff > TimeThresholdSeconds)
                    {
                        // End current batch if gap is too large
                        if (currentBatch.Count >= MinBatchSize)
                        {
                            batches.Add(new List<string>(currentBatch));
                        }
                        currentBatch.Clear();
                    }
                }
            }

            // Add the last batch if it meets the threshold
            if (currentBatch.Count >= MinBatchSize)
            {
                batches.Add(new List<string>(currentBatch));
            }

            return batches;
        }
    }
}