using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knapcode.ToStorage.Core.Abstractions
{
    public interface ISystemTime
    {
        DateTimeOffset UtcNow { get; }
    }

    public class SystemTime : ISystemTime
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
