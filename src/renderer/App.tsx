import React, { useState, useCallback } from 'react';
import BatchSelector from './components/BatchSelector';
import PhotoPairViewer from './components/PhotoPairViewer';
import ThumbnailStripCuller from './components/ThumbnailStripCuller';
import VideoMode from './components/VideoMode';
import CompletionPopup from './components/CompletionPopup';
import { Photo, PhotoBatch, SaveState, Video } from '../types';
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
    totalPhotosProcessed: number;
    photosDeleted: number;
    videosProcessed: number;
    totalSpaceSaved: number;
  } | null>(null);
  const [selectedMode, setSelectedMode] = useState<'tournament' | 'thumbnail' | 'video'>('tournament');
  const [showSkipProcessedPrompt, setShowSkipProcessedPrompt] = useState(false);
  const [flatPhotos, setFlatPhotos] = useState<Photo[]>([]);
  const [flatVideos, setFlatVideos] = useState<Video[]>([]);
  const [includeSubfoldersSelected, setIncludeSubfoldersSelected] = useState<boolean>(true);
  const [thumbnailSessionComplete, setThumbnailSessionComplete] = useState<boolean>(false);
  const [videoSessionComplete, setVideoSessionComplete] = useState<boolean>(false);
  const [undoStack, setUndoStack] = useState<UndoEntry[]>([]);
  const [isUndoing, setIsUndoing] = useState<boolean>(false);

  const handleFolderSelect = async (
    folderPath: string,
    includeSubfolders: boolean,
    settings: any,
    mode: 'tournament' | 'thumbnail' | 'video'
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
        if (mode === 'thumbnail' || mode === 'video') {
          // For thumbnail/video modes, ask whether to skip processed items instead of resume
          setShowSkipProcessedPrompt(true);
        } else {
          setShowResumePrompt(true);
        }
      } else {
        // No save state – create one for thumbnail/video so Keep/Delete persist
        if (mode === 'thumbnail') {
          const newSaveState = { folderPath, processedPhotos: [] as string[], selections: {} as Record<string, 'kept' | 'discarded'> };
          try {
            await window.electron?.ipcRenderer.invoke('save-save-state', newSaveState);
            setSaveState(newSaveState);
          } catch (e) {
            console.warn('Failed to create initial save state for thumbnail:', e);
          }
          const photos = await window.electron?.ipcRenderer.invoke('scan-folder-photos', folderPath, includeSubfolders, []);
          setFlatPhotos(photos || []);
        } else if (mode === 'video') {
          const newSaveState = { folderPath, processedPhotos: [] as string[], selections: {} as Record<string, 'kept' | 'discarded'> };
          try {
            await window.electron?.ipcRenderer.invoke('save-save-state', newSaveState);
            setSaveState(newSaveState);
          } catch (e) {
            console.warn('Failed to create initial save state for video:', e);
          }
          const videos = await window.electron?.ipcRenderer.invoke('scan-folder-videos', folderPath, includeSubfolders, []);
          setFlatVideos(videos || []);
        } else {
          await startProcessing(folderPath, includeSubfolders, [], settings);
        }
      }
    } catch (error) {
      console.error('Error checking save state:', error);
      // Fallback: create save state for thumbnail/video if needed, then load
      if (mode === 'thumbnail') {
        const newSaveState = { folderPath, processedPhotos: [] as string[], selections: {} as Record<string, 'kept' | 'discarded'> };
        try {
          await window.electron?.ipcRenderer.invoke('save-save-state', newSaveState);
          setSaveState(newSaveState);
        } catch (e) { /* ignore */ }
        const photos = await window.electron?.ipcRenderer.invoke('scan-folder-photos', folderPath, includeSubfolders, []);
        setFlatPhotos(photos || []);
      } else if (mode === 'video') {
        const newSaveState = { folderPath, processedPhotos: [] as string[], selections: {} as Record<string, 'kept' | 'discarded'> };
        try {
          await window.electron?.ipcRenderer.invoke('save-save-state', newSaveState);
          setSaveState(newSaveState);
        } catch (e) { /* ignore */ }
        const videos = await window.electron?.ipcRenderer.invoke('scan-folder-videos', folderPath, includeSubfolders, []);
        setFlatVideos(videos || []);
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
      if (selectedMode === 'thumbnail') {
      const photos = await window.electron?.ipcRenderer.invoke('scan-folder-photos', saveState.folderPath, includeSubfoldersSelected, saveState.processedPhotos);
      setFlatPhotos(photos || []);
      } else if (selectedMode === 'video') {
        const videos = await window.electron?.ipcRenderer.invoke('scan-folder-videos', saveState.folderPath, includeSubfoldersSelected, saveState.processedPhotos);
        setFlatVideos(videos || []);
      }
      setShowSkipProcessedPrompt(false);
    }
  };

  const handleSkipProcessedNo = async () => {
    // Include all photos regardless of previous processing
    if (saveState) {
      if (selectedMode === 'thumbnail') {
      const photos = await window.electron?.ipcRenderer.invoke('scan-folder-photos', saveState.folderPath, includeSubfoldersSelected, []);
      setFlatPhotos(photos || []);
      } else if (selectedMode === 'video') {
        const videos = await window.electron?.ipcRenderer.invoke('scan-folder-videos', saveState.folderPath, includeSubfoldersSelected, []);
        setFlatVideos(videos || []);
      }
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
    setFlatPhotos([]);
    setFlatVideos([]);
    setThumbnailSessionComplete(false);
    setVideoSessionComplete(false);
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
      const totalPhotos = batches.reduce((sum, batch) => sum + batch.photos.length, 0);
      
      // Derive cumulative delete stats across folder (_delete) and estimate per-session kept
      let keptPhotos = 0;
      if (saveState) {
        for (const path of saveState.processedPhotos) {
          if (saveState.selections[path] === 'kept') keptPhotos++;
        }
      }
      const detailed = await window.electron?.ipcRenderer.invoke('get-delete-stats-detailed', selectedFolderPath);
      // Compute requested four fields
      const imagesDeleted = detailed?.imageCount ?? 0;
      const videosDeleted = detailed?.videoCount ?? 0;
      // Estimate processed kept: count kept photos in saveState among images (heuristic: paths with image extensions)
      const imageExts = ['.jpg', '.jpeg', '.png', '.gif', '.bmp', '.tiff', '.webp'];
      const videoExts = ['.mp4', '.mov', '.avi', '.mkv', '.wmv', '.flv', '.webm', '.m4v', '.3gp', '.mpg', '.mpeg'];
      const getExt = (p: string) => p.slice(p.lastIndexOf('.')).toLowerCase();
      let keptImages = 0;
      let keptVideos = 0;
      if (saveState) {
        for (const p of saveState.processedPhotos) {
          if (saveState.selections[p] === 'kept') {
            const ext = getExt(p);
            if (imageExts.includes(ext)) keptImages++;
            else if (videoExts.includes(ext)) keptVideos++;
          }
        }
      }
      const stats = {
        totalPhotosProcessed: keptImages + imagesDeleted,
        photosDeleted: imagesDeleted,
        videosProcessed: keptVideos + videosDeleted,
        totalSpaceSaved: detailed?.bytes ?? 0
      };
      
      console.log('Completion stats:', stats);
      
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
        // Convert to new stats shape using detailed delete stats
        try {
          const detailed = await window.electron?.ipcRenderer.invoke('get-delete-stats-detailed', selectedFolderPath);
          const imagesDeleted = detailed?.imageCount ?? 0;
          const videosDeleted = detailed?.videoCount ?? 0;
          const imageExts = ['.jpg', '.jpeg', '.png', '.gif', '.bmp', '.tiff', '.webp'];
          const videoExts = ['.mp4', '.mov', '.avi', '.mkv', '.wmv', '.flv', '.webm', '.m4v', '.3gp', '.mpg', '.mpeg'];
          const getExt = (p: string) => p.slice(p.lastIndexOf('.')).toLowerCase();
          let keptImages2 = 0;
          let keptVideos2 = 0;
          for (const p of saveState.processedPhotos) {
            if (!allPaths.has(p)) continue;
            if (saveState.selections[p] !== 'kept') continue;
            const ext = getExt(p);
            if (imageExts.includes(ext)) keptImages2++;
            else if (videoExts.includes(ext)) keptVideos2++;
          }
          setCompletionStats({
            totalPhotosProcessed: keptImages2 + imagesDeleted,
            photosDeleted: imagesDeleted,
            videosProcessed: keptVideos2 + videosDeleted,
            totalSpaceSaved: detailed?.bytes ?? 0
          });
        } catch {
          setCompletionStats({
            totalPhotosProcessed: keptPhotos + 0,
            photosDeleted: 0,
            videosProcessed: 0,
            totalSpaceSaved: 0
          });
        }
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
  const shouldShowVideo = selectedMode === 'video' && flatVideos.length > 0 && !videoSessionComplete;

  return (
    <div className="app">
      {shouldShowThumbnail ? (
        <ThumbnailStripCuller 
          folderPath={selectedFolderPath} 
          photos={flatPhotos} 
          onExit={async () => {
            try {
              const detailed = await window.electron?.ipcRenderer.invoke('get-delete-stats-detailed', selectedFolderPath);
              // For thumbnail mode, focus on photos
              setCompletionStats({
                totalPhotosProcessed: (detailed?.imageCount ?? 0), // we don't track kept here; leave as deleted-only for now
                photosDeleted: (detailed?.imageCount ?? 0),
                videosProcessed: (detailed?.videoCount ?? 0),
                totalSpaceSaved: detailed?.bytes ?? 0
              });
              setShowCompletionPopup(true);
            } catch (e) {
              console.warn('Failed to compute thumbnail stats on save & quit:', e);
            }
          }}
          onComplete={async () => {
            try {
              const detailed = await window.electron?.ipcRenderer.invoke('get-delete-stats-detailed', selectedFolderPath);
              setCompletionStats({
                totalPhotosProcessed: (detailed?.imageCount ?? 0),
                photosDeleted: (detailed?.imageCount ?? 0),
                videosProcessed: (detailed?.videoCount ?? 0),
                totalSpaceSaved: detailed?.bytes ?? 0
              });
              setShowCompletionPopup(true);
            } catch (e) {
              console.warn('Failed to compute thumbnail stats on complete:', e);
            }
          }}
        />
      ) : shouldShowVideo ? (
        <VideoMode 
          folderPath={selectedFolderPath} 
          videos={flatVideos} 
          selections={saveState?.selections || {}}
          initialSortBy={(window as any).lastSettings?.video?.sortBy}
          initialSortOrder={(window as any).lastSettings?.video?.sortOrder}
          scrubForwardSeconds={(window as any).lastSettings?.video?.scrubForwardSeconds}
          scrubBackwardSeconds={(window as any).lastSettings?.video?.scrubBackwardSeconds}
          dateSource={(window as any).lastSettings?.video?.dateSource}
          onExit={async () => {
            try {
              if (saveState) {
                await window.electron?.ipcRenderer.invoke('save-save-state', saveState);
              }
              const detailed = await window.electron?.ipcRenderer.invoke('get-delete-stats-detailed', selectedFolderPath);
              const imagesDeleted = detailed?.imageCount ?? 0;
              const videosDeleted = detailed?.videoCount ?? 0;
              const imageExts = ['.jpg', '.jpeg', '.png', '.gif', '.bmp', '.tiff', '.webp'];
              const videoExts = ['.mp4', '.mov', '.avi', '.mkv', '.wmv', '.flv', '.webm', '.m4v', '.3gp', '.mpg', '.mpeg'];
              const getExt = (p: string) => p.slice(p.lastIndexOf('.')).toLowerCase();
              let keptImages = 0;
              let keptVideos = 0;
              if (saveState) {
                for (const p of saveState.processedPhotos) {
                  if (saveState.selections[p] === 'kept') {
                    const ext = getExt(p);
                    if (imageExts.includes(ext)) keptImages++;
                    else if (videoExts.includes(ext)) keptVideos++;
                  }
                }
              }
              setCompletionStats({
                totalPhotosProcessed: keptImages + imagesDeleted,
                photosDeleted: imagesDeleted,
                videosProcessed: keptVideos + videosDeleted,
                totalSpaceSaved: detailed?.bytes ?? 0
              });
              setShowCompletionPopup(true);
            } catch (e) {
              console.warn('Failed to compute video stats on save & quit:', e);
            }
          }}
          onComplete={() => setVideoSessionComplete(true)}
          onKeep={(video) => {
            setSaveState(prev => {
              if (!prev) return prev;
              const next = {
                ...prev,
                processedPhotos: prev.processedPhotos.includes(video.path)
                  ? prev.processedPhotos
                  : [...prev.processedPhotos, video.path],
                selections: { ...prev.selections, [video.path]: 'kept' as const }
              };
              window.electron?.ipcRenderer.invoke('save-save-state', next).catch(e => console.error('Failed to persist video keep:', e));
              return next;
            });
          }}
          onDelete={(video) => {
            setSaveState(prev => {
              if (!prev) return prev;
              const next = {
                ...prev,
                processedPhotos: prev.processedPhotos.includes(video.path)
                  ? prev.processedPhotos
                  : [...prev.processedPhotos, video.path],
                selections: { ...prev.selections, [video.path]: 'discarded' as const }
              };
              window.electron?.ipcRenderer.invoke('save-save-state', next).catch(e => console.error('Failed to persist video delete:', e));
              return next;
            });
          }}
          onRestore={(video) => {
            setSaveState(prev => {
              if (!prev) return prev;
              const { [video.path]: _, ...restSelections } = prev.selections || {};
              const next = {
                ...prev,
                processedPhotos: (prev.processedPhotos || []).filter(p => p !== video.path),
                selections: restSelections
              };
              window.electron?.ipcRenderer.invoke('save-save-state', next).catch(e => console.error('Failed to persist video restore:', e));
              return next;
            });
          }}
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

      {selectedMode === 'thumbnail' && thumbnailSessionComplete && null}

      {selectedMode === 'video' && videoSessionComplete && null}
    </div>
  );
};

export default App;