import React, { useEffect, useRef, useState, useCallback } from 'react';
import { Video } from '../../types';
import '../styles/VideoMode.css';
import Keycap from './Keycap';

interface VideoModeProps {
  folderPath: string;
  videos: Video[];
  onExit: () => void;
  onComplete?: () => void;
  onKeep?: (video: Video) => void;
  onDelete?: (video: Video) => void;
  onRestore?: (video: Video) => void;
  selections?: Record<string, 'kept' | 'discarded'>;
  initialSortBy?: 'date' | 'size' | 'filename' | 'duration';
  initialSortOrder?: 'asc' | 'desc';
  scrubForwardSeconds?: number;
  scrubBackwardSeconds?: number;
  dateSource?: 'filename' | 'created';
}

type Action = 'moveToDelete' | 'undo';

interface MovedRecord {
  video: Video;
  fromPath: string;
  toPath: string;
}

const VideoMode: React.FC<VideoModeProps> = ({ folderPath, videos, onExit, onComplete, onKeep, onDelete, onRestore, selections = {}, initialSortBy = 'date', initialSortOrder = 'asc', scrubForwardSeconds = 5, scrubBackwardSeconds = 3, dateSource = 'filename' }) => {
  const [index, setIndex] = useState<number>(0);
  const [movedStack, setMovedStack] = useState<MovedRecord[]>([]);
  const [movedPaths, setMovedPaths] = useState<Set<string>>(new Set());
  const [isPlaying, setIsPlaying] = useState<boolean>(false);
  const containerRef = useRef<HTMLDivElement | null>(null);
  const videoRef = useRef<HTMLVideoElement | null>(null);
  const [leadingSpacerPx, setLeadingSpacerPx] = useState<number>(0);
  const [durationMap, setDurationMap] = useState<Record<string, number>>({});
  const [sortBy, setSortBy] = useState<'date' | 'size' | 'filename' | 'duration'>(initialSortBy);
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>(initialSortOrder);

  useEffect(() => {
    setSortBy(initialSortBy);
  }, [initialSortBy]);

  useEffect(() => {
    setSortOrder(initialSortOrder);
  }, [initialSortOrder]);

  const getEffectiveDuration = useCallback((v: Video): number => {
    const d = (v.duration ?? durationMap[v.path]);
    return typeof d === 'number' && !isNaN(d) ? d : -1; // unknown duration sorts last
  }, [durationMap]);

  const displayVideos = React.useMemo(() => {
    const list = [...videos];
    const factor = sortOrder === 'asc' ? 1 : -1;
    switch (sortBy) {
      case 'size':
        list.sort((a, b) => ((a.fileSize || 0) - (b.fileSize || 0)) * factor);
        break;
      case 'filename':
        list.sort((a, b) => a.filename.localeCompare(b.filename, undefined, { numeric: true, sensitivity: 'base' }) * factor);
        break;
      case 'duration':
        list.sort((a, b) => (getEffectiveDuration(a) - getEffectiveDuration(b)) * factor);
        break;
      case 'date':
      default:
        list.sort((a, b) => (a.timestamp.getTime() - b.timestamp.getTime()) * factor);
        break;
    }
    return list;
  }, [videos, sortBy, sortOrder, getEffectiveDuration]);

  // Reset selection when sort changes
  useEffect(() => {
    setIndex(0);
  }, [sortBy]);

  const centerOnIndex = useCallback((i: number) => {
    const container = containerRef.current;
    const el = document.getElementById(`video-thumb-${i}`);
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
      const firstThumb = document.getElementById('video-thumb-0');
      if (!container || !firstThumb) return;
      const containerWidth = container.clientWidth;
      const thumbWidth = firstThumb.clientWidth;
      const spacer = Math.max(0, Math.floor((containerWidth - thumbWidth) / 2));
      setLeadingSpacerPx(spacer);
    };
    updateSpacer();
    window.addEventListener('resize', updateSpacer);
    return () => window.removeEventListener('resize', updateSpacer);
  }, [videos.length]);

  const handleMoveToDelete = useCallback(async (video: Video) => {
    try {
      const res: Array<{ fromPath: string; toPath: string }> | undefined = await (window as any).electron?.ipcRenderer.invoke(
        'process-photos',
        { selectedPhotos: [], photosToDelete: [video] }
      );
      if (res && res[0]) {
        setMovedStack(prev => [...prev, { video, fromPath: res[0].fromPath, toPath: res[0].toPath }]);
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

  const handleRestoreSpecific = useCallback(async (video: Video) => {
    const idx = [...movedStack].reverse().findIndex(r => r.fromPath === video.path);
    if (idx === -1) return;
    const record = movedStack[movedStack.length - 1 - idx];
    try {
      await (window as any).electron?.ipcRenderer.invoke('restore-photo', record);
      setMovedStack(prev => prev.filter(r => !(r.fromPath === record.fromPath && r.toPath === record.toPath)));
      setMovedPaths(prev => {
        const next = new Set(prev);
        next.delete(record.fromPath);
        return next;
      });
    } catch (e) {
      console.error('Failed to restore specific video:', e);
    }
  }, [movedStack]);

  const handleUndo = useCallback(async () => {
    const last = movedStack[movedStack.length - 1];
    if (!last) return;
    try {
      await (window as any).electron?.ipcRenderer.invoke('restore-photo', last);
      setMovedStack(prev => prev.slice(0, -1));
      setMovedPaths(prev => {
        const next = new Set(prev);
        next.delete(last.fromPath);
        return next;
      });
      // Also move back to previous video for user context
      setIndex(prev => Math.max(0, prev - 1));
      if (onRestore) onRestore(last.video);
    } catch (e) {
      console.error('Failed to undo move:', e);
    }
  }, [movedStack, onRestore]);

  const handlePlayPause = useCallback(() => {
    if (videoRef.current) {
      const el = videoRef.current;
      if (el.paused) {
        el.play();
      } else {
        el.pause();
      }
    }
  }, [isPlaying]);

  const formatFileSize = (bytes: number): string => {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
  };

  const formatDuration = (seconds?: number): string => {
    if (!seconds || isNaN(seconds)) return 'Unknown';
    const hours = Math.floor(seconds / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    const secs = Math.floor(seconds % 60);
    if (hours > 0) {
      return `${hours}:${minutes.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
    } else {
      return `${minutes}:${secs.toString().padStart(2, '0')}`;
    }
  };

  const onKeyDown = useCallback((e: KeyboardEvent) => {
    if (!displayVideos.length) return;
    // Do not blanket prevent default; handle per-key to avoid double-toggling with native video controls
    // If video is currently playing, use left/right to scrub timeline instead of navigating
    const player = videoRef.current;
    const isPlayerActive = !!player && !player.paused;
    const SCRUB_BACK = Math.max(1, scrubBackwardSeconds || 3);
    const SCRUB_FWD = Math.max(1, scrubForwardSeconds || 5);
    switch (e.code) {
      case 'ArrowLeft':
        e.preventDefault();
        if (isPlayerActive && player) {
          const nextTime = Math.max(0, player.currentTime - SCRUB_BACK);
          player.currentTime = nextTime;
        } else {
          setIndex(prev => Math.max(0, prev - 1));
        }
        break;
      case 'ArrowRight':
        e.preventDefault();
        if (isPlayerActive && player) {
          const dur = isNaN(player.duration) ? Infinity : player.duration;
          const nextTime = Math.min(dur, player.currentTime + SCRUB_FWD);
          player.currentTime = nextTime;
        } else {
          setIndex(prev => Math.min(displayVideos.length - 1, prev + 1));
        }
        break;
      case 'ArrowDown': {
        e.preventDefault();
        const current = displayVideos[index];
        if (!current) break;
        // Move to _delete and advance; if already moved, just advance
        if (!movedPaths.has(current.path)) {
          void handleMoveToDelete(current);
          if (onDelete) onDelete(current);
        }
        setIndex(prev => Math.min(displayVideos.length - 1, prev + 1));
        // If we have processed every video (moved or skipped), trigger completion
        if (index >= displayVideos.length - 1 && onComplete) {
          onComplete();
        }
        break;
      }
      case 'ArrowUp': {
        e.preventDefault();
        const current = displayVideos[index];
        if (!current) break;
        // Keep (ensure not in _delete) and advance
        if (movedPaths.has(current.path)) {
          void handleRestoreSpecific(current);
          if (onRestore) onRestore(current);
        }
        if (onKeep) onKeep(current);
        setIndex(prev => Math.min(displayVideos.length - 1, prev + 1));
        if (index >= displayVideos.length - 1 && onComplete) {
          onComplete();
        }
        break;
      }
      case 'Space':
        {
          const ae = (document && document.activeElement) as Element | null;
          const isVideoFocused = ae === player;
          if (!isVideoFocused) {
            e.preventDefault();
            handlePlayPause();
          }
        }
        break;
      case 'KeyZ':
        e.preventDefault();
        handleUndo();
        break;
    }
  }, [displayVideos, index, handleMoveToDelete, handleRestoreSpecific, movedPaths, handleUndo, handlePlayPause, onKeep, onDelete, onRestore, onComplete]);

  useEffect(() => {
    window.addEventListener('keydown', onKeyDown as any);
    return () => window.removeEventListener('keydown', onKeyDown as any);
  }, [onKeyDown]);

  const currentVideo = displayVideos[index];

  return (
    <div className="video-mode__wrapper">
      <div className="video-mode__main">
        {currentVideo && (
          <div className="video-mode__player">
            <video
              ref={videoRef}
              src={`file://${currentVideo.path}`}
              className="video-mode__video"
              onPlay={() => setIsPlaying(true)}
              onPause={() => setIsPlaying(false)}
              onEnded={() => setIsPlaying(false)}
              onLoadedMetadata={(e) => {
                const dur = (e.currentTarget as HTMLVideoElement).duration;
                if (!isNaN(dur)) {
                  setDurationMap(prev => ({ ...prev, [currentVideo.path]: dur }));
                }
              }}
              controls
            />
            <div className="video-mode__info">
              <h3 className="video-mode__filename">{currentVideo.filename}</h3>
              <div className="video-mode__metadata">
                <span className="video-mode__timestamp">
                  {(dateSource === 'created' || currentVideo.timestampSource === 'created') ? 'Date created: ' : 'Date: '}{currentVideo.timestamp.toLocaleString()}
                </span>
                <span className="video-mode__duration">
                  Duration: {formatDuration(currentVideo.duration ?? durationMap[currentVideo.path])}
                </span>
                <span className="video-mode__filesize">
                  Size: {formatFileSize(currentVideo.fileSize)}
                </span>
              </div>
            </div>
          </div>
        )}
      </div>

      <div className="video-mode__thumbnails" ref={containerRef}>
        <div style={{ flex: '0 0 auto', minWidth: `${leadingSpacerPx}px` }} />
        {displayVideos.map((v, i) => {
          const isSelected = i === index;
          const distance = Math.abs(i - index);
          const shouldRender = distance < 20; // window of 40 items
          return (
            <div
              id={`video-thumb-${i}`}
              key={v.id}
              className={`video-thumb ${isSelected ? 'video-thumb--selected' : ''} ${
                selections[v.path] === 'discarded' || movedPaths.has(v.path)
                  ? 'video-thumb--deleted'
                  : selections[v.path] === 'kept'
                    ? 'video-thumb--kept'
                    : 'video-thumb--undecided'
              }`}
              onClick={() => setIndex(i)}
              tabIndex={0}
            >
              {shouldRender ? (
                <video
                  src={`file://${v.path}`}
                  className="video-thumb__video"
                  muted
                  preload="metadata"
                  onLoadedMetadata={(e) => {
                    const dur = (e.currentTarget as HTMLVideoElement).duration;
                    if (!isNaN(dur)) {
                      setDurationMap(prev => ({ ...prev, [v.path]: dur }));
                    }
                  }}
                />
              ) : (
                <div className="video-thumb__placeholder" />
              )}
              <div className="video-thumb__meta">
                <span className="video-thumb__name">{v.filename}</span>
                <span className="video-thumb__index">{i + 1}/{displayVideos.length}</span>
                <span className="video-thumb__duration">{formatDuration(v.duration ?? durationMap[v.path])}</span>
                <span className="video-thumb__size">{formatFileSize(v.fileSize)}</span>
              </div>
            </div>
          );
        })}
        <div style={{ flex: '0 0 auto', minWidth: `${leadingSpacerPx}px` }} />
      </div>

      <div className="video-mode__footer">
        <button className="video-mode__btn" onClick={onExit}>
          Save and Quit
        </button>
        <div className="video-mode__sort">
          <label className="video-mode__sort-label">Sort:</label>
          <select
            className="video-mode__sort-select"
            value={sortBy}
            onChange={(e) => setSortBy(e.target.value as any)}
          >
            <option value="date">By date</option>
            <option value="size">By size</option>
            <option value="filename">By filename</option>
            <option value="duration">By duration</option>
          </select>
          <select
            className="video-mode__sort-select"
            value={sortOrder}
            onChange={(e) => setSortOrder(e.target.value as any)}
          >
            <option value="asc">Ascending</option>
            <option value="desc">Descending</option>
          </select>
        </div>
        <div className="video-mode__hint">
          Keys: <Keycap>←</Keycap>/<Keycap>→</Keycap> navigate, <Keycap>↑</Keycap>/<Keycap>↓</Keycap> delete, <Keycap>Space</Keycap> play/pause, <Keycap>Z</Keycap> undo
        </div>
      </div>
    </div>
  );
};

export default VideoMode;
