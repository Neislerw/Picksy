import * as fs from 'fs';
import * as path from 'path';
import { Video } from '../types';

// Supported video file extensions
const VIDEO_EXTENSIONS = ['.mp4', '.mov', '.avi', '.mkv', '.wmv', '.flv', '.webm', '.m4v', '.3gp', '.mpg', '.mpeg'];

/**
 * Check if a file is a video based on its extension
 */
export function isVideoFile(filename: string): boolean {
  const ext = path.extname(filename).toLowerCase();
  return VIDEO_EXTENSIONS.includes(ext);
}

/**
 * Extract video metadata using ffprobe (if available) or fallback to file stats
 */
export async function extractVideoMetadata(filePath: string): Promise<{
  duration?: number;
  timestamp?: Date;
  fileSize: number;
}> {
  const stats = await fs.promises.stat(filePath);
  const fileSize = stats.size;
  
  // Try to extract duration using ffprobe if available
  let duration: number | undefined;
  let timestamp: Date | undefined;
  
  try {
    // For now, we'll use file creation time as timestamp
    // In a full implementation, you'd use ffprobe to get actual video metadata
    timestamp = stats.birthtime;
    
    // TODO: Implement ffprobe integration for duration and creation time
    // This would require spawning ffprobe process and parsing JSON output
    console.log(`Video metadata extraction for ${path.basename(filePath)}:`, {
      fileSize,
      creationTime: stats.birthtime.toISOString(),
      // duration would be extracted here
    });
  } catch (error) {
    console.warn(`Failed to extract video metadata for ${filePath}:`, error);
  }
  
  return {
    duration,
    timestamp,
    fileSize
  };
}

/**
 * Parse timestamp from filename (format: YYYYMMDD_HHMMSS.mp4)
 */
function parseTimestampFromFilename(filename: string): Date | null {
  try {
    // Remove file extension
    const nameWithoutExt = path.parse(filename).name;
    
    // Check for pattern: YYYYMMDD_HHMMSS
    const timestampPattern = /^(\d{4})(\d{2})(\d{2})_(\d{2})(\d{2})(\d{2})/;
    const match = nameWithoutExt.match(timestampPattern);
    
    if (match) {
      const year = parseInt(match[1], 10);
      const month = parseInt(match[2], 10) - 1; // Month is 0-indexed
      const day = parseInt(match[3], 10);
      const hour = parseInt(match[4], 10);
      const minute = parseInt(match[5], 10);
      const second = parseInt(match[6], 10);
      
      const date = new Date(year, month, day, hour, minute, second);
      return isNaN(date.getTime()) ? null : date;
    }
    
    return null;
  } catch (error) {
    console.warn(`Failed to parse timestamp from filename: ${filename}`, error);
    return null;
  }
}

/**
 * Get the best available timestamp for a video
 * Priority: 1. Filename Timestamp > 2. File Created
 */
export async function getVideoTimestamp(filePath: string, stats: fs.Stats): Promise<Date> {
  try {
    const filename = path.basename(filePath);
    
    console.log(`\n=== Video timestamp extraction for ${filename} ===`);
    console.log(`File created time: ${stats.birthtime.toISOString()}`);
    
    // 1. Try timestamp from filename (format: YYYYMMDD_HHMMSS.mp4)
    const filenameTimestamp = parseTimestampFromFilename(filename);
    if (filenameTimestamp) {
      console.log(`${filename}: ✅ Using timestamp from filename: ${filenameTimestamp.toISOString()}`);
      return filenameTimestamp;
    } else {
      console.log(`${filename}: No timestamp pattern found in filename`);
    }
    
    // 2. Fallback to file created time
    console.log(`${filename}: ❌ No filename timestamp found, using file created time: ${stats.birthtime.toISOString()}`);
    return stats.birthtime;
  } catch (error) {
    console.warn(`Failed to get timestamp for ${filePath}:`, error);
    return stats.birthtime;
  }
}

/**
 * Recursively scan a directory for video files
 */
