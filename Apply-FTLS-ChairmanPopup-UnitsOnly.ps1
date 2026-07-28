[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$ProjectRoot = (Get-Location).Path
)

$ErrorActionPreference = 'Stop'

$viewRelativePath = 'FTLSV2\Pages\Chairman\ChairmanPage.cshtml'
$modelRelativePath = 'FTLSV2\Pages\Chairman\ChairmanPage.cshtml.cs'

$viewPath = Join-Path $ProjectRoot $viewRelativePath
$modelPath = Join-Path $ProjectRoot $modelRelativePath

foreach ($path in @($viewPath, $modelPath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required file not found: '$path'. Run this script from the FTLS repository root."
    }
}

$utf8NoBom = [System.Text.UTF8Encoding]::new($false)

function Normalize-NewLines {
    param(
        [Parameter(Mandatory = $true)][string]$Text,
        [Parameter(Mandatory = $true)][string]$NewLine
    )

    return [regex]::Replace($Text, "`r?`n", $NewLine)
}

function Replace-BetweenMarkers {
    param(
        [Parameter(Mandatory = $true)][string]$Content,
        [Parameter(Mandatory = $true)][string]$StartMarker,
        [Parameter(Mandatory = $true)][string]$EndMarker,
        [Parameter(Mandatory = $true)][string]$Replacement,
        [Parameter(Mandatory = $true)][string]$Description
    )

    $startIndex = $Content.IndexOf($StartMarker, [System.StringComparison]::Ordinal)
    if ($startIndex -lt 0) {
        throw "Could not find the start marker for $Description."
    }

    $endIndex = $Content.IndexOf($EndMarker, $startIndex, [System.StringComparison]::Ordinal)
    if ($endIndex -lt 0) {
        throw "Could not find the end marker for $Description."
    }

    return $Content.Substring(0, $startIndex) + $Replacement + $Content.Substring($endIndex)
}

$viewContent = [System.IO.File]::ReadAllText($viewPath)
$modelContent = [System.IO.File]::ReadAllText($modelPath)

$viewNewLine = if ($viewContent.Contains("`r`n")) { "`r`n" } else { "`n" }
$modelNewLine = if ($modelContent.Contains("`r`n")) { "`r`n" } else { "`n" }

$popupReplacement = @'
<!-- ============================================================== -->
<!-- FACULTY LOAD POPUP: UNIT SUMMARY ONLY                           -->
<!-- ============================================================== -->
<div id="facultyLoadModal" class="modal-overlay">
    <div class="modal-card modal-card-large">
        <div class="modal-header">
            <h3>Faculty Load Summary & Profiles (A.Y. @Model.SelectedAcademicYear)</h3>
            <button type="button" class="modal-close" onclick="closeModal('facultyLoadModal')">×</button>
        </div>
        <div class="modal-body">
            @if (Model.FacultyProfilesLoadList != null && Model.FacultyProfilesLoadList.Any())
            {
                @foreach (var faculty in Model.FacultyProfilesLoadList)
                {
                    <div class="faculty-profile-card">
                        <div class="faculty-profile-header">
                            <div>
                                <h4 class="faculty-name">Engr. @faculty.FirstName @faculty.LastName</h4>
                                <span style="font-size: 12px; color: #6b7280;">
                                    Email: @faculty.Email | Type: <strong>@faculty.EmploymentType</strong>
                                </span>
                            </div>
                            <div>
                                @if (faculty.IsUnderloaded)
                                {
                                    <span class="load-badge load-underload">
                                        Units: @faculty.TotalAssignedUnits (Below 18 Units)
                                    </span>
                                }
                                else if (faculty.IsOverloaded)
                                {
                                    <span class="load-badge load-overload">
                                        Units: @faculty.TotalAssignedUnits / @faculty.MaxUnits (Overloaded)
                                    </span>
                                }
                                else
                                {
                                    <span class="load-badge load-normal">
                                        Units: @faculty.TotalAssignedUnits / @faculty.MaxUnits
                                    </span>
                                }
                            </div>
                        </div>
                    </div>
                }
            }
            else
            {
                <p>No faculty loaded.</p>
            }
        </div>
        <div class="modal-actions">
            <button type="button" class="btn-close-red" onclick="closeModal('facultyLoadModal')">Close</button>
        </div>
    </div>
