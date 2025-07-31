using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DiffBackup
{
    internal class AtomicCounter
    {
        private long _value;

        public AtomicCounter()
        {
            _value = 0;
        }

        public void Increment()
        {
            Interlocked.Increment(ref _value);
        }

        public long Count => _value;
    }
}
