export interface XeroInvoice {
  invoiceId: string;
  invoiceNumber: string;
  contactName: string;
  total: number;
  status: string;
  onlineUrl?: string;
}

export interface IXeroPort {
  findInvoice(query: string): Promise<XeroInvoice | null>;
}
