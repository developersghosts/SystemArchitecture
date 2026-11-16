using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using ServicesA.Application.Abstractions;

namespace ServicesA.Infrastructure.Security;

/// <summary>
/// Encrypts secrets saved in the database with ASP.NET Data Protection (AES-256-CBC + HMACSHA256, rotating keys).
/// The keys are kept outside the database (DataProtection:KeysPath, a docker volume), so a copy of the
/// database alone does not reveal the secrets.
/// </summary>
public sealed class DataProtectionSecretProtector(IDataProtectionProvider provider) : ISecretProtector
{
    private readonly IDataProtector _protector = provider.CreateProtector("ServicesA.Settings.Secrets.v1");

    public string Protect(string plainText) => _protector.Protect(plainText);

    public string? Unprotect(string protectedText)
    {
        try
        {
            return _protector.Unprotect(protectedText);
        }
        catch (CryptographicException)
        {
            return null;
        }
    }
}
