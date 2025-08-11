import React, { useState, useCallback } from 'react';
import BatchSelector from './components/BatchSelector';
import PhotoPairViewer from './components/PhotoPairViewer';
import ThumbnailStripCuller from './components/ThumbnailStripCuller';
import CompletionPopup from './components/CompletionPopup';
import { Photo, PhotoBatch, SaveState } from '../types';
import './styles/App.css';

interface UndoEntry {
  batchIndex: number;
  selectedPhotos: Photo[];
  photosToDelete: Photo[];
  moveResults: Array<{ fromPath: string; toPath: string; status: 'moved' | 'skipped' | 'error'; reason?: string }>; 
}

const App: React.FC = () => {
  const [batches, setBatches] = useState<PhotoBatch[]>([]);
  const [currentBatchIndex, setCurrentBatchIndex] = useState<number>(-1);
  const [isLoading, setIsLoading] = useState(false);
  const [saveState, setSaveState] = useState<SaveState | null>(null);
  const [showResumePrompt, setShowResumePrompt] = useState(false);
  const [selectedFolderPath, setSelectedFolderPath] = useState<string>('');
  const [showCompletionPopup, setShowCompletionPopup] = useState(false);
  const [completionStats, setCompletionStats] = useState<{
    totalPhotos: number;
    totalBatches: number;
    keptPhotos: number;
    deletedPhotos: number;
    deletedSize: number;
  } | null>(null);
  const [selectedMode, setSelectedMode] = useState<'tournament' | 'thumbnail'>('tournament');
  const [showSkipProcessedPrompt, setShowSkipProcessedPrompt] = useState(false);
  const [flatPhotos, setFlatPhotos] = useState<Photo[]>([]);
  const [includeSubfoldersSelected, setIncludeSubfoldersSelected] = useState<boolean>(true);
  const [thumbnailSessionComplete, setThumbnailSessionComplete] = useState<boolean>(false);
  const [undoStack, setUndoStack] = useState<UndoEntry[]>([]);
  const [isUndoing, setIsUndoing] = useState<boolean>(false);

  const handleFolderSelect = async (
    folderPath: string,
    includeSubfolders: boolean,
    settings: any,
    mode: 'tournament' | 'thumbnail'
  ) => {
    setIsLoading(true);
    try {
      setSelectedMode(mode);
      setSelectedFolderPath(folderPath);
      setIncludeSubfoldersSelected(includeSubfolders);
      // Check if save state exists
      const hasSaveState = await window.electron?.ipcRenderer.invoke('save-state-exists', folderPath);
      
      if (hasSaveState) {
        // Load existing save state and show resume prompt
        const existingSaveState = await window.electron?.ipcRenderer.invoke('load-save-state', folderPath);
        setSaveState(existingSaveState);
        if (mode === 'thumbnail') {
          // For thumbnail mode, ask whether to skip processed photos instead of resume
          setShowSkipProcessedPrompt(true);
        } else {
          setShowResumePrompt(true);
        }
      } else {
        // No save state
        if (mode === 'thumbnail') {
          const photos = await window.electron?.ipcRenderer.invoke('scan-folder-photos', folderPath, includeSubfolders, []);
          setFlatPhotos(photos || []);
        } else {
          await startProcessing(folderPath, includeSubfolders, [], settings);
        }
      }
    } catch (error) {
      console.error('Error checking save state:', error);
      // Fallback: load something sensible for the chosen mode
      if (mode === 'thumbnail') {
        const photos = await window.electron?.ipcRenderer.invoke('scan-folder-photos', folderPath, includeSubfolders, []);
        setFlatPhotos(photos || []);
      } else {
        await startProcessing(folderPath, includeSubfolders, [], settings);
      }
    } finally {
      setIsLoading(false);
    }
  };

  const handleSkipProcessedYes = async () => {
    // Skip processed photos from the save state
    if (saveState) {
      // For thumbnail mode, we need a flat photo list filtered by processed
      const photos = await window.electron?.ipcRenderer.invoke('scan-folder-photos', saveState.folderPath, includeSubfoldersSelected, saveState.processedPhotos);
      setFlatPhotos(photos || []);
      setShowSkipProcessedPrompt(false);
    }
  };

  const handleSkipProcessedNo = async () => {
    // Include all photos regardless of previous processing
    if (saveState) {
      const photos = await window.electron?.ipcRenderer.invoke('scan-folder-photos', saveState.folderPath, includeSubfoldersSelected, []);
      setFlatPhotos(photos || []);
      setShowSkipProcessedPrompt(false);
    }
  };

  const startProcessing = async (folderPath: string, includeSubfolders: boolean, processedPhotos: string[] = [], settings?: any) => {
    setIsLoading(true);
    try {
      // Create new save state if none exists (do this first)
      if (!saveState) {
        const newSaveState = {
          folderPath,
          processedPhotos,
          selections: {}
        };
        
        // Save the initial save state to disk first
        try {
          console.log('Creating initial save state for:', folderPath);
          await window.electron?.ipcRenderer.invoke('save-save-state', newSaveState);
          console.log('Initial save state created successfully');
          setSaveState(newSaveState);
        } catch (error) {
          console.error('Failed to create initial save state:', error);
        }
      }
      
      // Call the main process to scan the folder and create batches
      const newBatches = await window.electron?.ipcRenderer.invoke('scan-folder', folderPath, includeSubfolders, processedPhotos, settings);
      if (newBatches && newBatches.length > 0) {
        setBatches(newBatches);
        setCurrentBatchIndex(0);
      }
    } catch (error) {
      console.error('Error scanning folder:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const handleResume = async () => {
    if (saveState) {
      await startProcessing(saveState.folderPath, includeSubfoldersSelected, saveState.processedPhotos);
      setShowResumePrompt(false);
    }
  };

  const handleStartOver = async () => {
    if (saveState) {
      // Delete the save state file
      try {
        await window.electron?.ipcRenderer.invoke('delete-save-state', saveState.folderPath);
      } catch (error) {
        console.warn('Failed to delete save state:', error);
      }
      
      // Start fresh
      await startProcessing(saveState.folderPath, includeSubfoldersSelected);
      setSaveState(null);
      setShowResumePrompt(false);
    }
  };

  const handleCompletionClose = () => {
    setShowCompletionPopup(false);
    setCompletionStats(null);
    setBatches([]);
    setCurrentBatchIndex(-1);
    setSaveState(null);
  };

  const handlePhotoSelection = async (selectedPhotos: Photo[], photosToDelete: Photo[]) => {
    if (!saveState) return;

    // Update save state with selections
    const updatedSaveState = { ...saveState };
    
    // Mark selected photos as kept
    for (const photo of selectedPhotos) {
      updatedSaveState.processedPhotos.push(photo.path);
      updatedSaveState.selections[photo.path] = 'kept';
    }
    
    // Mark deleted photos as discarded
    for (const photo of photosToDelete) {
      updatedSaveState.processedPhotos.push(photo.path);
      updatedSaveState.selections[photo.path] = 'discarded';
    }
    
    // Save updated state
    try {
      await window.electron?.ipcRenderer.invoke('save-save-state', updatedSaveState);
      setSaveState(updatedSaveState);
    } catch (error) {
      console.error('Failed to save state:', error);
    }

    // Handle the selected photos (keep them in place) and move others to delete folder
    try {
      const results: Array<{ fromPath: string; toPath: string; status: 'moved' | 'skipped' | 'error'; reason?: string }> | undefined = await window.electron?.ipcRenderer.invoke('process-photos', {
        selectedPhotos,
        photosToDelete
      });
      console.log('[UNDO] process-photos results count:', results?.length || 0);
      // Push to undo stack so we can restore across batches
      setUndoStack(prev => [
        ...prev,
        {
          batchIndex: currentBatchIndex,
          selectedPhotos,
          photosToDelete,
          moveResults: results || []
        }
      ]);
      console.log('[UNDO] Pushed undo entry. Stack size now:', (undoStack.length + 1));
    } catch (err) {
      console.error('process-photos failed:', err);
    }
  };

  const handleBatchComplete = useCallback(async () => {
    console.log('handleBatchComplete called', { currentBatchIndex, batchesLength: batches.length });
    // Move to next batch if available
    if (currentBatchIndex < batches.length - 1) {
      console.log('Moving to next batch', currentBatchIndex + 1);
      setCurrentBatchIndex(prev => prev + 1);
    } else {
      console.log('All batches complete, calculating stats');
      // All batches are complete - calculate stats
      const totalPhotos = batches.reduce((sum, batch) => sum + batch.photos.length, 0);
      
      // Count kept and deleted photos from save state
      let keptPhotos = 0;
      let deletedPhotos = 0;
      if (saveState) {
        for (const path of saveState.processedPhotos) {
          if (saveState.selections[path] === 'kept') {
            keptPhotos++;
          } else if (saveState.selections[path] === 'discarded') {
            deletedPhotos++;
          }
        }
      }
      
      // Calculate deleted file sizes
      let deletedSize = 0;
      if (saveState) {
        const deletedPaths = saveState.processedPhotos.filter(path => 
          saveState.selections[path] === 'discarded'
        );
        for (const path of deletedPaths) {
          try {
            const size = await window.electron?.ipcRenderer.invoke('get-file-size', path);
            deletedSize += size || 0;
          } catch (error) {
            console.error('Error getting file size for:', path, error);
          }
        }
      }
      
      const stats = {
        totalPhotos,
        totalBatches: batches.length,
        keptPhotos,
        deletedPhotos,
        deletedSize
      };
      
      console.log('Completion stats:', {
        totalPhotos,
        totalBatches: batches.length,
        keptPhotos,
        deletedPhotos,
        deletedSize,
        saveStateProcessedCount: saveState?.processedPhotos.length || 0,
        saveStateSelections: saveState?.selections || {}
      });
      
      setCompletionStats(stats);
      setShowCompletionPopup(true);
    }
  }, [currentBatchIndex, batches.length, saveState]);

  // Global undo (Ctrl+Z / Cmd+Z): restore last completed batch action
  const handleGlobalUndo = useCallback(async () => {
    if (isUndoing) return;
    setIsUndoing(true);
    console.log('[UNDO] Global undo invoked. Stack size:', undoStack.length);
    if (!undoStack.length || !saveState) {
      console.warn('[UNDO] Nothing to undo or missing saveState');
      setIsUndoing(false);
      return;
    }
    const last = undoStack[undoStack.length - 1];
    console.log('[UNDO] Last entry:', {
      batchIndex: last.batchIndex,
      selectedCount: last.selectedPhotos.length,
      deleteCount: last.photosToDelete.length,
      moveResults: last.moveResults?.length || 0
    });

    // 1) Restore moved files from _delete
    for (const r of last.moveResults) {
      if (r.status === 'moved') {
        try {
          await window.electron?.ipcRenderer.invoke('restore-photo', { photo: null, fromPath: r.fromPath, toPath: r.toPath });
          console.log('[UNDO] Restored', r.toPath, '->', r.fromPath);
        } catch (e) {
          console.warn('Failed to restore during undo:', r.fromPath, e);
        }
      }
    }

    // 2) Revert save state entries for this batch (both kept and deleted)
    const pathsToRevert = new Set<string>([...last.selectedPhotos.map(p => p.path), ...last.photosToDelete.map(p => p.path)]);
    const newProcessed = (saveState.processedPhotos || []).filter(p => !pathsToRevert.has(p));
    const newSelections = { ...saveState.selections } as Record<string, 'kept' | 'discarded'>;
    for (const p of pathsToRevert) {
      delete newSelections[p];
    }
    const reverted: SaveState = { ...saveState, processedPhotos: newProcessed, selections: newSelections };

    try {
      await window.electron?.ipcRenderer.invoke('save-save-state', reverted);
      setSaveState(reverted);
      console.log('[UNDO] Save state reverted for', pathsToRevert.size, 'paths');
    } catch (e) {
      console.error('Failed to persist reverted save state:', e);
    }

    // 3) Navigate back to the undone batch so the user can redo it
    setCurrentBatchIndex(last.batchIndex);
    if (showCompletionPopup) {
      setShowCompletionPopup(false);
      setCompletionStats(null);
    }

    // 4) Pop from undo stack
    setUndoStack(prev => prev.slice(0, -1));
    console.log('[UNDO] Popped undo entry. New stack size:', Math.max(0, undoStack.length - 1));
    setIsUndoing(false);
  }, [undoStack, saveState, showCompletionPopup, isUndoing]);

  // Global keybinding fallback: Z triggers undo regardless of which child is mounted
  React.useEffect(() => {
    const onKeyDown = (e: KeyboardEvent) => {
      if (!e.ctrlKey && !e.metaKey && e.code === 'KeyZ' && selectedMode === 'tournament') {
        e.preventDefault();
        void handleGlobalUndo();
      }
    };
    window.addEventListener('keydown', onKeyDown);
    return () => window.removeEventListener('keydown', onKeyDown);
  }, [handleGlobalUndo, selectedMode]);

  // Note: Z key for undo is handled inside PhotoPairViewer and routed here via onUndoLastAction

  const currentBatch = batches[currentBatchIndex];
  
  console.log('App render', { 
    currentBatchIndex, 
    batchesLength: batches.length, 
    currentBatchExists: !!currentBatch,
    showResumePrompt
  });

  // Safety net: if in tournament mode and all photos across all batches are processed, show completion
  React.useEffect(() => {
    if (selectedMode !== 'tournament' || showCompletionPopup || batches.length === 0 || !saveState) return;
    const allPaths = new Set<string>();
    for (const b of batches) {
      for (const p of b.photos) allPaths.add(p.path);
    }
    const processedCount = saveState.processedPhotos.filter(p => allPaths.has(p)).length;
    if (processedCount >= allPaths.size && allPaths.size > 0) {
      (async () => {
        const totalPhotos = allPaths.size;
        let keptPhotos = 0;
        let deletedPhotos = 0;
        for (const path of saveState.processedPhotos) {
          if (!allPaths.has(path)) continue;
          if (saveState.selections[path] === 'kept') keptPhotos++;
          else if (saveState.selections[path] === 'discarded') deletedPhotos++;
        }
        let deletedSize = 0;
        const deletedPaths = saveState.processedPhotos.filter(path => allPaths.has(path) && saveState.selections[path] === 'discarded');
        for (const path of deletedPaths) {
          try {
            const size = await window.electron?.ipcRenderer.invoke('get-file-size', path);
            deletedSize += size || 0;
          } catch {}
        }
        setCompletionStats({ totalPhotos, totalBatches: batches.length, keptPhotos, deletedPhotos, deletedSize });
        setShowCompletionPopup(true);
      })();
    }
  }, [selectedMode, showCompletionPopup, batches, saveState]);

  // Show resume prompt if save state exists
  if (showResumePrompt) {
    return (
      <div className="app">
        <div className="resume-prompt">
          <h2>Resume Previous Session?</h2>
          <p>A previous culling session was found for this folder.</p>
          <p>Would you like to resume where you left off, or start over?</p>
          <div className="resume-buttons">
            <button onClick={handleResume} disabled={isLoading}>
              {isLoading ? 'Loading...' : 'Resume'}
            </button>
            <button onClick={handleStartOver} disabled={isLoading}>
              Start Over
            </button>
          </div>
        </div>
      </div>
    );
  }

  // Show skip processed prompt for thumbnail mode
  if (showSkipProcessedPrompt) {
    return (
      <div className="app">
        <div className="resume-prompt">
          <h2>Skip Photos Already Processed?</h2>
          <p>We found a previous Tournament Mode session for this folder.</p>
          <p>Would you like to hide photos that were already processed?</p>
          <div className="resume-buttons">
            <button onClick={handleSkipProcessedYes} disabled={isLoading}>
              {isLoading ? 'Loading...' : 'Skip Processed'}
            </button>
            <button onClick={handleSkipProcessedNo} disabled={isLoading}>
              Show All
            </button>
          </div>
        </div>
      </div>
    );
  }

  const shouldShowThumbnail = selectedMode === 'thumbnail' && flatPhotos.length > 0 && !thumbnailSessionComplete;

  return (
    <div className="app">
      {shouldShowThumbnail ? (
        <ThumbnailStripCuller 
          folderPath={selectedFolderPath} 
          photos={flatPhotos} 
          onExit={() => {
            // Clear thumbnail state and go back to start
            setFlatPhotos([]);
            setBatches([]);
            setCurrentBatchIndex(-1);
            setSaveState(null);
          }}
          onComplete={() => setThumbnailSessionComplete(true)}
        />
      ) : currentBatchIndex !== -1 && currentBatch && selectedMode === 'tournament' ? (
        <PhotoPairViewer
          batch={currentBatch}
          currentBatchIndex={currentBatchIndex}
          totalBatches={batches.length}
          onSelection={handlePhotoSelection}
          onBatchComplete={handleBatchComplete}
          onUndoLastAction={handleGlobalUndo}
        />
      ) : (
        <BatchSelector 
          onFolderSelect={handleFolderSelect}
          isLoading={isLoading}
        />
      )}

      {showCompletionPopup && completionStats && (
        <CompletionPopup
          stats={completionStats}
          onClose={handleCompletionClose}
          extraActionLabel={selectedMode === 'tournament' ? 'Open in Thumbnail Mode (Skip Processed)' : undefined}
          onExtraActionClick={selectedMode === 'tournament' ? async () => {
            // Switch to thumbnail mode and load photos skipping processed
            setSelectedMode('thumbnail');
            const folderPath = saveState?.folderPath || selectedFolderPath;
            const processed = saveState?.processedPhotos || [];
            const photos = await window.electron?.ipcRenderer.invoke('scan-folder-photos', folderPath, includeSubfoldersSelected, processed);
            setFlatPhotos(photos || []);
            setShowCompletionPopup(false);
          } : undefined}
        />
      )}

      {selectedMode === 'thumbnail' && thumbnailSessionComplete && (
        <CompletionPopup
          stats={{
            totalPhotos: flatPhotos.length,
            totalBatches: 1,
            keptPhotos: flatPhotos.length - 0, // kept is whatever not deleted; we are not tracking per save here
            deletedPhotos: 0,
            deletedSize: 0
          }}
          title={'Thumbnail Session Complete!'}
          onClose={() => {
            setThumbnailSessionComplete(false);
            setFlatPhotos([]);
            setBatches([]);
            setCurrentBatchIndex(-1);
            setSaveState(null);
          }}
        />
      )}
    </div>
  );
};

export default App;