using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Bagile.Api.Services;
using Dapper;
using Google.Apis.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

namespace Bagile.Api.Endpoints;

public static class PortalEndpoints
{
    public static void MapPortalEndpoints(this WebApplication app)
    {
        app.MapPost("/portal/auth/google", HandleGoogleLogin);
        app.MapGet("/portal/keys", ListKeys);
        app.MapPost("/portal/keys", CreateKey);
        app.MapDelete("/portal/keys/{id}", RevokeKey);
    }

    private static async Task<IResult> HandleGoogleLogin(
        HttpContext context,
        IConfiguration config,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("PortalEndpoints");
        try
        {
            var body = await context.Request.ReadFromJsonAsync<GoogleLoginRequest>();
            if (body == null || string.IsNullOrWhiteSpace(body.IdToken))
                return Results.BadRequest(new { error = "idToken is required" });

            var googleClientId = config["Portal:GoogleClientId"];
            if (string.IsNullOrWhiteSpace(googleClientId))
            {
                logger.LogError("Portal:GoogleClientId is not configured");
                return Results.Json(new { error = "Server configuration error" }, statusCode: 500);
            }

            var jwtSecret = config["Portal:JwtSecret"];
            if (string.IsNullOrWhiteSpace(jwtSecret))
            {
                logger.LogError("Portal:JwtSecret is not configured");
                return Results.Json(new { error = "Server configuration error" }, statusCode: 500);
            }

            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(body.IdToken,
                    new GoogleJsonWebSignature.ValidationSettings
                    {
                        Audience = new[] { googleClientId }
                    });
            }
            catch (InvalidJwtException ex)
            {
                logger.LogWarning("Google JWT validation failed: {Message}", ex.Message);
                return Results.Unauthorized();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error validating Google JWT (possible network issue fetching Google certs)");
                return Results.Json(new { error = "Token validation failed — please try again" }, statusCode: 502);
            }

            var allowedEmails = (config["Portal:AllowedEmails"] ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (allowedEmails.Length > 0 &&
                !allowedEmails.Contains(payload.Email, StringComparer.OrdinalIgnoreCase))
            {
                return Results.Json(new { error = "Email not authorised" }, statusCode: 403);
            }

            var token = GenerateJwt(payload.Email, payload.Name ?? payload.Email, jwtSecret);

            // Auto-create an API key for the user if they don't have an active one
            var connStr = GetConnectionString(config);
            string? apiKey = null;
            await using var conn = new NpgsqlConnection(connStr);

            var existingCount = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM bagile.api_keys WHERE owner_email = @email AND is_active = TRUE",
                new { email = payload.Email });

            if (existingCount == 0)
            {
                var (rawKey, hash, prefix) = ApiKeyValidator.GenerateKey();
                await conn.ExecuteAsync(
                    @"INSERT INTO bagile.api_keys (key_hash, key_prefix, owner_email, owner_name, label)
                      VALUES (@hash, @prefix, @email, @name, 'Auto-created')",
                    new { hash, prefix, email = payload.Email, name = payload.Name ?? payload.Email });
                apiKey = rawKey;
            }

            return Results.Ok(new
            {
                token,
                email = payload.Email,
                name = payload.Name,
                apiKey
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception in HandleGoogleLogin");
            return Results.Json(new { error = "Internal server error" }, statusCode: 500);
        }
    }

    private static async Task<IResult> ListKeys(
        HttpContext context,
        IConfiguration config)
    {
        var claims = ValidatePortalJwt(context, config);
        if (claims == null) return Results.Unauthorized();

        var connStr = GetConnectionString(config);
        const string sql = @"
            SELECT id AS Id, key_prefix AS KeyPrefix, label AS Label,
                   owner_email AS OwnerEmail, owner_name AS OwnerName,
                   is_active AS IsActive, created_at AS CreatedAt,
                   last_used_at AS LastUsedAt, revoked_at AS RevokedAt
            FROM bagile.api_keys
            WHERE owner_email = @email
            ORDER BY created_at DESC;";

        await using var conn = new NpgsqlConnection(connStr);
        var keys = await conn.QueryAsync(sql, new { email = claims.Email });
        return Results.Ok(keys);
    }

    private static async Task<IResult> CreateKey(
        HttpContext context,
        IConfiguration config,
        ApiKeyValidator validator)
    {
        var claims = ValidatePortalJwt(context, config);
        if (claims == null) return Results.Unauthorized();

        var body = await context.Request.ReadFromJsonAsync<CreateKeyRequest>();
        var label = body?.Label ?? "Default";

        var (rawKey, hash, prefix) = ApiKeyValidator.GenerateKey();

        var connStr = GetConnectionString(config);
        const string sql = @"
            INSERT INTO bagile.api_keys (key_hash, key_prefix, owner_email, owner_name, label)
            VALUES (@hash, @prefix, @email, @name, @label)
            RETURNING id;";

        await using var conn = new NpgsqlConnection(connStr);
        var id = await conn.ExecuteScalarAsync<Guid>(sql, new
        {
            hash, prefix,
            email = claims.Email,
            name = claims.Name,
            label
        });

        return Results.Ok(new
        {
            id,
            key = rawKey,
            prefix,
            label,
            message = "Save this key now — you won't see it again."
        });
    }

    private static async Task<IResult> RevokeKey(
        Guid id,
        HttpContext context,
        IConfiguration config)
    {
        var claims = ValidatePortalJwt(context, config);
        if (claims == null) return Results.Unauthorized();

        var connStr = GetConnectionString(config);
        const string sql = @"
            UPDATE bagile.api_keys
            SET is_active = FALSE, revoked_at = NOW()
            WHERE id = @id AND owner_email = @email;";

        await using var conn = new NpgsqlConnection(connStr);
        var rows = await conn.ExecuteAsync(sql, new { id, email = claims.Email });

        return rows > 0
            ? Results.Ok(new { message = "Key revoked" })
            : Results.NotFound(new { error = "Key not found" });
    }

    private static string GenerateJwt(string email, string name, string secret)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "bagile-portal",
            audience: "bagile-api",
            claims: new[]
            {
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, name)
            },
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static PortalClaims? ValidatePortalJwt(HttpContext context, IConfiguration config)
    {
        var authHeader = context.Request.Headers.Authorization.ToString();
        if (!authHeader.StartsWith("Bearer ")) return null;

        var token = authHeader["Bearer ".Length..];
        var secret = config["Portal:JwtSecret"];
        if (string.IsNullOrWhiteSpace(secret)) return null;

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "bagile-portal",
                ValidateAudience = true,
                ValidAudience = "bagile-api",
                ValidateLifetime = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
            }, out _);

            var email = principal.FindFirst(ClaimTypes.Email)?.Value;
            var name = principal.FindFirst(ClaimTypes.Name)?.Value;
            if (email == null) return null;

            return new PortalClaims(email, name ?? email);
        }
        catch
        {
            return null;
        }
    }

    private static string GetConnectionString(IConfiguration config)
    {
        return config.GetConnectionString("DefaultConnection")
               ?? config.GetValue<string>("ConnectionStrings:DefaultConnection")
               ?? throw new InvalidOperationException("Connection string not found");
    }

    private record PortalClaims(string Email, string Name);
    private record GoogleLoginRequest(string IdToken);
    private record CreateKeyRequest(string? Label);
}
