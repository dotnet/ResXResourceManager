1.64
- #444: Xliff comments from MAT are not synchronized.
- Run Xliff synchronization before build.
- Fix #465: Adding a new language should create missing .xlf files if sync is enabled.
- Fix #467: Applying translation fails when language file does not exist.
- Closing solution clears web-export

1.63
- Fix #233: Automatically added the BOM 
- Support XLIFF
- #446: Add an option to set #TODO flag in the comment field, instead of on the value field
- Fix #452: Not installable in VS 2017
- Fix #460: ArgumentException when trying to enter text for any key for language Ewe (ee) 

1.62
- Fix #435: .NET6 Unable to Move to Resource

1.61
- Fix colors to better match VS2022
- Fix #433: possible NullReferenceException
- Avoid blocking of UI when iterating over a large project.

1.60
- Add possibility to filter resources for web export
- Make web files export configuration available on configuration tab

1.59
- Fix #428: Move to resource does not work for .html files

1.58
- Fix #426: Cannot Load HtmlAgilityPack When Using Azure to Translate (VS once again fails to load dependencies.)

1.57
- Fix #419: Adding a new key should focus the new item.
- VS2022 improvements

1.56
- Add Arabic question mark to NormalizePunctuation method.
- Fix #424: Broken resource files may crash application on launch
- Improve DGX filtering experience. Ctrl+F jumps to filter, arrow down jumps back to grid. Fixes #420

1.55
- Fix #418: Solution-level RESX files throw FileNotFoundException
- Fix #290: Can't make T4 template work with .NET Core project

1.54
- #415: Support Visual Studio 2022 (v17.0)

1.53
- Fix #405: allow to specify Deepl api url, so the free subscription can be used.
- Fix #406: Wrap EnvDTE.Document.ProjectItem to catch unexpected com exceptions.
- Fix #407: string format pattern parsing for patterns with custom format section

1.52
- #403: improve loading, only enumerate project files when view is visible or when needed by a feature.
 
1.51
- Fix #397: Tool window unexpectedly looses focus on ESC key, data is committed instead of reverted

1.50
- Fix #392: Occasional "The operation was canceled" messages
- Fix #368: Authentication for Azure: Region can be specified

1.49
- Fix #372: make XmlConfiguration reader more robust.
- Fix #379: Azure auth key not stored if "Save credentials" is checked and app is closed while the focus is still on the check box.
- Fix #357 Add scroll if too many languages
- Fix #376: support Custom dialect resource

1.48
- Fix #367: Update of grid after editing in the top-right text box is broken

1.47
- Fix #359: shield enumeration of project items against invalid or broken items.
- Fix #363: Revert using CollectionViewSource in XAML, has huge performance impact.
- Fix #364: Applying Translations do not create new resources to target language files.
- Support color themes for the standalone application 

1.46
- Fix #355: Grouping view: Group headers scroll horizontally with the content
- Fix #356: Sorting of columns does not work in grouped view

1.45
- Fix #354: Crash of devenv.exe when opening ResX window (via update DGX)

1.44
- Fix #345: Donate button layout
- Fix #352: Exception when loading .resw resources in VS extension.
- Fix #348: Auto-focus on Last Marked Row when crossing out the search pattern

1.43
- #341: Improve performance when editing large amounts of texts
- Show error message when loading of resx file fails
- Improve UI performance by using column-virtualization in DataGrid
- Improve loading experience by using background loading of files

1.42
- Fix #328: update broken links

1.41
- #191: Integration of DeepL translation service
- Fix #272: Allow to specify ranking for each translator.
- Fix #295: Do not treat invariant empty neutral strings as error.
- Improve UX of column selector.
- Fix: empty neutral values should be considered an error if they are not invariant
- Use a password box for credentials in TranslatorConfiguration
- Fix #315, Fix #307: Google/DeepL translate only 1 language at the same time

1.40
- Provide TS-functions for type-safe formatting.
- Visualize translator activity

