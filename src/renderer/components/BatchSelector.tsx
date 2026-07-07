import React, { useState, useEffect } from 'react';
import '../styles/BatchSelector.css';
import logoImage from '../../../resources/logo.png';
import { ImmichConnectionConfig, MediaSourceType } from '../../types';

interface Settings {
  sourceType: MediaSourceType;
  batchTimeWindow: number; // in seconds
  minBatchSize: number;
  maxBatchSize: number;
  includeSubfolders: boolean;
  supportedExtensions: string[];
  excludePatterns: string[];
  autoSaveInterval: number; // in minutes
  sortingMode?: 'dateTaken' | 'dateCreated' | 'filename';
  dateFrom?: string;
  dateTo?: string;
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
  onFolderSelect: (
    folderPath: string,
    includeSubfolders: boolean,
    settings: Settings,
    mode: CullingMode,
    immichConfig?: ImmichConnectionConfig | null
  ) => void;
  isLoading?: boolean;
}

const LOCAL_DEFAULT_EXTENSIONS = ['.jpg', '.jpeg', '.png', '.gif', '.bmp', '.tiff', '.webp'];
const IMMICH_DEFAULT_EXTENSIONS = [...LOCAL_DEFAULT_EXTENSIONS, '.heic', '.heif'];

const BatchSelector: React.FC<BatchSelectorProps> = ({ onFolderSelect, isLoading = false }) => {
  const [selectedPath, setSelectedPath] = useState<string>('');
  const [showSettings, setShowSettings] = useState<boolean>(false);
  const [mode, setMode] = useState<CullingMode>('tournament');
  const [progress, setProgress] = useState<{ stage: string; current: number; total: number } | null>(null);
  const [immichServerUrl, setImmichServerUrl] = useState('');
  const [immichApiKey, setImmichApiKey] = useState('');
  const [localPathPrefix, setLocalPathPrefix] = useState('');
  const [immichPathPrefix, setImmichPathPrefix] = useState('');
  const [connectionStatus, setConnectionStatus] = useState<string>('');
  const [isTestingConnection, setIsTestingConnection] = useState(false);
  
  // Default settings
  const [settings, setSettings] = useState<Settings>({
    sourceType: 'local',
    batchTimeWindow: 30, // 30 seconds
    minBatchSize: 2,
    maxBatchSize: 20,
    includeSubfolders: false,
    supportedExtensions: LOCAL_DEFAULT_EXTENSIONS,
    excludePatterns: [],
    autoSaveInterval: 0, // Auto-save after each selection (not interval-based)
    sortingMode: 'dateTaken',
    dateFrom: '',
    dateTo: '',
    video: {
      scrubForwardSeconds: 5,
      scrubBackwardSeconds: 3,
      sortBy: 'date',
      sortOrder: 'asc',
      dateSource: 'filename'
    }
  });

  const applySourceType = (sourceType: MediaSourceType) => {
    setSettings((prev) => ({
      ...prev,
      sourceType,
      includeSubfolders: sourceType === 'immich-external' ? true : prev.includeSubfolders,
      supportedExtensions:
        sourceType === 'immich-external' ? IMMICH_DEFAULT_EXTENSIONS : LOCAL_DEFAULT_EXTENSIONS,
      excludePatterns: sourceType === 'immich-external' ? ['**/Raw/**', '**/*.xmp'] : [],
    }));
    if (sourceType === 'local') {
      setConnectionStatus('');
    }
  };

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

  const buildImmichConfig = (): ImmichConnectionConfig | null => {
    if (settings.sourceType !== 'immich-external') return null;
    const config: ImmichConnectionConfig = {
      serverUrl: immichServerUrl.trim(),
      apiKey: immichApiKey.trim(),
    };
    if (localPathPrefix.trim() && immichPathPrefix.trim()) {
      config.pathMapping = {
        localPrefix: localPathPrefix.trim(),
        immichPrefix: immichPathPrefix.trim(),
      };
    }
    return config;
  };

  const handleTestConnection = async () => {
    const config = buildImmichConfig();
    if (!config?.serverUrl || !config?.apiKey) {
      setConnectionStatus('Enter server URL and API key');
      return;
    }
    setIsTestingConnection(true);
    setConnectionStatus('Testing connection...');
    try {
      const result = await window.electron?.ipcRenderer.invoke('immich-test-connection', config);
      if (result?.ok) {
        if (result.effectiveServerUrl) {
          setImmichServerUrl(result.effectiveServerUrl);
        }
        const versionMsg = result.serverVersion ? ` (v${result.serverVersion})` : '';
        const protocolMsg = result.protocolDowngraded ? ' — switched to HTTP' : '';
        setConnectionStatus(`Connected${versionMsg}${protocolMsg}`);
      } else {
        setConnectionStatus(result?.error || 'Connection failed');
      }
    } catch (e) {
      setConnectionStatus('Connection failed');
    } finally {
      setIsTestingConnection(false);
    }
  };

  const handleProcessFolder = () => {
    if (selectedPath) {
      const immichConfig = buildImmichConfig();
      if (settings.sourceType === 'immich-external') {
        if (!immichConfig?.serverUrl || !immichConfig?.apiKey) {
          setConnectionStatus('Immich server URL and API key are required');
          return;
        }
      }
      if (settings.dateFrom && settings.dateTo && settings.dateFrom > settings.dateTo) {
        setConnectionStatus('Start date must be on or before end date');
        return;
      }
      (window as any).lastSettings = settings;
      onFolderSelect(selectedPath, settings.includeSubfolders, settings, mode, immichConfig);
    }
  };

  const hasDateRange = !!(settings.dateFrom || settings.dateTo);
  const pct = progress && progress.total > 0
    ? Math.min(100, Math.round((progress.current / progress.total) * 100))
    : 0;
  const progressLabel = progress
    ? progress.total > 0
      ? `${progress.stage} ${pct}% (${progress.current}/${progress.total})`
      : progress.stage
    : 'Preparing...';

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
              {progressLabel}
            </div>
          </div>
        )}
        
        <div className="options">
          <div className="mode-selector">
            <span className="mode-selector__label">Source:</span>
            <label className="radio-option">
              <input
                type="radio"
                name="source-type"
                value="local"
                checked={settings.sourceType === 'local'}
                onChange={() => applySourceType('local')}
              />
              <span>Local Folder</span>
            </label>
            <label className="radio-option">
              <input
                type="radio"
                name="source-type"
                value="immich-external"
                checked={settings.sourceType === 'immich-external'}
                onChange={() => applySourceType('immich-external')}
              />
              <span>Immich External Library</span>
            </label>
          </div>
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

          <div className="date-range-filter" style={{ marginTop: '0.75rem', display: 'flex', flexWrap: 'wrap', alignItems: 'center', gap: '0.75rem' }}>
            <span className="mode-selector__label">Date range (optional):</span>
            <label className="setting-label" style={{ margin: 0 }}>
              From
              <input
                type="date"
                value={settings.dateFrom || ''}
                onChange={(e) => setSettings(prev => ({ ...prev, dateFrom: e.target.value }))}
                className="setting-input"
                style={{ marginLeft: 6 }}
              />
            </label>
            <label className="setting-label" style={{ margin: 0 }}>
              To
              <input
                type="date"
                value={settings.dateTo || ''}
                onChange={(e) => setSettings(prev => ({ ...prev, dateTo: e.target.value }))}
                className="setting-input"
                style={{ marginLeft: 6 }}
              />
            </label>
            {hasDateRange && (
              <button
                type="button"
                className="settings-toggle"
                onClick={() => setSettings(prev => ({ ...prev, dateFrom: '', dateTo: '' }))}
              >
                Clear dates
              </button>
            )}
          </div>
          {hasDateRange && (
            <span className="setting-help" style={{ display: 'block', marginTop: 4 }}>
              Only files whose date falls in this range will be scanned{progress && progress.total === 0 ? ' — progress shows matched count while scanning' : ''}.
            </span>
          )}

          <button
            onClick={() => setShowSettings(!showSettings)}
            className="settings-toggle"
          >
            {showSettings ? 'Hide Settings' : 'Show Settings'}
          </button>
        </div>

        {settings.sourceType === 'immich-external' && (
          <div className="settings-panel" style={{ marginBottom: '1rem' }}>
            <h3>Immich Connection</h3>
            <div className="settings-grid">
              <div className="setting-group">
                <label className="setting-label">
                  Server URL:
                  <input
                    type="text"
                    value={immichServerUrl}
                    onChange={(e) => setImmichServerUrl(e.target.value)}
                    className="setting-input"
                    placeholder="http://192.168.7.254:2283"
                  />
                </label>
              </div>
              <div className="setting-group">
                <label className="setting-label">
                  API Key:
                  <input
                    type="password"
                    value={immichApiKey}
                    onChange={(e) => setImmichApiKey(e.target.value)}
                    className="setting-input"
                    placeholder="Immich API key"
                  />
                </label>
                <span className="setting-help">Immich on port 2283 is usually HTTP. If you enter https:// by mistake, Test Connection will switch to http:// automatically. API key is stored in memory for this session only.</span>
              </div>
              <div className="setting-group">
                <label className="setting-label">
                  Local Path Prefix:
                  <input
                    type="text"
                    value={localPathPrefix}
                    onChange={(e) => setLocalPathPrefix(e.target.value)}
                    className="setting-input"
                    placeholder="E:\Photos"
                  />
                </label>
              </div>
              <div className="setting-group">
                <label className="setting-label">
                  Immich Path Prefix:
                  <input
                    type="text"
                    value={immichPathPrefix}
                    onChange={(e) => setImmichPathPrefix(e.target.value)}
                    className="setting-input"
                    placeholder="/mnt/photos"
                  />
                </label>
                <span className="setting-help">Optional mapping between your HDD path and Immich container path</span>
              </div>
              <div className="setting-group">
                <button
                  type="button"
                  onClick={handleTestConnection}
                  disabled={isTestingConnection || isLoading}
                  className="folder-input__button"
                >
                  {isTestingConnection ? 'Testing...' : 'Test Connection'}
                </button>
                {connectionStatus && (
                  <span className="setting-help" style={{ display: 'block', marginTop: 6 }}>
                    {connectionStatus}
                  </span>
                )}
              </div>
            </div>
          </div>
        )}
        
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