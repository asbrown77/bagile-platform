import type { IWooCommercePort } from '../../../domain/ports/IWooCommercePort.js';

export interface CancelCourseInput {
  productId: number;
}

export interface CancelCourseResult {
  productId: number;
  cancelled: boolean;
}

export class CancelCourseUseCase {
  constructor(private readonly wooPort: IWooCommercePort) {}

  async execute(input: CancelCourseInput): Promise<CancelCourseResult> {
    await this.wooPort.setProductOutOfStock(input.productId);
    return { productId: input.productId, cancelled: true };
  }
}
