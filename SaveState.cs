using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Picksy
{
    public class SaveState
    {
        [JsonPropertyName("folderPath")]
        public string FolderPath { get; set; } = string.Empty;

        [JsonPropertyName("totalPhotos")]
        public int TotalPhotos { get; set; }

        [JsonPropertyName("totalBatches")]
        public int TotalBatches { get; set; }

        [JsonPropertyName("batches")]
        public List<BatchInfo> Batches { get; set; } = new List<BatchInfo>();
    }

    public class BatchInfo
    {
        [JsonPropertyName("batchNumber")]
        public int BatchNumber { get; set; }

        [JsonPropertyName("photoCount")]
        public int PhotoCount { get; set; }

        [JsonPropertyName("creationMethod")]
        public string CreationMethod { get; set; } = string.Empty;

        [JsonPropertyName("photos")]
        public List<PhotoInfo> Photos { get; set; } = new List<PhotoInfo>();

        [JsonPropertyName("batchStatus")]
        public int BatchStatus { get; set; } // 0 for unprocessed, 1 for processed
    }

    public class PhotoInfo
    {
        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public int Status { get; set; } // 0 for unprocessed, 1 for processed

        [JsonPropertyName("fate")]
        public int Fate { get; set; } // 0 for delete, 1 for kept
    }
} 