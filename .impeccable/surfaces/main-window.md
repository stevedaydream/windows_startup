# Main window

MODE: Operate
FORM: User-confirmed native Windows index and details. seed:6e3a6b38f6cb2731; the user's explicit direction controls the implementation.

## Direction contract
Light cool background, white work areas, blue primary actions and Segoe UI. Left: source-grouped startup list showing name, explicit enabled/disabled state and publisher. Right: fixed selected-entry detail region with scrollable metadata and persistent actions. Search and status filter sit above the list. Details expose the complete command and executable path as selectable text. Existing startup-management capabilities and WPF/MVVM remain.

## First viewport
Heading and purpose, refresh/add actions, search/status/count, grouped list, selection guidance or selected-entry details, service status/error surface and administrator restart. The signature interaction is selecting an entry to reveal its full command and controls without leaving the list.

## Verification
Build: succeeded, zero warnings/errors. Core tests: 17 passed. Native Windows render inspected for main window, selected entry and add-entry dialog. Search with no matching entries verified before final styling correction. Minimum-size capture attempt failed; compact-size rendering remains unverified. No actual startup entries were modified during UI verification.

## Review
Delegated finish reviewer disposition: ship for inspected desktop and dialog, with compact-size coverage limitation. No approved comp or visual quality card; code-led build evaluated against the user's text-confirmed direction. No web detector ran because the implementation is WPF XAML.
