using System;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio.Shell;

[assembly: AssemblyTitle("ResXManager.VSIX")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: CLSCompliant(false)]
[assembly: NeutralResourcesLanguage("en-US", UltimateResourceFallbackLocation.MainAssembly)]

[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\TomsToolbox.Composition.dll")]
[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\TomsToolbox.Essentials.dll")]
[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\TomsToolbox.ObservableCollections.dll")]
[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\TomsToolbox.Wpf.Composition.dll")]
[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\TomsToolbox.Wpf.Composition.Mef.dll")]
[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\TomsToolbox.Wpf.Composition.Styles.dll")]
[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\TomsToolbox.Wpf.dll")]
[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\TomsToolbox.Wpf.Styles.dll")]
[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\Microsoft.Xaml.Behaviors.dll")]
[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\DataGridExtensions.dll")]