using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Juice.Domain
{
    public interface IModificationInfo
    {
        string? ModifiedUser { get; }
        DateTimeOffset? ModifiedDate { get; }
    }
}
