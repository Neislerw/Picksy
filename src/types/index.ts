export interface Photo {
  id: string;
  path: string;
  filename: string;
  timestamp: Date;
  timestampSource?: 'exif' | 'filename' | 'created';
  selected?: boolean;
  toDelete?: boolean;
}

export interface Video {
  id: string;
  path: string;
  filename: string;
  timestamp: Date;
  timestampSource?: 'metadata' | 'filename' | 'created';
  duration?: number; // in seconds
  fileSize: number; // in bytes
  selected?: boolean;
  toDelete?: boolean;
}

export interface PhotoBatch {
  id: string;
  photos: Photo[];
  processed: boolean;
  photosToDelete?: Photo[];
}

export interface SaveState {
  folderPath: string;
  processedPhotos: string[]; // Array of photo file paths that have been processed
  selections: Record<string, 'kept' | 'discarded'>; // photo path -> selection
}

export interface AppState {
  currentBatch?: PhotoBatch;
  currentPairIndex: number;
  saveState?: SaveState;
  isLoading: boolean;
  error?: string;
} 