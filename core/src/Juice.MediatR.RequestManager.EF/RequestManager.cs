using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Juice.MediatR.RequestManager.EF
{
    internal class RequestManager : IRequestManager
    {
        private ClientRequestContext _context;
        public RequestManager(ClientRequestContext context)
        {
            _context = context;
        }

        public async Task TryCompleteRequestAsync(Guid id, bool success)
        {
            try
            {
                var request = await _context.ClientRequests.FindAsync(id);
                if (request != null)
                {
                    if (success)
                    {
                        request.MarkAsDone();
                    }
                    else
                    {
                        request.MarkAsFailed();
                    }
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception)
            {

            }
        }

        public async Task<bool> TryCreateRequestForCommandAsync<T>(Guid id)
            where T : IBaseRequest
        {
            // retry failed or interupted conmmands
            if (await _context.ClientRequests.AnyAsync(r => r.Id == id
                && (r.State == RequestState.ProcessedFailed
                || (r.State == RequestState.New && r.Time < DateTimeOffset.Now.AddSeconds(-15)))))
            {
                return true;
            }
            if (await _context.ClientRequests.AnyAsync(r => r.Id == id))
            {
                return false;
            }
            try
            {
                _context.ClientRequests.Add(new ClientRequest(id, typeof(T).Name));
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                return false;
            }
        }
    }
}
