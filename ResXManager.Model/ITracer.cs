namespace tomenglertde.ResXManager.Model
{
    public interface ITracer
    {
        void TraceError(string value);
        void WriteLine(string value);
    }
}