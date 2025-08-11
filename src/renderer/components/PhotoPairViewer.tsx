import React, { useState, useEffect, useCallback } from 'react';
import { Photo, PhotoBatch } from '../../types';
import '../styles/PhotoPairViewer.css';

interface PhotoPairViewerProps {
  batch: PhotoBatch;
  currentBatchIndex: number;
  totalBatches: number;
  onSelection: (selectedPhotos: Photo[], photosToDelete: Photo[]) => void;
  onBatchComplete: () => void;
  onUndoLastAction?: () => void;
}

type SelectionAction = 'left' | 'right' | 'both' | 'neither';

const PhotoPairViewer: React.FC<PhotoPairViewerProps> = ({ 
  batch, 
  currentBatchIndex,
  totalBatches,
  onSelection, 
  onBatchComplete,
  onUndoLastAction
}) => {
  const [remainingPhotos, setRemainingPhotos] = useState<Photo[]>(batch.photos);
  const [photosToDelete, setPhotosToDelete] = useState<Photo[]>([]);
  const [selectedPhotos, setSelectedPhotos] = useState<Photo[]>([]);
  const [currentPairIndex, setCurrentPairIndex] = useState(0);
  const [batchCompleted, setBatchCompleted] = useState(false);
  const [seenPhotoIds, setSeenPhotoIds] = useState<Set<string>>(new Set());
  const [keptPhotoIds, setKeptPhotoIds] = useState<Set<string>>(new Set());
  const [hasSeenAllOnce, setHasSeenAllOnce] = useState<boolean>(false);
  const [rePassedPhotoIds, setRePassedPhotoIds] = useState<Set<string>>(new Set());
  
  // Initialize remaining photos when batch changes
  useEffect(() => {
    console.log('Batch changed, initializing state', { batchId: batch.id, photoCount: batch.photos.length });
    setRemainingPhotos(batch.photos);
    setCurrentPairIndex(0);
    setSelectedPhotos([]);
    setPhotosToDelete([]);
    setBatchCompleted(false);
    setSeenPhotoIds(new Set());
    setKeptPhotoIds(new Set());
    setHasSeenAllOnce(false);
    setRePassedPhotoIds(new Set());
  }, [batch]);

  // Check if batch is complete
  useEffect(() => {
    if (remainingPhotos.length === 1 && !batchCompleted) {
      // Auto-select the last photo
      setSelectedPhotos(prev => [...prev, remainingPhotos[0]]);
      onSelection([...selectedPhotos, remainingPhotos[0]], photosToDelete);
      setBatchCompleted(true);
      onBatchComplete();
    } else if (remainingPhotos.length === 0 && !batchCompleted) {
      setBatchCompleted(true);
      onBatchComplete();
    }
  }, [remainingPhotos.length, selectedPhotos, photosToDelete, batchCompleted, onSelection, onBatchComplete]);

  // Get current photo pair
  const getCurrentPair = (): [Photo, Photo] | null => {
    const startIndex = currentPairIndex * 2;
    if (startIndex + 1 >= remainingPhotos.length) {
      // If we have exactly 2 photos left, show them as the final pair
      if (remainingPhotos.length === 2) {
        return [remainingPhotos[0], remainingPhotos[1]];
      }
      return null; // No more pairs or only 1 photo left
    }
    return [remainingPhotos[startIndex], remainingPhotos[startIndex + 1]];
  };

  // Mark currently displayed pair as seen when it appears
  useEffect(() => {
    const currentPair = getCurrentPair();
    if (!currentPair) return;
    setSeenPhotoIds(prev => {
      const next = new Set(prev);
      next.add(currentPair[0].id);
      next.add(currentPair[1].id);
      return next;
    });
  }, [currentPairIndex, remainingPhotos]);

  // Track when all photos have been seen at least once
  useEffect(() => {
    const totalPhotos = batch.photos.length;
    if (!hasSeenAllOnce && seenPhotoIds.size >= totalPhotos && totalPhotos > 0) {
      setHasSeenAllOnce(true);
    }
  }, [seenPhotoIds, batch.photos.length, hasSeenAllOnce]);

  // Handle photo selection
  const handleSelection = useCallback((action: SelectionAction) => {
    const currentPair = getCurrentPair();
    if (!currentPair) return;

    const [leftPhoto, rightPhoto] = currentPair;
    let photosToKeep: Photo[] = [];
    let photosToRemove: Photo[] = [];

    switch (action) {
      case 'left':
        photosToKeep = [leftPhoto];
        photosToRemove = [rightPhoto];
        break;
      case 'right':
        photosToKeep = [rightPhoto];
        photosToRemove = [leftPhoto];
        break;
      case 'both':
        photosToKeep = [leftPhoto, rightPhoto];
        photosToRemove = [];
        break;
      case 'neither':
        photosToKeep = [];
        photosToRemove = [leftPhoto, rightPhoto];
        break;
    }

    // Update kept set
    if (photosToKeep.length > 0) {
      setKeptPhotoIds(prev => {
        const next = new Set(prev);
        photosToKeep.forEach(p => next.add(p.id));
        return next;
      });
      if (hasSeenAllOnce) {
        setRePassedPhotoIds(prev => {
          const next = new Set(prev);
          photosToKeep.forEach(p => next.add(p.id));
          return next;
        });
      }
    }

    // Update the remaining photos pool
    setRemainingPhotos(prev => {
      // Remove the current pair from the pool
      const withoutCurrentPair = prev.filter(
        photo => photo.id !== leftPhoto.id && photo.id !== rightPhoto.id
      );
      // Add back the photos we want to keep for further comparison
      return [...withoutCurrentPair, ...photosToKeep];
    });

    // Add photos to delete list
    setPhotosToDelete(prev => [...prev, ...photosToRemove]);

    // If we're dealing with a 2-photo batch and selecting both or neither, complete the batch
    if (remainingPhotos.length === 2 && (action === 'both' || action === 'neither')) {
      setBatchCompleted(true);
      if (action === 'both') {
        onSelection([...selectedPhotos, leftPhoto, rightPhoto], photosToDelete);
      } else {
        onSelection(selectedPhotos, [...photosToDelete, leftPhoto, rightPhoto]);
      }
      onBatchComplete();
      return;
    }

    // Adjust current pair index after removing a pair
    setCurrentPairIndex(prev => {
      const newRemainingCount = remainingPhotos.length - 2 + photosToKeep.length;
      const maxPairIndex = Math.floor(newRemainingCount / 2) - 1;
      
      // If we're at or beyond the last possible pair, reset to 0
      if (prev >= maxPairIndex) {
        return 0;
      }
      // Otherwise keep the same index to continue with the next pair
      return prev;
    });

    // If we have 2 or fewer photos remaining after this selection,
    // they'll be handled in getCurrentPair on the next render
  }, [remainingPhotos, currentPairIndex]);

  // Handle keeping all remaining photos in the batch
  const handleKeepAllRemaining = useCallback(() => {
    // Add all remaining photos to selected list and complete the batch
    setSelectedPhotos(prev => {
      const newSelectedPhotos = [...prev, ...remainingPhotos];
      // Complete the batch with updated state
      onSelection(newSelectedPhotos, photosToDelete);
      onBatchComplete();
      return newSelectedPhotos;
    });
    // Mark all remaining as kept
    setKeptPhotoIds(prev => {
      const next = new Set(prev);
      remainingPhotos.forEach(p => next.add(p.id));
      return next;
    });
    if (hasSeenAllOnce) {
      setRePassedPhotoIds(prev => {
        const next = new Set(prev);
        remainingPhotos.forEach(p => next.add(p.id));
        return next;
      });
    }
    // Clear the remaining photos pool
    setRemainingPhotos([]);
  }, [remainingPhotos, photosToDelete, onSelection, onBatchComplete]);

  // Handle moving all remaining photos in the batch to delete folder
  const handleMoveAllRemaining = useCallback(() => {
    console.log('handleMoveAllRemaining called with remainingPhotos:', remainingPhotos.length);
    // Add all remaining photos to delete list and complete the batch
    setPhotosToDelete(prev => {
      const newPhotosToDelete = [...prev, ...remainingPhotos];
      console.log('Moving all remaining photos to delete:', remainingPhotos.length, 'photos');
      // Complete the batch with updated state
      onSelection(selectedPhotos, newPhotosToDelete);
      onBatchComplete();
      return newPhotosToDelete;
    });
    // Clear the remaining photos pool
    setRemainingPhotos([]);
  }, [remainingPhotos, selectedPhotos, onSelection, onBatchComplete]);

  // Handle undo (go back to previous pair)
  const handleUndo = useCallback(() => {
    console.log('handleUndo called, currentPairIndex:', currentPairIndex);
    if (currentPairIndex > 0) {
      console.log('Undoing to previous pair');
      setCurrentPairIndex(prev => prev - 1);
      // Remove the last selected photos from the selection
      setSelectedPhotos(prev => {
        const currentPair = getCurrentPair();
        if (currentPair) {
          const [leftPhoto, rightPhoto] = currentPair;
          console.log('Removing photos from selection:', leftPhoto.filename, rightPhoto.filename);
          return prev.filter(photo => photo.id !== leftPhoto.id && photo.id !== rightPhoto.id);
        }
        return prev;
      });
      // Remove the last photos from delete list
      setPhotosToDelete(prev => {
        const currentPair = getCurrentPair();
        if (currentPair) {
          const [leftPhoto, rightPhoto] = currentPair;
          console.log('Removing photos from delete list:', leftPhoto.filename, rightPhoto.filename);
          return prev.filter(photo => photo.id !== leftPhoto.id && photo.id !== rightPhoto.id);
        }
        return prev;
      });
    } else {
      console.log('Cannot undo - already at first pair');
    }
  }, [currentPairIndex, remainingPhotos]);

  // Handle keyboard input
  const handleKeyPress = useCallback((event: KeyboardEvent) => {
    console.log('Key pressed:', event.key, event.key.toLowerCase(), 'code:', event.code);
    
    // Prevent default behavior for keys we handle
    if (['ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown', 'Enter', 'Space', 'KeyZ'].includes(event.code)) {
      event.preventDefault();
    }
    
    switch (event.key.toLowerCase()) {
      case 'arrowleft':
        console.log('Left arrow pressed');
        handleSelection('left');
        break;
      case 'arrowright':
        console.log('Right arrow pressed');
        handleSelection('right');
        break;
      case 'arrowup':
        console.log('Up arrow pressed');
        handleSelection('both');
        break;
      case 'arrowdown':
        console.log('Down arrow pressed');
        handleSelection('neither');
        break;
      case 'enter':
        console.log('Enter pressed');
        handleKeepAllRemaining();
        break;
      case ' ':
      case 'space':
        console.log('Space key pressed - moving all remaining photos to delete');
        handleMoveAllRemaining();
        break;
      case 'z':
        if (onUndoLastAction) {
          onUndoLastAction();
        }
        break;
      default:
        console.log('Unhandled key:', event.key, 'code:', event.code);
    }
  }, [handleSelection, handleKeepAllRemaining, handleMoveAllRemaining, handleUndo]);

  // Set up keyboard event listeners
  useEffect(() => {
    console.log('Setting up keyboard event listener');
    window.addEventListener('keydown', handleKeyPress);
    return () => {
      console.log('Removing keyboard event listener');
      window.removeEventListener('keydown', handleKeyPress);
    };
  }, [handleKeyPress]);

  const currentPair = getCurrentPair();
  const remainingCount = remainingPhotos.length;
  const totalPhotos = batch.photos.length;
  const seenCount = seenPhotoIds.size;
  const allRemainingSeen = remainingPhotos.every(photo => seenPhotoIds.has(photo.id));
  const deletedCount = photosToDelete.length;
  const keptCount = keptPhotoIds.size; // unique kept at least once
  const seenOnlyCount = Array.from(seenPhotoIds).filter(
    id => !keptPhotoIds.has(id) && !photosToDelete.some(p => p.id === id)
  ).length;
  const unseenCount = Math.max(0, totalPhotos - (deletedCount + keptCount + seenOnlyCount));
  const allRemainingPassed = remainingPhotos.every(photo => keptPhotoIds.has(photo.id));
  const rePassedCount = hasSeenAllOnce ? rePassedPhotoIds.size : 0;
  
  console.log('PhotoPairViewer render', { 
    batchId: batch.id, 
    remainingCount, 
    currentPair: currentPair ? 'exists' : 'null',
    currentPairIndex 
  });

  if (!currentPair || batchCompleted) {
    return (
      <div className="photo-pair-viewer">
        <div className="photo-pair-viewer__header">
          <h2>Processing...</h2>
          <p>Moving to next batch...</p>
        </div>
      </div>
    );
  }

  const [leftPhoto, rightPhoto] = currentPair;

  return (
    <div className="photo-pair-viewer">
      <div className="photo-pair-viewer__header">
        <h2>Batch {currentBatchIndex + 1}/{totalBatches}</h2>
        <div className="batch-progress">
          <div className="progress-bar progress-bar--stacked">
            {/* Red: deleted */}
            <div
              className="progress-segment progress-segment--red"
              style={{ width: `${(deletedCount / totalPhotos) * 100}%`, left: '0%' }}
            />
            {/* Yellow: seen-only */}
            <div
              className="progress-segment progress-segment--yellow"
              style={{ width: `${(seenOnlyCount / totalPhotos) * 100}%`, left: `${(deletedCount / totalPhotos) * 100}%` }}
            />
            {/* Dark Green: kept (passed at least once) */}
            <div
              className="progress-segment progress-segment--green-dark"
              style={{ width: `${(keptCount / totalPhotos) * 100}%`, left: `${((deletedCount + seenOnlyCount) / totalPhotos) * 100}%` }}
            />
            {/* Neon Green: re-passed after all seen */}
            {hasSeenAllOnce && (
              <div
                className="progress-segment progress-segment--green-neon"
                style={{ width: `${(rePassedCount / totalPhotos) * 100}%`, left: `${((deletedCount + seenOnlyCount) / totalPhotos) * 100}%` }}
              />
            )}
          </div>
          <div className="progress-legend">
            <span className="legend-item"><span className="legend-dot legend-dot--red" /> Deleted {deletedCount}</span>
            <span className="legend-item"><span className="legend-dot legend-dot--yellow" /> Seen {seenCount}</span>
            <span className="legend-item"><span className="legend-dot legend-dot--green-dark" /> Passed {keptCount}</span>
            {hasSeenAllOnce && (
              <span className="legend-item"><span className="legend-dot legend-dot--green-neon" /> Re-passed {rePassedCount}</span>
            )}
            <span className="legend-item"><span className="legend-dot legend-dot--gray" /> Unseen {unseenCount}</span>
          </div>
        </div>

        {allRemainingPassed && remainingCount > 0 && (
          <div className="progress-text" style={{ marginTop: 6, color: '#a0e3a8' }}>
            All remaining photos have been seen. Press Enter to keep all remaining.
          </div>
        )}

      </div>

      <div className="photo-pair-viewer__content">
        <div className="photo-pair">
          <div className="photo-container">
            <img 
              src={`file://${leftPhoto.path}`}
              alt={leftPhoto.filename}
              className="photo-image"
            />
            <div className="photo-info">
              <span className="photo-filename">{leftPhoto.filename}</span>
              <span className="photo-timestamp">
                {leftPhoto.timestamp.toLocaleString()}
              </span>
            </div>
          </div>

          <div className="photo-container">
            <img 
              src={`file://${rightPhoto.path}`}
              alt={rightPhoto.filename}
              className="photo-image"
            />
            <div className="photo-info">
              <span className="photo-filename">{rightPhoto.filename}</span>
              <span className="photo-timestamp">
                {rightPhoto.timestamp.toLocaleString()}
              </span>
            </div>
          </div>
        </div>


      </div>
    </div>
  );
};

export default PhotoPairViewer;