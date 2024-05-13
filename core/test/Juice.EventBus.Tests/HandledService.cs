using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Juice.EventBus.Tests
{
    public class HandledService
    {
        public List<string> Handlers { get; } = new List<string>();
    }
}
