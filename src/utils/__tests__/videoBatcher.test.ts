import { isVideoFile, formatFileSize, formatDuration } from '../videoBatcher';

describe('videoBatcher', () => {
  describe('isVideoFile', () => {
    it('should identify video files by extension', () => {
      expect(isVideoFile('test.mp4')).toBe(true);
      expect(isVideoFile('test.mov')).toBe(true);
      expect(isVideoFile('test.avi')).toBe(true);
      expect(isVideoFile('test.mkv')).toBe(true);
      expect(isVideoFile('test.wmv')).toBe(true);
      expect(isVideoFile('test.flv')).toBe(true);
      expect(isVideoFile('test.webm')).toBe(true);
      expect(isVideoFile('test.m4v')).toBe(true);
      expect(isVideoFile('test.3gp')).toBe(true);
      expect(isVideoFile('test.mpg')).toBe(true);
      expect(isVideoFile('test.mpeg')).toBe(true);
    });

    it('should not identify non-video files', () => {
      expect(isVideoFile('test.jpg')).toBe(false);
      expect(isVideoFile('test.png')).toBe(false);
      expect(isVideoFile('test.txt')).toBe(false);
      expect(isVideoFile('test')).toBe(false);
    });

    it('should be case insensitive', () => {
      expect(isVideoFile('test.MP4')).toBe(true);
      expect(isVideoFile('test.Mov')).toBe(true);
    });
  });

  describe('formatFileSize', () => {
    it('should format file sizes correctly', () => {
      expect(formatFileSize(0)).toBe('0 B');
      expect(formatFileSize(1024)).toBe('1 KB');
      expect(formatFileSize(1024 * 1024)).toBe('1 MB');
      expect(formatFileSize(1024 * 1024 * 1024)).toBe('1 GB');
      expect(formatFileSize(1536)).toBe('1.5 KB');
    });
  });

  describe('formatDuration', () => {
    it('should format durations correctly', () => {
      expect(formatDuration(0)).toBe('Unknown');
      expect(formatDuration(30)).toBe('0:30');
      expect(formatDuration(90)).toBe('1:30');
      expect(formatDuration(3661)).toBe('1:01:01');
      expect(formatDuration(undefined as any)).toBe('Unknown');
      expect(formatDuration(NaN)).toBe('Unknown');
    });
  });
});
