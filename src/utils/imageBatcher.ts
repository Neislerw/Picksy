import * as fs from 'fs';
import * as path from 'path';
import { Photo, PhotoBatch } from '../types';

// Supported image file extensions
const IMAGE_EXTENSIONS = ['.jpg', '.jpeg', '.png', '.gif', '.bmp', '.tiff', '.webp'];

// Default batch detection settings
const DEFAULT_BATCH_TIME_WINDOW = 30 * 1000; // 30 seconds
const DEFAULT_MIN_BATCH_SIZE = 2; // Minimum photos in a batch
const DEFAULT_MAX_BATCH_SIZE = 20; // Maximum photos in a batch

/**
 * Check if a file is an image based on its extension
 */
export function isImageFile(filename: string): boolean {
  const ext = path.extname(filename).toLowerCase();
  return IMAGE_EXTENSIONS.includes(ext);
}

/**
 * Extract EXIF data from image file
 */
export async function extractExifData(filePath: string): Promise<any> {
  try {
    const buffer = await fs.promises.readFile(filePath);
    
    // Look for EXIF data in JPEG files
    if (path.extname(filePath).toLowerCase() === '.jpg' || path.extname(filePath).toLowerCase() === '.jpeg') {
      const exifReader = require('exif-reader');
      
      // Find EXIF marker in JPEG
      let offset = 0;
      while (offset < buffer.length - 1) {
        if (buffer[offset] === 0xFF && buffer[offset + 1] === 0xE1) {
          // Found EXIF marker
          const exifLength = buffer.readUInt16BE(offset + 2);
          const exifData = buffer.slice(offset + 4, offset + 4 + exifLength);
          
          try {
            const exif = exifReader(exifData);
            return exif;
          } catch (error) {
            console.warn(`Failed to parse EXIF data for ${filePath}:`, error);
          }
        }
        offset++;
      }
    }
    
    return null;
  } catch (error) {
    console.warn(`Failed to read EXIF data for ${filePath}:`, error);
    return null;
  }
}

/**
 * Get the best available timestamp for a photo
 * Priority: EXIF Date Taken > File Modified > File Created
 */
export async function getPhotoTimestamp(filePath: string, stats: fs.Stats): Promise<Date> {
  try {
    const exifData = await extractExifData(filePath);
    
    if (exifData && exifData.exif && exifData.exif.DateTimeOriginal) {
      // Parse EXIF DateTimeOriginal (Date Taken)
      const dateTaken = new Date(exifData.exif.DateTimeOriginal);
      if (!isNaN(dateTaken.getTime())) {
        return dateTaken;
      }
    }
    
    if (exifData && exifData.exif && exifData.exif.DateTime) {
      // Parse EXIF DateTime (Date Modified)
      const dateTime = new Date(exifData.exif.DateTime);
      if (!isNaN(dateTime.getTime())) {
        return dateTime;
      }
    }
    
    // Fallback to file system dates
    // Use mtime (modified time) as it's more reliable than ctime (created time)
    return stats.mtime;
  } catch (error) {
    console.warn(`Failed to get timestamp for ${filePath}:`, error);
    return stats.mtime;
  }
}

/**
 * Get file stats (creation time, modification time, etc.)
 */
export async function getFileStats(filePath: string): Promise<fs.Stats> {
  return new Promise((resolve, reject) => {
    fs.stat(filePath, (err, stats) => {
      if (err) {
        reject(err);
      } else {
        resolve(stats);
      }
    });
  });
}

/**
 * Recursively scan a directory for image files
 */
export async function scanDirectoryForImages(dirPath: string, includeSubfolders: boolean = true): Promise<Photo[]> {
  const photos: Photo[] = [];
  
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
          const subPhotos = await scanDirectoryForImages(fullPath, includeSubfolders);
          photos.push(...subPhotos);
        }
      } else if (stats.isFile() && isImageFile(item)) {
        // Get the best available timestamp
        const timestamp = await getPhotoTimestamp(fullPath, stats);
        
        // Create Photo object with metadata
        const photo: Photo = {
          id: `${fullPath}_${timestamp.getTime()}`,
          path: fullPath,
          filename: item,
          timestamp: timestamp,
        };
        photos.push(photo);
      }
    }
  } catch (error) {
    console.error(`Error scanning directory ${dirPath}:`, error);
    throw error;
  }
  
  return photos;
}

