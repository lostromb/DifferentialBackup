namespace DiffBackup
{
    using System.Threading;

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
