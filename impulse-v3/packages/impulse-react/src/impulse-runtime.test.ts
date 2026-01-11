import { describe, it, expect, beforeEach, vi } from 'vitest';
import {
  impulseFetch,
  impulseMutate,
  isValidationError,
  getInitialPageData,
} from './impulse-runtime';

// Mock fetch
const mockFetch = vi.fn();
global.fetch = mockFetch;

describe('impulse-runtime', () => {
  beforeEach(() => {
    mockFetch.mockClear();
  });

  describe('impulseFetch', () => {
    it('should fetch a page with Impulse headers', async () => {
      const mockResponse = {
        props: { message: 'Hello' },
        component: '/test',
        version: '1.0.0',
      };

      mockFetch.mockResolvedValueOnce({
        ok: true,
        status: 200,
        headers: new Headers(),
        json: () => Promise.resolve(mockResponse),
      });

      const result = await impulseFetch('/test');

      expect(result).toEqual(mockResponse);
      expect(mockFetch).toHaveBeenCalledWith('/test', {
        method: 'GET',
        headers: expect.any(Headers),
        signal: undefined,
      });

      // Check headers
      const [, options] = mockFetch.mock.calls[0];
      expect(options.headers.get('X-Impulse')).toBe('true');
      expect(options.headers.get('Content-Type')).toBe('application/json');
    });

    it('should throw on 404', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 404,
        headers: new Headers(),
      });

      await expect(impulseFetch('/not-found')).rejects.toThrow('Page not found');
    });

    it('should reload page when X-Impulse-Reload header is set', async () => {
      const reloadMock = vi.fn();
      Object.defineProperty(window, 'location', {
        value: { reload: reloadMock },
        writable: true,
      });

      mockFetch.mockResolvedValueOnce({
        ok: true,
        status: 200,
        headers: new Headers({ 'X-Impulse-Reload': 'true' }),
        json: () => Promise.resolve({}),
      });

      await expect(impulseFetch('/test')).rejects.toThrow('Page reload required');
      expect(reloadMock).toHaveBeenCalled();
    });
  });

  describe('impulseMutate', () => {
    it('should send a mutation with correct method', async () => {
      const mockResponse = { success: true };

      mockFetch.mockResolvedValueOnce({
        ok: true,
        status: 200,
        headers: new Headers(),
        json: () => Promise.resolve(mockResponse),
      });

      const result = await impulseMutate('/api/create', { name: 'Test' }, 'POST');

      expect(result).toEqual(mockResponse);
      expect(mockFetch).toHaveBeenCalledWith('/api/create', {
        method: 'POST',
        headers: expect.any(Headers),
        body: JSON.stringify({ name: 'Test' }),
        signal: undefined,
      });
    });

    it('should resolve route params from data', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        status: 200,
        headers: new Headers(),
        json: () => Promise.resolve({}),
      });

      await impulseMutate('/api/items/:id', { id: 123, name: 'Test' }, 'PUT');

      expect(mockFetch).toHaveBeenCalledWith('/api/items/123', expect.anything());
    });

    it('should throw validation error on 422', async () => {
      const validationError = {
        errors: { name: ['Name is required'] },
      };

      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 422,
        headers: new Headers(),
        json: () => Promise.resolve(validationError),
      });

      await expect(
        impulseMutate('/api/create', { name: '' }, 'POST')
      ).rejects.toEqual(validationError);
    });
  });

  describe('isValidationError', () => {
    it('should return true for validation error objects', () => {
      expect(isValidationError({ errors: { field: ['Error'] } })).toBe(true);
    });

    it('should return false for non-validation errors', () => {
      expect(isValidationError(null)).toBe(false);
      expect(isValidationError(undefined)).toBe(false);
      expect(isValidationError('error')).toBe(false);
      expect(isValidationError({ message: 'error' })).toBe(false);
    });
  });

  describe('getInitialPageData', () => {
    it('should return null when no app element exists', () => {
      expect(getInitialPageData()).toBe(null);
    });

    it('should parse data from app element', () => {
      const data = {
        props: { message: 'Hello' },
        component: '/test',
        version: '1.0.0',
      };

      document.body.innerHTML = `<div id="app" data-impulse='${JSON.stringify(data)}'></div>`;

      expect(getInitialPageData()).toEqual(data);
    });
  });
});
