using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
            var imageExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var searchOption = includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            // Get all image files, excluding those in _delete folder
            var allPhotos = Directory.GetFiles(folderPath, "*.*", searchOption)
                .Where(f => imageExtensions.Contains(Path.GetExtension(f).ToLower()))
                .Where(f => !f.Contains(Path.DirectorySeparatorChar + "_delete" + Path.DirectorySeparatorChar))
                .ToList();

            // Sort photos based on the selected batch selection method
            switch (batchSelectionMethod)
            {
                case "By Name":
                    allPhotos.Sort((a, b) => string.Compare(Path.GetFileName(a), Path.GetFileName(b), StringComparison.OrdinalIgnoreCase));
                    break;
                case "By Date Created":
                    allPhotos.Sort((a, b) => File.GetCreationTime(a).CompareTo(File.GetCreationTime(b)));
                    break;
                case "By Date Modified":
                    allPhotos.Sort((a, b) => File.GetLastWriteTime(a).CompareTo(File.GetLastWriteTime(b)));
                    break;
                default:
                    throw new ArgumentException("Invalid batch selection method");
            }

            var batches = new List<List<string>>();
            var currentBatch = new List<string>();
            DateTime? lastPhotoTime = null;

            foreach (var photo in allPhotos)
            {
                DateTime currentPhotoTime;
                if (batchSelectionMethod == "By Date Created")
                {
                    currentPhotoTime = File.GetCreationTime(photo);
                }
                else
                {
                    currentPhotoTime = File.GetLastWriteTime(photo);
                }

                if (lastPhotoTime == null || (currentPhotoTime - lastPhotoTime.Value).TotalSeconds <= batchTimingMaximum)
                {
                    currentBatch.Add(photo);
                }
                else
                {
                    if (currentBatch.Count >= batchSizeMinimum)
                    {
                        batches.Add(currentBatch);
                    }
                    currentBatch = new List<string> { photo };
                }

                lastPhotoTime = currentPhotoTime;
            }

            if (currentBatch.Count >= batchSizeMinimum)
            {
                batches.Add(currentBatch);
            }

            return batches;
        }
    }
}