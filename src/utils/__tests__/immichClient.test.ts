import { ImmichClient, normalizeImmichServerUrl } from '../immichClient';
import * as immichHttp from '../immichHttp';

jest.mock('../immichHttp', () => ({
  ...jest.requireActual('../immichHttp'),
  immichHttpRequest: jest.fn(),
}));

const mockImmichHttpRequest = immichHttp.immichHttpRequest as jest.MockedFunction<
  typeof immichHttp.immichHttpRequest
>;

function mockResponse(body: unknown, status = 200): immichHttp.ImmichHttpResponse {
  return {
    ok: status >= 200 && status < 300,
    status,
    json: async () => body,
    text: async () => JSON.stringify(body),
  };
}

describe('normalizeImmichServerUrl', () => {
  it('adds http scheme and /api suffix for host:port', () => {
    expect(normalizeImmichServerUrl('192.168.7.254:2283')).toBe('http://192.168.7.254:2283/api');
  });

  it('fixes host/port slash typo', () => {
    expect(normalizeImmichServerUrl('192.168.7.254/2283')).toBe('http://192.168.7.254:2283/api');
  });

  it('preserves explicit http URL', () => {
    expect(normalizeImmichServerUrl('http://localhost:2283')).toBe('http://localhost:2283/api');
  });

  it('preserves URL that already ends with /api', () => {
    expect(normalizeImmichServerUrl('http://localhost:2283/api')).toBe('http://localhost:2283/api');
  });

  it('preserves https URL', () => {
    expect(normalizeImmichServerUrl('https://192.168.7.254:2283')).toBe('https://192.168.7.254:2283/api');
  });
});

describe('ImmichClient', () => {
  beforeEach(() => {
    jest.resetAllMocks();
  });

  it('testConnection returns ok on server-info success', async () => {
    mockImmichHttpRequest.mockResolvedValue(mockResponse({ version: '1.120.0' }));

    const client = new ImmichClient({
      serverUrl: 'http://localhost:2283',
      apiKey: 'test-key',
    });
    const result = await client.testConnection();
    expect(result.ok).toBe(true);
    expect(result.serverVersion).toBe('1.120.0');
  });

  it('testConnection returns error on failed auth', async () => {
    mockImmichHttpRequest
      .mockResolvedValueOnce(mockResponse({ message: 'Unauthorized' }, 401))
      .mockResolvedValueOnce(mockResponse({ message: 'Unauthorized' }, 401));

    const client = new ImmichClient({
      serverUrl: 'http://localhost:2283',
      apiKey: 'bad-key',
    });
    const result = await client.testConnection();
    expect(result.ok).toBe(false);
    expect(result.error).toBeTruthy();
  });

  it('trashAssets calls DELETE /assets with force false', async () => {
    mockImmichHttpRequest.mockResolvedValue(mockResponse(null, 200));
    const client = new ImmichClient({
      serverUrl: 'http://localhost:2283/',
      apiKey: 'test-key',
    });
    const result = await client.trashAssets(['asset-1', 'asset-2']);
    expect(result.ok).toBe(true);
    expect(mockImmichHttpRequest).toHaveBeenCalledWith(
      'http://localhost:2283/api/assets',
      expect.objectContaining({
        method: 'DELETE',
        body: JSON.stringify({ ids: ['asset-1', 'asset-2'], force: false }),
      })
    );
  });

  it('restoreAssets calls POST /trash/restore/assets', async () => {
    mockImmichHttpRequest.mockResolvedValue(mockResponse(null, 200));
    const client = new ImmichClient({
      serverUrl: 'http://localhost:2283',
      apiKey: 'test-key',
    });
    const result = await client.restoreAssets(['asset-1']);
    expect(result.ok).toBe(true);
    expect(mockImmichHttpRequest).toHaveBeenCalledWith(
      'http://localhost:2283/api/trash/restore/assets',
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({ ids: ['asset-1'] }),
      })
    );
  });

  it('testConnection downgrades https to http on TLS protocol mismatch', async () => {
    const tlsError = Object.assign(new Error('write EPROTO WRONG_VERSION_NUMBER'), { code: 'EPROTO' });
    mockImmichHttpRequest
      .mockRejectedValueOnce(tlsError)
      .mockResolvedValueOnce(mockResponse({ version: '1.120.0' }));

    const client = new ImmichClient({
      serverUrl: 'https://192.168.7.254:2283',
      apiKey: 'test-key',
    });
    const result = await client.testConnection();
    expect(result.ok).toBe(true);
    expect(result.protocolDowngraded).toBe(true);
    expect(result.effectiveServerUrl).toBe('http://192.168.7.254:2283');
    expect(mockImmichHttpRequest).toHaveBeenNthCalledWith(
      1,
      'https://192.168.7.254:2283/api/server-info',
      expect.any(Object)
    );
    expect(mockImmichHttpRequest).toHaveBeenNthCalledWith(
      2,
      'http://192.168.7.254:2283/api/server-info',
      expect.any(Object)
    );
  });

  it('matchLocalPaths maps assets by original path', async () => {
    mockImmichHttpRequest.mockResolvedValue(
      mockResponse({
        path: '/mnt/photos',
        folders: [],
        assets: [
          {
            id: 'abc-123',
            originalPath: '/mnt/photos/2024/photo.jpg',
            originalFileName: 'photo.jpg',
          },
        ],
      })
    );

    const client = new ImmichClient({
      serverUrl: 'http://localhost:2283',
      apiKey: 'test-key',
      pathMapping: {
        localPrefix: 'E:/Photos',
        immichPrefix: '/mnt/photos',
      },
    });

    const matches = await client.matchLocalPaths(
      ['E:/Photos/2024/photo.jpg'],
      'E:/Photos',
      false
    );
    expect(matches).toHaveLength(1);
    expect(matches[0].matched).toBe(true);
    expect(matches[0].assetId).toBe('abc-123');
  });
});
