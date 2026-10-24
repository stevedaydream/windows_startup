---
name: Windows Startup Manager
description: Clear startup entry management in a native Windows desktop interface.
colors:
  ink: "#18283E"
  muted: "#52647A"
  line: "#DCE3ED"
  accent: "#185ABD"
  ground: "#F4F6FA"
  surface: "#FFFFFF"
  table-header: "#F5F7FB"
  group-header: "#F8FAFD"
  row-divider: "#EEF1F6"
  row-hover: "#F2F6FC"
  row-selected: "#E7EFFC"
  text-selection: "#BDD5F8"
  enabled-background: "#E4F3ED"
  enabled-text: "#176346"
  disabled-background: "#EDF0F4"
  disabled-text: "#526074"
  warning: "#865C14"
  destructive: "#A12732"
typography:
  headline:
    fontFamily: "Segoe UI"
    fontSize: "28px"
    fontWeight: 600
  dialog-title:
    fontFamily: "Segoe UI"
    fontSize: "24px"
    fontWeight: 600
  title:
    fontFamily: "Segoe UI"
    fontSize: "22px"
    fontWeight: 600
  empty-title:
    fontFamily: "Segoe UI"
    fontSize: "20px"
    fontWeight: 600
  body:
    fontFamily: "Segoe UI"
    fontSize: "14px"
  label:
    fontFamily: "Segoe UI"
    fontSize: "12px"
  table-label:
    fontFamily: "Segoe UI"
    fontSize: "12px"
    fontWeight: 600
  command:
    fontFamily: "Consolas"
    fontSize: "12px"
rounded:
  status: "4px"
  button: "5px"
  panel: "7px"
spacing:
  action-gap: "8px"
  toolbar-gap: "10px"
  field-gap: "12px"
  section-gap: "16px"
  column-gap: "18px"
  detail-inset: "22px"
  header-gap: "24px"
  window-inset: "28px"
components:
  button-secondary:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.ink}"
    rounded: "{rounded.button}"
    padding: "8px 14px"
  button-primary:
    backgroundColor: "{colors.accent}"
    textColor: "{colors.surface}"
    rounded: "{rounded.button}"
    padding: "8px 14px"
  button-destructive:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.destructive}"
    rounded: "{rounded.button}"
    padding: "8px 14px"
  text-field:
    padding: "8px 10px"
  status-enabled:
    backgroundColor: "{colors.enabled-background}"
    textColor: "{colors.enabled-text}"
    rounded: "{rounded.status}"
    typography: "{typography.table-label}"
    padding: "3px 8px"
  status-disabled:
    backgroundColor: "{colors.disabled-background}"
    textColor: "{colors.disabled-text}"
    rounded: "{rounded.status}"
    typography: "{typography.table-label}"
    padding: "3px 8px"
  panel:
    backgroundColor: "{colors.surface}"
    rounded: "{rounded.panel}"
---

# Design System: Windows Startup Manager

## Overview

This is a native Windows desktop utility built with WPF. The user-confirmed direction is a light Windows ground, a blue action accent, and clear information with intuitive operation. The visual system supports reading startup entries and acting on a selected entry through familiar desktop controls.

The implemented main surface places a startup list on the left and a fixed details pane on the right. This document records implemented XAML decisions; it does not establish an additional brand metaphor. Measurements expressed as px in the portable tokens correspond to WPF device-independent units, not physical screen pixels.

**Key Characteristics:**

- Light ground and white working surfaces.
- Segoe UI hierarchy with restrained blue actions.
- Textual status labels and copyable technical details.
- Flat bordered panels and persistent entry controls.

## Colors

The palette uses a blue primary accent, cool neutrals, and functional status colors. The frontmatter is the normative value reference.

### Primary

- **Windows action blue (`accent`)** identifies Add entry and the selected entry's enable/disable action. It also marks custom button hover and keyboard focus borders.

### Neutral

- **Deep ink (`ink`)** carries primary content; **muted slate (`muted`)** carries supporting text, labels, and table headings.
- **Light ground (`ground`)** separates the window from white working surfaces (`surface`). The dialog uses the white surface.
- **Soft line (`line`)** defines panels and buttons. `table-header`, `group-header`, and `row-divider` provide subtle table structure.
- `row-hover` and `row-selected` distinguish interaction states; `text-selection` colors editable text selection.

