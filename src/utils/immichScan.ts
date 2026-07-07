import * as path from 'path';
import { DateRangeFilter, hasDateRangeFilter } from './dateFilter';

/** Immich data-volume folders that are not user photos (thumbnails, transcodes, etc.). */
export const IMMICH_INTERNAL_DIR_NAMES = new Set([
  'thumbs',
  'thumb',
  'encoded-video',
  'profile',
  'backups',
  'backups-borg',
  '.thumbnails',
  'tmp',
  'cache',
]);

export function shouldSkipImmichInternalDir(dirName: string): boolean {
  return IMMICH_INTERNAL_DIR_NAMES.has(dirName.toLowerCase());
}

function startOfDay(date: Date): Date {
  const d = new Date(date);
  d.setHours(0, 0, 0, 0);
  return d;
}

function endOfDay(date: Date): Date {
  const d = new Date(date);
  d.setHours(23, 59, 59, 999);
  return d;
}

function parseDateInput(value: string): Date | null {
  if (!value) return null;
  const parts = value.split('-').map((p) => parseInt(p, 10));
  if (parts.length !== 3 || parts.some((n) => Number.isNaN(n))) return null;
  const [year, month, day] = parts;
  const date = new Date(year, month - 1, day);
  return Number.isNaN(date.getTime()) ? null : date;
}

/**
 * Skip Immich upload-style YYYY/MM/DD folders that cannot contain files in the date range.
 */
export function shouldPruneDirectoryForDateRange(
  parentDirPath: string,
  dirName: string,
  filter?: DateRangeFilter
): boolean {
  if (!hasDateRangeFilter(filter)) return false;

  const from = filter?.dateFrom ? parseDateInput(filter.dateFrom) : null;
  const to = filter?.dateTo ? parseDateInput(filter.dateTo) : null;
  if (!from && !to) return false;

  const yearMatch = dirName.match(/^(19|20)\d{2}$/);
  if (yearMatch) {
    const year = parseInt(dirName, 10);
    if (from && year < from.getFullYear()) return true;
    if (to && year > to.getFullYear()) return true;
    return false;
  }

  const parentName = path.basename(parentDirPath);
  const monthMatch = dirName.match(/^(0?[1-9]|1[0-2])$/);
  if (monthMatch && parentName.match(/^(19|20)\d{2}$/)) {
    const year = parseInt(parentName, 10);
    const month = parseInt(dirName, 10);
    const monthStart = startOfDay(new Date(year, month - 1, 1));
    const monthEnd = endOfDay(new Date(year, month, 0));
    if (from && monthEnd < startOfDay(from)) return true;
    if (to && monthStart > endOfDay(to)) return true;
    return false;
  }

  const grandparentName = path.basename(path.dirname(parentDirPath));
  const dayMatch = dirName.match(/^(0?[1-9]|[12]\d|3[01])$/);
  if (
    dayMatch &&
    parentName.match(/^(0?[1-9]|1[0-2])$/) &&
    grandparentName.match(/^(19|20)\d{2}$/)
  ) {
    const year = parseInt(grandparentName, 10);
    const month = parseInt(parentName, 10);
    const day = parseInt(dirName, 10);
    const dayStart = startOfDay(new Date(year, month - 1, day));
    const dayEnd = endOfDay(new Date(year, month - 1, day));
    if (from && dayEnd < startOfDay(from)) return true;
    if (to && dayStart > endOfDay(to)) return true;
  }

  return false;
}

export function shouldSkipScanDirectory(
  parentDirPath: string,
  dirName: string,
  scanOptions?: DateRangeFilter
): boolean {
  if (dirName === '_delete' || dirName === '_favorites' || dirName === '.immich') {
    return true;
  }
  if (shouldSkipImmichInternalDir(dirName)) {
    return true;
  }
  if (shouldPruneDirectoryForDateRange(parentDirPath, dirName, scanOptions)) {
    return true;
  }
  return false;
}
