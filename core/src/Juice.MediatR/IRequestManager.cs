using MediatR;

namespace Juice.MediatR
{
    public interface IRequestManagerBase
    {
        Task<bool> TryCreateRequestForCommandAsync<T>(Guid id)
            where T : IBaseRequest;

        Task TryCompleteRequestAsync(Guid id, bool success);
    }

    public interface IRequestManager : IRequestManagerBase
    {

    }

    public interface IRequestManager<TContext> : IRequestManagerBase
    {

    }
}
