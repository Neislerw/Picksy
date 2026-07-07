import * as fs from 'fs';
import * as path from 'path';
import { ImmichPathMapping, MediaSourceType, SaveState } from '../types';

const SAVE_STATE_FILENAME = '.pic2-savestate.json';
export const SAVE_STATE_VERSION = 2;

/**
 * Get the save state file path for a given folder
 */
export function getSaveStatePath(folderPath: string): string {
  return path.join(folderPath, SAVE_STATE_FILENAME);
}

function normalizeLoadedSaveState(saveState: SaveState): SaveState {
  return {
    version: saveState.version ?? 1,
    sourceType: saveState.sourceType ?? 'local',
    folderPath: saveState.folderPath,
    processedPhotos: saveState.processedPhotos ?? [],
    selections: saveState.selections ?? {},
    immich: saveState.immich,
  };
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
    
    return normalizeLoadedSaveState(saveState);
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
    const payload: SaveState = {
      ...saveState,
      version: SAVE_STATE_VERSION,
    };
    const data = JSON.stringify(payload, null, 2);
    await fs.promises.writeFile(saveStatePath, data, 'utf8');
  } catch (error) {
    console.error('Failed to save state:', error);
    throw error;
  }
}

/**
 * Create a new save state for a folder
 */
export function createNewSaveState(
  folderPath: string,
  sourceType: MediaSourceType = 'local',
  immich?: { serverUrl: string; pathMapping?: ImmichPathMapping }
): SaveState {
  return {
    version: SAVE_STATE_VERSION,
    sourceType,
    folderPath,
    processedPhotos: [],
    selections: {},
    immich,
  };
}

/**
 * Update save state with a new photo selection
 */
export function updateSaveState(
  saveState: SaveState,
  reviewKey: string,
  selection: 'kept' | 'discarded'
): SaveState {
  return {
    ...saveState,
    processedPhotos: saveState.processedPhotos.includes(reviewKey)
      ? saveState.processedPhotos
      : [...saveState.processedPhotos, reviewKey],
    selections: {
      ...saveState.selections,
      [reviewKey]: selection
    }
  };
}

/**
 * Filter photos to exclude already processed ones
 */
export function filterUnprocessedPhotos(
  photos: Array<{ path: string; assetId?: string }>,
  processedPhotos: string[]
): Array<{ path: string; assetId?: string }> {
  const processedSet = new Set(processedPhotos);
  return photos.filter(
    (photo) => !processedSet.has(photo.path) && !(photo.assetId && processedSet.has(photo.assetId))
  );
}

export function isImmichSourceType(sourceType?: MediaSourceType): boolean {
  return sourceType === 'immich-external';
}
