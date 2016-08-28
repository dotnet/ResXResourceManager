// Guids.cs
// MUST match guids.h

// ReSharper disable InconsistentNaming
namespace tomenglertde.ResXManager.VSIX
{
    using System;

    internal static class GuidList
    {
        public const string guidResXManager_VSIXPkgString = "43b35fe0-1f30-48de-887a-68256474202a";
        public const string guidResXManager_VSIXCmdSetString = "4beab5e4-da91-4600-bd36-53a67b206b19";
        public const string guidToolWindowPersistanceString = "79664857-03bf-4bca-aa54-ec998b3328f8";

        public static readonly Guid guidResXManager_VSIXCmdSet = new Guid(guidResXManager_VSIXCmdSetString);
    };
}