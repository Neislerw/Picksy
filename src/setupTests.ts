// This file is used to set up the testing environment
import '@testing-library/jest-dom';

// Mock Electron APIs for testing
(global as any).electron = {
  ipcRenderer: {
    send: jest.fn(),
    on: jest.fn(),
    removeListener: jest.fn(),
  },
}; 