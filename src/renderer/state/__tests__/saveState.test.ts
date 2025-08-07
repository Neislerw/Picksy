import * as fs from 'fs';
import * as path from 'path';
import { 
  getSaveStatePath,
  saveStateExists,
  loadSaveState,
  saveSaveState,
  createNewSaveState,
  updateSaveState,
  filterUnprocessedPhotos 
} from '../saveState';
import { SaveState } from '../../../types';

// Mock fs module
jest.mock('fs', () => ({
  promises: {
    access: jest.fn(),
    readFile: jest.fn(),
    writeFile: jest.fn(),
  },
  constants: {
    F_OK: 0
  }
}));

// Mock path module
jest.mock('path', () => ({
  join: jest.fn()
}));

const mockFs = {
  promises: {
    access: fs.promises.access as jest.MockedFunction<typeof fs.promises.access>,
    readFile: fs.promises.readFile as jest.MockedFunction<typeof fs.promises.readFile>,
    writeFile: fs.promises.writeFile as jest.MockedFunction<typeof fs.promises.writeFile>,
  },
  constants: fs.constants
};

const mockPath = {
  join: path.join as jest.MockedFunction<typeof path.join>
};

describe('saveState', () => {
  const testFolderPath = '/test/folder';
  const testSaveStatePath = '/test/folder/.pic2-savestate.json';

  beforeEach(() => {
    jest.clearAllMocks();
    mockPath.join.mockReturnValue(testSaveStatePath);
  });

  describe('getSaveStatePath', () => {
    it('should return correct save state file path', () => {
      mockPath.join.mockReturnValue('/test/folder/.pic2-savestate.json');
      
      const result = getSaveStatePath('/test/folder');
      
      expect(mockPath.join).toHaveBeenCalledWith('/test/folder', '.pic2-savestate.json');
      expect(result).toBe('/test/folder/.pic2-savestate.json');
    });
  });

  describe('saveStateExists', () => {
    it('should return true when save state file exists', async () => {
      mockFs.promises.access.mockResolvedValue(undefined);
      
      const result = await saveStateExists(testFolderPath);
      
      expect(mockFs.promises.access).toHaveBeenCalledWith(testSaveStatePath, fs.constants.F_OK);
      expect(result).toBe(true);
    });

    it('should return false when save state file does not exist', async () => {
      mockFs.promises.access.mockRejectedValue(new Error('File not found'));
      
      const result = await saveStateExists(testFolderPath);
      
      expect(result).toBe(false);
    });
  });

  describe('loadSaveState', () => {
    it('should load valid save state from file', async () => {
      const mockSaveState: SaveState = {
        folderPath: testFolderPath,
        processedPhotos: ['/path/photo1.jpg', '/path/photo2.jpg'],
        selections: {
          '/path/photo1.jpg': 'kept',
          '/path/photo2.jpg': 'discarded'
        }
      };
      
      mockFs.promises.readFile.mockResolvedValue(JSON.stringify(mockSaveState));
      
      const result = await loadSaveState(testFolderPath);
      
      expect(mockFs.promises.readFile).toHaveBeenCalledWith(testSaveStatePath, 'utf8');
      expect(result).toEqual(mockSaveState);
    });

    it('should return null for invalid save state format', async () => {
      const invalidSaveState = {
        folderPath: testFolderPath,
        // Missing processedPhotos and selections
      };
      
      mockFs.promises.readFile.mockResolvedValue(JSON.stringify(invalidSaveState));
      
      const result = await loadSaveState(testFolderPath);
      
      expect(result).toBeNull();
    });

    it('should return null when file read fails', async () => {
      mockFs.promises.readFile.mockRejectedValue(new Error('File read error'));
      
      const result = await loadSaveState(testFolderPath);
      
      expect(result).toBeNull();
    });

    it('should return null for invalid JSON', async () => {
      mockFs.promises.readFile.mockResolvedValue('invalid json');
      
      const result = await loadSaveState(testFolderPath);
      
      expect(result).toBeNull();
    });
  });

  describe('saveSaveState', () => {
    it('should save save state to file', async () => {
      const mockSaveState: SaveState = {
        folderPath: testFolderPath,
        processedPhotos: ['/path/photo1.jpg'],
        selections: {
          '/path/photo1.jpg': 'kept'
        }
      };
      
      mockFs.promises.writeFile.mockResolvedValue(undefined);
      
      await saveSaveState(mockSaveState);
      
      expect(mockFs.promises.writeFile).toHaveBeenCalledWith(
        testSaveStatePath,
        JSON.stringify(mockSaveState, null, 2),
        'utf8'
      );
    });

    it('should throw error when file write fails', async () => {
      const mockSaveState: SaveState = {
        folderPath: testFolderPath,
        processedPhotos: [],
        selections: {}
      };
      
      const writeError = new Error('Write failed');
      mockFs.promises.writeFile.mockRejectedValue(writeError);
      
      await expect(saveSaveState(mockSaveState)).rejects.toThrow('Write failed');
    });
  });

  describe('createNewSaveState', () => {
    it('should create new save state with empty arrays', () => {
      const result = createNewSaveState(testFolderPath);
      
      expect(result).toEqual({
        folderPath: testFolderPath,
        processedPhotos: [],
        selections: {}
      });
    });
  });

  describe('updateSaveState', () => {
    it('should update save state with new photo selection', () => {
      const initialSaveState: SaveState = {
        folderPath: testFolderPath,
        processedPhotos: ['/path/photo1.jpg'],
        selections: {
          '/path/photo1.jpg': 'kept'
        }
      };
      
      const result = updateSaveState(initialSaveState, '/path/photo2.jpg', 'discarded');
      
      expect(result).toEqual({
        folderPath: testFolderPath,
        processedPhotos: ['/path/photo1.jpg', '/path/photo2.jpg'],
        selections: {
          '/path/photo1.jpg': 'kept',
          '/path/photo2.jpg': 'discarded'
        }
      });
    });

    it('should not mutate original save state', () => {
      const initialSaveState: SaveState = {
        folderPath: testFolderPath,
        processedPhotos: ['/path/photo1.jpg'],
        selections: {
          '/path/photo1.jpg': 'kept'
        }
      };
      
      const originalProcessedCount = initialSaveState.processedPhotos.length;
      const originalSelectionsCount = Object.keys(initialSaveState.selections).length;
      
      updateSaveState(initialSaveState, '/path/photo2.jpg', 'discarded');
      
      expect(initialSaveState.processedPhotos.length).toBe(originalProcessedCount);
      expect(Object.keys(initialSaveState.selections).length).toBe(originalSelectionsCount);
    });
  });

  describe('filterUnprocessedPhotos', () => {
    it('should filter out processed photos', () => {
      const photos = [
        { path: '/path/photo1.jpg' },
        { path: '/path/photo2.jpg' },
        { path: '/path/photo3.jpg' }
      ];
      
      const processedPhotos = ['/path/photo1.jpg', '/path/photo3.jpg'];
      
      const result = filterUnprocessedPhotos(photos, processedPhotos);
      
      expect(result).toEqual([
        { path: '/path/photo2.jpg' }
      ]);
    });

    it('should return all photos when none are processed', () => {
      const photos = [
        { path: '/path/photo1.jpg' },
        { path: '/path/photo2.jpg' }
      ];
      
      const processedPhotos: string[] = [];
      
      const result = filterUnprocessedPhotos(photos, processedPhotos);
      
      expect(result).toEqual(photos);
    });

    it('should return empty array when all photos are processed', () => {
      const photos = [
        { path: '/path/photo1.jpg' },
        { path: '/path/photo2.jpg' }
      ];
      
      const processedPhotos = ['/path/photo1.jpg', '/path/photo2.jpg'];
      
      const result = filterUnprocessedPhotos(photos, processedPhotos);
      
      expect(result).toEqual([]);
    });

    it('should handle empty photo array', () => {
      const photos: Array<{ path: string }> = [];
      const processedPhotos = ['/path/photo1.jpg'];
      
      const result = filterUnprocessedPhotos(photos, processedPhotos);
      
      expect(result).toEqual([]);
    });
  });
});