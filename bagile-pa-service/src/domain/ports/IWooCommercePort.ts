export interface IWooCommercePort {
  setProductOutOfStock(productId: number): Promise<void>;
}
