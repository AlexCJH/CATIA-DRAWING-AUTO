# Changelog

## [0.0.28] - STEP 6B Manual Dimension Input Pause

- Added a user pause before manual dimension diagnostics.
- Allows the user to create manual dimensions in CATIA before the program reads FRONT_VIEW.Dimensions.
- Kept automatic dimension creation disabled.

## [0.0.27] - STEP 6B Manual Dimension Object Diagnostics

- Added diagnostics for manually created Drawing Dimension objects.
- Investigated Dimension object properties and linked geometry after Add2 and generated geometry reference attempts failed.
- Did not create Drawing Dimensions in this step.

## [0.0.26] - STEP 6B Drawing View Generated Geometry Diagnostics

- Added diagnostics for FRONT_VIEW generated geometry references.
- Investigated whether Drawing Dimension API requires 2D generated geometry instead of direct 3D references.
- Kept RED surface target detection and SaveAs flow stable.

## [0.0.24] - STEP 6B Color Based Surface Distance Dimension Experiment

- Added first Drawing Dimension API experiment using two RED Surface/Face dimension targets.
- Reused STEP 6A color based dimension target detection.
- Kept drawing SaveAs flow even when dimension creation fails.
- Limited STEP 6B to one surface-to-surface distance dimension experiment.

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
