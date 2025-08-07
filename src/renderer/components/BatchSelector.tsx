import React, { useState } from 'react';
import '../styles/BatchSelector.css';

interface BatchSelectorProps {
  onFolderSelect: (folderPath: string, includeSubfolders: boolean) => void;
  isLoading?: boolean;
}

const BatchSelector: React.FC<BatchSelectorProps> = ({ onFolderSelect, isLoading = false }) => {
  const [selectedPath, setSelectedPath] = useState<string>('');
  const [includeSubfolders, setIncludeSubfolders] = useState<boolean>(true);

  const handleFolderSelect = async () => {
    try {
      // In a real implementation, this would use Electron's dialog API
      // For now, we'll simulate folder selection
      const folderPath = await window.electron?.ipcRenderer?.invoke('select-folder');
      if (folderPath) {
        setSelectedPath(folderPath);
        onFolderSelect(folderPath, includeSubfolders);
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
      onFolderSelect(selectedPath, includeSubfolders);
    }
  };

  return (
    <div className="batch-selector">
      <div className="batch-selector__header">
        <h1>Picksy</h1>
        <p>Select a folder to start culling your photos</p>
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
          <label className="checkbox-option">
            <input
              type="checkbox"
              checked={includeSubfolders}
              onChange={(e) => setIncludeSubfolders(e.target.checked)}
            />
            <span>Include subfolders</span>
          </label>
        </div>
        
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