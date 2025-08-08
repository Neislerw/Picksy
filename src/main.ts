import { app, BrowserWindow, ipcMain, dialog } from 'electron';
import * as path from 'path';
import * as fs from 'fs';

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
  // Handle folder selection
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
    
    return await scanFolderAndCreateBatches(folderPath, timeWindow, minBatchSize, maxBatchSize, includeSubfolders, processedPhotos, sortingMode);
  });

  // Flat scan for photos (no batching) with optional processed filtering
  ipcMain.handle('scan-folder-photos', async (event, folderPath: string, includeSubfolders: boolean = true, processedPhotos: string[] = []) => {
    const { scanFolderForImages } = require('./utils/imageBatcher');
    const allPhotos = await scanFolderForImages(folderPath, includeSubfolders);
    if (processedPhotos && processedPhotos.length) {
      return allPhotos.filter((p: any) => !processedPhotos.includes(p.path));
    }
    return allPhotos;
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

  // Handle photo processing
  ipcMain.handle('process-photos', async (event, { selectedPhotos, photosToDelete }) => {
    const results: Array<{ fromPath: string; toPath: string; status: 'moved' | 'skipped' | 'error'; reason?: string }> = [];
    for (const photo of photosToDelete || []) {
      try {
        const photoPath: string = photo.path;
        const rootDir = path.dirname(photoPath);
        const deleteFolder = path.join(rootDir, '_delete');
        const fileName = path.basename(photoPath);

        if (!fs.existsSync(deleteFolder)) {
          fs.mkdirSync(deleteFolder);
        }

        if (!fs.existsSync(photoPath)) {
          results.push({ fromPath: photoPath, toPath: path.join(deleteFolder, fileName), status: 'skipped', reason: 'source-missing' });
          continue;
        }

        if (photoPath.includes(`${path.sep}_delete${path.sep}`)) {
          results.push({ fromPath: photoPath, toPath: photoPath, status: 'skipped', reason: 'already-in-delete' });
          continue;
        }

        const uniqueTargetPath = ensureUniquePath(deleteFolder, fileName);
        try {
          fs.renameSync(photoPath, uniqueTargetPath);
          results.push({ fromPath: photoPath, toPath: uniqueTargetPath, status: 'moved' });
        } catch (moveErr: any) {
          results.push({ fromPath: photoPath, toPath: uniqueTargetPath, status: 'error', reason: String(moveErr?.message || moveErr) });
        }
      } catch (error: any) {
        results.push({ fromPath: (photo as any)?.path ?? 'unknown', toPath: 'unknown', status: 'error', reason: String(error?.message || error) });
      }
    }
    return results;
  });

  // Handle restoring a moved photo from _delete back to original path (for undo)
  ipcMain.handle('restore-photo', async (event, payload: { photo: any; fromPath: string; toPath: string }) => {
    try {
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