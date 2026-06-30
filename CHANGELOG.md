# Changelog

## [0.0.23] - STEP 6A Geometry Type Detection Diagnostics

- Improved diagnostics for color based dimension target geometry type detection.
- Added measurable extraction probes for dimension target candidates.
- Kept color detection and drawing SaveAs flow stable.
- Did not create Drawing Dimensions in this step.

## [0.0.22] - STEP 6A Color Based Dimension Target Detection Experiment

- Added DimensionGenerator experiment for color based dimension target detection.
- Added logs for candidate name, COM type, color read attempt, and geometry type.
- Kept drawing SaveAs flow even when color detection is unsupported or inconclusive.
- Did not create Drawing Dimensions in this step.

## [0.0.21] - STEP 6A Direction Update For Color Based Dimension Targets

- Reframed STEP 6A as color based dimension target detection instead of immediate point-to-point dimension creation.
- Added color based, geometrical set based, and name prefix based dimension target strategies.
- Clarified that STEP 6A does not create dimensions yet.
- Kept marker based partial dimension generation as the later implementation goal.

## [0.0.20] - STEP 5A Manual Verification Complete

- Manually verified CATIA API generated TOP_VIEW and RIGHT_VIEW as Projection View icons in CATIA tree.
- Confirmed FRONT_VIEW, TOP_VIEW, and RIGHT_VIEW update after source 3D model change and drawing update.
- Kept independent generative TOP_VIEW/RIGHT_VIEW as fallback.
