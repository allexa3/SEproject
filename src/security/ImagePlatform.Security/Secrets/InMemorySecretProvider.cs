using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ImagePlatform.Security.Secrets;

public sealed class InMemorySecretProvider : ISecretProvider
{
    private readonly IReadOnlyDictionary<string, string> _secrets;

    public InMemorySecretProvider(IReadOnlyDictionary<string, string> secrets)
    {
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
    }

    public Task<string?> GetSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_secrets.TryGetValue(name, out var value) ? value : null);
    }
}


