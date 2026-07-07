import { app, BrowserWindow, ipcMain, dialog } from 'electron';
import * as path from 'path';
import * as fs from 'fs';
import { ImmichConnectionConfig } from './types';
import { createImmichClient } from './utils/immichClient';

let sessionImmichConfig: ImmichConnectionConfig | null = null;
let sessionImmichRootFolder: string | null = null;
let sessionImmichIncludeSubfolders = true;

function buildImageScanOptions(settings?: any) {
  const { buildImmichImageScanOptions } = require('./utils/imageBatcher');
  if (settings?.sourceType === 'immich-external') {
    return buildImmichImageScanOptions(settings);
  }
  return {
    supportedExtensions: settings?.supportedExtensions,
    excludePatterns: settings?.excludePatterns,
    dateFrom: settings?.dateFrom,
    dateTo: settings?.dateTo,
  };
}

function buildVideoScanOptions(settings?: any) {
  const { buildImmichVideoScanOptions } = require('./utils/videoBatcher');
  if (settings?.sourceType === 'immich-external') {
    return buildImmichVideoScanOptions(settings);
  }
  return {
    supportedExtensions: settings?.supportedExtensions,
    excludePatterns: settings?.excludePatterns,
    dateFrom: settings?.dateFrom,
    dateTo: settings?.dateTo,
  };
}

function createWindow(): void {
  // Create the browser window.
  const mainWindow = new BrowserWindow({
    width: 1200,
    height: 800,
    icon: path.join(__dirname, '../resources/logo.ico'),
    webPreferences: {
      nodeIntegration: false,
      contextIsolation: true,
      preload: path.join(__dirname, 'preload.js')
    },
  });

  // Load the index.html of the app.
  mainWindow.loadFile(path.join(__dirname, 'index.html'));

  // Open the DevTools in development.
  if (process.env.NODE_ENV === 'development') {
    mainWindow.webContents.openDevTools();
  }
}

