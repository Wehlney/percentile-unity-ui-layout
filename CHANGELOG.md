# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.2] - 2026-04-02

### Added
- Added `SquareChildrenAnchorLayout` to the runtime assembly.

### Changed
- Expanded the README with usage guidance for all three layouts.

## [0.1.1] - 2026-04-01

### Changed
- Moved runtime types to `Wehlney.PercentileUILayout.*` namespaces to avoid collisions when embedded in projects.
- Runtime assemblies no longer depend on `UnityEditor` (editor-only code lives in the editor assembly).

### Added
- Added a more detailed README with usage guidance.

## [0.1.0] - 2026-04-01

### Added
- Initial release: percent-based horizontal and vertical `LayoutGroup` components.
- `[NamedList]` attribute and editor drawer support.
