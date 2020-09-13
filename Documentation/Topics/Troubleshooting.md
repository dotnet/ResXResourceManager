### The extension is not loaded.

Sometimes after an update you might get message like this:
```
CreateInstance failed for package [VsPackage]Source: 'mscorlib' Description: Exception has been thrown by the target of an invocation.
System.Reflection.TargetInvocationException: Exception has been thrown by the target of an invocation. ---> System.IO.FileNotFoundException: Could not load file or assembly 'TomsToolbox.Composition, Version=2.1.1.0, Culture=neutral, PublicKeyToken=60b39f873a8e3fc2' or one of its dependencies. The system cannot find the file specified.
   at ResXManager.VSIX.VsPackage..ctor()
   --- End of inner exception stack trace ---
```

__This is not bug in ResX Resource Manager!__ This seems to be a caching problem in Visual Studios extension loader/manager. 

#### Solution
- Uninstall ResX Resource Manager using Visual Studios extension manager (Menu "Extensions" => "Manage Extensions")
- __Close Visual Studio__
- Wait until the VSIX-Installer has uninstalled the extension
- Start Visual Studio
- Verify that ResX Resource Manager is no longer listed by the extension manager
- __Close Visual Studio__
- Now install ResX Resource Manager again, either by double clicking the .vsix file, or from within Visual Studios extension manager

### The extension is always disabled after restarting VS
__This is not bug in ResX Resource Manager!__
- The VSIX installer sometimes fails to remove the old version of an extension during update
- if the same extension is installed twice, VS disables it.
##### Solutions
- Close VS and use [DuplicateExtensionFinder](https://github.com/remcoros/DuplicateExtensionFinder) to remove duplicates.
- Uninstall the extension and restart VS. Repeat until VS does not list the extension any longer. Now install the latest version.

### The content of the ResxManager window is empty. 
__This is not bug in ResX Resource Manager!__

This may have several root causes
1. When you see the message `Error: 'Provide value on 'System.Windows.StaticResourceExtension' threw an exception.'`
  - Multiple versions of some assemblies are used by different extensions. This is a WPF bug that was fixed in .Net 4.7.2 (see https://github.com/Microsoft/dotnet/blob/master/releases/net472/dotnet472-changes.md#wpf). To get rid of this, install .Net 4.7.2 and set `HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\.NETFramework\AppContext\Switch.System.Windows.Baml2006.AppendLocalAssemblyVersionForSourceUri="true"`. (https://raw.githubusercontent.com/tom-englert/WpfResourceIssue/master/ActivateNet472WpfFix.reg)

2. Some assemblies (usually System.Windows.Interactivity) are loaded twice from different locations. Check the output window to see what locations the assembly is loaded from. 
  - If it is loaded from the folder of another extension, disable that extension.
  - If it is loaded from `C:/Program Files (x86)/Microsoft Visual Studio/2017/<edition>/Common7/IDE/PrivateAssemblies`, you can delete it from there.
  - If it is loaded from the GAC, use gacutil.exe to remove it from there.


