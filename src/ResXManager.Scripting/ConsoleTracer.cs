namespace ResXManager.Scripting
{
    using System;
    using System.Composition;

    using ResXManager.Infrastructure;

    [Export(typeof(ITracer)), Shared]
    internal sealed class ConsoleTracer : ITracer
    {
        public void TraceError(string value)
        {
            WriteLine("Error: " + value);
        }

        public void TraceWarning(string value)
        {
            WriteLine("Warning: " + value);
        }

        public void WriteLine(string value)
        {
            Console.WriteLine(value);
        }
    }
}