# Changelog

## [0.0.6] - STEP 3 Template Open and SaveAs

- Changed drawing creation flow from empty CATDrawing creation to template open and SaveAs flow.
- Added drawing size selection for A4/A3/A2/A1.
- Added size-based CATDrawing template map to appsettings.
- Improved CATIA COM and TargetInvocationException logging.
- Updated project, MVP, rules, README, and templates documentation for the STEP 3 direction change.

## [0.0.5] - STEP 3 CATDrawing Creation

- Implemented new CATDrawing document creation in DrawingGenerator.
- Added active CATPart validation before drawing creation.
- Added output folder save flow for generated CATDrawing files.
- Enabled MainForm drawing generation button.

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
