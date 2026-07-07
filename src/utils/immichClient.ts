import * as crypto from 'crypto';
import * as fs from 'fs';
import { ImmichConnectionConfig, ImmichPathMapping } from '../types';
import { localPathToImmichPath, normalizePathForCompare } from './pathMapping';
import { formatImmichFetchError, immichHttpRequest, ImmichHttpResponse, isTlsProtocolMismatchError } from './immichHttp';

export interface ImmichAssetSummary {
  id: string;
  originalPath?: string;
  originalFileName?: string;
  type?: string;
}

export interface ImmichConnectionResult {
  ok: boolean;
  error?: string;
  serverVersion?: string;
  effectiveServerUrl?: string;
  protocolDowngraded?: boolean;
}

export interface ImmichMatchResult {
  path: string;
  assetId?: string;
  originalPath?: string;
  matched: boolean;
}

interface FolderViewResponse {
  path?: string;
  folders?: string[];
  assets?: Array<{
    id: string;
    originalPath?: string;
    originalFileName?: string;
    type?: string;
  }>;
}

/**
 * Normalize user-entered Immich server URL to a valid API base URL.
 * Handles missing scheme, trailing slashes, and host/port entered with a slash.
 */
export function normalizeImmichServerUrl(url: string): string {
  let trimmed = url.trim();
  if (!trimmed) return '';

  trimmed = trimmed.replace(
    /^([a-zA-Z0-9.-]+)\/(\d{2,5})(\/.*)?$/,
    (_match, host: string, port: string, rest = '') => `${host}:${port}${rest}`
  );

  if (!/^https?:\/\//i.test(trimmed)) {
    trimmed = `http://${trimmed}`;
  }

  trimmed = trimmed.replace(/\/+$/, '');

  if (trimmed.endsWith('/api')) {
    return trimmed;
  }

  return `${trimmed}/api`;
}

function authHeaders(apiKey: string): Record<string, string> {
  return {
    'x-api-key': apiKey,
    Accept: 'application/json',
    'Content-Type': 'application/json',
  };
}

async function parseErrorResponse(res: ImmichHttpResponse): Promise<string> {
  try {
    const body = (await res.json()) as { message?: string; error?: string };
    return body?.message || body?.error || `HTTP ${res.status}`;
  } catch {
    return `HTTP ${res.status}`;
  }
}

function stripApiSuffix(baseUrl: string): string {
  return baseUrl.replace(/\/api$/, '');
}

function extractSearchAssetItems(data: unknown): Array<{
  id: string;
  originalPath?: string;
  originalFileName?: string;
}> {
  if (!data || typeof data !== 'object') return [];
  const record = data as Record<string, unknown>;
  if (Array.isArray(record.assets)) {
    return record.assets as Array<{ id: string; originalPath?: string; originalFileName?: string }>;
  }
  const assets = record.assets as { items?: unknown[] } | undefined;
  if (Array.isArray(assets?.items)) {
    return assets.items as Array<{ id: string; originalPath?: string; originalFileName?: string }>;
  }
  return [];
}

async function getFileSha1Base64(filePath: string): Promise<string> {
  return new Promise((resolve, reject) => {
    const hash = crypto.createHash('sha1');
    const stream = fs.createReadStream(filePath);
    stream.on('data', (chunk) => hash.update(chunk));
    stream.on('end', () => resolve(hash.digest('base64')));
    stream.on('error', reject);
  });
}

export class ImmichClient {
  private baseUrl: string;
  private readonly apiKey: string;
  private readonly pathMapping?: ImmichPathMapping;
  private protocolDowngraded = false;

  constructor(config: ImmichConnectionConfig) {
    this.baseUrl = normalizeImmichServerUrl(config.serverUrl);
    this.apiKey = config.apiKey;
    this.pathMapping = config.pathMapping;
  }

  getEffectiveServerUrl(): string {
    return stripApiSuffix(this.baseUrl);
  }

  wasProtocolDowngraded(): boolean {
    return this.protocolDowngraded;
  }

  private async request(
    path: string,
    init: { method?: string; headers?: Record<string, string>; body?: string } = {}
  ): Promise<ImmichHttpResponse> {
    const url = `${this.baseUrl}${path}`;
    try {
      return await immichHttpRequest(url, init);
    } catch (error) {
      if (this.baseUrl.startsWith('https://') && isTlsProtocolMismatchError(error)) {
        this.baseUrl = this.baseUrl.replace(/^https:/, 'http:');
        this.protocolDowngraded = true;
        return await immichHttpRequest(`${this.baseUrl}${path}`, init);
      }
      throw new Error(formatImmichFetchError(error));
    }
  }

