using LoreBridge.Core.Vault;

namespace LoreBridge.Application.Vault;

public interface IVaultService
{
    Task<VaultSnapshot> GetSnapshotAsync(
        string workspaceId,
        CancellationToken cancellationToken = default);
}
