[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$ProjectRoot = (Get-Location).Path
)

$ErrorActionPreference = 'Stop'

$relativePath = 'FTLSV2\Pages\Chairman\ChairmanPage.cshtml'
$targetPath = Join-Path $ProjectRoot $relativePath

if (-not (Test-Path -LiteralPath $targetPath -PathType Leaf)) {
    throw "Could not find '$relativePath' under '$ProjectRoot'. Run this script from the FTLS repository root, or pass -ProjectRoot with the correct path."
}

$utf8NoBom = [System.Text.UTF8Encoding]::new($false)
$content = [System.IO.File]::ReadAllText($targetPath)

$subjectHeaderPattern = '(?m)^[\t ]*<th>Subject</th>\r?\n'
$subjectCellPattern = '(?m)^[\t ]*<td><strong>@sched\.CourseCode</strong> - @sched\.SubjectTitle</td>\r?\n'

$headerMatches = [regex]::Matches($content, $subjectHeaderPattern)
$cellMatches = [regex]::Matches($content, $subjectCellPattern)

if ($headerMatches.Count -eq 0 -and $cellMatches.Count -eq 0) {
    Write-Host 'No change was needed. The Subject column is already absent from the chairman faculty-load popup.' -ForegroundColor Yellow
    exit 0
}

if ($headerMatches.Count -ne 1 -or $cellMatches.Count -ne 1) {
    throw "Safety check failed. Expected exactly one popup Subject header and one popup Subject cell, but found $($headerMatches.Count) header(s) and $($cellMatches.Count) cell(s). No file was changed."
}

$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$backupPath = "$targetPath.before-no-subjects-$timestamp.bak"
[System.IO.File]::Copy($targetPath, $backupPath, $false)

$updated = [regex]::Replace($content, $subjectHeaderPattern, '', 1)
$updated = [regex]::Replace($updated, $subjectCellPattern, '', 1)

[System.IO.File]::WriteAllText($targetPath, $updated, $utf8NoBom)

$verification = [System.IO.File]::ReadAllText($targetPath)
if ([regex]::IsMatch($verification, $subjectHeaderPattern) -or [regex]::IsMatch($verification, $subjectCellPattern)) {
    [System.IO.File]::Copy($backupPath, $targetPath, $true)
    throw "Verification failed. The original file was restored from '$backupPath'."
}

Write-Host ''
Write-Host 'FTLS chairman popup updated successfully.' -ForegroundColor Green
Write-Host "Modified: $targetPath"
Write-Host "Backup:   $backupPath"
Write-Host ''
Write-Host 'The popup still shows faculty details, employment type, load status, time/day, room, semester/academic year, and units. Only the Subject code/title column was removed.'