  async testConnection(): Promise<ImmichConnectionResult> {
    try {
      const res = await this.request('/server-info', {
        headers: authHeaders(this.apiKey),
      });
      if (!res.ok) {
        const usersRes = await this.request('/users/me', {
          headers: authHeaders(this.apiKey),
        });
        if (!usersRes.ok) {
          return { ok: false, error: await parseErrorResponse(usersRes) };
        }
        return {
          ok: true,
          effectiveServerUrl: this.protocolDowngraded ? this.getEffectiveServerUrl() : undefined,
          protocolDowngraded: this.protocolDowngraded,
        };
      }
      const data = (await res.json()) as { version?: string; majorVersion?: string };
      return {
        ok: true,
        serverVersion: data?.version ?? data?.majorVersion,
        effectiveServerUrl: this.protocolDowngraded ? this.getEffectiveServerUrl() : undefined,
        protocolDowngraded: this.protocolDowngraded,
      };
    } catch (e: unknown) {
      const err = e as Error;
      return { ok: false, error: err?.message || String(e) };
    }
  }

  async trashAssets(assetIds: string[]): Promise<{ ok: boolean; error?: string }> {
    if (!assetIds.length) return { ok: true };
    try {
      const res = await this.request('/assets', {
        method: 'DELETE',
        headers: authHeaders(this.apiKey),
        body: JSON.stringify({ ids: assetIds, force: false }),
      });
      if (!res.ok) {
        return { ok: false, error: await parseErrorResponse(res) };
      }
      return { ok: true };
    } catch (e: unknown) {
      const err = e as Error;
      return { ok: false, error: err?.message || String(e) };
    }
  }

  async restoreAssets(assetIds: string[]): Promise<{ ok: boolean; error?: string }> {
    if (!assetIds.length) return { ok: true };
    try {
      const res = await this.request('/trash/restore/assets', {
        method: 'POST',
        headers: authHeaders(this.apiKey),
        body: JSON.stringify({ ids: assetIds }),
      });
      if (!res.ok) {
        return { ok: false, error: await parseErrorResponse(res) };
      }
      return { ok: true };
    } catch (e: unknown) {
      const err = e as Error;
      return { ok: false, error: err?.message || String(e) };
    }
  }

  private async fetchFolderView(immichFolderPath: string): Promise<FolderViewResponse | null> {
    const query = encodeURIComponent(immichFolderPath);
    const res = await this.request(`/view/folder?path=${query}`, {
      headers: authHeaders(this.apiKey),
    });
    if (!res.ok) {
      return null;
    }
    return (await res.json()) as FolderViewResponse;
  }

  private async indexFolderAssets(
    immichFolderPath: string,
    assetMap: Map<string, ImmichAssetSummary>,
    visited: Set<string>
  ): Promise<void> {
    const normalized = normalizePathForCompare(immichFolderPath);
    if (!normalized || visited.has(normalized)) return;
    visited.add(normalized);

    let view: FolderViewResponse | null;
    try {
      view = await this.fetchFolderView(normalized);
    } catch (error) {
      console.warn(`[Immich] folder view failed for ${normalized}:`, error);
      return;
    }
    if (!view) return;

    for (const asset of view.assets || []) {
      if (!asset.id) continue;
      const originalPath = asset.originalPath
        ? normalizePathForCompare(asset.originalPath)
        : `${normalized}/${asset.originalFileName || ''}`;
      assetMap.set(originalPath, {
        id: asset.id,
        originalPath,
        originalFileName: asset.originalFileName,
        type: asset.type,
      });
    }
  }

