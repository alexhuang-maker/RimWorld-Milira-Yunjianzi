param([string]$ExtensionRoot = '')
$ErrorActionPreference = 'Stop'
$modRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
function Assert-Check($condition, [string]$message) {
    if (!$condition) { throw $message }
}
function Read-Defs([string]$root) {
    [xml]$combined = '<Defs />'
    foreach ($file in Get-ChildItem -LiteralPath (Join-Path $root '1.6/Defs') -Recurse -Filter *.xml) {
        [xml]$xml = Get-Content -LiteralPath $file.FullName -Raw
        foreach ($node in $xml.DocumentElement.ChildNodes) {
            if ($node.NodeType -eq 'Element') { $null = $combined.DocumentElement.AppendChild($combined.ImportNode($node, $true)) }
        }
    }
    return ,$combined
}
function Get-AssemblyInfo([string]$path) {
    $stream = [IO.File]::OpenRead($path)
    $pe = [System.Reflection.PortableExecutable.PEReader]::new($stream)
    try {
        $reader = [System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($pe)
        $types = @(foreach ($handle in $reader.TypeDefinitions) {
            $type = $reader.GetTypeDefinition($handle)
            $ns = $reader.GetString($type.Namespace)
            if ($ns -eq 'Yunjianzi') { $ns + '.' + $reader.GetString($type.Name) }
        })
        $references = @(foreach ($handle in $reader.AssemblyReferences) {
            $reader.GetString($reader.GetAssemblyReference($handle).Name)
        })
        return @{Types=$types; References=$references}
    } finally { $pe.Dispose(); $stream.Dispose() }
}
Add-Type -AssemblyName System.Reflection.Metadata
$defs = Read-Defs $modRoot
$ids = @($defs.SelectNodes('/Defs/*/defName') | ForEach-Object { $_.InnerText })
$required = @('YJZ_TreasureCasket', 'YJZ_Building_TrueJadeForge', 'YJZ_Building_SpiritConvergencePool',
    'YJZ_Building_CelestialDaoGate', 'YJZ_Building_SwordRainTurret', 'YJZ_Building_YunjianziBanner')
foreach ($id in $required) {
    Assert-Check ($defs.SelectNodes("/Defs/ThingDef[defName='$id']").Count -eq 1) "Missing or duplicate building: $id"
}
$definitionFiles = @('Yunjianzi_TreasureCasket.xml', 'Yunjianzi_TrueJadeSteel.xml',
    'Yunjianzi_AdvancedCultivationFacilities.xml', 'Yunjianzi_SwordRainTurret.xml', 'Yunjianzi_Banner.xml')
$assembly = Get-AssemblyInfo (Join-Path $modRoot '1.6/Assemblies/Yunjianzi.FixedTemplate.dll')
Assert-Check ($assembly.References -notcontains 'Yunjianzi.Buildings') 'Main DLL depends on the optional extension'
$textures = @()
foreach ($filename in $definitionFiles) {
    [xml]$doc = Get-Content -LiteralPath (Join-Path $modRoot "1.6/Defs/ThingDefs/$filename") -Raw
    foreach ($node in $doc.SelectNodes('//*')) {
        if ($node.ChildNodes.Count -eq 1 -and $node.FirstChild.NodeType -eq 'Text') {
            $value = $node.InnerText
            if ($value -match '^YJZ_\w+$') { Assert-Check ($ids -contains $value) "Unresolved main-only Def: $value" }
            if ($value -match '^Yunjianzi\.\w+$') { Assert-Check ($assembly.Types -contains $value) "Missing main DLL type: $value" }
        }
        if ($node.LocalName -match '^YJZ_') { Assert-Check ($ids -contains $node.LocalName) "Unresolved cost or product: $($node.LocalName)" }
    }
    $textures += @($doc.SelectNodes('//texPath | //uiIconPath') | ForEach-Object { $_.InnerText })
}
foreach ($filename in @('YunjianziTreasureCasket.cs', 'YunjianziSwordRainTurret.cs', 'YunjianziFurnitureDraw.cs')) {
    $source = Get-Content -LiteralPath (Join-Path $modRoot "Source/$filename") -Raw
    $textures += @([regex]::Matches($source, '"(Yunjianzi/[^"\r\n]+)"') | ForEach-Object { $_.Groups[1].Value })
}
foreach ($texture in $textures | Sort-Object -Unique) {
    $path = Join-Path $modRoot "Content/Textures/$texture"
    $found = (Test-Path -LiteralPath "$path.png") -or (Test-Path -LiteralPath "${path}_north.png")
    Assert-Check $found "Missing main texture: $texture"
}
# Apply the migrated Add/Replace operations to actual, combined main Defs.
# This catches patches that still depend on a definition in the extension.
$patchCount = 0
foreach ($filename in @('Yunjianzi_MeditationFacilities.xml', 'Yunjianzi_ShenjiRecipes_TrueJadeForge.xml', 'Yunjianzi_ScenarioStartingTrueJade.xml')) {
    [xml]$patch = Get-Content -LiteralPath (Join-Path $modRoot "1.6/Patches/$filename") -Raw
    foreach ($operation in $patch.SelectNodes('//*[@Class="PatchOperationAdd" or @Class="PatchOperationReplace"]')) {
        $targets = @($defs.SelectNodes($operation.xpath))
        Assert-Check ($targets.Count -gt 0) "Patch target missing: $($operation.xpath)"
        foreach ($target in $targets) {
            foreach ($value in $operation.value.ChildNodes) {
                if ($value.NodeType -ne 'Element') { continue }
                $copy = $defs.ImportNode($value, $true)
                if ($operation.Class -eq 'PatchOperationAdd') { $null = $target.AppendChild($copy) }
                else { $null = $target.ParentNode.ReplaceChild($copy, $target) }
            }
        }
        $patchCount++
    }
}
$links = $defs.SelectNodes('/Defs/ThingDef[@Name="YJZ_MeditationBuildingBase"]/comps/li[@Class="CompProperties_AffectedByFacilities"]/linkableFacilities/li')
Assert-Check ($links.Count -eq 3) 'Meditation facility links are incomplete or duplicated'
$forgeRecipes = $defs.SelectNodes('/Defs/RecipeDef[recipeUsers/li="YJZ_Building_TrueJadeForge"]')
Assert-Check ($forgeRecipes.Count -eq 14) 'Forge recipes are incomplete'
Assert-Check ($defs.SelectNodes('/Defs/WorkGiverDef[fixedBillGiverDefs/li="YJZ_Building_TrueJadeForge"]').Count -eq 1) 'Forge lacks a bill work giver'
if ($ExtensionRoot) {
    $extensionDefs = Read-Defs $ExtensionRoot
    foreach ($node in $extensionDefs.SelectNodes('/Defs/*[defName]')) {
        $matches = $defs.SelectNodes("/Defs/$($node.Name)[defName='$($node.defName)']")
        Assert-Check ($matches.Count -eq 0) "Duplicate Def in extension: $($node.defName)"
    }
    $extensionAssembly = Get-AssemblyInfo (Join-Path $ExtensionRoot '1.6/Assemblies/Yunjianzi.Buildings.dll')
    foreach ($type in $extensionAssembly.Types) {
        Assert-Check ($assembly.Types -notcontains $type) "Duplicate compiled type: $type"
    }
    foreach ($id in $required) {
        Assert-Check ($extensionDefs.SelectNodes("/Defs/ThingDef[defName='$id']").Count -eq 0) "Extension still defines: $id"
    }
    'PASS: main + extension have no duplicate Defs or Yunjianzi types.'
}
"PASS: six main buildings, standalone DLL types/textures, $patchCount patch operations, 14 forge recipes and three facility links."