// Set up IPC handlers
function setupIpcHandlers() {
  async function renameWithRetry(fromPath: string, toPath: string, maxRetries = 3, delayMs = 150): Promise<{ ok: boolean; error?: string }> {
    for (let i = 0; i < maxRetries; i++) {
      try {
        fs.renameSync(fromPath, toPath);
        return { ok: true };
      } catch (e: unknown) {
        const err = e as NodeJS.ErrnoException;
        const msg = err?.message ?? String(e);
        const isEBUSY = err?.code === 'EBUSY' || msg.includes('EBUSY') || msg.includes('busy') || msg.includes('locked');
        if (isEBUSY && i < maxRetries - 1) {
          await new Promise(r => setTimeout(r, delayMs));
        } else {
          return { ok: false, error: msg };
        }
      }
    }
    return { ok: false, error: 'EBUSY after retries' };
  }

  function ensureUniquePath(targetDir: string, fileName: string): string {
    const base = path.parse(fileName).name;
    const ext = path.parse(fileName).ext;
    let candidate = path.join(targetDir, fileName);
    let counter = 1;
    while (fs.existsSync(candidate)) {
      candidate = path.join(targetDir, `${base} (${counter})${ext}`);
      counter += 1;
    }
    return candidate;
  }
  // Immich session config (API key kept in memory only)
  ipcMain.handle(
    'immich-set-session',
    async (
      _event,
      config: ImmichConnectionConfig | null,
      rootFolderPath?: string | null,
      includeSubfolders?: boolean
    ) => {
      sessionImmichConfig = config;
      sessionImmichRootFolder = rootFolderPath ?? null;
      sessionImmichIncludeSubfolders = includeSubfolders ?? true;
      return { ok: true };
    }
  );

  ipcMain.handle('immich-test-connection', async (_event, config: ImmichConnectionConfig) => {
    try {
      const client = createImmichClient(config);
      const result = await client.testConnection();
      if (result.ok && result.effectiveServerUrl) {
        sessionImmichConfig = { ...config, serverUrl: result.effectiveServerUrl };
      }
      return result;
    } catch (error: any) {
      return { ok: false, error: String(error?.message || error) };
    }
  });

  ipcMain.handle(
    'immich-match-assets',
    async (
      _event,
      payload: {
        config?: ImmichConnectionConfig;
        localPaths: string[];
        rootFolderPath: string;
        includeSubfolders: boolean;
      }
    ) => {
      const config = payload.config || sessionImmichConfig;
      if (!config?.serverUrl || !config?.apiKey) {
        return { ok: false, error: 'Immich connection not configured', matches: [] };
      }
      try {
        const client = createImmichClient(config);
        const matches = await client.matchLocalPaths(
          payload.localPaths,
          payload.rootFolderPath,
          payload.includeSubfolders
        );
        return { ok: true, matches };
      } catch (error: any) {
        return { ok: false, error: String(error?.message || error), matches: [] };
      }
    }
  );

  ipcMain.handle('immich-trash-assets', async (_event, payload: { assetIds: string[]; config?: ImmichConnectionConfig }) => {
    const config = payload.config || sessionImmichConfig;
    if (!config?.serverUrl || !config?.apiKey) {
      return { ok: false, error: 'Immich connection not configured' };
    }
    const client = createImmichClient(config);
    return client.trashAssets(payload.assetIds);
  });

  ipcMain.handle('immich-restore-assets', async (_event, payload: { assetIds: string[]; config?: ImmichConnectionConfig }) => {
    const config = payload.config || sessionImmichConfig;
    if (!config?.serverUrl || !config?.apiKey) {
      return { ok: false, error: 'Immich connection not configured' };
    }
    const client = createImmichClient(config);
    return client.restoreAssets(payload.assetIds);
  });

  ipcMain.handle('select-folder', async () => {
    const result = await dialog.showOpenDialog({
      properties: ['openDirectory']
    });
    return result.filePaths[0];
  });

  // Handle save state operations
  ipcMain.handle('save-state-exists', async (event, folderPath: string) => {
    try {
      const { saveStateExists } = require('./utils/saveState');
      console.log('save-state-exists called for:', folderPath);
      return await saveStateExists(folderPath);
    } catch (error) {
      console.error('Error in save-state-exists:', error);
      return false;
    }
  });

  ipcMain.handle('load-save-state', async (event, folderPath: string) => {
    try {
      const { loadSaveState } = require('./utils/saveState');
      console.log('load-save-state called for:', folderPath);
      return await loadSaveState(folderPath);
    } catch (error) {
      console.error('Error in load-save-state:', error);
      return null;
    }
  });

  ipcMain.handle('save-save-state', async (event, saveState) => {
    try {
      const { saveSaveState } = require('./utils/saveState');
      console.log('save-save-state called for:', saveState.folderPath);
      return await saveSaveState(saveState);
    } catch (error) {
      console.error('Error in save-save-state:', error);
      throw error;
    }
  });

  ipcMain.handle('delete-save-state', async (event, folderPath: string) => {
    try {
      const { getSaveStatePath } = require('./utils/saveState');
      const saveStatePath = getSaveStatePath(folderPath);
      console.log('delete-save-state called for:', folderPath);
      await fs.promises.unlink(saveStatePath);
    } catch (error) {
      console.warn('Failed to delete save state:', error);
      throw error;
    }
  });

  // Handle folder scanning with save state support
  ipcMain.handle('scan-folder', async (event, folderPath: string, includeSubfolders: boolean = true, processedPhotos: string[] = [], settings?: any) => {
    // Import the imageBatcher utility
    const { scanFolderAndCreateBatches } = require('./utils/imageBatcher');
    
    // Use settings if provided, otherwise use defaults
    const timeWindow = settings?.batchTimeWindow ? settings.batchTimeWindow * 1000 : 30 * 1000; // Convert to milliseconds
    const minBatchSize = settings?.minBatchSize || 2;
    const maxBatchSize = settings?.maxBatchSize || 20;
    const sortingMode = settings?.sortingMode || 'dateTaken';

    const webContents = (event?.sender);
    const sendProgress = (update: { stage: string; current: number; total: number; path?: string }) => {
      try {
        webContents?.send('scan-progress', update);
      } catch {}
    };

    return await scanFolderAndCreateBatches(
      folderPath,
      timeWindow,
      minBatchSize,
      maxBatchSize,
      includeSubfolders,
      processedPhotos,
      sortingMode,
      sendProgress,
      buildImageScanOptions(settings)
    );
  });

  // Flat scan for photos (no batching) with optional processed filtering
  ipcMain.handle('scan-folder-photos', async (event, folderPath: string, includeSubfolders: boolean = true, processedPhotos: string[] = [], settings?: any) => {
    const { scanFolderForImages } = require('./utils/imageBatcher');
    const webContents = (event?.sender);
    const sendProgress = (update: { stage: string; current: number; total: number; path?: string }) => {
      try {
        webContents?.send('scan-progress', update);
      } catch {}
    };
    const allPhotos = await scanFolderForImages(
      folderPath,
      includeSubfolders,
      settings?.sortingMode || 'dateTaken',
      sendProgress,
      buildImageScanOptions(settings)
    );
    if (processedPhotos && processedPhotos.length) {
      return allPhotos.filter((p: any) => !processedPhotos.includes(p.path));
    }
    return allPhotos;
  });

  // Flat scan for videos (no batching) with optional processed filtering
  ipcMain.handle('scan-folder-videos', async (event, folderPath: string, includeSubfolders: boolean = true, processedVideos: string[] = [], settings?: any) => {
    const { scanFolderForVideos } = require('./utils/videoBatcher');
    const webContents = (event?.sender);
    const sendProgress = (update: { stage: string; current: number; total: number; path?: string }) => {
      try {
        webContents?.send('scan-progress', update);
      } catch {}
    };
    const allVideos = await scanFolderForVideos(
      folderPath,
      includeSubfolders,
      sendProgress,
      buildVideoScanOptions(settings)
    );
    if (processedVideos && processedVideos.length) {
      return allVideos.filter((v: any) => !processedVideos.includes(v.path));
    }
    return allVideos;
  });

  // Handle getting file size (check both original location and _delete folder)
  ipcMain.handle('get-file-size', async (event, filePath: string) => {
    try {
      // First try the original path
      const stats = await fs.promises.stat(filePath);
      return stats.size;
    } catch (error) {
      // If original path doesn't exist, try the _delete folder
      try {
        const rootDir = path.dirname(filePath);
        const fileName = path.basename(filePath);
        const deleteFolder = path.join(rootDir, '_delete');
        const deletePath = path.join(deleteFolder, fileName);
        
        const stats = await fs.promises.stat(deletePath);
        return stats.size;
      } catch (deleteError) {
        console.error('Error getting file size for:', filePath, deleteError);
        return 0;
      }
    }
  });

  // Compute stats for all files currently in any '_delete' folder under the given root
  ipcMain.handle('get-delete-stats', async (event, folderPath: string) => {
    async function walk(dir: string): Promise<{ count: number; bytes: number }> {
      let totalCount = 0;
      let totalBytes = 0;
      try {
        const entries = await fs.promises.readdir(dir);
        for (const entry of entries) {
          const full = path.join(dir, entry);
          const st = await fs.promises.stat(full);
          if (st.isDirectory()) {
            if (entry === '_delete') {
              // Sum all files recursively under this _delete folder
              const stack: string[] = [full];
              while (stack.length) {
                const d = stack.pop() as string;
                const items = await fs.promises.readdir(d);
                for (const it of items) {
                  const p = path.join(d, it);
                  const s = await fs.promises.stat(p);
                  if (s.isDirectory()) stack.push(p);
                  else if (s.isFile()) {
                    totalCount += 1;
                    totalBytes += s.size;
                  }
                }
              }
            } else {
              const sub = await walk(full);
              totalCount += sub.count;
              totalBytes += sub.bytes;
            }
          }
        }
      } catch (e) {
        console.warn('Error walking for delete stats:', dir, e);
      }
      return { count: totalCount, bytes: totalBytes };
    }

    return await walk(folderPath);
  });

  // Detailed delete stats separating images and videos
  ipcMain.handle('get-delete-stats-detailed', async (event, folderPath: string) => {
    let imageCount = 0;
    let videoCount = 0;
    let bytes = 0;
    try {
      const { isImageFile } = require('./utils/imageBatcher');
      const { isVideoFile } = require('./utils/videoBatcher');

      const stack: string[] = [folderPath];
      while (stack.length) {
        const dir = stack.pop() as string;
        let entries: string[] = [];
        try {
          entries = await fs.promises.readdir(dir);
        } catch { continue; }
        for (const entry of entries) {
          const full = path.join(dir, entry);
          let st: fs.Stats;
          try { st = await fs.promises.stat(full); } catch { continue; }
          if (st.isDirectory()) {
            if (entry === '_delete') {
              // walk this _delete directory fully
              const s2: string[] = [full];
              while (s2.length) {
                const d = s2.pop() as string;
                let items: string[] = [];
                try { items = await fs.promises.readdir(d); } catch { continue; }
                for (const it of items) {
                  const p = path.join(d, it);
                  let s: fs.Stats;
                  try { s = await fs.promises.stat(p); } catch { continue; }
                  if (s.isDirectory()) s2.push(p);
                  else if (s.isFile()) {
                    bytes += s.size;
                    if (isImageFile(it)) imageCount += 1;
                    else if (isVideoFile(it)) videoCount += 1;
                  }
                }
              }
            } else {
              stack.push(full);
            }
          }
        }
      }
    } catch (e) {
      console.warn('get-delete-stats-detailed failed:', e);
    }
    return { imageCount, videoCount, bytes };
  });

  // Handle photo/video processing: move items to _delete (local) or trash via Immich API.
  ipcMain.handle('process-photos', async (event, { selectedPhotos, photosToDelete, sourceType, rootFolderPath, includeSubfolders }) => {
    const results: Array<{
      fromPath: string;
      toPath: string;
      status: 'moved' | 'skipped' | 'error';
      reason?: string;
      assetId?: string;
    }> = [];

    if (sourceType === 'immich-external') {
      const config = sessionImmichConfig;
      const rootFolder = rootFolderPath || sessionImmichRootFolder;
      const scanSubfolders = includeSubfolders ?? sessionImmichIncludeSubfolders;
      if (!config?.serverUrl || !config?.apiKey) {
        for (const item of photosToDelete || []) {
          results.push({
            fromPath: item.path,
            toPath: item.path,
            status: 'error',
            reason: 'immich-not-configured',
          });
        }
        return results;
      }

      const client = createImmichClient(config);
      const resolvedItems: Array<{ path: string; assetId: string }> = [];

      for (const item of photosToDelete || []) {
        const itemPath: string = item.path;
        let assetId: string | undefined = item.assetId;
        if (!assetId && rootFolder) {
          try {
            const match = await client.resolveAssetForLocalPath(itemPath, rootFolder, scanSubfolders);
            assetId = match.assetId;
          } catch (error) {
            console.warn('[process-photos] Immich match failed for', itemPath, error);
          }
        }
        if (!assetId) {
          console.warn('[process-photos] missing Immich asset id for', itemPath);
          results.push({
            fromPath: itemPath,
            toPath: itemPath,
            status: 'error',
            reason: 'missing-asset-id',
          });
          continue;
        }
        resolvedItems.push({ path: itemPath, assetId });
      }

      if (resolvedItems.length) {
        const assetIds = resolvedItems.map((item) => item.assetId);
        const trashResult = await client.trashAssets(assetIds);
        for (const item of resolvedItems) {
          if (trashResult.ok) {
            results.push({
              fromPath: item.path,
              toPath: item.path,
              status: 'moved',
              assetId: item.assetId,
            });
          } else {
            results.push({
              fromPath: item.path,
              toPath: item.path,
              status: 'error',
              reason: trashResult.error,
              assetId: item.assetId,
            });
          }
        }
      }
      return results;
    }

    for (const item of photosToDelete || []) {
      try {
        const itemPath: string = item.path;
        const rootDir = path.dirname(itemPath);
        const deleteFolder = path.join(rootDir, '_delete');
        const fileName = path.basename(itemPath);

        if (!fs.existsSync(deleteFolder)) {
          fs.mkdirSync(deleteFolder);
        }

        if (!fs.existsSync(itemPath)) {
          const r = { fromPath: itemPath, toPath: path.join(deleteFolder, fileName), status: 'skipped' as const, reason: 'source-missing' };
          results.push(r);
          console.warn('[process-photos] skip source-missing:', itemPath);
          continue;
        }

        if (itemPath.includes(`${path.sep}_delete${path.sep}`)) {
          const r = { fromPath: itemPath, toPath: itemPath, status: 'skipped' as const, reason: 'already-in-delete' };
          results.push(r);
          continue;
        }

        const uniqueTargetPath = ensureUniquePath(deleteFolder, fileName);
        const { ok, error } = await renameWithRetry(itemPath, uniqueTargetPath);
        if (ok) {
          results.push({ fromPath: itemPath, toPath: uniqueTargetPath, status: 'moved' });
          if (process.env.NODE_ENV === 'development') {
            console.log('[process-photos] Moved to _delete:', itemPath, '->', uniqueTargetPath);
          }
        } else {
          results.push({ fromPath: itemPath, toPath: uniqueTargetPath, status: 'error', reason: error });
          console.warn('[process-photos] move error:', itemPath, error);
        }
      } catch (error: any) {
        const r = { fromPath: (item as any)?.path ?? 'unknown', toPath: 'unknown', status: 'error' as const, reason: String(error?.message || error) };
        results.push(r);
        console.warn('[process-photos] handler error:', r.reason);
      }
    }
    return results;
  });

  // Handle moving photos/videos to _favorites folder (same layout as _delete, different target)
  ipcMain.handle('move-to-favorites', async (event, { itemsToFavorite }: { itemsToFavorite: Array<{ path: string }> }) => {
    const results: Array<{ fromPath: string; toPath: string; status: 'moved' | 'skipped' | 'error'; reason?: string }> = [];
    for (const item of itemsToFavorite || []) {
      try {
        const itemPath: string = item.path;
        const rootDir = path.dirname(itemPath);
        const favoritesFolder = path.join(rootDir, '_favorites');
        const fileName = path.basename(itemPath);

        if (!fs.existsSync(favoritesFolder)) {
          fs.mkdirSync(favoritesFolder);
        }

        if (!fs.existsSync(itemPath)) {
          results.push({ fromPath: itemPath, toPath: path.join(favoritesFolder, fileName), status: 'skipped', reason: 'source-missing' });
          console.warn('[move-to-favorites] skip source-missing:', itemPath);
          continue;
        }

        if (itemPath.includes(`${path.sep}_favorites${path.sep}`)) {
          results.push({ fromPath: itemPath, toPath: itemPath, status: 'skipped', reason: 'already-in-favorites' });
          continue;
        }

        const uniqueTargetPath = ensureUniquePath(favoritesFolder, fileName);
        const { ok, error } = await renameWithRetry(itemPath, uniqueTargetPath);
        if (ok) {
          results.push({ fromPath: itemPath, toPath: uniqueTargetPath, status: 'moved' });
          if (process.env.NODE_ENV === 'development') {
            console.log('[move-to-favorites] Moved to _favorites:', itemPath, '->', uniqueTargetPath);
          }
        } else {
          results.push({ fromPath: itemPath, toPath: uniqueTargetPath, status: 'error', reason: error });
          console.warn('[move-to-favorites] move error:', itemPath, error);
        }
      } catch (error: any) {
        results.push({ fromPath: (item as any)?.path ?? 'unknown', toPath: 'unknown', status: 'error', reason: String(error?.message || error) });
        console.warn('[move-to-favorites] handler error:', (error as any)?.message ?? error);
      }
    }
    return results;
  });

  // Handle restoring a moved photo from _delete/_favorites (local) or Immich trash (API undo)
  ipcMain.handle('restore-photo', async (event, payload: { photo?: any; fromPath: string; toPath: string; assetId?: string; sourceType?: string }) => {
    try {
      if (payload.sourceType === 'immich-external') {
        const assetId = payload.assetId || payload.photo?.assetId;
        if (!assetId) {
          throw new Error('Missing assetId for Immich restore');
        }
        const config = sessionImmichConfig;
        if (!config?.serverUrl || !config?.apiKey) {
          throw new Error('Immich connection not configured');
        }
        const client = createImmichClient(config);
        const result = await client.restoreAssets([assetId]);
        if (!result.ok) {
          throw new Error(result.error || 'Immich restore failed');
        }
        return;
      }

      const { fromPath, toPath } = payload;
      // If the toPath exists (in _delete) and original fromPath does not, move back
      if (fs.existsSync(toPath) && !fs.existsSync(fromPath)) {
        // Ensure parent directory exists
        const parent = path.dirname(fromPath);
        if (!fs.existsSync(parent)) {
          fs.mkdirSync(parent, { recursive: true });
        }
        fs.renameSync(toPath, fromPath);
      }
    } catch (error) {
      console.error('Error restoring photo:', error);
      throw error;
    }
  });
}

// This method will be called when Electron has finished
// initialization and is ready to create browser windows.
app.whenReady().then(() => {
  setupIpcHandlers();
  createWindow();
});

// Quit when all windows are closed.
app.on('window-all-closed', () => {
  // On macOS it is common for applications and their menu bar
  // to stay active until the user quits explicitly with Cmd + Q
  if (process.platform !== 'darwin') {
    app.quit();
  }
});

app.on('activate', () => {
  // On macOS it's common to re-create a window in the app when the
  // dock icon is clicked and there are no other windows open.
  if (BrowserWindow.getAllWindows().length === 0) {
    createWindow();
  }
}); 