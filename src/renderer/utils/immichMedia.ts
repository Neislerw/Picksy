import { ImmichConnectionConfig, Photo, Video } from '../../types';

export class ImmichMatchError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'ImmichMatchError';
  }
}

export async function enrichPhotosWithImmich(
  photos: Photo[],
  folderPath: string,
  includeSubfolders: boolean,
  config: ImmichConnectionConfig
): Promise<Photo[]> {
  if (!photos.length) return photos;
  const result = await window.electron?.ipcRenderer.invoke('immich-match-assets', {
    config,
    localPaths: photos.map((p) => p.path),
    rootFolderPath: folderPath,
    includeSubfolders,
  });
  if (result?.ok === false && result?.error) {
    throw new ImmichMatchError(result.error);
  }
  const matches: Array<{ path: string; assetId?: string; originalPath?: string }> =
    result?.matches || [];
  const matchMap = new Map(matches.map((m) => [m.path, m]));
  return photos.map((photo) => {
    const match = matchMap.get(photo.path);
    return {
      ...photo,
      source: 'immich-external' as const,
      assetId: match?.assetId,
      originalPath: match?.originalPath,
      displayPath: photo.path,
    };
  });
}

export async function enrichVideosWithImmich(
  videos: Video[],
  folderPath: string,
  includeSubfolders: boolean,
  config: ImmichConnectionConfig
): Promise<Video[]> {
  if (!videos.length) return videos;
  const result = await window.electron?.ipcRenderer.invoke('immich-match-assets', {
    config,
    localPaths: videos.map((v) => v.path),
    rootFolderPath: folderPath,
    includeSubfolders,
  });
  if (result?.ok === false && result?.error) {
    throw new ImmichMatchError(result.error);
  }
  const matches: Array<{ path: string; assetId?: string; originalPath?: string }> =
    result?.matches || [];
  const matchMap = new Map(matches.map((m) => [m.path, m]));
  return videos.map((video) => {
    const match = matchMap.get(video.path);
    return {
      ...video,
      source: 'immich-external' as const,
      assetId: match?.assetId,
      originalPath: match?.originalPath,
      displayPath: video.path,
    };
  });
}

export async function setImmichSession(
  config: ImmichConnectionConfig | null,
  rootFolderPath?: string | null,
  includeSubfolders?: boolean
): Promise<void> {
  await window.electron?.ipcRenderer.invoke(
    'immich-set-session',
    config,
    rootFolderPath ?? null,
    includeSubfolders ?? true
  );
}
