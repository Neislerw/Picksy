import * as path from 'path';

/**
 * Normalize a path for comparison (forward slashes, no trailing slash).
 */
export function normalizePathForCompare(p: string): string {
  const normalized = path.normalize(p).replace(/\\/g, '/');
  return normalized.endsWith('/') && normalized.length > 1
    ? normalized.slice(0, -1)
    : normalized;
}

/**
 * Convert a local filesystem path to an Immich container path using optional prefix mapping.
 */
export function localPathToImmichPath(
  localPath: string,
  mapping?: { localPrefix: string; immichPrefix: string }
): string {
  const normalizedLocal = normalizePathForCompare(localPath);
  if (!mapping?.localPrefix || !mapping?.immichPrefix) {
    return normalizedLocal;
  }
  const localPrefix = normalizePathForCompare(mapping.localPrefix);
  const immichPrefix = normalizePathForCompare(mapping.immichPrefix);
  const lowerLocal = normalizedLocal.toLowerCase();
  const lowerPrefix = localPrefix.toLowerCase();
  if (lowerLocal === lowerPrefix) {
    return immichPrefix;
  }
  if (lowerLocal.startsWith(lowerPrefix + '/')) {
    const suffix = normalizedLocal.slice(localPrefix.length);
    return immichPrefix + suffix.replace(/\\/g, '/');
  }
  return normalizedLocal;
}

/**
 * Convert an Immich path back to a local path using optional prefix mapping.
 */
export function immichPathToLocalPath(
  immichPath: string,
  mapping?: { localPrefix: string; immichPrefix: string }
): string {
  const normalizedImmich = normalizePathForCompare(immichPath);
  if (!mapping?.localPrefix || !mapping?.immichPrefix) {
    return normalizedImmich;
  }
  const localPrefix = normalizePathForCompare(mapping.localPrefix);
  const immichPrefix = normalizePathForCompare(mapping.immichPrefix);
  const lowerImmich = normalizedImmich.toLowerCase();
  const lowerPrefix = immichPrefix.toLowerCase();
  if (lowerImmich === lowerPrefix) {
    return localPrefix;
  }
  if (lowerImmich.startsWith(lowerPrefix + '/')) {
    const suffix = normalizedImmich.slice(immichPrefix.length);
    return path.join(localPrefix, ...suffix.split('/'));
  }
  return normalizedImmich;
}
