import React, { useState, useCallback } from 'react';
import BatchSelector from './components/BatchSelector';
import PhotoPairViewer from './components/PhotoPairViewer';
import ThumbnailStripCuller from './components/ThumbnailStripCuller';
import VideoMode from './components/VideoMode';
import CompletionPopup from './components/CompletionPopup';
import {
  getMediaReviewKey,
  ImmichConnectionConfig,
  MediaSourceType,
  Photo,
  PhotoBatch,
  SaveState,
  Video,
} from '../types';
import {
  enrichPhotosWithImmich,
  enrichVideosWithImmich,
  setImmichSession,
  ImmichMatchError,
} from './utils/immichMedia';
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
  const [sourceType, setSourceType] = useState<MediaSourceType>('local');
  const [immichConfig, setImmichConfig] = useState<ImmichConnectionConfig | null>(null);
  const [scanSettings, setScanSettings] = useState<any>(null);
  const [immichError, setImmichError] = useState<string | null>(null);
  const [tournamentSessionComplete, setTournamentSessionComplete] = useState(false);

  const imageExts = ['.jpg', '.jpeg', '.png', '.gif', '.bmp', '.tiff', '.webp', '.heic', '.heif'];
  const videoExts = ['.mp4', '.mov', '.avi', '.mkv', '.wmv', '.flv', '.webm', '.m4v', '.3gp', '.mpg', '.mpeg'];
  const getExt = (p: string) => p.slice(p.lastIndexOf('.')).toLowerCase();

  const buildSessionStatsFromSaveState = (state: SaveState | null) => {
    let keptImages = 0;
    let keptVideos = 0;
    let discardedImages = 0;
    let discardedVideos = 0;
    if (state) {
      for (const key of state.processedPhotos) {
        const selection = state.selections[key];
        if (!selection) continue;
        const ext = key.includes('.') ? getExt(key) : '.jpg';
        if (selection === 'kept') {
          if (imageExts.includes(ext)) keptImages++;
          else if (videoExts.includes(ext)) keptVideos++;
        } else if (selection === 'discarded') {
          if (imageExts.includes(ext)) discardedImages++;
          else if (videoExts.includes(ext)) discardedVideos++;
          else discardedImages++;
        }
      }
    }
    return {
      totalPhotosProcessed: keptImages + discardedImages,
      photosDeleted: discardedImages,
      videosProcessed: keptVideos + discardedVideos,
      totalSpaceSaved: 0,
    };
  };

  const handleImmichFailure = (error: unknown) => {
    if (error instanceof ImmichMatchError) {
      setImmichError(error.message);
      return true;
    }
    return false;
  };

  const buildInitialSaveState = (
    folderPath: string,
    settings: any,
    config: ImmichConnectionConfig | null
  ): SaveState => ({
    version: 2,
    sourceType: settings?.sourceType || 'local',
    folderPath,
    processedPhotos: [],
    selections: {},
    immich:
      settings?.sourceType === 'immich-external' && config
        ? {
            serverUrl: config.serverUrl,
            pathMapping: config.pathMapping,
          }
        : undefined,
  });

  const enrichBatchesWithImmich = async (
    inputBatches: PhotoBatch[],
    folderPath: string,
    includeSubfolders: boolean,
    config: ImmichConnectionConfig
  ): Promise<PhotoBatch[]> => {
    const allPhotos = inputBatches.flatMap((batch) => batch.photos);
    const enrichedPhotos = await enrichPhotosWithImmich(allPhotos, folderPath, includeSubfolders, config);
    const byPath = new Map(enrichedPhotos.map((photo) => [photo.path, photo]));
    return inputBatches.map((batch) => ({
      ...batch,
      photos: batch.photos.map((photo) => byPath.get(photo.path) || photo),
    }));
  };

  const handleFolderSelect = async (
    folderPath: string,
    includeSubfolders: boolean,
    settings: any,
    mode: 'tournament' | 'thumbnail' | 'video',
    config: ImmichConnectionConfig | null = null
  ) => {
    setIsLoading(true);
    setImmichError(null);
    setTournamentSessionComplete(false);
    try {
      setSelectedMode(mode);
      setSelectedFolderPath(folderPath);
      setIncludeSubfoldersSelected(includeSubfolders);
      setScanSettings(settings);
      setSourceType(settings?.sourceType || 'local');
      setImmichConfig(config);
      if (settings?.sourceType === 'immich-external' && config) {
        await setImmichSession(config, folderPath, includeSubfolders);
      } else {
        await setImmichSession(null);
      }

      const hasSaveState = await window.electron?.ipcRenderer.invoke('save-state-exists', folderPath);
      
      if (hasSaveState) {
        const existingSaveState = await window.electron?.ipcRenderer.invoke('load-save-state', folderPath);
        setSaveState(existingSaveState);
        if (existingSaveState?.sourceType) {
          setSourceType(existingSaveState.sourceType);
        }
        if (mode === 'thumbnail' || mode === 'video') {
          setShowSkipProcessedPrompt(true);
        } else {
          setShowResumePrompt(true);
        }
      } else {
        if (mode === 'thumbnail') {
          const newSaveState = buildInitialSaveState(folderPath, settings, config);
          try {
            await window.electron?.ipcRenderer.invoke('save-save-state', newSaveState);
            setSaveState(newSaveState);
          } catch (e) {
            console.warn('Failed to create initial save state for thumbnail:', e);
          }
          let photos = await window.electron?.ipcRenderer.invoke(
            'scan-folder-photos',
            folderPath,
            includeSubfolders,
            [],
            settings
          );
          if (settings?.sourceType === 'immich-external' && config) {
            photos = await enrichPhotosWithImmich(photos || [], folderPath, includeSubfolders, config);
          }
          setFlatPhotos(photos || []);
        } else if (mode === 'video') {
          const newSaveState = buildInitialSaveState(folderPath, settings, config);
          try {
            await window.electron?.ipcRenderer.invoke('save-save-state', newSaveState);
            setSaveState(newSaveState);
          } catch (e) {
            console.warn('Failed to create initial save state for video:', e);
          }
          let videos = await window.electron?.ipcRenderer.invoke(
            'scan-folder-videos',
            folderPath,
            includeSubfolders,
            [],
            settings
          );
          if (settings?.sourceType === 'immich-external' && config) {
            videos = await enrichVideosWithImmich(videos || [], folderPath, includeSubfolders, config);
          }
          setFlatVideos(videos || []);
        } else {
          await startProcessing(folderPath, includeSubfolders, [], settings, config);
        }
      }
    } catch (error) {
      console.error('Error checking save state:', error);
      if (handleImmichFailure(error)) {
        return;
      }
      if (mode === 'thumbnail') {
        const newSaveState = buildInitialSaveState(folderPath, settings, config);
        try {
          await window.electron?.ipcRenderer.invoke('save-save-state', newSaveState);
          setSaveState(newSaveState);
        } catch (e) { /* ignore */ }
        let photos = await window.electron?.ipcRenderer.invoke(
          'scan-folder-photos',
          folderPath,
          includeSubfolders,
          [],
          settings
        );
        if (settings?.sourceType === 'immich-external' && config) {
          photos = await enrichPhotosWithImmich(photos || [], folderPath, includeSubfolders, config);
        }
        setFlatPhotos(photos || []);
      } else if (mode === 'video') {
        const newSaveState = buildInitialSaveState(folderPath, settings, config);
        try {
          await window.electron?.ipcRenderer.invoke('save-save-state', newSaveState);
          setSaveState(newSaveState);
        } catch (e) { /* ignore */ }
        let videos = await window.electron?.ipcRenderer.invoke(
          'scan-folder-videos',
          folderPath,
          includeSubfolders,
          [],
          settings
        );
        if (settings?.sourceType === 'immich-external' && config) {
          videos = await enrichVideosWithImmich(videos || [], folderPath, includeSubfolders, config);
        }
        setFlatVideos(videos || []);
      } else {
        await startProcessing(folderPath, includeSubfolders, [], settings, config);
      }
    } finally {
      setIsLoading(false);
    }
  };

  const handleSkipProcessedYes = async () => {
    if (saveState) {
      const settings = scanSettings || (window as any).lastSettings;
      if (selectedMode === 'thumbnail') {
        let photos = await window.electron?.ipcRenderer.invoke(
          'scan-folder-photos',
          saveState.folderPath,
          includeSubfoldersSelected,
          saveState.processedPhotos,
          settings
        );
        if (sourceType === 'immich-external' && immichConfig) {
          photos = await enrichPhotosWithImmich(
            photos || [],
            saveState.folderPath,
            includeSubfoldersSelected,
            immichConfig
          );
        }
        setFlatPhotos(photos || []);
      } else if (selectedMode === 'video') {
        let videos = await window.electron?.ipcRenderer.invoke(
          'scan-folder-videos',
          saveState.folderPath,
          includeSubfoldersSelected,
          saveState.processedPhotos,
          settings
        );
        if (sourceType === 'immich-external' && immichConfig) {
          videos = await enrichVideosWithImmich(
            videos || [],
            saveState.folderPath,
            includeSubfoldersSelected,
            immichConfig
          );
        }
        setFlatVideos(videos || []);
      }
      setShowSkipProcessedPrompt(false);
    }
  };

  const handleSkipProcessedNo = async () => {
    if (saveState) {
      const settings = scanSettings || (window as any).lastSettings;
      if (selectedMode === 'thumbnail') {
        let photos = await window.electron?.ipcRenderer.invoke(
          'scan-folder-photos',
          saveState.folderPath,
          includeSubfoldersSelected,
          [],
          settings
        );
        if (sourceType === 'immich-external' && immichConfig) {
          photos = await enrichPhotosWithImmich(
            photos || [],
            saveState.folderPath,
            includeSubfoldersSelected,
            immichConfig
          );
        }
        setFlatPhotos(photos || []);
      } else if (selectedMode === 'video') {
        let videos = await window.electron?.ipcRenderer.invoke(
          'scan-folder-videos',
          saveState.folderPath,
          includeSubfoldersSelected,
          [],
          settings
        );
        if (sourceType === 'immich-external' && immichConfig) {
          videos = await enrichVideosWithImmich(
            videos || [],
            saveState.folderPath,
            includeSubfoldersSelected,
            immichConfig
          );
        }
        setFlatVideos(videos || []);
      }
      setShowSkipProcessedPrompt(false);
    }
  };

  const startProcessing = async (
    folderPath: string,
    includeSubfolders: boolean,
    processedPhotos: string[] = [],
    settings?: any,
    config: ImmichConnectionConfig | null = immichConfig
  ) => {
    setIsLoading(true);
    try {
      const activeSettings = settings || scanSettings || (window as any).lastSettings;
      if (!saveState) {
        const newSaveState = buildInitialSaveState(folderPath, activeSettings, config);
        try {
          console.log('Creating initial save state for:', folderPath);
          await window.electron?.ipcRenderer.invoke('save-save-state', newSaveState);
          console.log('Initial save state created successfully');
          setSaveState(newSaveState);
        } catch (error) {
          console.error('Failed to create initial save state:', error);
        }
      }
      
      let newBatches = await window.electron?.ipcRenderer.invoke(
        'scan-folder',
        folderPath,
        includeSubfolders,
        processedPhotos,
        activeSettings
      );
      if (activeSettings?.sourceType === 'immich-external' && config) {
        newBatches = await enrichBatchesWithImmich(newBatches || [], folderPath, includeSubfolders, config);
      }
      if (newBatches && newBatches.length > 0) {
        setBatches(newBatches);
        setCurrentBatchIndex(0);
      }
    } catch (error) {
      console.error('Error scanning folder:', error);
      if (handleImmichFailure(error)) {
        return;
      }
    } finally {
      setIsLoading(false);
    }
  };

  const handleResume = async () => {
    if (saveState) {
      const settings = scanSettings || (window as any).lastSettings || { sourceType: saveState.sourceType };
      if (settings?.sourceType === 'immich-external' && immichConfig) {
        await setImmichSession(
          immichConfig,
          saveState.folderPath,
          includeSubfoldersSelected
        );
      }
      await startProcessing(
        saveState.folderPath,
        includeSubfoldersSelected,
        saveState.processedPhotos,
        settings,
        immichConfig
      );
      setShowResumePrompt(false);
    }
  };

  const handleStartOver = async () => {
    if (saveState) {
      try {
        await window.electron?.ipcRenderer.invoke('delete-save-state', saveState.folderPath);
      } catch (error) {
        console.warn('Failed to delete save state:', error);
      }
      
      const settings = scanSettings || (window as any).lastSettings || { sourceType: saveState.sourceType };
      await startProcessing(saveState.folderPath, includeSubfoldersSelected, [], settings, immichConfig);
      setSaveState(null);
      setShowResumePrompt(false);
    }
  };

  const handleCompletionClose = () => {
    setShowCompletionPopup(false);
    setCompletionStats(null);
    setTournamentSessionComplete(false);
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
    if (!photosToDelete.length && !selectedPhotos.length) return;

    const newDeletes = photosToDelete.filter((photo) => {
      const key = getMediaReviewKey(photo);
      return saveState.selections[key] !== 'discarded';
    });
    const newKeeps = selectedPhotos.filter((photo) => {
      const key = getMediaReviewKey(photo);
      return saveState.selections[key] !== 'kept';
    });
    if (!newDeletes.length && !newKeeps.length) return;

    const updatedSaveState = { ...saveState };
    
    for (const photo of newKeeps) {
      const key = getMediaReviewKey(photo);
      if (!updatedSaveState.processedPhotos.includes(key)) {
        updatedSaveState.processedPhotos.push(key);
      }
      updatedSaveState.selections[key] = 'kept';
    }
    
    for (const photo of newDeletes) {
      const key = getMediaReviewKey(photo);
      if (!updatedSaveState.processedPhotos.includes(key)) {
        updatedSaveState.processedPhotos.push(key);
      }
      updatedSaveState.selections[key] = 'discarded';
    }
    
    try {
      await window.electron?.ipcRenderer.invoke('save-save-state', updatedSaveState);
      setSaveState(updatedSaveState);
    } catch (error) {
      console.error('Failed to save state:', error);
    }

    if (!newDeletes.length) return;

    try {
      const results: Array<{ fromPath: string; toPath: string; status: 'moved' | 'skipped' | 'error'; reason?: string }> | undefined = await window.electron?.ipcRenderer.invoke('process-photos', {
        selectedPhotos: newKeeps,
        photosToDelete: newDeletes,
        sourceType,
        rootFolderPath: selectedFolderPath,
        includeSubfolders: includeSubfoldersSelected,
      });
      const moved = results?.filter((r) => r.status === 'moved').length ?? 0;
      const failed = results?.filter((r) => r.status === 'error') ?? [];
      console.log('[process-photos] moved:', moved, 'failed:', failed.length, 'of', newDeletes.length);
      if (failed.length > 0) {
        console.warn('[process-photos] failures:', failed);
        const missingIds = failed.filter((r) => r.reason === 'missing-asset-id').length;
        if (missingIds > 0) {
          setImmichError(
            `${missingIds} photo(s) could not be matched to Immich assets. Check path mapping (local path prefix ↔ Immich path prefix) in settings.`
          );
        } else {
          setImmichError(failed[0]?.reason || 'Failed to move photos to Immich trash');
        }
      }
      setUndoStack(prev => [
        ...prev,
        {
          batchIndex: currentBatchIndex,
          selectedPhotos: newKeeps,
          photosToDelete: newDeletes,
          moveResults: results || []
        }
      ]);
    } catch (err) {
      console.error('process-photos failed:', err);
      setImmichError(err instanceof Error ? err.message : 'Failed to trash photos in Immich');
    }
  };

  const handleBatchComplete = useCallback(async () => {
    console.log('handleBatchComplete called', { currentBatchIndex, batchesLength: batches.length });
    if (currentBatchIndex < batches.length - 1) {
      console.log('Moving to next batch', currentBatchIndex + 1);
      setCurrentBatchIndex(prev => prev + 1);
      return;
    }

    console.log('All batches complete');
    setTournamentSessionComplete(true);
    setCompletionStats(buildSessionStatsFromSaveState(saveState));
    setShowCompletionPopup(true);

    if (sourceType !== 'immich-external') {
      try {
        const detailed = await window.electron?.ipcRenderer.invoke('get-delete-stats-detailed', selectedFolderPath);
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
          totalPhotosProcessed: keptImages + (detailed?.imageCount ?? 0),
          photosDeleted: detailed?.imageCount ?? 0,
          videosProcessed: keptVideos + (detailed?.videoCount ?? 0),
          totalSpaceSaved: detailed?.bytes ?? 0,
        });
      } catch (error) {
        console.warn('Failed to compute local delete stats:', error);
      }
    }
  }, [currentBatchIndex, batches.length, saveState, sourceType, selectedFolderPath]);

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

    // 1) Restore moved files from _delete or Immich trash
    for (const r of last.moveResults) {
      if (r.status === 'moved') {
        try {
          const photo =
            last.photosToDelete.find((p) => p.path === r.fromPath) ||
            last.selectedPhotos.find((p) => p.path === r.fromPath);
          await window.electron?.ipcRenderer.invoke('restore-photo', {
            photo,
            fromPath: r.fromPath,
            toPath: r.toPath,
            assetId: photo?.assetId,
            sourceType,
          });
          console.log('[UNDO] Restored', r.toPath, '->', r.fromPath);
        } catch (e) {
          console.warn('Failed to restore during undo:', r.fromPath, e);
        }
      }
    }

    // 2) Revert save state entries for this batch (both kept and deleted)
    const keysToRevert = new Set<string>([
      ...last.selectedPhotos.map((p) => getMediaReviewKey(p)),
      ...last.photosToDelete.map((p) => getMediaReviewKey(p)),
    ]);
    const newProcessed = (saveState.processedPhotos || []).filter((p) => !keysToRevert.has(p));
    const newSelections = { ...saveState.selections } as Record<string, 'kept' | 'discarded'>;
    for (const key of keysToRevert) {
      delete newSelections[key];
    }
    const reverted: SaveState = { ...saveState, processedPhotos: newProcessed, selections: newSelections };

    try {
      await window.electron?.ipcRenderer.invoke('save-save-state', reverted);
      setSaveState(reverted);
      console.log('[UNDO] Save state reverted for', keysToRevert.size, 'items');
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
          sourceType={sourceType}
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
          sourceType={sourceType}
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
              const key = getMediaReviewKey(video);
              const next = {
                ...prev,
                processedPhotos: prev.processedPhotos.includes(key)
                  ? prev.processedPhotos
                  : [...prev.processedPhotos, key],
                selections: { ...prev.selections, [key]: 'kept' as const }
              };
              window.electron?.ipcRenderer.invoke('save-save-state', next).catch(e => console.error('Failed to persist video keep:', e));
              return next;
            });
          }}
          onDelete={(video) => {
            setSaveState(prev => {
              if (!prev) return prev;
              const key = getMediaReviewKey(video);
              const next = {
                ...prev,
                processedPhotos: prev.processedPhotos.includes(key)
                  ? prev.processedPhotos
                  : [...prev.processedPhotos, key],
                selections: { ...prev.selections, [key]: 'discarded' as const }
              };
              window.electron?.ipcRenderer.invoke('save-save-state', next).catch(e => console.error('Failed to persist video delete:', e));
              return next;
            });
          }}
          onRestore={(video) => {
            setSaveState(prev => {
              if (!prev) return prev;
              const key = getMediaReviewKey(video);
              const { [key]: _, ...restSelections } = prev.selections || {};
              const next = {
                ...prev,
                processedPhotos: (prev.processedPhotos || []).filter(p => p !== key),
                selections: restSelections
              };
              window.electron?.ipcRenderer.invoke('save-save-state', next).catch(e => console.error('Failed to persist video restore:', e));
              return next;
            });
          }}
        />
      ) : !tournamentSessionComplete && currentBatchIndex !== -1 && currentBatch && selectedMode === 'tournament' ? (
        <>
          {immichError && (
            <div className="resume-prompt" style={{ margin: '1rem' }}>
              <h2>Immich Error</h2>
              <p>{immichError}</p>
              <p style={{ fontSize: 14, color: '#ccc' }}>
                Set Local Path Prefix and Immich Path Prefix to match what Immich shows in photo Info.
              </p>
              <div className="resume-buttons">
                <button type="button" onClick={() => setImmichError(null)}>
                  Dismiss
                </button>
              </div>
            </div>
          )}
          <PhotoPairViewer
          batch={currentBatch}
          currentBatchIndex={currentBatchIndex}
          totalBatches={batches.length}
          sourceType={sourceType}
          onSelection={handlePhotoSelection}
          onBatchComplete={handleBatchComplete}
          onUndoLastAction={handleGlobalUndo}
        />
        </>
      ) : (
        <>
          {immichError && (
            <div className="resume-prompt" style={{ margin: '1rem' }}>
              <h2>Immich Error</h2>
              <p>{immichError}</p>
              <p style={{ fontSize: 14, color: '#ccc' }}>
                Check path mapping (local HDD path vs Immich container path) and that the folder is an Immich external library import path.
              </p>
              <div className="resume-buttons">
                <button type="button" onClick={() => setImmichError(null)}>
                  Dismiss
                </button>
              </div>
            </div>
          )}
          <BatchSelector 
            onFolderSelect={handleFolderSelect}
            isLoading={isLoading}
          />
        </>
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
            const photos = await window.electron?.ipcRenderer.invoke(
              'scan-folder-photos',
              folderPath,
              includeSubfoldersSelected,
              processed,
              scanSettings || (window as any).lastSettings
            );
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