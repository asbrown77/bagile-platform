import { describe, it, expect, vi } from 'vitest';
import { LookupXeroInvoiceUseCase } from './LookupXeroInvoiceUseCase.js';
import type { IXeroPort, XeroInvoice } from '../../../domain/ports/IXeroPort.js';

const SAMPLE_INVOICE: XeroInvoice = {
  invoiceId: 'inv-uuid-001',
  invoiceNumber: 'INV-0042',
  contactName: 'Acme Corp',
  total: 1140,
  status: 'AUTHORISED',
};

function makeXeroPort(result: XeroInvoice | null): IXeroPort {
  return { findInvoice: vi.fn().mockResolvedValue(result) };
}

describe('LookupXeroInvoiceUseCase', () => {
  it('returns invoice when found', async () => {
    const port = makeXeroPort(SAMPLE_INVOICE);
    const useCase = new LookupXeroInvoiceUseCase(port);

    const result = await useCase.execute({ query: 'INV-0042' });

    expect(result).toEqual(SAMPLE_INVOICE);
    expect(port.findInvoice).toHaveBeenCalledWith('INV-0042');
  });

  it('returns null when not found', async () => {
    const port = makeXeroPort(null);
    const useCase = new LookupXeroInvoiceUseCase(port);

    const result = await useCase.execute({ query: 'Unknown Company' });

    expect(result).toBeNull();
  });
});
