using System.Threading;
using System.Threading.Tasks;

namespace ImagePlatform.Security.Secrets;

/// <summary>
/// Abstraction for retrieving secrets (connection strings, encryption keys, API keys).
/// In prod this is backed by Azure Key Vault; locally it can be in-memory/user-secrets.
/// </summary>
public interface ISecretProvider
{
    Task<string?> GetSecretAsync(string name, CancellationToken cancellationToken = default);
}


