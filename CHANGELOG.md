# Changelog

## [0.0.4] - STEP 2 Model Marker Inspection

- Implemented active CATPart marker inspection in ModelInspector.
- Added checks for GS_DRAWING_INFO, MAIN_VIEW_PLANE, and TOP_DIRECTION.
- Added minimal UI trigger and status display for model inspection.

## [0.0.3] - STEP 1 ActiveDocument Read

- Implemented ActiveDocument retrieval in CatiaConnectionService.
- Added ActiveDocument.Name and ActiveDocument.Type reading.
- Added CATIA not running, missing ActiveDocument, and COM exception handling.

## [0.0.2] - STEP 0 CATIA Connection Check

- Implemented running CATIA V5 COM connection check in CatiaConnectionService.
- Connected MainForm CATIA connection check button to the service result.
- Added success and failure logging for STEP 0.

## [0.0.1] - Initial Architecture

- Created initial project structure.
- Added placeholder modules.
- Added configuration files.
- Added documentation files.
