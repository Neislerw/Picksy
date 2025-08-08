import React, { useEffect, useRef, useState, useCallback } from 'react';
import { Photo } from '../../types';
import '../styles/ThumbnailStripCuller.css';

interface ThumbnailStripCullerProps {
  folderPath: string;
  photos: Photo[];
  onExit: () => void;
  onComplete?: () => void;
}

type Action = 'moveToDelete' | 'undo';

interface MovedRecord {
  photo: Photo;
  fromPath: string;
  toPath: string;
}

const ThumbnailStripCuller: React.FC<ThumbnailStripCullerProps> = ({ folderPath, photos, onExit, onComplete }) => {
  const [index, setIndex] = useState<number>(0);
  const [movedStack, setMovedStack] = useState<MovedRecord[]>([]);
  const [movedPaths, setMovedPaths] = useState<Set<string>>(new Set());
  const containerRef = useRef<HTMLDivElement | null>(null);
  const [leadingSpacerPx, setLeadingSpacerPx] = useState<number>(0);

  const centerOnIndex = useCallback((i: number) => {
    const container = containerRef.current;
    const el = document.getElementById(`thumb-${i}`);
    if (!container || !el) return;
    const containerRect = container.getBoundingClientRect();
    const elRect = el.getBoundingClientRect();
    const delta = (elRect.left + elRect.width / 2) - (containerRect.left + containerRect.width / 2);
    container.scrollLeft += delta;
  }, []);

  useEffect(() => {
    centerOnIndex(index);
  }, [index, centerOnIndex]);

  // Measure container and first thumbnail to compute leading spacer so the first item can be centered
  useEffect(() => {
    const updateSpacer = () => {
      const container = containerRef.current;
      const firstThumb = document.getElementById('thumb-0');
      if (!container || !firstThumb) return;
      const containerWidth = container.clientWidth;
      const thumbWidth = firstThumb.clientWidth;
      const spacer = Math.max(0, Math.floor((containerWidth - thumbWidth) / 2));
      setLeadingSpacerPx(spacer);
    };
    updateSpacer();
    window.addEventListener('resize', updateSpacer);
    return () => window.removeEventListener('resize', updateSpacer);
  }, [photos.length]);

  const handleMoveToDelete = useCallback(async (photo: Photo) => {
    try {
      const res: Array<{ fromPath: string; toPath: string }> | undefined = await window.electron?.ipcRenderer.invoke(
        'process-photos',
        { selectedPhotos: [], photosToDelete: [photo] }
      );
      if (res && res[0]) {
        setMovedStack(prev => [...prev, { photo, fromPath: res[0].fromPath, toPath: res[0].toPath }]);
        setMovedPaths(prev => {
          const next = new Set(prev);
          next.add(res[0].fromPath);
          return next;
        });
      }
    } catch (e) {
      console.error('Failed to move to _delete:', e);
    }
  }, []);

  const handleRestoreSpecific = useCallback(async (photo: Photo) => {
    const idx = [...movedStack].reverse().findIndex(r => r.fromPath === photo.path);
    if (idx === -1) return;
    const record = movedStack[movedStack.length - 1 - idx];
    try {
      await window.electron?.ipcRenderer.invoke('restore-photo', record);
      setMovedStack(prev => prev.filter(r => !(r.fromPath === record.fromPath && r.toPath === record.toPath)));
      setMovedPaths(prev => {
        const next = new Set(prev);
        next.delete(record.fromPath);
        return next;
      });
    } catch (e) {
      console.error('Failed to restore specific photo:', e);
    }
  }, [movedStack]);

  const handleUndo = useCallback(async () => {
    const last = movedStack[movedStack.length - 1];
    if (!last) return;
    try {
      await window.electron?.ipcRenderer.invoke('restore-photo', last);
      setMovedStack(prev => prev.slice(0, -1));
      setMovedPaths(prev => {
        const next = new Set(prev);
        next.delete(last.fromPath);
        return next;
      });
    } catch (e) {
      console.error('Failed to undo move:', e);
    }
  }, [movedStack]);

  const onKeyDown = useCallback((e: KeyboardEvent) => {
    if (!photos.length) return;
    if (['ArrowLeft', 'ArrowRight', 'ArrowDown', 'ArrowUp', 'Space', 'KeyZ'].includes(e.code)) {
      e.preventDefault();
    }
    switch (e.code) {
      case 'ArrowLeft':
        setIndex(prev => Math.max(0, prev - 1));
        break;
      case 'ArrowRight':
        setIndex(prev => Math.min(photos.length - 1, prev + 1));
        break;
      case 'ArrowDown':
      case 'ArrowUp':
      case 'Space': {
        const current = photos[index];
        if (!current) break;
        if (movedPaths.has(current.path)) {
          void handleRestoreSpecific(current);
        } else {
          void handleMoveToDelete(current);
          setIndex(prev => Math.min(photos.length - 1, prev + 1));
        }
        // If we have processed every photo (moved or skipped), trigger completion
        if (index >= photos.length - 1 && onComplete) {
          onComplete();
        }
        break;
      }
      case 'KeyZ':
        handleUndo();
        break;
    }
  }, [photos, index, handleMoveToDelete, handleRestoreSpecific, movedPaths, handleUndo]);

  useEffect(() => {
    window.addEventListener('keydown', onKeyDown);
    return () => window.removeEventListener('keydown', onKeyDown);
  }, [onKeyDown]);

  return (
    <div className="thumb-strip__wrapper">
      <div className="thumb-strip" ref={containerRef}>
        <div style={{ flex: '0 0 auto', minWidth: `${leadingSpacerPx}px` }} />
        {/* simple lazy loading: render nearby thumbnails only */}
        {photos.map((p, i) => {
          const isSelected = i === index;
          const distance = Math.abs(i - index);
          const shouldRender = distance < 50; // window of 100 items
          return (
            <div
              id={`thumb-${i}`}
              key={p.id}
              className={`thumb ${isSelected ? 'thumb--selected' : ''} ${movedPaths.has(p.path) ? 'thumb--moved' : ''}`}
              onClick={() => setIndex(i)}
              tabIndex={0}
            >
              {shouldRender ? (
                <img
                  src={`file://${p.path}`}
                  alt={p.filename}
                  className="thumb__img"
                  loading="lazy"
                />
              ) : (
                <div className="thumb__placeholder" />
              )}
              <div className="thumb__meta">
                <span className="thumb__name">{p.filename}</span>
                <span className="thumb__index">{i + 1}/{photos.length}</span>
              </div>
            </div>
          );
        })}
        <div style={{ flex: '0 0 auto', minWidth: `${leadingSpacerPx}px` }} />
      </div>
      <div className="thumb-strip__footer">
        <button className="thumb-strip__btn" onClick={onExit}>
          Save and Quit
        </button>
        <div className="thumb-strip__hint">Keys: Left/Right to navigate, Space/Down to delete, Z to undo</div>
      </div>
    </div>
  );
};

export default ThumbnailStripCuller;



