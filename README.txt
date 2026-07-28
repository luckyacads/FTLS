FTLS — REMOVE BROKEN FACULTY POPUP CLOSE ICON
================================================

WHAT THE RED "Ã—" WAS
---------------------
It was intended to be a multiplication-sign character (×) used as a top-right
close button. It displayed as Ã— because the character was decoded using the
wrong text encoding.

It still called closeModal('facultyLoadModal'), but it was redundant because
the popup already has a red Close button at the bottom.

WHAT THIS UPDATE DOES
---------------------
Removes only the broken top-right close-icon button from the Faculty Load
Summary popup.

PRESERVED
---------
- Bottom red Close button
- Faculty unit totals and warnings
- Automatic popup after Chairman login
- Escape-key closing
- Backdrop-click closing
- Edit Schedule popup's top-right close icon
- Shared .modal-close CSS, because the Edit Schedule popup still uses it

FILE MODIFIED
-------------
FTLSV2\Pages\Chairman\ChairmanPage.cshtml

INSTALLATION
------------
1. Extract this ZIP into the FTLS repository root, where FTLSV2.sln is located.
2. Run:

   powershell -NoProfile -ExecutionPolicy Bypass -File .\Apply-Remove-FacultyPopup-CloseIcon.ps1

3. Review:

   git diff -- FTLSV2/Pages/Chairman/ChairmanPage.cshtml

4. If dotnet watch is running, save/restart it with Ctrl+R. Otherwise run:

   dotnet watch run --project .\FTLSV2\FTLSV2.csproj --launch-profile https

ROLLBACK
--------
The script creates a timestamped .bak copy before editing the source file.
You may also restore the tracked file with Git:

git restore FTLSV2/Pages/Chairman/ChairmanPage.cshtml