1.39
- Fix #265: Add new language dialog always opens in center of VS
- Fix: AzureTranslator rating should be 1.0
- Fix #270: exclude keyboard shortcut prefixes from punctuation checks
- Fix #273: neutral language image missing in some places
- Fix #279: Code reference detection defaults for vb razor
- Fix #283: ignore language specific punctuation in Resource warnings
- #293: Auto-detect HTML for properly setting textType parameter in AzureTranslator
- Optionally start a PowerShell script after saving (Standalone only)
- Optionally generate .ts and .json files to support web apps (s. https://github.com/tom-englert/L10N-Web-Demo) 

1.38
- Fix context menu style

1.37
- #203: Empty trailing header columns raises exception
- #205: Updated to Azure Translations V3
- #229: Sort by key when opening
- #236: Additional fixed checks for the translation terms
- #257: Selection of wrong .resx file when project contains custom controls
- #146: Icon overlay on tool bar

1.36
- #227: Hindi numeral in format specifier crashes Visual Studio
- #228: Google API key not remembered across restarts

1.35
- #209: Show proper error message when a language is not defined.
- #224: Error when opening from solution explorer when filter was previously applied.

1.34
- #204: Delete messages must not refer to rows, could be also columns
- #202: Trim invalid characters from translations.

1.33
- #200: fix conflicting assembly loading.

1.32
- #199: Sometimes no designer code is generated.

1.31
- #196: Finalize VS2019 support, drop VS2013 support (no longer compatible)

1.30
- #189: fix typo in datetime pattern
- #194: Exceeding Excel's limits should abort when calling ExportExcel
- #196: Support for Visual Studio 2019
- #193: reduce noise of messages in the output window

1.29
- Fix #192: Do not reset filter if requested entry already matches.
- Fix #189: Default Excel-export file name contains time stamp

1.28
- Fix #188: Revert extra null check
- Fix #186: change default configuration for ConfirmAddLanguageFile to false.

1.27
- Fix #184: double-check command parameters
- Fix #185: broken translator url

1.26
- Fix #180: Improve Copy/Paste UX.
- Fix #181: Revert skipping small words in the key
- Fix #182: Improve UX when using the entity FilterText: Auto-escape backslashes if Regex is not valid.

1.25
- Fix #166: Option for automatic removing of empty entries
- Fix #171: Bad caret visualization on Move to resource utility with Visual Studio dark theme
- Fix #172: Support pseudo-locales
- Fix #177: Lock untranslatable columns in Excel

1.24
- Fix #151 and #84: Enhance the Move To Resource dialog.
- Fix #143: Add a workaround for the Visual Studio UWP Designer bug to make it work until it gets finally fixed by Microsoft.

1.23
- Fix #144: Avoid usage of file info to not fail on long file names according to provided stack trace.
- Fix #148: Improve UX, delete is only enabled if all selected cells can be cleared.
- Fix #152: Workaround for VB special 'My' folder

1.22
- Fix #142: Sorting in UI is not culture aware
- Fix #136: Add text only (csv) export/import feature
- Fix #135: MoveToResource patterns for aspx - fix existing pattern and add alternatives.
 
1.21
- Fix #133: Member name same as containing type generates compile error.
- Fix #132: Missing/empty neutral resources aren't shown if "show only warnings" is enabled

1.20
- Fix #75: indenting of comments
- Fix #129: MoveToResource - improve usability, better strategy to remember the last selected resource

1.19
- Update DGX to fix #91

1.18
- Fix Update all dependencies to use and reference only the latest version of System.Windows.Interactivity

1.17
- Fix #117, #122, #123 and related: Issues with mixed versions of System.Windows.Interactivity

1.16
- Fix #117 and related: Issues with mixed versions of System.Windows.Interactivity

1.15
- Fix #72 and related: Issues with mixed versions of System.Windows.Interactivity

1.14
- Fix #107/#108: Excel import of unchanged or formatted cells.
- Update Chinese translation

1.13
- Fix #98: Newtonsoft.json was excluded by MS build task.
- Fix #95: zh-hant support for Google Translator.
- Fix #97: Too many warnings about missing translations block the build.

1.12
- Fix #60: Center dialog on the window containing the ResourceView
- Fix #33: Text wrapping in cells can be enabled.
- Fix #28: output warnings in the error list.
- Fix #69: Excel translates \r into `_x000D_` during paste => remove extra `_x000D_`
- Fix #76 / Fix #77: deferred commit of changes prohibits saving from scripting model.
- Fix #78: UWP (.resw) projects need to detect the neutral language from the settings.
- Fix #88: Only import primary cell text, ignore any metadata.
- MoveToResource: Add patterns as suggested by huntertran in #82
- Fix #92: Add Microsoft Terminology Services as a translation option
- Fix #93: Remove keyboard shortcut specifier ('&' or '_') in strings before translating 

1.11
- add CellReference to make ClosedXML and EPPlus excel libraries happy

1.10
- Fix #57: Don't use the outdated VS internal browser to show web pages
- Fix #58: Only log solution events if performance tracing is on

1.9
- Fix #51: Designer files are not created after modification of resources.
- Fix target culture selection in translation view.
- Fix: preserve comments in WinForms designer resources

1.8
- Fix #44: avoid reporting errors when items are not available
- Fix #48: extend default list of file extensions for web projects
- Fix #46: Make languages scroll vertically if panel size exceeds container size

1.7
- Fix #39: SortNodesByKey is not active when saving files by Visual Studios "Save All" button.
- Fix #42: Update toolbox to fix performance issue with TextBoxVisibleWhiteSpaceDecorator

1.6
- Fix #36, #39: Performance issues during save when SortFileContentOnSave is active.
- Only allow to manually change index if SortFileContentOnSave is off.

1.5
- **Library references updated: In case of crashes, [.Net Framework August 2017 Preview of quality rollup](https://blogs.msdn.microsoft.com/dotnet/2017/08/16/net-framework-august-2017-preview-of-quality-rollup/) might be needed!** (WPF fails to load resources if two versions of the same assembly are loaded. [378607])
- Fix #34: allow invariant per cell.
- Delete cell content in cell selection mode.

1.4
- Fix #29: new files are not created when applying translations, VS crashes on 32bit
- Fix #26: excel column index >26 was wrong

1.3
- Improve copy/paste usability
- Fix #20, #21: Avoid unnecessary selection of all items, and remember selection
- Speed up loading of translation tab

1.2
- #4: Add support for Simplified Chinese

1.1
- Provide latest build via Open VsixGallery (http://vsixgallery.com/extension/43b35fe0-1f30-48de-887a-68256474202a)
- #11: Fix scripting engine and support file exclusion filter in scripting and VS extension.

1.0.0.99:
- #8: type mismatch in generated code patterns

1.0.0.98:
- #8: Move to resource dialog fields are not properly updated on load.
- #9: AuthenticationKey not saved when changing only the key

1.0.0.97:
- Add configurable option to exclude sourcefiles from being processed as soon as possible (by Christophe Gijbels <git@gijbels-it.be>)
- Migrate all links from codeplex to github, move click-once install/update location.
- Optimize load sequence, startup time, and startup error logging.

1.0.0.96:
- Improve usability of MoveToResource (apply the resource filter to exclude e.g. EF Migrations / put preferred and last used entity on top of the list)
- Improve handling of resources with empty keys.

1.0.0.95:
- WI4778: Support for csthml / Move to resource.
- WI4779: Sort resx on save only works in standalone, but not in VS Extension.
- Index column does not show up.
- Index column does not refresh when sort resx on save changes index.

1.0.0.94:
- Update packages to align with other extensions (to avoid https://connect.microsoft.com/VisualStudio/feedback/details/2993889/)

1.0.0.93:
- Update copyright and VS gallery link.

1.0.0.92:
- GH#1: Skip the creation of empty resx files when importing from excel.

1.0.0.91:
- WI4762: Solution specific configuration is not reloaded correctly when the solution changes.

1.0.0.90:
- Support the new Azure translator.

1.0.0.89:
- TT generated code does not require TomsToolbox.Desktop NuGet package by default.
- Drop Support of VS2010 to be able to support VS2017. For VS2010 projects you can use V1.0.0.88 or the standalone version.
- Support VS2017 RC

1.0.0.88:
- WI4756: Change solution neutral language doesn't work.

1.0.0.87:
- WI4746: Move To Resource dialog is empty.
- WI6471, WI4722, WI4747: Add scripting module for automating resource tasks with PowerShell scripts.

1.0.0.86:
- WI4723: Background saving / on-demand saving. (Configurable via options tab)
- WI4740: Improved loading performance.
- WI4741: Show less columns to improve scrolling performance.
- WI4742: Remember size and position of window.
- WI4743: Switching tabs loses sort and scroll info
- WI4744: Show also language codes in the column headers.

1.0.0.85:
- Speed up SelectGroupOnGroupHeaderClick.
- Defer loading of resources until really needed to not interfere with solution load time.
- Fix: Preserve comments in WinForms designer resources.

1.0.0.84:
- WI4736: Fix crash if WebRequest.DefaultWebProxy is null.

1.0.0.83:
- WI4736: ToolWindow fails to load if other extensions using different versions of DataGridExtensions are installed.

1.0.0.82:
- Use .Net 4.5x to make use of "VirtualizingWhenGrouping" to speed up the grouped view with large projects.
- Bypass legacy SCM support when files are not read only.
- Fix block copy of cells when columns have been reordered.
- Double click on group header selects only the group.

1.0.0.81:
- WI4716: Fix: Spell check is always disabled in Win7
- Refresh of model is running too often.
- Add color schemed styles for windows.
- WI4711: Shield packages against crashes when VS installation is broken.

1.0.0.80:
- WI4721: Fix context menu style and some other visual improvements.
- Improve speed of code reference tracker analysis (thanks to Pavel Kotrč!)
- WI7419: Continue loading other projects if one fails.
- WI4716: Another crash when starting elevated as different user due to unavailable spell checker.
- WI4714: Preserve target culture selection for translations
- "Move to Resource": include type and member name in key if possible

1.0.0.79:
- WI1146: UI follows VS color themes.
- WI4712: Error messages in output window when loading MPF (e.g. wix installer) projects
- WI4709, 4717: Always enable the add key button and show a hint if it's not possible to add one now.
- WI4715: Excel export: Preserve leading whitespace.
- WI4711: Avoid crash when VS is unable to create the view.

1.0.0.78:
- "Move to Resource": many small improvements.
- WI4707: New resource values are not saved / lost if Visual Localizer is installed.
- WI4706: Crash when starting elevated as different user.
- WI4703: Excel import from multiple sheet export is broken.

1.0.0.77:
- WI4704: Unable to run extension (when there are broken items in the project)
- WI4668: VS Crashes when showing lines with warnings (changing the filter while there are uncommitted changes in the data grid)

1.0.0.76:
- WI4693: Automatically referencing System.ComponentModel.DataAnnotations.
- WI4698: Changes in designer delete resources => disable "add" for WinForms designer resources.
- WI4699: Use an invariant sortable name as default file name for snapshots.
- WI4700: Open ResxManager to specific form resource.
- WI4701: Crashes VS 2015 on click Configuration tab.
- WI4702: Can't install your extension in new VS"15"Preview.

1.0.0.75:
- Use new DGX to decouple resource location to resolve conflicts with other plugins.

1.0.0.74:
- Fix missing synchronization of main table.

1.0.0.73:
- WI4694: Fix load time degradation.
- WI4694: Bing Credentials not saving.
- WI4669: "Move to resource" feature added.

1.0.0.72:
- Speed up refresh of the grid
- Allow regular expressions in the grids columns filters.
- Improve filter tool tip in the project list.
- Fix: new cultures are not instantly available for translation.
- WI4689: After inserting a new key automatically scroll to the newly inserted key.

1.0.0.71:
- Fix broken grouping view.

1.0.0.70:
- WI4683: T4 Templates are added as build action "Content", should be "None".
- WI4685: Cell selection should not be active by default.
- WI4686: Trigger custom tool when any language changes.
- WI4687: WinForms designer drops comments in resx, removing @Invariant.
- WI4653: Keep sorting after file change.

1.0.0.69:
- WI4680: Problem when copying/cutting unicode resource strings.

1.0.0.68:
- Fix broken copy/paste for complex content.

1.0.0.67:
- WI4650: Show namespace in the resource list (for project residing in the solution folder)
- WI4651: Resources are not found if the resx file has a default namespace
- WI4668: VS Crashes when showing lines with warnings
- WI4660: Select & Copy in column mode

1.0.0.66:
- WI4604, WI4638, WI4642: New snapshot feature allows to filter for changes (https://github.com/dotnet/ResXResourceManager/blob/master/Documentation/Topics/Snapshots.md)
- WI4657: Allow to enter tab characters in the top-right edit field.

1.0.0.65:
- WI4646: Cannot add new key time to time - exception triggered.
- WI4652: Code generator can be selected in the project list (VS Extension only).
- WI4654: Properly handle xml files with encodings other than UTF8

1.0.0.64:
- WI4645: VS extension fails to load: "The composition produced a single composition error. ..." 

1.0.0.63:
- WI4634: Fix: Duplicate keys prevent application to open.
- WI4637: Entries can be ordered by specifying an index.
- WI4641: Fix: ClickOnce Version crashes while browse for new directory (pre Vista systems only)

1.0.0.62:
- WI1244: Transform text templates.
- WI4636: File name collision in Excel export.

1.0.0.61:
- WI4623: Fix: Value cannot be null
- WI4622: Detect inconsistent string format parameters

1.0.0.60:
- WI4630: Add New Key not working

1.0.0.59:
- WI4610: Open ResxManager to an specific file
- WI4625: One Button Click For All Locales
- WI4623: Improve error logging

1.0.0.58:
- WI4614: Regression in spreadsheet import
- WI4621: excel import/export broken in 1.0.0.57
- WI4615: App crashing when trying to load resx folder
- WI4609: Issue with path length

1.0.0.57:
- WI4595: Excel sheet name too long: Export can now be configured to create one single sheet.

1.0.0.56:
- WI4592: Word wrap for the top-right edit box.

1.0.0.55:
- WI4584: V 1.0.0.54 does not start in VS, only an exception is shown

1.0.0.54:
- WI1360: Highlight entries where the RESX file is missing at all with a hatch brush.
- WI1360: Add all missing RESX files. (Configurable)
- WI4578: Mark auto-translated texts. Configurable prefix can be added.
- WI4582: Translation not working from zh-CHT to zh-CHS.

1.0.0.53:
- WI1171: Automatic translations. (fix some minor issues)
- WI1434: Sort order when sorting RESX files by key is configurable.
- WI1438: Link to translator homepage is broken in VS-Extension.

1.0.0.52:
- WI1171: Automatic translations.
- WI1435: Reuse translations.

1.0.0.51:
- WI1420: Extend/fix default code reference detection patterns.
- WI1421: Visibility of new added resource files.
- WI1425: Improve keyboard navigation.

1.0.0.50:
- WI1404: Improve error messages when Excel import fails
- WI1407: XML declaration being improperly removed from saved resource file
- WI1412: Paste should not clear existing text
- WI1413: Support for RTL languages

1.0.0.49:
- Added configuration page to support more features. 
- Detect code references algorithm is now fully configurable.
- WI1359: FTP access -> FileNotFoundException.
- WI1391: Show stats in status bar.
- WI1321: Add an option to sort resx file content by key => on the configuration page.

1.0.0.48:
- WI1388: Support VS2014 (CTP)
- WI1378: Select first .resx file in list when opening window => select all, not only first.
- WI1359: Improved column sizing experience.

1.0.0.47:
- WI1384: "Add new key" shortcut not working - added "Shift+Ins" as alternative shortcut.
- WI1359: Save column settings; visible/hidden columns are persisted during sessions.
- WI1374: System.ArgumentException: Cannot add instance of type 'PopupFocusManagerBehavior' to a collection of type 'BehaviorCollection'; 
- WI1375: Detect code references not detecting...; relax detection criteria.
- WI1375: Flat View - show Project/Resx File Path on row hover; is shown in the status bar.
- WI1381: Error in import from excel - Precondition failed: !string.IsNullOrEmpty(key); skip columns with data but empty key.

1.0.0.46:
- WI1368: Some of the country flags are mixed up: Refined algorithm to lookup default flag for neutral culture; add overview of languages, flags for neutral cultures are editable.
- WI1358: Add possibility to increase the font size: Default font size follows VS text size, supports zoom with "ctrl+mouse wheel" like in the VS text editor. 
- WI1361: Visual Studio 2012 crashes when turning on "Detect code references".

1.0.0.45:
- WI1337: Export selection exports only selected lines and columns.
- WI1338/1339: "Show only lines with missing strings" filters only the visible columns.

1.0.0.44:
- WI1334: Delete is deleting too much. (after changing the key, an item stays selected without selection being visible)
- WI1329: Crash when adding new resource file (try to avoid unnecessary refresh while adding new resource file)

1.0.0.43:
- WI1327: ArgumentException when starting VS (occurred when ResXManager window is open but no solution is loaded)

1.0.0.42:
- WI1325: Empty cell does not remove "data" tag in code behind. => Empty nodes are removed, except for the neutral language.
- WI1319: Cannot add new keys in V1.0.0.41 => Restored input dialog for new keys, since inline editing conflicts with filtering.
- WI1320: List of unused Resources: Support for Web projects. => Web projects and attribute references are supported.

1.0.0.41:
- WI1313: Excel import does not properly handle cells with individual formatting.
- WI1314: Show the number of references found in code to be able to detect unused resources.
- WI1315: Add keyboard shortcut for new key and refactor/simplify the whole workflow. 
- Add shortcuts for cut/copy/paste as well.
- Add a help button that opens the documentation page.

1.0.0.40:
- WI1313: Excel import does not properly handle empty cells.
- WI1312: Huge performance issues since few versions.

1.0.0.39:
- WI1272: Crash when trying to delete an item => Fixed raise condition in list enumerator.

1.0.0.38:
- Comment can be set if value is empty.
- WI1305: Comments are now included in import/export and in copy/cut/paste.
- WI1294: Context menu does not work in the grouped view.
- WI1293: Model now has a Save() method.
- WI1272: Crash when trying to delete an item => Improved error handling for all commands.

1.0.0.37:
- Fix some minor selection and refresh issues.

1.0.0.36
- WI1238: Excel export: Only export the selected files.
- WI1243: Preserve the selected projects when refreshing/reloading
- WI1258: Excel export fails
- WI1264: Showing files with namespace

1.0.0.35
- WI1105: Renaming the resource key is possible, references are not updated.
- WI1237: Fixed: Top edit field does no spell checking.
- WI1239: Key column is fixed 
- WI1240: Copy&Paste refactored, does now copy/cut all selected rows.

1.0.0.34
- WI1230: ResX Manager causes errors in TFS team working
- WI1233: Error opening exported xlsx to Excel 2013

1.0.0.33
- WI1228: Resource File Names are Case Sensitive
- WI1188: Feature Request - Height Adjustable Rows (Excel likes editing filed on the top)

1.0.0.32
- WI1206: Exporting error

1.0.0.31
- WI1165: improved - automatically add keys and languages when importing.

1.0.0.30
- WI1165: Excel export and import.

1.0.0.29
- WI1164: Allow file creation in the stand alone application

1.0.0.28
- WI1157: Make all comments accessible. Comments are hidden by default, and can be made visible by using the "show columns" toolbar button.
- Fix WI1155.

1.0.0.27
- Fix WI1149 & WI1153: Resources not visible
- Add Feature WI1136: Support .resw files for windows store apps

1.0.0.26
- WI1141: Improve message when an xml file fails to load. Ignore projects that fail to load.

1.0.0.25
- WI1133: Improve error message

1.0.0.24
- WI1121: Improve error messages

1.0.0.23
- WI1098: VS Crash after changing filter

1.0.0.22
- WI1091: Context menu for copy resource key.
- WI1090: Enable multi-line paste.

1.0.0.21
- WI1060: ResX Manager crashes after update.

1.0.0.20
- WI958: Reference something in DGX, so we don't need to load the assembly dynamically.
- WI1055: Add the possibility to mark items that do not need translation as invariant.
- WI1056: Support VS 2013

1.0.0.19
- Fix WI1046: VS Crash when sorting the table
- New WI1055: Add possibility to mark items that do not need translation as invariant

1.0.0.18
- Enable spell checker (if .Net language pack for the target language is installed)
- German localization
- Fix WI1002: Wrong Spanish flag
- Fix WI993: Details of load errors are shown in the output pane

1.0.0.17
- Catch errors and show in output window.
- Make the language of the comment column selectable.
- Add a "Like" button.

1.0.0.16
- New filter "Show only rows with missing entries" makes it easier to find untranslated entries
- Support linked items shared by multiple projects (fixes WI980, 982)
- Fix WI986: Filtering on column doesn't work most of the time

1.0.0.15
- Add new languages (works in extension only)
- Edit multiple projects simultaneously
- Flat or grouped view of multiple projects
- Choose what columns to show or hide
- Extended project/file filter

1.0.0.14
- FIX WI909: Added filter for resource files
- Sort files alphabetically

1.0.0.13
- FIX WI889: Support legacy language tags (e.g. zh-CHS and zh-CHT)

1.0.0.12
- FIX WI821: Re-create designer files after modifying neutral language.

1.0.0.11
- FIX WI880: column filters restored.

1.0.0.10
- FIX WI871: web forms support (files with nested extension like *.aspx.resx)
- FIX WI870: Sort by key/Keep sorting - Default sort order now is by key. 
- FIX WI849/833: Improved auto-refresh when activating the view, + explicit refresh button. 
- FIX WI821: New strings can be added in the resource manager.

1.0.0.9
- Usability improvements
-* DEL key deletes rows
-* Preserve column width and order during session
-* Preserve selection during session
-* Resizable navigation panel
-* Editable comments

1.0.0.8
- FIX WI801: Extensions does not find ResX in Solution Folders
- Improve keyboard navigation: Ctrl-Return starts editing without overwriting the content of a cell.
- Improve multi-line editing: Return navigates instead of adding a new line. Ctrl-Return inserts a new line.

1.0.0.7
- Import/Export tables also with multi-line text in cells.

1.0.0.6
- Show the comment of the neutral language file
- Import/Export tables via clipboard

1.0.0.5
- VisualStudio Extension: Support VS 2012

1.0.0.4
- Delete: Allow to delete multiple rows and correctly update display.
- Improve context menus.
- Show tooltip how to change the neutral language icon.

1.0.0.3
- Show an icon for the neutral language, too.

1.0.0.2
- Show display name and icon of the languages.
- Automatic checkout of files upon editing, warn on file access errors.
- Highlight empty entries.