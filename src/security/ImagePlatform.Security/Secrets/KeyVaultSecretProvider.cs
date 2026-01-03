using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace ImagePlatform.Security.Secrets;

/// <summary>
/// Azure Key Vault implementation of <see cref="ISecretProvider"/>.
/// Uses DefaultAzureCredential, so authentication is environment-dependent (VS, Azure CLI, Managed Identity, etc.).
/// </summary>
public sealed class KeyVaultSecretProvider : ISecretProvider
{
    private readonly SecretClient _client;

    public KeyVaultSecretProvider(Uri vaultUri)
    {
        if (vaultUri is null) throw new ArgumentNullException(nameof(vaultUri));
        _client = new SecretClient(vaultUri, new DefaultAzureCredential());
    }

    public async Task<string?> GetSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Secret name is required.", nameof(name));

        try
        {
            var response = await _client.GetSecretAsync(name, cancellationToken: cancellationToken);
            return response.Value.Value;
        }
        catch
        {
            // Caller decides how to handle missing/unauthorized secrets; for now, normalize to null.
            return null;
        }
    }
}


