import {
  shouldPruneDirectoryForDateRange,
  shouldSkipImmichInternalDir,
  shouldSkipScanDirectory,
} from '../immichScan';

describe('immichScan', () => {
  it('skips Immich internal directories', () => {
    expect(shouldSkipImmichInternalDir('thumbs')).toBe(true);
    expect(shouldSkipImmichInternalDir('encoded-video')).toBe(true);
    expect(shouldSkipImmichInternalDir('upload')).toBe(false);
  });

  it('prunes year folders outside date range', () => {
    const filter = { dateFrom: '2025-01-01', dateTo: '2025-06-30' };
    expect(shouldPruneDirectoryForDateRange('/data/upload/user', '2024', filter)).toBe(true);
    expect(shouldPruneDirectoryForDateRange('/data/upload/user', '2025', filter)).toBe(false);
    expect(shouldPruneDirectoryForDateRange('/data/upload/user', '2026', filter)).toBe(true);
  });

  it('prunes month folders outside date range', () => {
    const filter = { dateFrom: '2025-03-01', dateTo: '2025-06-30' };
    expect(shouldPruneDirectoryForDateRange('/data/upload/user/2025', '02', filter)).toBe(true);
    expect(shouldPruneDirectoryForDateRange('/data/upload/user/2025', '04', filter)).toBe(false);
  });

  it('combines internal dir and date pruning in shouldSkipScanDirectory', () => {
    expect(shouldSkipScanDirectory('/data', 'thumbs', { dateFrom: '2025-01-01' })).toBe(true);
    expect(shouldSkipScanDirectory('/data/upload/user', '2024', { dateFrom: '2025-01-01' })).toBe(
      true
    );
    expect(shouldSkipScanDirectory('/data/upload/user', '2025', { dateFrom: '2025-01-01' })).toBe(
      false
    );
  });
});
