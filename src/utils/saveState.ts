import * as fs from 'fs';
import * as path from 'path';
import { SaveState } from '../types';

const SAVE_STATE_FILENAME = '.pic2-savestate.json';

/**
 * Get the save state file path for a given folder
 */
export function getSaveStatePath(folderPath: string): string {
  return path.join(folderPath, SAVE_STATE_FILENAME);
}

/**
 * Check if a save state exists for the given folder
 */
export async function saveStateExists(folderPath: string): Promise<boolean> {
  try {
    const saveStatePath = getSaveStatePath(folderPath);
    await fs.promises.access(saveStatePath, fs.constants.F_OK);
    return true;
  } catch {
    return false;
  }
}

/**
 * Load save state from disk
 */
export async function loadSaveState(folderPath: string): Promise<SaveState | null> {
  try {
    const saveStatePath = getSaveStatePath(folderPath);
    const data = await fs.promises.readFile(saveStatePath, 'utf8');
    const saveState: SaveState = JSON.parse(data);
    
    // Validate the save state
    if (!saveState.folderPath || !Array.isArray(saveState.processedPhotos) || !saveState.selections) {
      console.warn('Invalid save state format');
      return null;
    }
    
    return saveState;
  } catch (error) {
    console.warn('Failed to load save state:', error);
    return null;
  }
}

/**
 * Save state to disk
 */
export async function saveSaveState(saveState: SaveState): Promise<void> {
  try {
    const saveStatePath = getSaveStatePath(saveState.folderPath);
    const data = JSON.stringify(saveState, null, 2);
    await fs.promises.writeFile(saveStatePath, data, 'utf8');
  } catch (error) {
    console.error('Failed to save state:', error);
    throw error;
  }
}

/**
 * Create a new save state for a folder
 */
export function createNewSaveState(folderPath: string): SaveState {
  return {
    folderPath,
    processedPhotos: [],
    selections: {}
  };
}

/**
 * Update save state with a new photo selection
 */
export function updateSaveState(
  saveState: SaveState,
  photoPath: string,
  selection: 'kept' | 'discarded'
): SaveState {
  return {
    ...saveState,
    processedPhotos: [...saveState.processedPhotos, photoPath],
    selections: {
      ...saveState.selections,
      [photoPath]: selection
    }
  };
}

/**
 * Filter photos to exclude already processed ones
 */
export function filterUnprocessedPhotos(
  photos: Array<{ path: string }>,
  processedPhotos: string[]
): Array<{ path: string }> {
  return photos.filter(photo => !processedPhotos.includes(photo.path));
}