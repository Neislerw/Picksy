import React from 'react';
import '../styles/CompletionPopup.css';

interface CompletionStats {
  totalPhotos: number;
  totalBatches: number;
  keptPhotos: number;
  deletedPhotos: number;
  deletedSize: number;
}

interface CompletionPopupProps {
  stats: CompletionStats;
  onClose: () => void;
  title?: string;
  extraActionLabel?: string;
  onExtraActionClick?: () => void;
}

const CompletionPopup: React.FC<CompletionPopupProps> = ({ stats, onClose, title, extraActionLabel, onExtraActionClick }) => {
  const formatFileSize = (bytes: number): string => {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  };

  return (
    <div className="completion-popup-overlay">
      <div className="completion-popup">
        <div className="completion-popup__header">
          <h2>🎉 {title || 'All Batches Processed!'}</h2>
          <p>Your photo culling session is complete</p>
        </div>
        
        <div className="completion-popup__stats">
          <div className="stat-item">
            <span className="stat-label">Total Photos:</span>
            <span className="stat-value">{stats.totalPhotos}</span>
          </div>
          <div className="stat-item">
            <span className="stat-label">Total Batches:</span>
            <span className="stat-value">{stats.totalBatches}</span>
          </div>
          <div className="stat-item">
            <span className="stat-label">Photos Kept:</span>
            <span className="stat-value stat-value--kept">{stats.keptPhotos}</span>
          </div>
          <div className="stat-item">
            <span className="stat-label">Photos Deleted:</span>
            <span className="stat-value stat-value--deleted">{stats.deletedPhotos}</span>
          </div>
          <div className="stat-item">
            <span className="stat-label">Space Saved:</span>
            <span className="stat-value stat-value--saved">{formatFileSize(stats.deletedSize)}</span>
          </div>
        </div>
        
        <div className="completion-popup__actions">
          {extraActionLabel && onExtraActionClick && (
            <button onClick={onExtraActionClick} className="completion-popup__button" style={{ marginRight: 8 }}>
              {extraActionLabel}
            </button>
          )}
          <button onClick={onClose} className="completion-popup__button">
            Start New Session
          </button>
        </div>
      </div>
    </div>
  );
};

export default CompletionPopup; 