export async function countVideoFiles(dirPath: string, includeSubfolders: boolean = true): Promise<number> {
  let count = 0;
  try {
    const items = await fs.promises.readdir(dirPath);
    for (const item of items) {
      const fullPath = path.join(dirPath, item);
      const stats = await fs.promises.stat(fullPath);
      if (stats.isDirectory()) {
        if (item === '_delete') continue;
        if (includeSubfolders) {
          count += await countVideoFiles(fullPath, includeSubfolders);
        }
      } else if (stats.isFile() && isVideoFile(item)) {
        count += 1;
      }
    }
  } catch (error) {
    console.error(`Error counting videos in ${dirPath}:`, error);
    throw error;
  }
  return count;
}

export async function scanDirectoryForVideos(
  dirPath: string,
  includeSubfolders: boolean = true,
  onProgress?: (update: { stage: string; current: number; total: number; path?: string }) => void,
  progressCtx?: { processed: number; total: number }
): Promise<Video[]> {
  const videos: Video[] = [];
  
  try {
    const items = await fs.promises.readdir(dirPath);
    
    for (const item of items) {
      const fullPath = path.join(dirPath, item);
      const stats = await fs.promises.stat(fullPath);
      
      if (stats.isDirectory()) {
        // Always skip _delete folders
        if (item === '_delete') {
          continue;
        }
        // Only scan subdirectories if includeSubfolders is true
        if (includeSubfolders) {
          const subVideos = await scanDirectoryForVideos(fullPath, includeSubfolders, onProgress, progressCtx);
          videos.push(...subVideos);
        }
      } else if (stats.isFile() && isVideoFile(item)) {
        // Extract video metadata
        const metadata = await extractVideoMetadata(fullPath);
        const timestamp = metadata.timestamp || await getVideoTimestamp(fullPath, stats);
        
        // Create Video object with metadata
        const video: Video = {
          id: `${fullPath}_${timestamp.getTime()}`,
          path: fullPath,
          filename: item,
          timestamp: timestamp,
          duration: metadata.duration,
          fileSize: metadata.fileSize,
        };
        videos.push(video);
        if (progressCtx && onProgress) {
          progressCtx.processed += 1;
          onProgress({ stage: 'Scanning videos', current: progressCtx.processed, total: progressCtx.total, path: fullPath });
        }
      }
    }
  } catch (error) {
    console.error(`Error scanning directory ${dirPath}:`, error);
    throw error;
  }
  
  return videos;
}

/**
 * Sort videos by timestamp (oldest first)
 */
export function sortVideosByTimestamp(videos: Video[]): Video[] {
  return [...videos].sort((a, b) => a.timestamp.getTime() - b.timestamp.getTime());
}

/**
 * Main function to scan a folder and return sorted video files
 */
export async function scanFolderForVideos(
  folderPath: string,
  includeSubfolders: boolean = true,
  onProgress?: (update: { stage: string; current: number; total: number; path?: string }) => void
): Promise<Video[]> {
  console.log(`Scanning folder for videos: ${folderPath} (includeSubfolders: ${includeSubfolders})`);
  
  try {
    const total = await countVideoFiles(folderPath, includeSubfolders);
    const progressCtx = { processed: 0, total };
    if (onProgress) onProgress({ stage: 'Preparing scan', current: 0, total });
    const videos = await scanDirectoryForVideos(folderPath, includeSubfolders, onProgress, progressCtx);
    const sortedVideos = sortVideosByTimestamp(videos);
    
    console.log(`Found ${sortedVideos.length} video files`);
    if (onProgress) onProgress({ stage: 'Sorting videos', current: progressCtx.total, total: progressCtx.total });
    return sortedVideos;
  } catch (error) {
    console.error('Error scanning folder for videos:', error);
    throw error;
  }
}

/**
 * Format file size in human readable format
 */
export function formatFileSize(bytes: number): string {
  if (bytes === 0) return '0 B';
  
  const k = 1024;
  const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  
  return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
}

/**
 * Format duration in human readable format
 */
export function formatDuration(seconds: number): string {
  if (!seconds || isNaN(seconds)) return 'Unknown';
  
  const hours = Math.floor(seconds / 3600);
  const minutes = Math.floor((seconds % 3600) / 60);
  const secs = Math.floor(seconds % 60);
  
  if (hours > 0) {
    return `${hours}:${minutes.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
  } else {
    return `${minutes}:${secs.toString().padStart(2, '0')}`;
  }
}
