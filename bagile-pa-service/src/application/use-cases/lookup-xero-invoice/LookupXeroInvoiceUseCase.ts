import type { IXeroPort, XeroInvoice } from '../../../domain/ports/IXeroPort.js';

export interface LookupXeroInvoiceInput {
  query: string;
}

export class LookupXeroInvoiceUseCase {
  constructor(private readonly xeroPort: IXeroPort) {}

  async execute(input: LookupXeroInvoiceInput): Promise<XeroInvoice | null> {
    return this.xeroPort.findInvoice(input.query);
  }
}
