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
    const ext = path.extname(filePath).toLowerCase();
    
    // Only process JPEG files for EXIF (most common format with EXIF data)
    if (ext === '.jpg' || ext === '.jpeg') {
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
            console.log(`EXIF data extracted for ${path.basename(filePath)}:`, {
              hasExif: !!exif,
              hasExifData: !!(exif && exif.exif),
              dateTimeOriginal: exif?.exif?.DateTimeOriginal,
              dateTime: exif?.exif?.DateTime,
              subsecTimeOriginal: exif?.exif?.SubsecTimeOriginal
            });
            return exif;
          } catch (error) {
            console.warn(`Failed to parse EXIF data for ${filePath}:`, error);
          }
        }
        offset++;
      }
      console.log(`No EXIF marker found in ${path.basename(filePath)}`);
    } else {
      console.log(`Skipping EXIF extraction for ${path.basename(filePath)} (format: ${ext})`);
    }
    
    return null;
  } catch (error) {
    console.warn(`Failed to read EXIF data for ${filePath}:`, error);
    return null;
  }
}

/**
 * Parse EXIF date string (format: "YYYY:MM:DD HH:MM:SS")
 */
function parseExifDate(dateString: string): Date | null {
  try {
    // EXIF date format is typically "YYYY:MM:DD HH:MM:SS"
    const parts = dateString.split(' ');
    if (parts.length !== 2) return null;
    
    const datePart = parts[0].split(':');
    const timePart = parts[1].split(':');
    
    if (datePart.length !== 3 || timePart.length !== 3) return null;
    
    const year = parseInt(datePart[0], 10);
    const month = parseInt(datePart[1], 10) - 1; // Month is 0-indexed
    const day = parseInt(datePart[2], 10);
    const hour = parseInt(timePart[0], 10);
    const minute = parseInt(timePart[1], 10);
    const second = parseInt(timePart[2], 10);
    
    const date = new Date(year, month, day, hour, minute, second);
    return isNaN(date.getTime()) ? null : date;
  } catch (error) {
    console.warn(`Failed to parse EXIF date: ${dateString}`, error);
    return null;
  }
}

/**
 * Extract timestamp from filename (format: YYYYMMDD_HHMMSS.jpg)
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
 * Get the best available timestamp for a photo
 * Priority: 1. EXIF Date Taken > 2. Filename Timestamp > 3. File Created
 */
