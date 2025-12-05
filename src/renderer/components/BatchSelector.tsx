import React, { useState, useEffect } from 'react';
import '../styles/BatchSelector.css';
import logoImage from '../../../resources/logo.png';

interface Settings {
  batchTimeWindow: number; // in seconds
  minBatchSize: number;
  maxBatchSize: number;
  includeSubfolders: boolean;
  supportedExtensions: string[];
  autoSaveInterval: number; // in minutes
  sortingMode?: 'dateTaken' | 'dateCreated' | 'filename';
  video?: {
    scrubForwardSeconds: number;
    scrubBackwardSeconds: number;
    sortBy: 'date' | 'size' | 'filename' | 'duration';
    sortOrder: 'asc' | 'desc';
    dateSource: 'filename' | 'created';
  };
}

type CullingMode = 'tournament' | 'thumbnail' | 'video';

interface BatchSelectorProps {
  onFolderSelect: (folderPath: string, includeSubfolders: boolean, settings: Settings, mode: CullingMode) => void;
  isLoading?: boolean;
}

const BatchSelector: React.FC<BatchSelectorProps> = ({ onFolderSelect, isLoading = false }) => {
  const [selectedPath, setSelectedPath] = useState<string>('');
  const [showSettings, setShowSettings] = useState<boolean>(false);
  const [mode, setMode] = useState<CullingMode>('tournament');
  const [progress, setProgress] = useState<{ stage: string; current: number; total: number } | null>(null);
  
  // Default settings
  const [settings, setSettings] = useState<Settings>({
    batchTimeWindow: 30, // 30 seconds
    minBatchSize: 2,
    maxBatchSize: 20,
    includeSubfolders: false,
    supportedExtensions: ['.jpg', '.jpeg', '.png', '.gif', '.bmp', '.tiff', '.webp'],
    autoSaveInterval: 0, // Auto-save after each selection (not interval-based)
    sortingMode: 'dateTaken',
    video: {
      scrubForwardSeconds: 5,
      scrubBackwardSeconds: 3,
      sortBy: 'date',
      sortOrder: 'asc',
      dateSource: 'filename'
    }
  });

  useEffect(() => {
    const handler = (update: { stage: string; current: number; total: number }) => {
      setProgress(update);
    };
    window.electron?.ipcRenderer.on('scan-progress', handler);
    return () => {
      window.electron?.ipcRenderer.removeListener('scan-progress', handler as any);
    };
  }, []);

  useEffect(() => {
    if (!isLoading) setProgress(null);
  }, [isLoading]);

  const handleFolderSelect = async () => {
    try {
      const folderPath = await window.electron?.ipcRenderer?.invoke('select-folder');
      if (folderPath) {
        setSelectedPath(folderPath);
        // Do not auto-process; require explicit Process click
      }
    } catch (error) {
      console.error('Error selecting folder:', error);
    }
  };

  const handleManualPathChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setSelectedPath(e.target.value);
  };

  const handleProcessFolder = () => {
    if (selectedPath) {
      // Expose settings for App to pass down to VideoMode as initial props
      (window as any).lastSettings = settings;
      onFolderSelect(selectedPath, settings.includeSubfolders, settings, mode);
    }
  };

  const pct = progress && progress.total > 0 ? Math.min(100, Math.round((progress.current / progress.total) * 100)) : 0;

  return (
    <div className="batch-selector">
      <div className="batch-selector__header">
        <div className="logo-container">
          <img src={logoImage} alt="Picksy Logo" className="logo" />
        </div>
      </div>
      
      <div className="batch-selector__content">
        <div className="folder-input">
          <input
            type="text"
            value={selectedPath}
            onChange={handleManualPathChange}
            placeholder="Enter folder path or browse"
            className="folder-input__field"
          />
          <button
            onClick={handleFolderSelect}
            disabled={isLoading}
            className="folder-input__button"
          >
            Browse
          </button>
          {selectedPath && !isLoading && (
            <button
              onClick={handleProcessFolder}
              className="folder-input__button folder-input__button--primary"
            >
              Process Folder
            </button>
          )}
        </div>
        
        {isLoading && (
          <div style={{ marginBottom: '1rem' }}>
            <div style={{ height: 8, background: '#444', borderRadius: 4, overflow: 'hidden' }}>
              <div style={{ width: `${pct}%`, height: '100%', background: '#27ae60', transition: 'width 0.2s ease' }} />
            </div>
            <div style={{ color: '#cccccc', marginTop: 6, fontSize: 12 }}>
              {progress ? `${progress.stage} ${pct}% (${progress.current}/${progress.total})` : 'Preparing...'}
            </div>
          </div>
        )}
        
        <div className="options">
          <div className="mode-selector">
            <span className="mode-selector__label">Mode:</span>
            <label className="radio-option">
              <input
                type="radio"
                name="culling-mode"
                value="tournament"
                checked={mode === 'tournament'}
                onChange={() => setMode('tournament')}
              />
              <span>Tournament</span>
            </label>
            <label className="radio-option">
              <input
                type="radio"
                name="culling-mode"
                value="thumbnail"
                checked={mode === 'thumbnail'}
                onChange={() => setMode('thumbnail')}
              />
              <span>Thumbnail Strip</span>
            </label>
            <label className="radio-option">
              <input
                type="radio"
                name="culling-mode"
                value="video"
                checked={mode === 'video'}
                onChange={() => setMode('video')}
              />
              <span>Video Mode</span>
            </label>
          </div>
          <label className="checkbox-option" style={{ marginLeft: 16 }}>
            <input
              type="checkbox"
              checked={settings.includeSubfolders}
              onChange={(e) => setSettings(prev => ({ ...prev, includeSubfolders: e.target.checked }))}
            />
            <span>Include subfolders</span>
          </label>

          <button
            onClick={() => setShowSettings(!showSettings)}
            className="settings-toggle"
          >
            {showSettings ? 'Hide Settings' : 'Show Settings'}
          </button>
        </div>
        
        {showSettings && (
          <div className="settings-panel">
            <h3>Tournament Settings</h3>
            
            <div className="settings-grid">
              <div className="setting-group">
                <label className="setting-label">
                  Sorting Mode:
                  <select
                    className="setting-input"
                    value={settings.sortingMode}
                    onChange={(e) => setSettings(prev => ({ ...prev, sortingMode: e.target.value as any }))}
                  >
                    <option value="dateTaken">Date Taken</option>
                    <option value="dateCreated">Date Created</option>
                    <option value="filename">Filename</option>
                  </select>
                </label>
                <span className="setting-help">Choose how photos are ordered and batched</span>
              </div>
              <div className="setting-group">
                <label className="setting-label">
                  Batch Time Window (seconds):
                  <input
                    type="number"
                    min="1"
                    max="300"
                    value={settings.batchTimeWindow}
                    onChange={(e) => setSettings(prev => ({ ...prev, batchTimeWindow: parseInt(e.target.value) || 30 }))}
                    className="setting-input"
                  />
                </label>
                <span className="setting-help">Maximum time gap between photos to group them in the same batch</span>
              </div>
              
              <div className="setting-group">
                <label className="setting-label">
                  Minimum Batch Size:
                  <input
                    type="number"
                    min="1"
                    max="10"
                    value={settings.minBatchSize}
                    onChange={(e) => setSettings(prev => ({ ...prev, minBatchSize: parseInt(e.target.value) || 2 }))}
                    className="setting-input"
                  />
                </label>
                <span className="setting-help">Minimum number of photos required to form a batch</span>
              </div>
              
              <div className="setting-group">
                <label className="setting-label">
                  Maximum Batch Size:
                  <input
                    type="number"
                    min="2"
                    max="50"
                    value={settings.maxBatchSize}
                    onChange={(e) => setSettings(prev => ({ ...prev, maxBatchSize: parseInt(e.target.value) || 20 }))}
                    className="setting-input"
                  />
                </label>
                <span className="setting-help">Maximum number of photos in a batch</span>
              </div>
              
              <div className="setting-group">
                <label className="setting-label">
                  Supported File Extensions:
                  <input
                    type="text"
                    value={settings.supportedExtensions.join(', ')}
                    onChange={(e) => setSettings(prev => ({ 
                      ...prev, 
                      supportedExtensions: e.target.value.split(',').map(ext => ext.trim()).filter(ext => ext)
                    }))}
                    className="setting-input"
                    placeholder=".jpg, .jpeg, .png, .gif, .bmp, .tiff, .webp"
                  />
                </label>
                <span className="setting-help">Comma-separated list of file extensions to process</span>
              </div>
              
              {/* Include subfolders moved outside */}
            </div>
            <h3 style={{ marginTop: 16 }}>Thumbnail Settings</h3>
            <div className="settings-grid">
              <div className="setting-group">
                <span className="setting-help">No extra settings yet</span>
              </div>
            </div>

            <h3 style={{ marginTop: 16 }}>Video Settings</h3>
            <div className="settings-grid">
              <div className="setting-group">
                <label className="setting-label">
                  Scrub Forward (seconds):
                  <input
                    type="number"
                    min="1"
                    max="60"
                    value={settings.video?.scrubForwardSeconds ?? 5}
                    onChange={(e) => setSettings(prev => ({ ...prev, video: { ...(prev.video || { sortBy: 'date', sortOrder: 'asc', dateSource: 'filename', scrubForwardSeconds: 5, scrubBackwardSeconds: 3 }), scrubForwardSeconds: Math.max(1, parseInt(e.target.value) || 5) } }))}
                    className="setting-input"
                  />
                </label>
                <span className="setting-help">Right arrow while playing scrubs forward by this amount</span>
              </div>

              <div className="setting-group">
                <label className="setting-label">
                  Scrub Backward (seconds):
                  <input
                    type="number"
                    min="1"
                    max="60"
                    value={settings.video?.scrubBackwardSeconds ?? 3}
                    onChange={(e) => setSettings(prev => ({ ...prev, video: { ...(prev.video || { sortBy: 'date', sortOrder: 'asc', dateSource: 'filename', scrubForwardSeconds: 5, scrubBackwardSeconds: 3 }), scrubBackwardSeconds: Math.max(1, parseInt(e.target.value) || 3) } }))}
                    className="setting-input"
                  />
                </label>
                <span className="setting-help">Left arrow while playing scrubs backward by this amount</span>
              </div>

              <div className="setting-group">
                <label className="setting-label">
                  Sort By:
                  <select
                    className="setting-input"
                    value={settings.video?.sortBy ?? 'date'}
                    onChange={(e) => setSettings(prev => ({
                      ...prev,
                      video: {
                        ...(prev.video || { scrubForwardSeconds: 5, scrubBackwardSeconds: 3, sortOrder: 'asc', dateSource: 'filename', sortBy: 'date' }),
                        sortBy: e.target.value as any
                      }
                    }))}
                  >
                    <option value="date">Date</option>
                    <option value="size">Size</option>
                    <option value="filename">Filename</option>
                    <option value="duration">Duration</option>
                  </select>
                </label>
              </div>

              <div className="setting-group">
                <label className="setting-label">
                  Sort Order:
                  <select
                    className="setting-input"
                    value={settings.video?.sortOrder ?? 'asc'}
                    onChange={(e) => setSettings(prev => ({
                      ...prev,
                      video: {
                        ...(prev.video || { scrubForwardSeconds: 5, scrubBackwardSeconds: 3, sortBy: 'date', dateSource: 'filename', sortOrder: 'asc' }),
                        sortOrder: e.target.value as any
                      }
                    }))}
                  >
                    <option value="asc">Ascending</option>
                    <option value="desc">Descending</option>
                  </select>
                </label>
              </div>

              <div className="setting-group">
                <label className="setting-label">
                  Date Source:
                  <select
                    className="setting-input"
                    value={settings.video?.dateSource ?? 'filename'}
                    onChange={(e) => setSettings(prev => ({
                      ...prev,
                      video: {
                        ...(prev.video || { scrubForwardSeconds: 5, scrubBackwardSeconds: 3, sortBy: 'date', sortOrder: 'asc', dateSource: 'filename' }),
                        dateSource: e.target.value as any
                      }
                    }))}
                  >
                    <option value="filename">Filename Timestamp</option>
                    <option value="created">File Created</option>
                  </select>
                </label>
                <span className="setting-help">If filename has no timestamp, created date is used</span>
              </div>
            </div>
          </div>
        )}
        
        {selectedPath && (
          <div className="folder-info">
            <p>Selected folder: {selectedPath}</p>
          </div>
        )}
      </div>
    </div>
  );
};

export default BatchSelector; 