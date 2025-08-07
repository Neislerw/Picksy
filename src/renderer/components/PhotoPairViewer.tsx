import React, { useState, useEffect, useCallback } from 'react';
import { Photo, PhotoBatch } from '../../types';
import '../styles/PhotoPairViewer.css';

interface PhotoPairViewerProps {
  batch: PhotoBatch;
  onSelection: (selectedPhotos: Photo[], photosToDelete: Photo[]) => void;
  onBatchComplete: () => void;
}

type SelectionAction = 'left' | 'right' | 'both' | 'neither';

const PhotoPairViewer: React.FC<PhotoPairViewerProps> = ({ 
  batch, 
  onSelection, 
  onBatchComplete 
}) => {
  const [remainingPhotos, setRemainingPhotos] = useState<Photo[]>(batch.photos);
  const [photosToDelete, setPhotosToDelete] = useState<Photo[]>([]);
  const [selectedPhotos, setSelectedPhotos] = useState<Photo[]>([]);
  const [currentPairIndex, setCurrentPairIndex] = useState(0);
  const [batchCompleted, setBatchCompleted] = useState(false);
  
  // Initialize remaining photos when batch changes
  useEffect(() => {
    console.log('Batch changed, initializing state', { batchId: batch.id, photoCount: batch.photos.length });
    setRemainingPhotos(batch.photos);
    setCurrentPairIndex(0);
    setSelectedPhotos([]);
    setPhotosToDelete([]);
    setBatchCompleted(false);
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
    // Add all remaining photos to selected list
    setSelectedPhotos(prev => [...prev, ...remainingPhotos]);
    // Clear the remaining photos pool
    setRemainingPhotos([]);
    // Complete the batch
    onSelection([...selectedPhotos, ...remainingPhotos], photosToDelete);
    onBatchComplete();
  }, [remainingPhotos, selectedPhotos, photosToDelete, onSelection, onBatchComplete]);

  // Handle moving all remaining photos in the batch to delete folder
  const handleMoveAllRemaining = useCallback(() => {
    // Add all remaining photos to delete list
    setPhotosToDelete(prev => [...prev, ...remainingPhotos]);
    // Clear the remaining photos pool
    setRemainingPhotos([]);
    // Complete the batch
    onSelection(selectedPhotos, [...photosToDelete, ...remainingPhotos]);
    onBatchComplete();
  }, [remainingPhotos, selectedPhotos, photosToDelete, onSelection, onBatchComplete]);

  // Handle undo (go back to previous pair)
  const handleUndo = useCallback(() => {
    if (currentPairIndex > 0) {
      setCurrentPairIndex(prev => prev - 1);
      // Remove the last selected photos from the selection
      setSelectedPhotos(prev => {
        const currentPair = getCurrentPair();
        if (currentPair) {
          const [leftPhoto, rightPhoto] = currentPair;
          return prev.filter(photo => photo.id !== leftPhoto.id && photo.id !== rightPhoto.id);
        }
        return prev;
      });
      // Remove the last photos from delete list
      setPhotosToDelete(prev => {
        const currentPair = getCurrentPair();
        if (currentPair) {
          const [leftPhoto, rightPhoto] = currentPair;
          return prev.filter(photo => photo.id !== leftPhoto.id && photo.id !== rightPhoto.id);
        }
        return prev;
      });
    }
  }, [currentPairIndex, remainingPhotos]);

  // Handle keyboard input
  const handleKeyPress = useCallback((event: KeyboardEvent) => {
    switch (event.key.toLowerCase()) {
      case 'arrowleft':
        handleSelection('left');
        break;
      case 'arrowright':
        handleSelection('right');
        break;
      case 'arrowup':
        handleSelection('both');
        break;
      case 'arrowdown':
        handleSelection('neither');
        break;
      case 'enter':
        handleKeepAllRemaining();
        break;
      case 'space':
        handleMoveAllRemaining();
        break;
      case 'z':
        handleUndo();
        break;
    }
  }, [handleSelection, handleKeepAllRemaining, handleMoveAllRemaining, handleUndo]);

  // Set up keyboard event listeners
  useEffect(() => {
    window.addEventListener('keydown', handleKeyPress);
    return () => {
      window.removeEventListener('keydown', handleKeyPress);
    };
  }, [handleKeyPress]);

  const currentPair = getCurrentPair();
  const remainingCount = remainingPhotos.length;
  
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
        <h2>Batch {batch.id}</h2>
        <p>Select your preferred photos ({remainingCount} remaining)</p>
        <div className="photo-pair-viewer__controls">
          <div className="control-hint">
            <span className="key">←</span> - Keep Left Photo
          </div>
          <div className="control-hint">
            <span className="key">→</span> - Keep Right Photo
          </div>
          <div className="control-hint">
            <span className="key">↑</span> - Keep Both Photos
          </div>
          <div className="control-hint">
            <span className="key">↓</span> - Move Both to Delete
          </div>
          <div className="control-hint">
            <span className="key">Enter</span> - Keep All Remaining
          </div>
          <div className="control-hint">
            <span className="key">Space</span> - Move All Remaining to Delete
          </div>
          <div className="control-hint">
            <span className="key">Z</span> - Undo
          </div>
        </div>
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

        <div className="photo-pair-viewer__actions">
          <button 
            onClick={() => handleSelection('left')}
            className="action-button action-button--left"
          >
            Keep Left
          </button>
          <button 
            onClick={() => handleSelection('both')}
            className="action-button action-button--both"
          >
            Keep Both
          </button>
          <button 
            onClick={() => handleSelection('right')}
            className="action-button action-button--right"
          >
            Keep Right
          </button>
          <button 
            onClick={() => handleSelection('neither')}
            className="action-button action-button--neither"
          >
            Move Both to Delete
          </button>
          <button 
            onClick={handleKeepAllRemaining}
            className="action-button action-button--keep-all"
          >
            Keep All Remaining
          </button>
          <button 
            onClick={handleMoveAllRemaining}
            className="action-button action-button--move-all"
          >
            Move All Remaining to Delete
          </button>
          <button 
            onClick={handleUndo}
            className="action-button action-button--undo"
            disabled={currentPairIndex === 0}
          >
            Undo
          </button>
        </div>
      </div>
    </div>
  );
};

export default PhotoPairViewer;