export async function getPhotoTimestamp(filePath: string, stats: fs.Stats): Promise<Date> {
  try {
    const exifData = await extractExifData(filePath);
    const filename = path.basename(filePath);
    
    console.log(`\n=== Timestamp extraction for ${filename} ===`);
    console.log(`File created time: ${stats.birthtime.toISOString()}`);
    
    // 1. Try EXIF DateTimeOriginal (Date Taken) - highest priority
    if (exifData?.exif?.DateTimeOriginal) {
      console.log(`Found EXIF DateTimeOriginal: ${exifData.exif.DateTimeOriginal}`);
      const dateTaken = parseExifDate(exifData.exif.DateTimeOriginal);
      if (dateTaken) {
        console.log(`${filename}: ✅ Using EXIF DateTimeOriginal (Date Taken): ${dateTaken.toISOString()}`);
        return dateTaken;
      } else {
        console.log(`${filename}: ❌ Failed to parse EXIF DateTimeOriginal: ${exifData.exif.DateTimeOriginal}`);
      }
    } else {
      console.log(`${filename}: No EXIF DateTimeOriginal found`);
    }
    
    // 2. Try timestamp from filename (format: YYYYMMDD_HHMMSS.jpg)
    const filenameTimestamp = parseTimestampFromFilename(filename);
    if (filenameTimestamp) {
      console.log(`${filename}: ✅ Using timestamp from filename: ${filenameTimestamp.toISOString()}`);
      return filenameTimestamp;
    } else {
      console.log(`${filename}: No timestamp pattern found in filename`);
    }
    
    // 3. Fallback to file created time (NOT modified time)
    console.log(`${filename}: ❌ No EXIF or filename timestamp found, using file created time: ${stats.birthtime.toISOString()}`);
    return stats.birthtime;
  } catch (error) {
    console.warn(`Failed to get timestamp for ${filePath}:`, error);
    return stats.birthtime; // Use birthtime instead of mtime
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

  console.log(`Grouping ${photos.length} photos into batches (timeWindow: ${timeWindow}ms, minSize: ${minBatchSize}, maxSize: ${maxBatchSize})`);
  
  // Log first few photos with their timestamps for debugging
  photos.slice(0, 5).forEach((photo, index) => {
    console.log(`Photo ${index + 1}: ${path.basename(photo.filename)} - ${photo.timestamp.toISOString()}`);
  });
  
  // Log time differences between consecutive photos to check for large gaps
  for (let i = 1; i < Math.min(photos.length, 10); i++) {
    const timeDiff = photos[i].timestamp.getTime() - photos[i-1].timestamp.getTime();
    const timeDiffMinutes = timeDiff / (1000 * 60);
    const timeDiffHours = timeDiff / (1000 * 60 * 60);
    const timeDiffDays = timeDiff / (1000 * 60 * 60 * 24);
    
    console.log(`Time diff between ${path.basename(photos[i-1].filename)} and ${path.basename(photos[i].filename)}: ${timeDiffMinutes.toFixed(1)} minutes (${timeDiffHours.toFixed(1)} hours, ${timeDiffDays.toFixed(1)} days)`);
  }

  const batches: PhotoBatch[] = [];
  let currentBatch: Photo[] = [];
  let batchId = 1;

  for (let i = 0; i < photos.length; i++) {
    const currentPhoto = photos[i];
    
    if (currentBatch.length === 0) {
      // Start a new batch
      currentBatch = [currentPhoto];
      console.log(`Starting new batch ${batchId} with: ${path.basename(currentPhoto.filename)}`);
    } else {
      const lastPhotoInBatch = currentBatch[currentBatch.length - 1];
      const timeDiff = currentPhoto.timestamp.getTime() - lastPhotoInBatch.timestamp.getTime();
      
      const timeDiffMinutes = timeDiff / (1000 * 60);
      const timeDiffHours = timeDiff / (1000 * 60 * 60);
      const timeDiffDays = timeDiff / (1000 * 60 * 60 * 24);
      
      console.log(`Comparing ${path.basename(currentPhoto.filename)} (${currentPhoto.timestamp.toISOString()}) with last photo in batch (${lastPhotoInBatch.timestamp.toISOString()}) - timeDiff: ${timeDiff}ms (${timeDiffMinutes.toFixed(1)} minutes)`);
      
      // Check if this photo should be added to the current batch
      if (timeDiff <= timeWindow && currentBatch.length < maxBatchSize) {
        // Warn if photos with large time differences are being grouped
        if (timeDiffMinutes > 5) {
          console.warn(`⚠️  WARNING: Grouping photos with ${timeDiffMinutes.toFixed(1)} minute gap! This might indicate incorrect timestamps.`);
        }
        currentBatch.push(currentPhoto);
        console.log(`Added to current batch (${currentBatch.length}/${maxBatchSize})`);
      } else {
        console.log(`Time difference (${timeDiff}ms, ${timeDiffMinutes.toFixed(1)} minutes) exceeds window (${timeWindow}ms, ${timeWindow/1000/60} minutes) or batch full, finalizing batch ${batchId}`);
        // Finalize current batch if it meets minimum size
        if (currentBatch.length >= minBatchSize) {
          batches.push({
            id: `batch_${batchId}`,
            photos: [...currentBatch],
            processed: false
          });
          console.log(`Created batch ${batchId} with ${currentBatch.length} photos`);
          batchId++;
        } else {
          console.log(`Batch ${batchId} too small (${currentBatch.length} < ${minBatchSize}), discarding`);
        }
        
        // Start a new batch
        currentBatch = [currentPhoto];
        console.log(`Starting new batch ${batchId} with: ${path.basename(currentPhoto.filename)}`);
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
    console.log(`Created final batch ${batchId} with ${currentBatch.length} photos`);
  } else if (currentBatch.length > 0) {
    console.log(`Final batch too small (${currentBatch.length} < ${minBatchSize}), discarding`);
  }

  console.log(`Created ${batches.length} batches total`);
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