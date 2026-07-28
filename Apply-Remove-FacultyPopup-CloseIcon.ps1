[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$ProjectRoot = (Get-Location).Path
)

$ErrorActionPreference = 'Stop'

$relativePath = 'FTLSV2\Pages\Chairman\ChairmanPage.cshtml'
$filePath = Join-Path $ProjectRoot $relativePath

if (-not (Test-Path -LiteralPath $filePath -PathType Leaf)) {
    throw "Required file not found: '$filePath'. Run this script from the FTLS repository root."
}

$content = [System.IO.File]::ReadAllText($filePath)
$newLine = if ($content.Contains("`r`n")) { "`r`n" } else { "`n" }

$modalStartMarker = '<div id="facultyLoadModal" class="modal-overlay">'
$modalEndMarker = '<div id="editScheduleModal" class="modal-overlay">'

$startIndex = $content.IndexOf($modalStartMarker, [System.StringComparison]::Ordinal)
if ($startIndex -lt 0) {
    throw 'Could not find the faculty-load popup.'
}

$endIndex = $content.IndexOf($modalEndMarker, $startIndex, [System.StringComparison]::Ordinal)
if ($endIndex -lt 0) {
    throw 'Could not find the end boundary of the faculty-load popup.'
}

$before = $content.Substring(0, $startIndex)
$modal = $content.Substring($startIndex, $endIndex - $startIndex)
$after = $content.Substring($endIndex)

# Remove only the top-right close button in the faculty-load popup.
# This matches the correct × character as well as common mojibake variants.
$buttonPattern = '(?m)^[ \t]*<button\s+type="button"\s+class="modal-close"\s+onclick="closeModal\(''facultyLoadModal''\)">(?:×|Ã—|Â×|&times;|&#215;)</button>[ \t]*\r?\n?'

$updatedModal = [regex]::Replace($modal, $buttonPattern, '', 1)

if ($updatedModal -eq $modal) {
    throw 'The faculty popup close-icon button was not found. It may already have been removed.'
}

$updatedContent = $before + $updatedModal + $after

# Safety checks:
# 1. The bottom Close button must remain.
# 2. The Edit Schedule modal top-right close button must remain.
if (-not $updatedModal.Contains('class="btn-close-red" onclick="closeModal(''facultyLoadModal'')"')) {
    throw 'Safety check failed: the bottom faculty popup Close button is missing.'
}

$editModalSection = $updatedContent.Substring(
    $updatedContent.IndexOf($modalEndMarker, [System.StringComparison]::Ordinal)
)

if (-not $editModalSection.Contains('class="modal-close" onclick="closeModal(''editScheduleModal'')"')) {
    throw 'Safety check failed: the Edit Schedule modal close icon is missing.'
}

$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$backupPath = "$filePath.before-remove-broken-close-$timestamp.bak"
[System.IO.File]::Copy($filePath, $backupPath, $false)

$utf8NoBom = [System.Text.UTF8Encoding]::new($false)
[System.IO.File]::WriteAllText($filePath, $updatedContent, $utf8NoBom)

Write-Host ''
Write-Host 'Removed the broken/redundant top-right close icon from the faculty-load popup.' -ForegroundColor Green
Write-Host ''
Write-Host 'Preserved:'
Write-Host '  - Bottom red Close button'
Write-Host '  - Escape-key and backdrop closing'
Write-Host '  - Edit Schedule modal close icon'
Write-Host '  - All faculty unit badges and warnings'
Write-Host ''
Write-Host "Backup created: $backupPath"
