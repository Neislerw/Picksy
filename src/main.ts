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
  ipcMain.handle('scan-folder', async (event, folderPath: string, includeSubfolders: boolean = true, processedPhotos: string[] = []) => {
    // Import the imageBatcher utility
    const { scanFolderAndCreateBatches } = require('./utils/imageBatcher');
    return await scanFolderAndCreateBatches(folderPath, undefined, undefined, undefined, includeSubfolders, processedPhotos);
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
    try {
      for (const photo of photosToDelete) {
        const photoPath = photo.path;
        // Get the root folder (where the photos were selected from)
        const rootDir = path.dirname(photoPath);
        const deleteFolder = path.join(rootDir, '_delete');
        const fileName = path.basename(photoPath);
        const newPath = path.join(deleteFolder, fileName);

        // Create _delete folder if it doesn't exist
        if (!fs.existsSync(deleteFolder)) {
          fs.mkdirSync(deleteFolder);
        }

        // Only move the file if it exists and isn't already in the _delete folder
        if (fs.existsSync(photoPath) && !photoPath.includes('_delete')) {
          fs.renameSync(photoPath, newPath);
        }
      }
    } catch (error) {
      console.error('Error processing photos:', error);
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