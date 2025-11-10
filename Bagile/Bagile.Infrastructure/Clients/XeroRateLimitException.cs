using System;

namespace Bagile.Infrastructure.Clients
{
    public class XeroRateLimitException : Exception
    {
        public TimeSpan? RetryAfter { get; }

        public XeroRateLimitException(string message, TimeSpan? retryAfter = null)
            : base(message)
        {
            RetryAfter = retryAfter;
        }
    }
}