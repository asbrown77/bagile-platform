using Microsoft.Extensions.Configuration;

namespace Bagile.Infrastructure.Configuration;

public class CompanySettingsConfigurationSource : IConfigurationSource
{
    private readonly string _connectionString;
    private readonly string _encryptionKey;

    public CompanySettingsConfigurationSource(string connectionString, string encryptionKey)
    {
        _connectionString = connectionString;
        _encryptionKey    = encryptionKey;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
        => new CompanySettingsConfigurationProvider(_connectionString, _encryptionKey);
}
