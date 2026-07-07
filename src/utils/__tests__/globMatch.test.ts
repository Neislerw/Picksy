import { isExcludedByPatterns, matchesGlobPattern } from '../globMatch';
import { isImageFile } from '../imageBatcher';

describe('globMatch', () => {
  it('matches xmp sidecar exclusion', () => {
    expect(matchesGlobPattern('/photos/IMG_001.xmp', '**/*.xmp')).toBe(true);
    expect(isExcludedByPatterns('E:\\photos\\IMG_001.xmp', ['**/*.xmp'])).toBe(true);
  });

  it('matches Raw folder exclusion', () => {
    expect(isExcludedByPatterns('/photos/Raw/IMG_001.CR2', ['**/Raw/**'])).toBe(true);
    expect(isExcludedByPatterns('/photos/2024/IMG_001.jpg', ['**/Raw/**'])).toBe(false);
  });
});

describe('imageBatcher immich extensions', () => {
  it('includes HEIC and HEIF as image files', () => {
    expect(isImageFile('photo.heic')).toBe(true);
    expect(isImageFile('photo.heif')).toBe(true);
  });

  it('respects custom extension list', () => {
    expect(isImageFile('photo.heic', ['.jpg'])).toBe(false);
    expect(isImageFile('photo.jpg', ['.jpg', '.heic'])).toBe(true);
  });
});
