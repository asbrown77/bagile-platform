namespace Bagile.EtlService.Transformers
{
    public interface ITransformer<TInput, TOutput>
    {
        IEnumerable<TOutput> Transform(IEnumerable<TInput> input);
    }
}