  async matchLocalPaths(
    localPaths: string[],
    rootFolderPath: string,
    includeSubfolders: boolean
  ): Promise<ImmichMatchResult[]> {
    const immichRoot = normalizePathForCompare(
      localPathToImmichPath(rootFolderPath, this.pathMapping)
    );
    const assetMap = new Map<string, ImmichAssetSummary>();
    const visited = new Set<string>();

    const foldersToIndex = new Set<string>();
    foldersToIndex.add(immichRoot);

    for (const localPath of localPaths) {
      const immichPath = normalizePathForCompare(
        localPathToImmichPath(localPath, this.pathMapping)
      );
      foldersToIndex.add(parentDir(immichPath));
      if (includeSubfolders) {
        foldersToIndex.add(immichPath);
      }
    }

    for (const folder of foldersToIndex) {
      if (!folder) continue;
      await this.indexFolderAssets(folder, assetMap, visited);
    }

    const byFileName = new Map<string, ImmichAssetSummary[]>();
    for (const asset of assetMap.values()) {
      const name = asset.originalFileName || asset.originalPath?.split('/').pop();
      if (!name) continue;
      const list = byFileName.get(name.toLowerCase()) || [];
      list.push(asset);
      byFileName.set(name.toLowerCase(), list);
    }

    return localPaths.map((localPath) => {
      const immichPath = normalizePathForCompare(
        localPathToImmichPath(localPath, this.pathMapping)
      );
      let asset = assetMap.get(immichPath);
      if (!asset) {
        const fileName = localPath.split(/[/\\]/).pop()?.toLowerCase();
        if (fileName) {
          const candidates = byFileName.get(fileName) || [];
          asset =
            candidates.find(
              (c) => c.originalPath && normalizePathForCompare(c.originalPath) === immichPath
            ) || candidates[0];
        }
      }
      return {
        path: localPath,
        assetId: asset?.id,
        originalPath: asset?.originalPath,
        matched: !!asset?.id,
      };
    });
  }

  private async searchAssetByFileName(
    fileName: string,
    localPath?: string
  ): Promise<ImmichAssetSummary | null> {
    try {
      const res = await this.request('/search/metadata', {
        method: 'POST',
        headers: authHeaders(this.apiKey),
        body: JSON.stringify({ originalFileName: fileName }),
      });
      if (!res.ok) return null;
      const items = extractSearchAssetItems(await res.json());
      if (!items.length) return null;

      const normalizedLocal = localPath ? normalizePathForCompare(localPath) : '';
      const lowerName = fileName.toLowerCase();
      const ranked = items
        .filter((item) => item.id)
        .map((item) => {
          const origPath = item.originalPath
            ? normalizePathForCompare(item.originalPath)
            : '';
          let score = 1;
          if (normalizedLocal && origPath === normalizedLocal) score = 4;
          else if (normalizedLocal && origPath.endsWith(normalizedLocal)) score = 3;
          else if (origPath.endsWith(lowerName)) score = 2;
          return { item, score };
        })
        .sort((a, b) => b.score - a.score);
      const match = ranked[0]?.item;
      if (!match?.id) return null;
      return {
        id: match.id,
        originalPath: match.originalPath
          ? normalizePathForCompare(match.originalPath)
          : undefined,
        originalFileName: match.originalFileName,
      };
    } catch {
      return null;
    }
  }

  private async searchAssetByChecksum(checksum: string): Promise<ImmichAssetSummary | null> {
    try {
      const res = await this.request('/search/metadata', {
        method: 'POST',
        headers: authHeaders(this.apiKey),
        body: JSON.stringify({ checksum }),
      });
      if (!res.ok) return null;
      const items = extractSearchAssetItems(await res.json());
      const match = items.find((item) => item.id);
      if (!match?.id) return null;
      return {
        id: match.id,
        originalPath: match.originalPath
          ? normalizePathForCompare(match.originalPath)
          : undefined,
        originalFileName: match.originalFileName,
      };
    } catch {
      return null;
    }
  }

  async resolveAssetForLocalPath(
    localPath: string,
    rootFolderPath: string,
    includeSubfolders: boolean
  ): Promise<ImmichMatchResult> {
    const [match] = await this.matchLocalPaths([localPath], rootFolderPath, includeSubfolders);
    if (match?.assetId) return match;

    const fileName = localPath.split(/[/\\]/).pop();
    if (fileName) {
      const asset = await this.searchAssetByFileName(fileName, localPath);
      if (asset?.id) {
        return {
          path: localPath,
          assetId: asset.id,
          originalPath: asset.originalPath,
          matched: true,
        };
      }
    }

    try {
      const checksum = await getFileSha1Base64(localPath);
      const asset = await this.searchAssetByChecksum(checksum);
      if (asset?.id) {
        return {
          path: localPath,
          assetId: asset.id,
          originalPath: asset.originalPath,
          matched: true,
        };
      }
    } catch {
      // checksum read/search failed
    }

    return match || { path: localPath, matched: false };
  }
}

function parentDir(filePath: string): string {
  const normalized = normalizePathForCompare(filePath);
  const idx = normalized.lastIndexOf('/');
  return idx > 0 ? normalized.slice(0, idx) : normalized;
}

export function createImmichClient(config: ImmichConnectionConfig): ImmichClient {
  return new ImmichClient(config);
}