### Functional colors

- `enabled-background` and `enabled-text` form the Enabled badge. `disabled-background` and `disabled-text` form the Disabled badge. Keep the textual label so status remains understandable without color.
- `warning` marks administrator access information. `destructive` identifies removal and errors.

## Typography

Segoe UI supplies the desktop hierarchy; Consolas is reserved for command text. Font size and weight are specified in the frontmatter. WPF supplies unspecified line metrics and control font defaults; no custom tracking or line-height scale is implemented.

The main heading is the headline role. The add-entry heading uses dialog-title; selected entry names use title; empty and unselected states use empty-title. Body text handles descriptions and values. Small labels handle metadata and helper text, with semibold table-label for table headings and status badges. Command text uses the command role; executable paths use the small Segoe UI size and wrap.

## Layout

The main window initially measures 1280 by 800 device-independent units, with a minimum of 1000 by 620. Its outer inset is 28 horizontally and 24 vertically. The header sits above search, status filter, and entry count; the footer holds operation feedback and administrator restart.

The list expands into remaining space with a minimum width of 440. An 18-unit gutter separates it from the 340-unit details column. Details have a 22-unit inset; their content scrolls vertically while entry actions remain below the scroll region. The list columns have fixed widths for Name, Status, and Publisher, so horizontal scrolling may be necessary at smaller supported widths. There is no mobile breakpoint or pane stacking implementation.

The add-entry dialog is 580 units wide, sizes vertically to content, cannot be resized, and opens centered on its owner. It uses a 28-unit inset, vertically stacked labeled fields, a command field beside Browse, and right-aligned Cancel and Add entry actions.

These are source-derived sizing rules. Desktop and add-entry screenshots were reviewed by the design reviewer; this document makes no claim of compact-window visual verification.

## Elevation & Depth

Application-authored surfaces use no shadows or blur. White surfaces, subtle background changes, and thin borders establish depth. Window chrome, native control rendering, and any operating-system window shadow remain platform-managed.

## Shapes

Buttons use modest corners; panels use a slightly larger radius; status badges use the smallest radius. The exact values live in the frontmatter. Panel and button strokes are one unit at rest. Input and ComboBox geometry follows WPF's platform templates rather than a custom shared corner token.

## Components

### Buttons

Primary buttons use the accent fill and white text. Secondary buttons use white, ink text, and a line border; removal uses destructive text. All share the button padding and a minimum height of 36 units. Hover changes the border to accent; keyboard focus uses an accent border two units thick. Pressed surface opacity is 0.75; disabled surface opacity is 0.45 and the cursor returns to the arrow. Busy state disables the shared button style. Access-key underscores are rendered as native mnemonics.

### Inputs / Fields

Text fields use the extracted padding and line border with vertically centered content and the text-selection brush. ComboBoxes use padding of 10 horizontally and 7 vertically, with minimum height 38. Native templates supply unspecified hover, focus, and disabled treatment. Dialog labels associate with their fields and support access keys.

### Cards / Containers

List and details containers are flat white panels with the shared line border and panel corners. The details pane has a consistent inset; the list uses row and column padding instead of a card inset.

### Startup list

The read-only DataGrid uses single full-row selection, visible column headers, horizontal dividers, and source-group headers. Rows have a minimum height of 50; cells and headers use 14 horizontal and 12 vertical padding. Hover and selection apply tonal row fills. Status badges retain their words and distinct functional colors. The empty view gives a heading and a short recovery suggestion.

### Entry details

The selected entry name leads a wrapping metadata stack. Read-only borderless text fields keep source, command, paths, and application information selectable. The action stack sits outside the scrolling metadata area. An unselected state explains what selecting an entry will reveal.

## Do's and Don'ts

### Do:

- **Do** reuse the implemented resource colors and Segoe UI hierarchy.
- **Do** keep startup status in words alongside its badge color.
- **Do** preserve keyboard focus borders, access keys, and selectable technical values.
- **Do** keep the details actions reachable beneath scrolling metadata.

### Don't:

- **Don't** substitute web, iOS, or Android conventions for the native Windows context.
- **Don't** introduce shadows or ornamental surface effects into these flat working panels without revisiting the design direction.
- **Don't** claim responsive stacking or compact-window visual verification from source sizing alone.
