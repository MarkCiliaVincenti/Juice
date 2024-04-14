using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Juice.Domain
{
    public interface ICreationInfo
    {
        string? CreatedUser { get; }
        DateTimeOffset CreatedDate { get; }
    }
}
