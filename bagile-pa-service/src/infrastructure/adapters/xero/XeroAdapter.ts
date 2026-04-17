import type { IXeroPort, XeroInvoice } from '../../../domain/ports/IXeroPort.js';
import { fetchJson } from '../../http/fetchJson.js';

interface XeroApiInvoice {
  InvoiceID: string;
  InvoiceNumber: string;
  Contact: { Name: string };
  Total: number;
  Status: string;
}

function mapInvoice(raw: XeroApiInvoice): XeroInvoice {
  return {
    invoiceId: raw.InvoiceID,
    invoiceNumber: raw.InvoiceNumber,
    contactName: raw.Contact.Name,
    total: raw.Total,
    status: raw.Status,
  };
}

export class XeroAdapter implements IXeroPort {
  constructor(
    private readonly accessToken: string | (() => Promise<string>),
    private readonly tenantId: string
  ) {}

  async findInvoice(query: string): Promise<XeroInvoice | null> {
    const byNumber = await this.searchByInvoiceNumber(query);
    if (byNumber) return byNumber;
    return this.searchByContactName(query);
  }

  private async searchByInvoiceNumber(invoiceNumber: string): Promise<XeroInvoice | null> {
    try {
      const data = await fetchJson<{ Invoices: XeroApiInvoice[] }>(
        `https://api.xero.com/api.xro/2.0/Invoices?InvoiceNumbers=${encodeURIComponent(invoiceNumber)}`,
        { headers: await this.headers() }
      );
      return data.Invoices.length > 0 ? mapInvoice(data.Invoices[0]) : null;
    } catch {
      return null;
    }
  }

  private async searchByContactName(name: string): Promise<XeroInvoice | null> {
    try {
      const safeName = name.replace(/"/g, '');
      const where = `Contact.Name.Contains("${safeName}")`;
      const data = await fetchJson<{ Invoices: XeroApiInvoice[] }>(
        `https://api.xero.com/api.xro/2.0/Invoices?where=${encodeURIComponent(where)}&order=UpdatedDateUTC DESC`,
        { headers: await this.headers() }
      );
      return data.Invoices.length > 0 ? mapInvoice(data.Invoices[0]) : null;
    } catch {
      return null;
    }
  }

  private async resolveToken(): Promise<string> {
    if (typeof this.accessToken === 'function') return this.accessToken();
    return this.accessToken;
  }

  private async headers(): Promise<Record<string, string>> {
    return {
      'Authorization': `Bearer ${await this.resolveToken()}`,
      'xero-tenant-id': this.tenantId,
      'Accept': 'application/json',
    };
  }
}
