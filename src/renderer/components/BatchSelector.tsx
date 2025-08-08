import React, { useState } from 'react';
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
}

type CullingMode = 'tournament' | 'thumbnail';

interface BatchSelectorProps {
  onFolderSelect: (folderPath: string, includeSubfolders: boolean, settings: Settings, mode: CullingMode) => void;
  isLoading?: boolean;
}

const BatchSelector: React.FC<BatchSelectorProps> = ({ onFolderSelect, isLoading = false }) => {
  const [selectedPath, setSelectedPath] = useState<string>('');
  const [showSettings, setShowSettings] = useState<boolean>(false);
  const [mode, setMode] = useState<CullingMode>('tournament');
  
  // Default settings
  const [settings, setSettings] = useState<Settings>({
    batchTimeWindow: 30, // 30 seconds
    minBatchSize: 2,
    maxBatchSize: 20,
    includeSubfolders: true,
    supportedExtensions: ['.jpg', '.jpeg', '.png', '.gif', '.bmp', '.tiff', '.webp'],
    autoSaveInterval: 0, // Auto-save after each selection (not interval-based)
    sortingMode: 'dateTaken'
  });

  const handleFolderSelect = async () => {
    try {
      // In a real implementation, this would use Electron's dialog API
      // For now, we'll simulate folder selection
      const folderPath = await window.electron?.ipcRenderer?.invoke('select-folder');
      if (folderPath) {
        setSelectedPath(folderPath);
        onFolderSelect(folderPath, settings.includeSubfolders, settings, mode);
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
      onFolderSelect(selectedPath, settings.includeSubfolders, settings, mode);
    }
  };

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
          {selectedPath && (
            <button
              onClick={handleProcessFolder}
              disabled={isLoading}
              className="folder-input__button folder-input__button--primary"
            >
              {isLoading ? 'Processing...' : 'Process Folder'}
            </button>
          )}
        </div>
        
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
          </div>
          <button
            onClick={() => setShowSettings(!showSettings)}
            className="settings-toggle"
          >
            {showSettings ? 'Hide Settings' : 'Show Settings'}
          </button>
        </div>
        
        {showSettings && (
          <div className="settings-panel">
            <h3>Batch Settings</h3>
            
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
              
              <div className="setting-group setting-group--full-width">
                <label className="checkbox-option">
                  <input
                    type="checkbox"
                    checked={settings.includeSubfolders}
                    onChange={(e) => setSettings(prev => ({ ...prev, includeSubfolders: e.target.checked }))}
                  />
                  <span>Include subfolders</span>
                </label>
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