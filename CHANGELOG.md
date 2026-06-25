# Changelog

## [0.0.17] - STEP 5 Independent Orthographic Views

- Disabled DefineProjectionView execution for the STEP 5 experiment.
- Added TOP_VIEW and RIGHT_VIEW generation as independent generative views using DefineFrontView vectors derived from FRONT_VIEW.
- Reused the stabilized marker based Front View vectors without changing MAIN_VIEW_PLANE/TOP_DIRECTION extraction or ViewSide/ViewRotation logic.
## [0.0.16] - STEP 5 Projection View Validation

- Improved Projection View generation diagnostics around FRONT_VIEW generative behavior and sheet updates.
- Added projection view size validation so empty TOP_VIEW/RIGHT_VIEW results fail STEP 5 instead of reporting success.
- Kept Marker based Front View orientation and removed Global Axis UI/config/context unchanged.
## [0.0.15] - STEP 5 Projection Views

- Added Top and Right Projection View generation from FRONT_VIEW in ViewGenerator.
- Kept DrawingGenerator responsible only for workflow coordination and SaveAs handling.
- Added STEP 5 logs and failure handling so CATDrawing SaveAs continues when Projection View creation fails.
- Kept obsolete Global Axis direction UI/config/context removed.
## [0.0.14] - STEP 5 Preparation Global Axis UI Cleanup

- Removed obsolete global axis direction UI.
- Standardized front view orientation on marker-based direction control.
- Removed unused Global Axis direction defaults and context values while keeping ViewSide/ViewRotation correction.

## [0.0.13] - STEP 4 View Side and Rotation Correction

- Added View Side and View Rotation options for marker based Front View correction.
- Kept MAIN_VIEW_PLANE as the viewed face source and TOP_DIRECTION as the base 0 degree up direction.
- Applied 0/90/180/270 rotation mapping through viewRight and viewUp vectors without changing GetPlane/GetDirection dynamic ref extraction.
## [0.0.12] - STEP 4-1B DefineFrontView Vector Mapping

- Changed DefineFrontView mapping to use viewRight and viewUp vectors instead of passing the MAIN_VIEW_PLANE normal directly.
- Added logs for front normal, view up, view right, and DefineFrontView vector arguments.
## [0.0.11] - STEP 4-1B GetPlane Variant Array Experiment
- Added dynamic ref object[] and double[] fallback experiments for GetPlane/GetDirection output arrays.
- Updated GetPlane/GetDirection experiment to pass object arrays as ByRef single COM arguments.
- Updated GetPlane/GetDirection experiment to pass object arrays as a single COM argument instead of expanded params.

- Changed MAIN_VIEW_PLANE Measurable.GetPlane argument from double[] to object[] for CATIA COM Variant/SafeArray compatibility testing.
- Changed TOP_DIRECTION Measurable.GetDirection argument from double[] to object[].
- Added null index and Convert.ToDouble failure diagnostics for measurable result arrays.
- Updated MainForm drawing button log text to avoid duplicate DrawingGenerator request logs.
## [0.0.10] - STEP 4-1B Marker Based Front View Direction

- Added marker based Front View orientation using GS_DRAWING_INFO markers.
- Added MAIN_VIEW_PLANE and TOP_DIRECTION lookup in ViewGenerator.
- Added SPAWorkbench measurable vector extraction for plane normal and top direction.
- Applied normalized marker vectors to CATIA DefineFrontView.
- Documented Global Axis direction selection as an auxiliary workflow and Marker based orientation as the final direction principle.
## [0.0.9] - STEP 4-1A Manual Front View Direction

- Added manual Front View Direction and Top Direction selection to MainForm.
- Added default direction settings to appsettings.
- Passed selected directions through DrawingGenerationContext and DrawingGenerator to ViewGenerator.
- Converted selected directions to vectors in ViewGenerator and rejected parallel front/top direction combinations.
- Applied selected orientation vectors to CATIA DefineFrontView.

## [0.0.8] - STEP 4 Front View Generation

- Added STEP 4 scope for creating one Front View on the opened template CATDrawing.
- Implemented Front View creation in ViewGenerator and kept DrawingGenerator as the workflow coordinator.
- Saved the template copy after the Front View attempt and separated Front View failure logs from SaveAs logs.

## [0.0.7] - STEP 3 Template Open Diagnostics

- Added detailed pre-open diagnostics for CATDrawing templates.
- Logged template display path, absolute path, file existence, size, read-only state, current directory, and the exact CATIA Documents.Open argument.
- Added warning hints when CATIA Documents.Open fails.

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