</div>

'@
$popupReplacement = Normalize-NewLines -Text $popupReplacement -NewLine $viewNewLine

$popupStart = '<!-- ============================================================== -->' + $viewNewLine +
              '<!-- FACULTY LOAD POPUP MATCHING ACADEMIC YEAR DIRECTORY            -->'
$popupEnd = '<div id="editScheduleModal" class="modal-overlay">'

if ($viewContent.Contains('<!-- FACULTY LOAD POPUP: UNIT SUMMARY ONLY')) {
    $popupStart = '<!-- ============================================================== -->' + $viewNewLine +
                  '<!-- FACULTY LOAD POPUP: UNIT SUMMARY ONLY                           -->'
}

$updatedView = Replace-BetweenMarkers `
    -Content $viewContent `
    -StartMarker $popupStart `
    -EndMarker $popupEnd `
    -Replacement $popupReplacement `
    -Description 'chairman faculty-load popup'

# The profile header no longer needs spacing reserved for the removed table.
$updatedView = [regex]::Replace(
    $updatedView,
    '(?ms)(^[ \t]{4}\.faculty-profile-header[ \t]*\{.*?^[ \t]{8})margin-bottom:[ \t]*10px;',
    '${1}margin-bottom: 0;',
    1
)

# Remove the now-unused popup schedule-table CSS, if it is still present.
$updatedView = [regex]::Replace(
    $updatedView,
    '(?ms)^[ \t]{4}\.schedule-table[ \t]*\{.*?(?=^[ \t]{4}\.edit-form-grid[ \t]*\{)',
    '',
    1
)

$profileRecordReplacement = @'
        public record FacultyLoadProfileView
        {
            public int FacultyId { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
            public string EmploymentType { get; set; }
            public int MaxUnits { get; set; }
            public int TotalAssignedUnits { get; set; }
            public bool IsUnderloaded { get; set; }
            public bool IsOverloaded { get; set; }
        }

'@
$profileRecordReplacement = Normalize-NewLines -Text $profileRecordReplacement -NewLine $modelNewLine

$recordStart = '        public record FacultyLoadProfileView'
$recordEnd = '        // --- DROPDOWN LISTS ---'
$updatedModel = Replace-BetweenMarkers `
    -Content $modelContent `
    -StartMarker $recordStart `
    -EndMarker $recordEnd `
    -Replacement $profileRecordReplacement `
    -Description 'FacultyLoadProfileView record'

$unitSummaryReplacement = @'
            var assignedUnitsByFaculty = _context.Schedules
                .AsNoTracking()
                .Where(s => s.AcademicYear == currentAY)
                .GroupBy(s => s.FacultyId)
                .Select(group => new
                {
                    FacultyId = group.Key,
                    TotalAssignedUnits = group.Sum(s => s.AssignedUnits)
                })
                .ToDictionary(item => item.FacultyId, item => item.TotalAssignedUnits);

            FacultyProfilesLoadList = ActiveFaculty.Select(f =>
            {
                assignedUnitsByFaculty.TryGetValue(f.FacultyId, out int totalUnits);

                int facultyMaxUnits = f.MaxUnits;
                bool isFullTime = facultyMaxUnits >= 18;
                string employmentType = isFullTime ? "Full-Time" : "Part-Time";

                return new FacultyLoadProfileView
                {
                    FacultyId = f.FacultyId,
                    FirstName = f.FirstName,
                    LastName = f.LastName,
                    Email = f.Email,
                    EmploymentType = employmentType,
                    MaxUnits = facultyMaxUnits,
                    TotalAssignedUnits = totalUnits,
                    IsUnderloaded = isFullTime && totalUnits < 18,
                    IsOverloaded = totalUnits > facultyMaxUnits
                };
            }).ToList();

'@
$unitSummaryReplacement = Normalize-NewLines -Text $unitSummaryReplacement -NewLine $modelNewLine

$oldSummaryStart = '            var currentYearSchedules = _context.Schedules'
$newSummaryStart = '            var assignedUnitsByFaculty = _context.Schedules'
$alertsMarker = '            // --- CALCULATE ALL FACULTY ALERTS ---'

