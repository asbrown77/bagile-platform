using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Bagile.Infrastructure.Configuration;

/// <summary>
/// Loads secrets from bagile.company_settings at startup, decrypts them,
/// and injects into IConfiguration. Added last in the chain so DB values
/// override env vars. All exceptions are swallowed — if the DB is
/// unreachable the API still boots using env var fallback.
/// </summary>
public class CompanySettingsConfigurationProvider : ConfigurationProvider
{
    private static readonly Dictionary<string, string> KeyMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["scrumorg_username"]  = "ScrumOrg:Username",
        ["scrumorg_password"]  = "ScrumOrg:Password",
        ["xero_redirect_uri"]  = "Xero:RedirectUri",
        ["xero_client_id"]     = "Xero:ClientId",
        ["xero_client_secret"] = "Xero:ClientSecret",
        ["xero_refresh_token"] = "Xero:RefreshToken",
        ["xero_tenant_id"]     = "Xero:TenantId",
        ["wc_base_url"]        = "WooCommerce:BaseUrl",
        ["wp_admin_url"]       = "WooCommerce:AdminUrl",
        ["n8n_base_url"]       = "N8n:BaseUrl",
        ["bagile_api_url"]     = "BagileApi:Url",
        ["bagile_api_key"]     = "BagileApi:ApiKey",
    };

    private readonly string _connectionString;
    private readonly string _encryptionKey;
    private readonly string _tenantId;

    public CompanySettingsConfigurationProvider(
        string connectionString,
        string encryptionKey,
        string tenantId = "bagile")
    {
        _connectionString = connectionString;
        _encryptionKey    = encryptionKey;
        _tenantId         = tenantId;
    }

    public override void Load()
    {
        try
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand(
                "SELECT key, value_enc FROM bagile.company_settings WHERE tenant_id = @tenantId",
                conn);
            cmd.Parameters.AddWithValue("tenantId", _tenantId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var dbKey    = reader.GetString(0);
                var valueEnc = reader.GetString(1);

                if (!KeyMap.TryGetValue(dbKey, out var configKey))
                    continue;

                try
                {
                    var plaintext = CredentialDecryptor.Decrypt(valueEnc, _encryptionKey);

                    // IConfiguration uses ":" as separator; .NET config flattens with "__" from env vars
                    // but the key here is already in colon form (e.g. "ScrumOrg:Username"), which is
                    // exactly what IConfiguration expects for hierarchical lookup.
                    Data[configKey] = plaintext;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(
                        $"[CompanySettings] Failed to decrypt '{dbKey}': {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"[CompanySettings] Could not load settings from DB (falling back to env vars): {ex.Message}");
        }
    }
}
