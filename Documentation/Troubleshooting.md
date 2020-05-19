### The extension is always disabled after restarting VS
The VSIX installer sometimes fails to remove the old version of an extension during update; if the same extension is installed twice, VS disables it.
Solution: use https://github.com/remcoros/DuplicateExtensionFinder to remove duplicates.

### The content of the ResxManager window is empty. 
This may have several root causes
1. When you see the message `Error: 'Provide value on 'System.Windows.StaticResourceExtension' threw an exception.'`
  - Multiple versions of some assemblies are used by different extensions. This is a WPF bug that was fixed in .Net 4.7.2 (see https://github.com/Microsoft/dotnet/blob/master/releases/net472/dotnet472-changes.md#wpf). To get rid of this, install .Net 4.7.2 and set `HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\.NETFramework\AppContext\Switch.System.Windows.Baml2006.AppendLocalAssemblyVersionForSourceUri="true"`. (https://raw.githubusercontent.com/tom-englert/WpfResourceIssue/master/ActivateNet472WpfFix.reg)

2. Some assemblies (usually System.Windows.Interactivity) are loaded twice from different locations. Check the output window to see what locations the assembly is loaded from. 
  - If it is loaded from the folder of another extension, disable that extension.
  - If it is loaded from `C:/Program Files (x86)/Microsoft Visual Studio/2017/<edition>/Common7/IDE/PrivateAssemblies`, you can delete it from there.
  - If it is loaded from the GAC, use gacutil.exe to remove it from there.


