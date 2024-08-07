using MediatR;

namespace Juice.MediatR
{
    public interface IRequestManager
    {
        Task<bool> TryCreateRequestForCommandAsync<T>(Guid id)
            where T : IBaseRequest;

        Task TryCompleteRequestAsync(Guid id, bool success);
    }
}
