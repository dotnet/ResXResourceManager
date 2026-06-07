namespace ResXManager.VSIX.Compatibility.x64;

using System;
using System.Collections.Generic;

public interface ICustomToolRunner : IDisposable
{
    void Enqueue(IEnumerable<EnvDTE.ProjectItem>? projectItems);
}
