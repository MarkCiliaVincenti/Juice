using MediatR;

namespace Juice.MediatR
{
    public interface IRequestManagerBase
    {
        Task<bool> TryCreateRequestForCommandAsync<T>(Guid id)
            where T : IBaseRequest;

        Task TryCompleteRequestAsync<T>(Guid id, bool success)
            where T : IBaseRequest;
    }

    public interface IRequestManager : IRequestManagerBase
    {

    }

    public interface IRequestManager<TContext> : IRequestManagerBase
    {

    }
}
