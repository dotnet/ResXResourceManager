[<img src="https://raw.githubusercontent.com/dotnet-foundation/swag/master/logo/dotnetfoundation_v4.svg" alt=".NET Foundation" width=100>](https://dotnetfoundation.org)

# ResX Resource Manager 
[![Build Status](https://dev.azure.com/tom-englert/Open%20Source/_apis/build/status/ResXResourceManager?branchName=master)](https://dev.azure.com/tom-englert/Open%20Source/_build/latest?definitionId=35&branchName=master)
[![Sponsor](https://img.shields.io/badge/-Sponsor-fafbfc?logo=GitHub%20Sponsors)](https://github.com/sponsors/tom-englert)
[<img src="https://api.gitsponsors.com/api/badge/img?id=60344691" height="20">](https://api.gitsponsors.com/api/badge/link?p=FvTYDNU2vjlfIiyQ/Xt3lqNu5WSkM2nBDdrkEbFn9pwUyJlQPy8xa2x0whq4DmesGdh2TpAH5F2xZKY5empjmZ3MbhPCdsTkhNkgdI9YKm3KVT0gfaRMVCZOXNBHcgVnOufMOKJY1SZmAswXshqfBg==)

The most popular tool to manage localization of all kind of applications with resx-based resources; one of the highest rated extensions on the [Visual Studio Marketplace](https://marketplace.visualstudio.com/items?itemName=TomEnglert.ResXManager).

---

This is a community driven Open Source project. 
As such it needs your support to keep it alive and evolving. 
The best way to contribute is to help [fixing bugs, or adding new features](https://github.com/tom-englert/ResXResourceManager/issues).
However, if you cannot help with coding, consider to [donate a small amount](Documentation/Topics/Donate.md) to ensure <span>ResX&nbsp;Resource&nbsp;Manager</span> will continue to be a great project that can help you in your daily work.

---

This tool provides central access to all ResX-based string resources in your solution. You can quickly navigate through all resource files and view the content in a well-arranged data grid.
All available languages are displayed side by side in columns, to make it easy to find untranslated strings or clean up orphaned entries. All strings can be quickly edited in place, untranslated entries will be created on the fly while typing.

Suitable for any .Net application; WPF is supported out of the box, just use the x:Static markup extension to access the ResX - resources.

Web applications using Typescript (e.g. Angular) are also supported, see [L10N-Web-Demo](https://github.com/tom-englert/L10N-Web-Demo)

Xamarin apps can be supported using e.g. [XamarinLocalizationSync](https://github.com/maruhe/XamarinLocalizationSync)

## Code of Conduct
This project has adopted the code of conduct defined by the Contributor Covenant to clarify expected behavior in our community.
For more information see the [.NET Foundation Code of Conduct](https://dotnetfoundation.org/code-of-conduct).

## .NET Foundation
This project is supported by the [.NET Foundation](https://dotnetfoundation.org).

## Documentation
Can be found in the [Documentation folder](Documentation/Readme.md).

## Installation
ResXResourceManager is available as VS2019-2022 extension and as a standalone executable to support VS2017 and older or users without Visual Studio at all.
A scripting module is available as well, so you can easily automate resource tasks, e.g. export untranslated string during build.

All versions can be downloaded from the [releases](../../releases) page.

The Visual Studio Extension is also available at the [Visual Studio Marketplace](https://marketplace.visualstudio.com/items?itemName=TomEnglert.ResXManager), or by searching for RESX in the Visual Studio extension manager.
The latest build of the Visual Studio Extension is available at the [Open VsixGallery](http://vsixgallery.com/extension/43b35fe0-1f30-48de-887a-68256474202a)

The standalone version can be installed as click-once application [here](https://clickonce-tom-englert.azurewebsites.net/ResXResourceManager/ResXManager.application).

### Visual Studio Extension:
![Visual Studio Extension](Assets/VisualStudioMainScreen.png)
![MoveToResource](Documentation/Topics/MoveToResource.gif)

### Standalone Application:
![Standalone Application](Assets/StandaloneMainScreen.png)

<img style="float: right;" src="Assets/VS2017%20Launch%20Partner%20Logo%20Small.png">

### Support this Project: 

<a href="https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=TQQR8AKGNHELQ"><img title="Donate" src="https://www.paypalobjects.com/en_US/i/btn/btn_donate_SM.gif" alt="Donate" /></a>

### Powered by 

<a href="http://www.jetbrains.com/resharper/"><img src="http://www.tom-englert.de/Images/icon_ReSharper.png" alt="ReSharper" width="64" height="64" /></a>

### Contributors

This project exists thanks to all the people who contribute. <img src="https://opencollective.com/ResXResourceManager/contributors.svg?width=890&button=false" />


### Backers

Thank you to all our backers! 🙏 [[Become a backer](https://opencollective.com/ResXResourceManager#backer)]

<a href="https://opencollective.com/ResXResourceManager#backers" target="_blank"><img src="https://opencollective.com/ResXResourceManager/backers.svg?width=890"></a>


### Sponsors

Support this project by becoming a sponsor. Your logo will show up here with a link to your website. [[Become a sponsor](https://opencollective.com/ResXResourceManager#sponsor)]

<a href="https://opencollective.com/ResXResourceManager/sponsor/0/website" target="_blank"><img src="https://opencollective.com/ResXResourceManager/sponsor/0/avatar.svg"></a>
<a href="https://opencollective.com/ResXResourceManager/sponsor/1/website" target="_blank"><img src="https://opencollective.com/ResXResourceManager/sponsor/1/avatar.svg"></a>
<a href="https://opencollective.com/ResXResourceManager/sponsor/2/website" target="_blank"><img src="https://opencollective.com/ResXResourceManager/sponsor/2/avatar.svg"></a>
<a href="https://opencollective.com/ResXResourceManager/sponsor/3/website" target="_blank"><img src="https://opencollective.com/ResXResourceManager/sponsor/3/avatar.svg"></a>
<a href="https://opencollective.com/ResXResourceManager/sponsor/4/website" target="_blank"><img src="https://opencollective.com/ResXResourceManager/sponsor/4/avatar.svg"></a>
<a href="https://opencollective.com/ResXResourceManager/sponsor/5/website" target="_blank"><img src="https://opencollective.com/ResXResourceManager/sponsor/5/avatar.svg"></a>
<a href="https://opencollective.com/ResXResourceManager/sponsor/6/website" target="_blank"><img src="https://opencollective.com/ResXResourceManager/sponsor/6/avatar.svg"></a>
<a href="https://opencollective.com/ResXResourceManager/sponsor/7/website" target="_blank"><img src="https://opencollective.com/ResXResourceManager/sponsor/7/avatar.svg"></a>
<a href="https://opencollective.com/ResXResourceManager/sponsor/8/website" target="_blank"><img src="https://opencollective.com/ResXResourceManager/sponsor/8/avatar.svg"></a>
<a href="https://opencollective.com/ResXResourceManager/sponsor/9/website" target="_blank"><img src="https://opencollective.com/ResXResourceManager/sponsor/9/avatar.svg"></a>


