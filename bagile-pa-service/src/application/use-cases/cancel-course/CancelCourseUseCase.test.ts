import { describe, it, expect, vi } from 'vitest';
import { CancelCourseUseCase } from './CancelCourseUseCase.js';
import type { IWooCommercePort } from '../../../domain/ports/IWooCommercePort.js';

function makeWooPort(): IWooCommercePort {
  return { setProductOutOfStock: vi.fn().mockResolvedValue(undefined) };
}

describe('CancelCourseUseCase', () => {
  it('calls wooPort.setProductOutOfStock with the productId', async () => {
    const port = makeWooPort();
    const useCase = new CancelCourseUseCase(port);

    await useCase.execute({ productId: 42 });

    expect(port.setProductOutOfStock).toHaveBeenCalledOnce();
    expect(port.setProductOutOfStock).toHaveBeenCalledWith(42);
  });

  it('returns { cancelled: true } with the productId', async () => {
    const port = makeWooPort();
    const useCase = new CancelCourseUseCase(port);

    const result = await useCase.execute({ productId: 99 });

    expect(result).toEqual({ productId: 99, cancelled: true });
  });
});
