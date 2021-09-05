namespace ResXManager.VSIX
{
    using System;
    using System.Collections.Generic;

    public interface ICustomToolRunner : IDisposable
    {
        void Enqueue(IEnumerable<EnvDTE.ProjectItem>? projectItems);
    }
}