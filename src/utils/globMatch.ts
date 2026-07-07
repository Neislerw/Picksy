import * as path from 'path';

/**
 * Convert a simple glob pattern to a RegExp.
 * Supports **, *, ?, and brace groups like *.{tif,jpg}
 */
function globToRegExp(globPattern: string): RegExp {
  let pattern = globPattern.replace(/\\/g, '/');
  const braceMatch = pattern.match(/\*\.\{([^}]+)\}/);
  if (braceMatch) {
    const exts = braceMatch[1].split(',').map(e => e.trim().replace(/^\./, ''));
    pattern = pattern.replace(/\*\.\{[^}]+\}/, `*\\.(${exts.join('|')})`);
  }

  const escaped = pattern
    .replace(/[.+^${}()|[\]\\]/g, '\\$&')
    .replace(/\*\*/g, '§§')
    .replace(/\*/g, '[^/]*')
    .replace(/§§/g, '.*')
    .replace(/\?/g, '[^/]');

  return new RegExp(`^${escaped}$`, 'i');
}

/**
 * Match a file path against a glob pattern using forward-slash normalized paths.
 */
export function matchesGlobPattern(filePath: string, globPattern: string): boolean {
  const normalized = filePath.replace(/\\/g, '/');
  const regex = globToRegExp(globPattern.replace(/\\/g, '/'));
  return regex.test(normalized) || regex.test(`/${normalized}`);
}

/**
 * Return true if the file path matches any exclusion pattern.
 */
export function isExcludedByPatterns(filePath: string, patterns: string[]): boolean {
  if (!patterns.length) return false;
  const normalized = filePath.replace(/\\/g, '/');
  const fileName = path.basename(filePath);
  for (const pattern of patterns) {
    if (matchesGlobPattern(normalized, pattern) || matchesGlobPattern(fileName, pattern)) {
      return true;
    }
  }
  return false;
}

export const DEFAULT_IMMICH_EXCLUDE_PATTERNS = [
  '**/*.xmp',
  '**/*.XMP',
  '**/Raw/**',
  '**/@eaDir/**',
  '**/thumbs/**',
  '**/thumb/**',
  '**/encoded-video/**',
  '**/profile/**',
  '**/backups/**',
  '**/.thumbnails/**',
];