if ($updatedModel.Contains($oldSummaryStart)) {
    $updatedModel = Replace-BetweenMarkers `
        -Content $updatedModel `
        -StartMarker $oldSummaryStart `
        -EndMarker $alertsMarker `
        -Replacement $unitSummaryReplacement `
        -Description 'faculty unit-summary query'
}
elseif (-not $updatedModel.Contains($newSummaryStart)) {
    throw 'Could not locate either the old or the updated faculty unit-summary query.'
}

$alertsReplacement = @'
            // --- CALCULATE ALL FACULTY ALERTS ---
            FacultyAlerts = new List<FacultyAlertView>();

            foreach (var facultyProfile in FacultyProfilesLoadList)
            {
                string facultyName = $"Engr. {facultyProfile.FirstName} {facultyProfile.LastName}";

                if (facultyProfile.IsUnderloaded)
                {
                    FacultyAlerts.Add(new FacultyAlertView(
                        facultyProfile.FacultyId,
                        facultyName,
                        currentAY,
                        facultyProfile.TotalAssignedUnits,
                        18,
                        $"Full-Time faculty is underloaded with {facultyProfile.TotalAssignedUnits} units (Minimum required: 18 units).",
                        "Underload"
                    ));
                }
                else if (facultyProfile.IsOverloaded)
                {
                    FacultyAlerts.Add(new FacultyAlertView(
                        facultyProfile.FacultyId,
                        facultyName,
                        currentAY,
                        facultyProfile.TotalAssignedUnits,
                        facultyProfile.MaxUnits,
                        $"{facultyProfile.EmploymentType} faculty is overloaded with {facultyProfile.TotalAssignedUnits} units (Maximum allowed: {facultyProfile.MaxUnits} units).",
                        "Overload"
                    ));
                }
            }

'@
$alertsReplacement = Normalize-NewLines -Text $alertsReplacement -NewLine $modelNewLine

$academicYearsMarker = '            AvailableAcademicYears = new List<string>();'
$updatedModel = Replace-BetweenMarkers `
    -Content $updatedModel `
    -StartMarker $alertsMarker `
    -EndMarker $academicYearsMarker `
    -Replacement $alertsReplacement `
    -Description 'faculty load warnings'

# Final safety checks before writing either file.
if ($updatedView.Contains('faculty.Schedules') -or $updatedView.Contains('schedule-table')) {
    throw 'Verification failed: schedule-detail UI still exists in the faculty popup.'
}

if ($updatedModel.Contains('public List<AssignedLoad> Schedules')) {
    throw 'Verification failed: the popup view model still exposes schedule-detail records.'
}

if (-not $updatedModel.Contains('TotalAssignedUnits = totalUnits') -or
    -not $updatedModel.Contains('FacultyAlerts.Add')) {
    throw 'Verification failed: unit totals or warnings are missing from the updated model.'
}

$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$viewBackup = "$viewPath.before-units-only-$timestamp.bak"
$modelBackup = "$modelPath.before-units-only-$timestamp.bak"

[System.IO.File]::Copy($viewPath, $viewBackup, $false)
[System.IO.File]::Copy($modelPath, $modelBackup, $false)

try {
    [System.IO.File]::WriteAllText($viewPath, $updatedView, $utf8NoBom)
    [System.IO.File]::WriteAllText($modelPath, $updatedModel, $utf8NoBom)
}
catch {
    [System.IO.File]::Copy($viewBackup, $viewPath, $true)
    [System.IO.File]::Copy($modelBackup, $modelPath, $true)
    throw
}

Write-Host ''
Write-Host 'FTLS chairman faculty popup updated successfully.' -ForegroundColor Green
Write-Host ''
Write-Host 'Removed from the popup:'
Write-Host '  - Subject/course details'
Write-Host '  - Time and day'
Write-Host '  - Room'
Write-Host '  - Semester and academic-year rows'
Write-Host '  - Per-schedule unit rows'
Write-Host ''
Write-Host 'Preserved:'
Write-Host '  - Faculty name, email, and employment type'
Write-Host '  - Total assigned units'
Write-Host '  - Underload, normal-load, and overload badges'
Write-Host '  - Faculty warning alerts'
Write-Host '  - Automatic popup after chairman login'
Write-Host ''
Write-Host 'The backend now calculates aggregate unit totals without loading Subject and Room details for this popup.'
Write-Host ''
Write-Host "View backup:  $viewBackup"
Write-Host "Model backup: $modelBackup"
