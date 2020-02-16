param([string]$solutionDir)

# Create the scripting host of ResXManager:
Add-Type -Path 'ResXManager.Scripting.dll'
$myhost = New-Object -TypeName 'ResXManager.Scripting.Host' 

# Load a project by specifying the solution folder (like in the standalone version)
$myhost.Load($solutionDir)

$allCultures = $myhost.ResourceManager.Cultures
$entities = $myhost.ResourceManager.ResourceEntities

foreach($culture in $allCultures) {
    $nodes = @{}

    foreach ($entity in $entities) {
        $entityNode = @{}
        $nodeName = $entity.UniqueName
        $nodes[$nodeName] = $entityNode
        foreach($entry in $entity.Entries) {
            $entityNode[$entry.Key] = $entry.Values[$culture]
        }
    }

    $cultureName = $culture.ToString('')
    $json = ConvertTo-Json $nodes

    if (!$cultureName) {
        Set-Content -Path "d:\temp\test.ts" -Value "class Resources $json"
    }

    Set-Content -Path "d:\temp\test$cultureName.json" -Value "$json"
}
