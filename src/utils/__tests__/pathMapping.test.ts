import { localPathToImmichPath, immichPathToLocalPath } from '../pathMapping';

describe('pathMapping', () => {
  it('maps local Windows path to Immich container path', () => {
    const mapped = localPathToImmichPath('E:\\Photos\\2024\\photo.jpg', {
      localPrefix: 'E:\\Photos',
      immichPrefix: '/mnt/photos',
    });
    expect(mapped).toBe('/mnt/photos/2024/photo.jpg');
  });

  it('maps Immich path back to local path', () => {
    const mapped = immichPathToLocalPath('/mnt/photos/2024/photo.jpg', {
      localPrefix: 'E:\\Photos',
      immichPrefix: '/mnt/photos',
    });
    expect(mapped.replace(/\\/g, '/')).toBe('E:/Photos/2024/photo.jpg');
  });
});
