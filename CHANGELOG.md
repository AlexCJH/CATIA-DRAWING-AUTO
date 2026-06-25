# Changelog

## [0.0.21] - STEP 6A Direction Update For Color Based Dimension Targets

- Reframed STEP 6A as color based dimension target detection instead of immediate point-to-point dimension creation.
- Added color based, geometrical set based, and name prefix based dimension target strategies.
- Clarified that STEP 6A does not create dimensions yet.
- Kept marker based partial dimension generation as the later implementation goal.

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