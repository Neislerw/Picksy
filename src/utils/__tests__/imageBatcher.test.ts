import { 
  isImageFile, 
  sortPhotosByTimestamp, 
  groupPhotosIntoBatches,
  extractExifData,
  getPhotoTimestamp
} from '../imageBatcher';
import { Photo } from '../../types';
import * as fs from 'fs';

// Mock fs module
jest.mock('fs', () => ({
  promises: {
    readFile: jest.fn(),
    readdir: jest.fn(),
    stat: jest.fn()
  }
}));

// Mock exif-reader
jest.mock('exif-reader', () => jest.fn());

describe('imageBatcher', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('isImageFile', () => {
    it('should return true for valid image extensions', () => {
      expect(isImageFile('photo.jpg')).toBe(true);
      expect(isImageFile('photo.jpeg')).toBe(true);
      expect(isImageFile('photo.png')).toBe(true);
      expect(isImageFile('photo.gif')).toBe(true);
      expect(isImageFile('photo.bmp')).toBe(true);
      expect(isImageFile('photo.tiff')).toBe(true);
      expect(isImageFile('photo.webp')).toBe(true);
    });

    it('should return false for non-image extensions', () => {
      expect(isImageFile('document.pdf')).toBe(false);
      expect(isImageFile('video.mp4')).toBe(false);
      expect(isImageFile('text.txt')).toBe(false);
      expect(isImageFile('photo.JPG')).toBe(true); // Case insensitive
    });
  });

  describe('sortPhotosByTimestamp', () => {
    it('should sort photos by timestamp in ascending order', () => {
      const photos: Photo[] = [
        {
          id: '3',
          path: '/path/photo3.jpg',
          filename: 'photo3.jpg',
          timestamp: new Date('2023-01-03T10:00:00Z')
        },
        {
          id: '1',
          path: '/path/photo1.jpg',
          filename: 'photo1.jpg',
          timestamp: new Date('2023-01-01T10:00:00Z')
        },
        {
          id: '2',
          path: '/path/photo2.jpg',
          filename: 'photo2.jpg',
          timestamp: new Date('2023-01-02T10:00:00Z')
        }
      ];

      const sorted = sortPhotosByTimestamp(photos);
      expect(sorted[0].id).toBe('1');
      expect(sorted[1].id).toBe('2');
      expect(sorted[2].id).toBe('3');
    });

    it('should return empty array for empty input', () => {
      expect(sortPhotosByTimestamp([])).toEqual([]);
    });
  });

  describe('groupPhotosIntoBatches', () => {
    const createMockPhoto = (id: string, timestamp: Date): Photo => ({
      id,
      path: `/path/${id}.jpg`,
      filename: `${id}.jpg`,
      timestamp
    });

    it('should group photos within time window into batches', () => {
      const photos: Photo[] = [
        createMockPhoto('1', new Date('2023-01-01T10:00:00Z')),
        createMockPhoto('2', new Date('2023-01-01T10:00:25Z')), // 25 seconds later
        createMockPhoto('3', new Date('2023-01-01T10:00:50Z')), // 50 seconds later
        createMockPhoto('4', new Date('2023-01-01T10:01:30Z')), // 40 seconds gap
        createMockPhoto('5', new Date('2023-01-01T10:01:45Z'))  // 15 seconds later
      ];

      const batches = groupPhotosIntoBatches(photos, 30 * 1000); // 30 second window

      expect(batches).toHaveLength(2);
      expect(batches[0].photos).toHaveLength(3); // photos 1, 2, 3
      expect(batches[1].photos).toHaveLength(2); // photos 4, 5
    });

    it('should respect minimum batch size', () => {
      const photos: Photo[] = [
        createMockPhoto('1', new Date('2023-01-01T10:00:00Z')),
        createMockPhoto('2', new Date('2023-01-01T10:00:25Z')),
        createMockPhoto('3', new Date('2023-01-01T10:01:30Z')) // Outside window
      ];

      const batches = groupPhotosIntoBatches(photos, 30 * 1000, 3); // Min size 3

      expect(batches).toHaveLength(0); // No batches meet minimum size of 3
    });

    it('should respect maximum batch size', () => {
      const photos: Photo[] = [
        createMockPhoto('1', new Date('2023-01-01T10:00:00Z')),
        createMockPhoto('2', new Date('2023-01-01T10:00:10Z')),
        createMockPhoto('3', new Date('2023-01-01T10:00:20Z')),
        createMockPhoto('4', new Date('2023-01-01T10:00:30Z')),
        createMockPhoto('5', new Date('2023-01-01T10:00:40Z'))
      ];

      const batches = groupPhotosIntoBatches(photos, 60 * 1000, 2, 3); // Max size 3

      expect(batches).toHaveLength(2);
      expect(batches[0].photos).toHaveLength(3);
      expect(batches[1].photos).toHaveLength(2);
    });

    it('should return empty array for empty input', () => {
      expect(groupPhotosIntoBatches([])).toEqual([]);
    });

    it('should handle single photo (no batches if below minimum)', () => {
      const photos: Photo[] = [
        createMockPhoto('1', new Date('2023-01-01T10:00:00Z'))
      ];

      const batches = groupPhotosIntoBatches(photos, 30 * 1000, 2);

      expect(batches).toEqual([]);
    });
  });

  describe('extractExifData', () => {
    it('should return null for non-JPEG files', async () => {
      const result = await extractExifData('/path/photo.png');
      expect(result).toBeNull();
    });

    it('should handle file read errors gracefully', async () => {
      (fs.promises.readFile as jest.Mock).mockRejectedValue(new Error('File not found'));
      
      const result = await extractExifData('/path/photo.jpg');
      expect(result).toBeNull();
    });
  });

  describe('getPhotoTimestamp', () => {
    const mockStats = {
      mtime: new Date('2023-01-01T10:00:00Z')
    } as fs.Stats;

    it('should fallback to file stats when EXIF extraction fails', async () => {
      const result = await getPhotoTimestamp('/path/photo.jpg', mockStats);
      expect(result).toEqual(mockStats.mtime);
    });

    it('should handle errors gracefully', async () => {
      const result = await getPhotoTimestamp('/path/photo.jpg', mockStats);
      expect(result).toEqual(mockStats.mtime);
    });
  });
}); 