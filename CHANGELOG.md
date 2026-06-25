# Changelog

## [0.0.20] - STEP 5A Manual Verification Complete

- Manually verified CATIA API generated TOP_VIEW and RIGHT_VIEW as Projection View icons in CATIA tree.
- Confirmed FRONT_VIEW, TOP_VIEW, and RIGHT_VIEW update after source 3D model change and drawing update.
- Kept independent generative TOP_VIEW/RIGHT_VIEW as fallback.

## [0.0.19] - STEP 5A CATIA API Projection View Experiment

- Added CATIA API based Projection View experiment path.
- Kept independent generative TOP_VIEW/RIGHT_VIEW generation as fallback.
- Added logs to distinguish API success, API candidate requiring manual verification, API failure with fallback, and total failure.
- Documented that unverified Projection API behavior must not replace the stable fallback.

## [0.0.18] - Roadmap Update Toward Marker Based Dimensions

- Reframed project roadmap around marker based basic views and partial dimension generation.
- Marked view layout automation, PDF export, Detail View, Section View, and title block automation as deferred.
- Kept independent generative TOP_VIEW/RIGHT_VIEW generation as the current stable fallback.
- Added STEP 5A for future CATIA API based Projection View experiments.
- Promoted marker based partial dimension generation as the next major functional direction.

## [0.0.17] - STEP 5 Independent Orthographic Views

- Disabled DefineProjectionView execution for the STEP 5 experiment.
- Added TOP_VIEW and RIGHT_VIEW generation as independent generative views using DefineFrontView vectors derived from FRONT_VIEW.
- Reused the stabilized marker based Front View vectors without changing MAIN_VIEW_PLANE/TOP_DIRECTION extraction or ViewSide/ViewRotation logic.

## [0.0.16] - STEP 5 Projection View Validation

- Improved Projection View generation diagnostics around FRONT_VIEW generative behavior and sheet updates.
- Added projection view size validation so empty TOP_VIEW/RIGHT_VIEW results fail STEP 5 instead of reporting success.
- Kept Marker based Front View orientation and removed Global Axis UI/config/context unchanged.