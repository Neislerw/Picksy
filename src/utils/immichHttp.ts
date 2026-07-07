import * as http from 'http';
import * as https from 'https';
import { URL } from 'url';

export interface ImmichHttpResponse {
  ok: boolean;
  status: number;
  json(): Promise<unknown>;
  text(): Promise<string>;
}

function isPrivateOrLocalHost(hostname: string): boolean {
  const host = hostname.toLowerCase();
  return (
    host === 'localhost' ||
    host.endsWith('.local') ||
    /^127\./.test(host) ||
    /^10\./.test(host) ||
    /^192\.168\./.test(host) ||
    /^172\.(1[6-9]|2\d|3[0-1])\./.test(host)
  );
}

export function isTlsProtocolMismatchError(error: unknown): boolean {
  const err = error as Error & { code?: string };
  const combined = `${err?.message || ''} ${err?.code || ''}`.toLowerCase();
  return combined.includes('wrong_version_number') || combined.includes('eproto');
}

export function convertHttpsToHttpUrl(urlString: string): string | null {
  try {
    const url = new URL(urlString);
    if (url.protocol !== 'https:') return null;
    url.protocol = 'http:';
    return url.toString();
  } catch {
    return null;
  }
}

export function formatImmichFetchError(error: unknown): string {
  const err = error as Error & { cause?: { code?: string; message?: string }; code?: string };
  const causeCode = err.cause?.code || err.code || '';
  const causeMessage = err.cause?.message || '';
  const combined = `${err.message || ''} ${causeMessage} ${causeCode}`.toLowerCase();

  if (isTlsProtocolMismatchError(error)) {
    return 'This server is not using HTTPS on this port. Try http:// instead of https:// (Immich on port 2283 is usually HTTP).';
  }
  if (
    combined.includes('cert') ||
    combined.includes('self signed') ||
    combined.includes('unable to verify')
  ) {
    return 'TLS certificate rejected. For self-hosted Immich, http:// is often easier on your LAN.';
  }
  if (combined.includes('econnrefused') || combined.includes('enotfound')) {
    return 'Could not reach the Immich server. Check the URL, port, and that Immich is running.';
  }
  if (combined.includes('etimedout') || combined.includes('timeout')) {
    return 'Immich request timed out. The server may be slow or the library path may be wrong.';
  }
  return err.message || String(error);
}

/**
 * HTTP(S) request for Immich API calls from the Electron main process.
 * Allows self-signed certificates on private/LAN hosts (common for homelab Immich).
 */
export function immichHttpRequest(
  urlString: string,
  options: {
    method?: string;
    headers?: Record<string, string>;
    body?: string;
    timeoutMs?: number;
  } = {}
): Promise<ImmichHttpResponse> {
  const url = new URL(urlString);
  const isHttps = url.protocol === 'https:';
  const lib = isHttps ? https : http;
  const allowInsecureTls = isHttps && isPrivateOrLocalHost(url.hostname);

  return new Promise((resolve, reject) => {
    const req = lib.request(
      {
        protocol: url.protocol,
        hostname: url.hostname,
        port: url.port || (isHttps ? 443 : 80),
        path: `${url.pathname}${url.search}`,
        method: options.method || 'GET',
        headers: options.headers,
        rejectUnauthorized: !allowInsecureTls,
      },
      (res) => {
        const chunks: Buffer[] = [];
        res.on('data', (chunk) => chunks.push(Buffer.from(chunk)));
        res.on('end', () => {
          const text = Buffer.concat(chunks).toString('utf8');
          const status = res.statusCode || 0;
          resolve({
            ok: status >= 200 && status < 300,
            status,
            json: async () => (text ? JSON.parse(text) : null),
            text: async () => text,
          });
        });
      }
    );

    req.setTimeout(options.timeoutMs ?? 30000, () => {
      req.destroy(new Error('Request timed out'));
    });

    req.on('error', reject);

    if (options.body) {
      req.write(options.body);
    }
    req.end();
  });
}
