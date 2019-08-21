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

[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\TomsToolbox.Essentials.dll")]
[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\TomsToolbox.Wpf.dll")]
[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\TomsToolbox.Wpf.Styles.dll")]
[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\TomsToolbox.Wpf.Composition.Styles.dll")]
[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\System.Windows.Interactivity.dll")]
[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\Microsoft.Expression.Interactions.dll")]
[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\DataGridExtensions.dll")]