/**
 * Sort photos by timestamp (oldest first)
 */
export function sortPhotosByTimestamp(photos: Photo[]): Photo[] {
  return [...photos].sort((a, b) => a.timestamp.getTime() - b.timestamp.getTime());
}

/**
 * Group photos into batches based on time proximity
 */
export function groupPhotosIntoBatches(
  photos: Photo[],
  timeWindow: number = DEFAULT_BATCH_TIME_WINDOW,
  minBatchSize: number = DEFAULT_MIN_BATCH_SIZE,
  maxBatchSize: number = DEFAULT_MAX_BATCH_SIZE
): PhotoBatch[] {
  if (photos.length === 0) {
    return [];
  }

  const batches: PhotoBatch[] = [];
  let currentBatch: Photo[] = [];
  let batchId = 1;

  for (let i = 0; i < photos.length; i++) {
    const currentPhoto = photos[i];
    
    if (currentBatch.length === 0) {
      // Start a new batch
      currentBatch = [currentPhoto];
    } else {
      const lastPhotoInBatch = currentBatch[currentBatch.length - 1];
      const timeDiff = currentPhoto.timestamp.getTime() - lastPhotoInBatch.timestamp.getTime();
      
      // Check if this photo should be added to the current batch
      if (timeDiff <= timeWindow && currentBatch.length < maxBatchSize) {
        currentBatch.push(currentPhoto);
      } else {
        // Finalize current batch if it meets minimum size
        if (currentBatch.length >= minBatchSize) {
          batches.push({
            id: `batch_${batchId}`,
            photos: [...currentBatch],
            processed: false
          });
          batchId++;
        }
        
        // Start a new batch
        currentBatch = [currentPhoto];
      }
    }
  }

  // Handle the last batch
  if (currentBatch.length >= minBatchSize) {
    batches.push({
      id: `batch_${batchId}`,
      photos: [...currentBatch],
      processed: false
    });
  }

  return batches;
}

/**
 * Main function to scan a folder and return sorted image files
 */
export async function scanFolderForImages(folderPath: string, includeSubfolders: boolean = true): Promise<Photo[]> {
  console.log(`Scanning folder: ${folderPath} (includeSubfolders: ${includeSubfolders})`);
  
  try {
    const photos = await scanDirectoryForImages(folderPath, includeSubfolders);
    const sortedPhotos = sortPhotosByTimestamp(photos);
    
    console.log(`Found ${sortedPhotos.length} image files`);
    return sortedPhotos;
  } catch (error) {
    console.error('Error scanning folder for images:', error);
    throw error;
  }
}

/**
 * Main function to scan a folder and return photo batches
 */
export async function scanFolderAndCreateBatches(
  folderPath: string,
  timeWindow: number = DEFAULT_BATCH_TIME_WINDOW,
  minBatchSize: number = DEFAULT_MIN_BATCH_SIZE,
  maxBatchSize: number = DEFAULT_MAX_BATCH_SIZE,
  includeSubfolders: boolean = true,
  processedPhotos: string[] = []
): Promise<PhotoBatch[]> {
  console.log(`Scanning folder and creating batches: ${folderPath} (includeSubfolders: ${includeSubfolders})`);
  
  try {
    const allPhotos = await scanFolderForImages(folderPath, includeSubfolders);
    
    // Filter out already processed photos
    const unprocessedPhotos = allPhotos.filter(photo => !processedPhotos.includes(photo.path));
    
    console.log(`Found ${allPhotos.length} total photos, ${unprocessedPhotos.length} unprocessed`);
    
    const batches = groupPhotosIntoBatches(unprocessedPhotos, timeWindow, minBatchSize, maxBatchSize);
    
    console.log(`Created ${batches.length} batches from ${unprocessedPhotos.length} unprocessed photos`);
    return batches;
  } catch (error) {
    console.error('Error scanning folder and creating batches:', error);
    throw error;
  }
} 