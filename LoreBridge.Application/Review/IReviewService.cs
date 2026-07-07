using LoreBridge.Core.Review;

namespace LoreBridge.Application.Review;

public interface IReviewService
{
    Task<IReadOnlyList<ReviewItem>> GetReviewQueueAsync(string workspaceId, CancellationToken cancellationToken = default);
}
