namespace ResXManager.Scripting
{
    using System;
    using System.ComponentModel.Composition;

    using tomenglertde.ResXManager.Infrastructure;

    [Export(typeof(ITracer))]
    internal class ConsoleTracer : ITracer
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