export interface IpcRenderer {
  invoke(channel: string, ...args: any[]): Promise<any>;
  send(channel: string, ...args: any[]): void;
  on(channel: string, func: (...args: any[]) => void): void;
  removeListener(channel: string, func: (...args: any[]) => void): void;
}

declare global {
  interface Window {
    electron?: {
      ipcRenderer: IpcRenderer;
    };
  }
}