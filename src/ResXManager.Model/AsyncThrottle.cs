namespace ResXManager.Model
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public class AsyncThrottle
    {
        private readonly Action _target;
        private int _counter;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncThrottle"/> class.
        /// </summary>
        /// <param name="target">The target action to invoke when the throttle condition is hit.</param>
        public AsyncThrottle(Action target)
        {
            _target = target;
        }

        /// <summary>
        /// Ticks this instance to trigger the throttle.
        /// </summary>
        public async void Tick()
        {
            try
            {
                Interlocked.Increment(ref _counter);

                await Task.Delay(250).ConfigureAwait(true);

                if (Interlocked.Decrement(ref _counter) != 0)
                    return;

                _target();
            }
            catch
            {
                // nothing we can do here...
            }
        }
    }
}
