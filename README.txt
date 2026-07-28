FTLS CHAIRMAN FACULTY POPUP — UNIT SUMMARY ONLY
================================================

PURPOSE
-------
This package removes the entire per-faculty schedule table from the chairman
popup. The popup will keep only:

- Faculty name
- Email
- Full-Time / Part-Time classification
- Total assigned units
- Underload warning badge
- Normal-load badge
- Overload warning badge

The popup still appears automatically after chairman login, and the existing
"View Faculty Load Profiles" button still opens it.

BACKEND OPTIMIZATION
--------------------
The original popup view model loaded each schedule's Subject and Room details.
Because those rows are no longer displayed, this update changes the popup query
to calculate only aggregate assigned units per faculty and academic year.

The main "Assigned Teaching Loads" interface is NOT removed or changed.
Creating, editing, deleting, room selection, subject selection, and all normal
scheduler features remain intact.

FILES MODIFIED
--------------
FTLSV2\Pages\Chairman\ChairmanPage.cshtml
FTLSV2\Pages\Chairman\ChairmanPage.cshtml.cs

RECOMMENDED INSTALLATION
------------------------
1. Extract this ZIP.
2. Copy all extracted files into the FTLS repository root, where FTLSV2.sln is.
3. Open PowerShell in the repository root.
4. Run:

   powershell -NoProfile -ExecutionPolicy Bypass -File .\Apply-FTLS-ChairmanPopup-UnitsOnly.ps1

You may alternatively double-click Apply-Patch.cmd after copying it to the
repository root.

The script supports both:
- The original repository version that still shows Subject details.
- The earlier modified version where only the Subject column was removed.

It creates timestamped .bak copies before writing the two modified files.

VERIFY
------
Run:

dotnet build .\FTLSV2.sln

Then:

dotnet watch run --project .\FTLSV2\FTLSV2.csproj --launch-profile https

Log in as Chairman. The popup should contain compact faculty cards with unit
badges only—no schedule table.

Review the exact source changes:

git diff -- FTLSV2/Pages/Chairman/ChairmanPage.cshtml
git diff -- FTLSV2/Pages/Chairman/ChairmanPage.cshtml.cs

ROLLBACK
--------
Option 1 — Git:

git restore FTLSV2/Pages/Chairman/ChairmanPage.cshtml
git restore FTLSV2/Pages/Chairman/ChairmanPage.cshtml.cs

Option 2 — Backups:

The script prints the paths of the timestamped .bak files. Copy each backup
over its corresponding source file.

PACKAGE CONTENTS
----------------
Apply-FTLS-ChairmanPopup-UnitsOnly.ps1
    One-step, guarded source-code updater.

Apply-Patch.cmd
    Double-clickable Windows wrapper.

ChairmanPage-Popup-Replacement.cshtml.txt
    Exact replacement markup for the popup.

ChairmanPageModel-FacultyLoadProfileView.cs.txt
    Exact compact popup view-model record.

ChairmanPageModel-UnitSummaryQuery.cs.txt
    Exact aggregate-unit query and profile construction.

ChairmanPageModel-Warnings.cs.txt
    Exact warnings logic based on the aggregate profiles.
