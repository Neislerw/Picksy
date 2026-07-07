export type MediaSourceType = 'local' | 'immich-external';

export interface Photo {
  id: string;
  path: string;
  filename: string;
  timestamp: Date;
  timestampSource?: 'exif' | 'filename' | 'created';
  selected?: boolean;
  toDelete?: boolean;
  source?: MediaSourceType;
  assetId?: string;
  originalPath?: string;
  displayPath?: string;
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
  source?: MediaSourceType;
  assetId?: string;
  originalPath?: string;
  displayPath?: string;
}

export interface PhotoBatch {
  id: string;
  photos: Photo[];
  processed: boolean;
  photosToDelete?: Photo[];
}

export interface ImmichPathMapping {
  localPrefix: string;
  immichPrefix: string;
}

export interface ImmichConnectionConfig {
  serverUrl: string;
  apiKey: string;
  pathMapping?: ImmichPathMapping;
}

export interface SaveState {
  version?: number;
  sourceType?: MediaSourceType;
  folderPath: string;
  processedPhotos: string[];
  selections: Record<string, 'kept' | 'discarded'>;
  immich?: {
    serverUrl: string;
    pathMapping?: ImmichPathMapping;
  };
}

export interface ScanSettings {
  batchTimeWindow?: number;
  minBatchSize?: number;
  maxBatchSize?: number;
  sortingMode?: 'dateTaken' | 'dateCreated' | 'filename';
  supportedExtensions?: string[];
  excludePatterns?: string[];
  sourceType?: MediaSourceType;
  dateFrom?: string;
  dateTo?: string;
}

export interface AppState {
  currentBatch?: PhotoBatch;
  currentPairIndex: number;
  saveState?: SaveState;
  isLoading: boolean;
  error?: string;
}

export function getMediaReviewKey(item: Pick<Photo | Video, 'path' | 'assetId'>): string {
  return item.assetId || item.path;
}
