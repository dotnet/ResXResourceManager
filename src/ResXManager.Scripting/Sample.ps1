# If script can't be started: Set-ExecutionPolicy -ExecutionPolicy Unrestricted -Scope Process

# Define file locations
$snapshotFileName = 'D:\Temp\1.snapshot'
$excelFileName = 'd:\Temp\Test.xlsx'
$solutionFolder = '..\..\..\..\'

# Create the scripting host of ResXManager:
Add-Type -Path 'ResXManager.Scripting.dll'
$myhost = New-Object -TypeName 'ResXManager.Scripting.Host' 

# Load a project by specifying the solution folder (like in the standalone version)
$myhost.Load($solutionFolder)

# Get all cultures the solution has resources for
$allCultures = $myhost.ResourceManager.Cultures
$allCultures

# To define a culture use any valid culture name; use $null or "" for the projects neutral culture 
# Optionally prefix the culture name with '.' to simplify building file names using the same string, e.g. $fileName = $baseName + $cultureName + ".resx"
$neutralCulture = ""
$outputCulture = ".de"

# Get the list of all resource entries
$allEntries = $myhost.ResourceManager.TableEntries

# Print a list of all entries
"All entires: " + $allEntries.Count
$allEntries | Select-Object Key, 
	@{ Name="Text"; Expression={$_.Values.GetValue($outputCulture)}}, 
	@{ Name="Entity"; Expression={$_.Container}} | 
	Format-Table -GroupBy Entity

# Load a snapshot and detect all changes in the neutral culture
"Changes compared to snapshot:"

$snapshot = Get-Content $snapshotFileName -ErrorAction Ignore
$myhost.LoadSnapshot($snapshot)

$changes = $allEntries | 
	Where-Object { $_.Values.GetValue($neutralCulture) -ne $_.SnapshotValues.GetValue($neutralCulture)  }

"Number of changes: " + $changes.Count
$changes | Select-Object Key, 
	@{ Name="Text"; Expression={$_.Values.GetValue($neutralCulture)}}, 
	@{ Name="Snapshot"; Expression={$_.SnapshotValues.GetValue($neutralCulture)}}, 
	@{ Name="Entity"; Expression={$_.Container}} |
	Format-Table

# if there were changes in the neutral culture compared to the last snapshot, export the changed entries in neutral and output culuture 
if ($changes -ne $null)
{
	# Export to excel; parameters are <file name>, [<items>, [<languag(es) of values>, [<languag(es) of comments>, [<export mode>]]]]
	# All parameters except file name are optional:
	#  items:				  default = $null (export all items)
	#  languages of values:   default = $null (export all languages)
	#  languages of comments: default = $null (export no comments)
	#  export mode:           default = 0 (single sheet), 1 = multiple sheets, 2 = text only tab delimited
	"Export changes since last snapshot to Excel"
	$myhost.ExportExcel($excelFileName, $changes, @($neutralCulture, $outputCulture))
}

Read-Host "Now you can modify the excel file: " $excelFileName

"Importing modified excel file"
$myhost.ImportExcel($excelFileName)
# Save has an optional parameter of type System.StringComparison; if specified, the content of the resx files is sorted by the key, using the specified comparison.
$myhost.Save()

$input = Read-Host "Create a new snapshot with the latest data? (Y/N)"
if ("Y", "y" -contains $input)
{
	"Updating snaphot " + $snapshotFileName
	$snapshot = $myhost.CreateSnapshot()
	Set-Content $snapshotFileName $snapshot
}

Read-Host "Done" | Out-Null
$myhost.Dispose()
