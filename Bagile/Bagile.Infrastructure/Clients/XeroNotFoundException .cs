public class XeroNotFoundException : Exception
{
    public string? Url { get; }
    public XeroNotFoundException(string message, string? url = null) : base(message)
    {
        Url = url;
    }
}