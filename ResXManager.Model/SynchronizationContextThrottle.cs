namespace ResXManager.Model
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using JetBrains.Annotations;

    public class SynchronizationContextThrottle
    {
        [NotNull]
        private readonly TaskFactory _taskFactory = new TaskFactory(TaskScheduler.FromCurrentSynchronizationContext());
        [NotNull]
        private readonly Action _target;

        private int _counter;

        /// <summary>
        /// Initializes a new instance of the <see cref="SynchronizationContextThrottle"/> class.
        /// </summary>
        /// <param name="target">The target action to invoke when the throttle condition is hit.</param>
        public SynchronizationContextThrottle([NotNull] Action target)
        {
            _target = target;
        }

        /// <summary>
        /// Ticks this instance to trigger the throttle.
        /// </summary>
        public void Tick()
        {
            if (Interlocked.CompareExchange(ref _counter, 1, 0) != 0)
                return;

            _taskFactory.StartNew(() =>
            {
                _target();
                Interlocked.Exchange(ref _counter, 0);
            });
        }
    }
}
