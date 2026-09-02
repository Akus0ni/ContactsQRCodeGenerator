# DESIGN.md — ContactQR

**Design brief. Written against `PRD.md`. To be settled before any code is written.**

| | |
|---|---|
| Status | Draft for review |
| Date | 2 September 2026 |
| Platform | Windows desktop, WPF / .NET 10. Single main window. |
| Primary user | Nikhil Rao, 36 — solo print & design studio. Adobe CS daily. Two monitors. |
| Acceptance user | Meera D'Souza, 52 — never opens this app. Her three seconds define success. |

> **Two deliberate deviations from the brief, argued in §11.** §9 was requested as *mobile / tablet / desktop*; the PRD makes mobile and web explicit non-goals, so §9 specifies window breakpoints, Windows DPI scaling and dual-monitor behaviour instead. §10 was requested as *ARIA*; WPF has no ARIA, so §10 specifies the UI Automation equivalents that actually compile.

---

## 1. Design principles — three rules this UI must obey

Not values. Testable constraints. A design that breaks one is wrong even if it looks better.

### P1 — The verdict is the interface, not a result of it

**Rule.** The computed module size in millimetres and its Safe / Marginal / Will fail verdict are visible at all times in the Editor, at display type size, without scrolling, hovering, clicking or expanding. Every other panel may collapse. This one may not.

**Why this product.** The PRD's thesis is that competing tools render a failing QR without complaint. Their failure is not that they compute the wrong answer — they never compute it at all. If our answer lives behind a disclosure triangle we have shipped the same product with better internals. The number that decides whether Nikhil eats a reprint gets the typographic weight normally reserved for a marketing hero, because in this product it *is* the product.

**Test.** Screenshot any Editor state at minimum window size. If the mm/module figure and verdict word are not legible, the layout is rejected.

### P2 — Price every choice at the point of choice

**Rule.** Any control that spends the byte budget — each optional field, the logo toggle, the ECC selector, the print-width input — shows its cost inline, before commitment, in bytes *and* in resulting mm/module. No control may spend budget silently and report the damage elsewhere.

**Why this product.** The logo is the most expensive decision on the card, roughly halving capacity by forcing ECC H (FR-5.0). Every tool on the market presents it as a free cosmetic checkbox. Pricing it *at the toggle* — "forces ECC H: capacity 362 → 196 B. Payload 250 B → **Will fail** at 25 mm" — is the exact moment a reprint gets prevented. It also does the teaching: the per-field byte costs in FR-1.3 are how Nikhil learns what density means, without a manual.

**Test.** For every control that changes payload, ECC or physical size, point at where its cost renders. If the answer is "in the budget panel," that control fails P2.

### P3 — Never render a comfortable lie

**Rule.** The preview must not be more legible than the print. No anti-aliased module edges, no drop shadow on the QR, no transparent background, no default zoom that flatters. A true 1:1 physical view is one keystroke away and honours actual display DPI, never an assumed 96.

**Why this product.** Every failure in the PRD is a screen-versus-paper failure. A 400-pixel QR on a 27-inch monitor is *always* readable and therefore tells Nikhil nothing (FR-3.6). A drop shadow is actively harmful: it implies the code floats over any background, which is precisely the transparent-background trap FR-5.9 bans. A rounded frame implies the code itself is decorative. The preview's job is not to look good. It is to be a proxy for the printed card, and where it cannot be, it must say so.

**Test.** Hold a printed 25 mm QR against the 1:1 view at 100%, 150% and 200% Windows scaling. They must match physically. Any discrepancy is a P3 defect, not a polish item.

---

## 2. Visual direction

### The one-line brief

**A measuring instrument, not a design tool and emphatically not a SaaS app.** ContactQR sits beside InDesign and does the one thing InDesign cannot: tells Nikhil a number he can trust. It should feel like a densitometer or a preflight panel — calm, dense, numerically literate, uninterested in delighting him.

### Where the visual language comes from

The distinctive material here is **print production**, and specifically the prepress artifacts that exist to verify that a sheet is correct: the **control strip** of ink patches printed along the edge of every press sheet, **crop marks**, the non-printing **cyan guides** of a page-layout document, and **DIN**-lettered rulers and gauges. This app is an instrument in that lineage. Everything below is derived from it rather than from a UI kit.

### Mood

Precise. Quiet. Slightly clinical. High information density with rigorous alignment. Confidence expressed through legible figures and hairlines, never through colour, illustration or motion. When the app is content it says almost nothing. When it blocks an export it is unmissable.

### References — what to take, what to leave

| Reference | Take | Leave |
|---|---|---|
| **InDesign / Acrobat preflight panel** | The structural model: a persistent panel that inspects work in progress and reports faults *before* output, each fault clickable to its fix. Structurally identical to the Scannability Budget. | Adobe's cramped legacy chrome and inconsistent icons. |
| **Capture One / Affinity tool panels** | Chromatically neutral chrome so client colour can be judged honestly. Dense right-rail parameter stacks. Numeric fields with unit suffixes and scrub-to-adjust. | Their sheer control count. We do one job. |
| **Press control strip / colour bar** | The signature component (below). A row of measured patches read at a glance to confirm a sheet is good. | Literal CMYK rainbow decoration. |
| **DIN 1451 rulers, gauges, technical drawings** | The display typeface and the tone of the numerals. Measurement as an aesthetic. | Skeuomorphism. No drawn calipers, no textures. |
| **Leica / Fujifilm readouts** | Large tabular figures, unit set small and quiet beneath the number, status as one glyph plus one word. | Dials, knurling, faux metal. |

### The signature element — the Control Strip

**The byte-budget meter is drawn as a press control strip:** a flush, square-cornered band of segmented patches running the full width of the preview canvas's lower edge, where segments fill left-to-right as payload consumes capacity, a solid tick marks the ceiling at the current ECC level, and a hollow "ghost" tick marks the ceiling that *would* apply if the logo were removed.

This is the one memorable thing in the app, and it earns its place three ways. It is the literal artifact a press operator reads to decide whether a sheet is good, so it is native to Nikhil's world rather than imported from a dashboard. It shows two ceilings at once, which is the only way to make the logo's cost legible as a *distance* rather than a sentence (P2). And it sits directly beneath the preview, binding the abstract number to the thing it measures.

Everything else in the app stays quiet so this can be loud. Chanel's rule applies: the control strip and the crop-mark canvas framing are the two devices. There is no third.

### Canvas framing — crop marks, not a border

The preview canvas is bounded by four **crop marks** at its corners — hairline L-shapes offset from the artwork — not by a rectangle, card, or rounded frame. Subject-true, and it satisfies P3: crop marks describe a trim edge, which is exactly what the QR's boundary is, whereas a rounded card frame implies decoration.

### What to avoid, and why each is specifically wrong here

- **Material elevation and cards.** Shadow-as-hierarchy muddies adjacent panels at this density, and it puts a drop shadow within reach of the QR preview, which P3 bans.
- **Friendly SaaS roundness** — 8–12 px radii, pill buttons, soft shadows. Reads as consumer software. Nikhil is deciding whether to send 500 cards to a press.
- **Illustration, mascots, empty-state cartoons, emoji.** Empty states here are working states, not moments for personality.
- **Success animation** — confetti, bouncing ticks, celebratory toasts. A successful export is the expected outcome roughly twenty times a month. Celebrating it is noise, and it trains Nikhil to dismiss our notifications reflexively — exactly what must not happen when the export is blocked.
- **A brand accent applied broadly** — tinted greys, coloured panel headers, gradient chrome. The most product-specific prohibition in this document: **Nikhil is judging his client's brand colours on our canvas.** A blue-tinted UI shifts his perception of a warm client palette. Our greys are neutral by construction (R = G = B, exactly), for the same reason Photoshop's are.
- **Gradients, glassmorphism, translucency** anywhere near the canvas. Any translucency behind the preview compromises colour judgement.
- **Near-black chrome with one acid accent.** A recognisable default, and wrong here on its own merits: it makes the QR preview glare and it is not the environment print work is judged in.
- **Icon-only controls for anything consequential.** Verdicts, gates and remedies are words first, always.

### Theme, and the one surface that ignores it

**Neutral graphite chrome by default; a light theme fully supported.** Graphite rather than near-black because the largest surface in the window is the mid-grey preview canvas, and the chrome should sit in the same tonal family as a photographic grey card rather than fight it. Graphite rather than light because Nikhil lives in Adobe CS all day and a bright application in that context is a flashbang.

**The preview canvas is a fixed neutral mid-grey — `#8C8C8C` — in both themes and does not follow the theme.** Argued rather than assumed: a dark canvas flatters a light-background QR, a white canvas flatters a dark one. Since the operator must judge foreground/background contrast (FR-5.7) and the code's honesty as printed, the surround has to be a constant, chromatically neutral reference field — the standing convention in print and photography. Theme-dependent canvas would mean the same QR looks differently trustworthy in light mode than dark, which breaks P3.

*Flagged: graphite-by-default is a recommendation, not settled. If Nikhil proofs primarily against white stock on screen, light-by-default may be right. See §11 Q3.*

---

## 3. Design tokens

All greys are **exactly neutral** (R = G = B). This is a hard rule, not a rounding convention — see the brand-accent prohibition above.

### 3.1 Colour — surfaces and text

Contrast ratios are computed against the stated background and are the values the build must hold.

#### Dark theme (default)

| Token | Hex | Usage | Contrast |
|---|---|---|---|
| `surface.window` | `#1F1F1F` | Window backdrop, title bar, gutters between panels | — |
| `surface.panel` | `#292929` | Form pane, right rail, dialog bodies. The reference background. | — |
| `surface.raised` | `#333333` | Input fields, list rows, segmented-control track | — |
| `surface.hover` | `#3D3D3D` | Row and control hover | — |
| `surface.selected` | `#454545` | Selected library row, active segment | — |
| `border.hairline` | `#3D3D3D` | 1 px dividers inside a panel | — |
| `border.default` | `#4F4F4F` | Input borders, panel edges | 2.0:1 vs panel — decorative only |
| `border.strong` | `#6B6B6B` | Focused/active input border, crop marks | 3.1:1 vs panel ✓ non-text |
| `text.primary` | `#F2F2F2` | Values, headings, body | **13.0:1** on panel ✓ AAA |
| `text.secondary` | `#B0B0B0` | Field labels, units, helper text | **6.7:1** ✓ AA+ |
| `text.tertiary` | `#949494` | Placeholders, timestamps, disabled-adjacent | **4.8:1** ✓ AA |
| `text.disabled` | `#6B6B6B` | Disabled control text | 3.1:1 — non-essential only |
| `canvas.field` | `#8C8C8C` | **Preview canvas. Fixed across themes.** | — |
| `canvas.field.shadowline` | `#7A7A7A` | 1 px inner line where canvas meets panel | — |

#### Light theme

| Token | Hex | Usage | Contrast |
|---|---|---|---|
| `surface.window` | `#E8E8E8` | Window backdrop | — |
| `surface.panel` | `#F5F5F5` | Panels, dialog bodies. Reference background. | — |
| `surface.raised` | `#FFFFFF` | Inputs, list rows | — |
| `surface.hover` | `#EBEBEB` | Hover | — |
| `surface.selected` | `#DCDCDC` | Selection | — |
| `border.hairline` | `#DCDCDC` | Dividers | — |
| `border.default` | `#BFBFBF` | Input borders | — |
| `border.strong` | `#8C8C8C` | Focus/active borders, crop marks | 3.0:1 ✓ non-text |
| `text.primary` | `#1A1A1A` | Values, headings | **15.9:1** ✓ AAA |
| `text.secondary` | `#525252` | Labels, units | **7.4:1** ✓ AA+ |
| `text.tertiary` | `#6E6E6E` | Placeholders | **4.9:1** ✓ AA |
| `canvas.field` | `#8C8C8C` | **Unchanged from dark theme, deliberately** | — |

### 3.2 Colour — interactive accent

**Process cyan.** Reserved exclusively for focus, selection and the primary action. It never carries status.

| Token | Dark | Light | Usage | Contrast |
|---|---|---|---|---|
| `accent.base` | `#00B8D4` | `#00758C` | Primary button fill, active segment, links | **6.1:1** dark / 5.3:1 light ✓ |
| `accent.hover` | `#22CBE4` | `#00637A` | Hover on primary | — |
| `accent.focus` | `#5CDBEE` | `#0093AD` | Focus ring outer stroke | 8.9:1 ✓ far above the 3:1 non-text floor |
| `accent.subtle` | `#0E3A42` | `#DBF3F8` | Selected-row wash, focus field tint | — |
| `accent.onFill` | `#04222A` | `#FFFFFF` | Text on `accent.base` | 7.2:1 / 4.8:1 ✓ |

**Why cyan, specifically.** Cyan is the default colour of **non-printing guides** in every page-layout application Nikhil uses. Our entire interface is a non-printing layer over the client's artwork, so it wears the colour of one — the accent tells him, without a word, which pixels are ours and which are the client's. It is also a process ink, native to the subject's world, and it sits far from amber and red in hue space, so an accent-coloured focus ring can never be misread as a status signal. That last property is why the accent is *forbidden* from carrying status: cyan means "you are here," never "this is fine."

### 3.3 Colour — verdict system

The verdict is the product. It gets its own scale, used nowhere else.

| Token | Dark | Light | Contrast (dark) | Glyph | Word |
|---|---|---|---|---|---|
| `verdict.safe.text` | `#3FBF6F` | `#12703F` | **6.2:1** ✓ | `✓` | Safe |
| `verdict.safe.fill` | `#2E9E58` | `#12703F` | white text 4.6:1 ✓ | | |
| `verdict.safe.wash` | `#16311F` | `#E4F4EA` | — | | |
| `verdict.marginal.text` | `#F5B544` | `#8A5600` | **8.0:1** ✓ | `!` | Marginal |
| `verdict.marginal.fill` | `#D99A22` | `#8A5600` | | | |
| `verdict.marginal.wash` | `#332711` | `#FBF0DC` | — | | |
| `verdict.fail.text` | `#FF6B6B` | `#B3161C` | **5.2:1** ✓ | `✕` | Will fail |
| `verdict.fail.fill` | `#D93036` | `#B3161C` | white text 4.7:1 ✓ | | |
| `verdict.fail.wash` | `#3A1A1B` | `#FBE5E6` | — | | |
| `verdict.override` | `#C77DFF` | `#6B21A8` | 6.4:1 ✓ | `⚑` | Overridden |

**Three decisions inside this table.**

*Colour is never the signal.* Every verdict renders as **glyph + word + colour**, in that order of authority, and always in the same screen position. This matters more than it usually does: deuteranopia affects roughly 8% of men and our sole user is a 36-year-old man. Because the three verdicts are sequential states of one element rather than three things compared side by side, the word does the work and colour is reinforcement — which is the right division regardless of vision.

*`verdict.fail` splits into `text` and `fill`.* A single red that reads well as small text on graphite is too light to carry white text in a solid badge, and a red dark enough for a badge fails as 13 px text. Two tokens, each meeting AA in its own role, rather than one token failing quietly in both.

*The override state is violet, not red.* An overridden unsafe export (FR-4.5) is not the same condition as a blocked one — it is a decision Nikhil consciously took and must be able to recognise later in the export log. Giving it its own hue stops "I overrode this" from being visually indistinguishable from "this is broken."

### 3.4 Colour — the client's artwork

| Token | Value | Usage |
|---|---|---|
| `qr.fg` / `qr.bg` | User-defined | The client's chosen colours. **Never overridden, never themed, never tinted by us.** |
| `qr.quietzone.hint` | `#00B8D4` @ 24% | Optional overlay showing the quiet-zone boundary. Non-printing guide, hence accent cyan. Off by default, toggled with `Q`. |
| `qr.logo.keepout` | `#FF6B6B` @ 30% | Overlay marking finder/alignment patterns the logo may not enter (FR-5.5). Appears only while dragging a logo. |

### 3.5 Typography

Three faces, three jobs, no overlap.

| Role | Face | Source | Why this face for this product |
|---|---|---|---|
| **Display / measurement** | **Bahnschrift** (weights 400, 600; width axis 75–100) | **Ships with Windows 10/11.** No bundling, no licence, no download. | Bahnschrift is Microsoft's variable **DIN 1451** — the lettering standard of German industrial norms, used on rulers, gauges, technical drawings and road signage. It is, literally, the typeface of measurements you are meant to trust, which is the entire claim this product makes. Its variable width axis lets the hero readout stay dense at 40 px without a second family. And being a system face, it satisfies FR-8.2's ban on fetching anything over the network with zero effort. |
| **Body / UI** | **IBM Plex Sans** (400, 500, 600) | OFL 1.1, **bundled with the installer** | Drawn for technical documentation, with true tabular lining figures and enough humanist warmth to stay comfortable at 12–13 px in a dense panel. Its slightly odd forms — the flat-topped `a`, the tailed `l` — keep the app from reading as a generic Windows utility, which Segoe UI would guarantee. OFL permits bundling, so nothing is fetched. |
| **Data / mono** | **IBM Plex Mono** (400, 500) | OFL 1.1, bundled | Raw vCard payload, file paths, hex values, filenames, export-log rows. Same family metrics as Plex Sans, so mixed rows align on a shared baseline grid. |

**Figure handling — a correctness requirement, not a preference.** All live-updating numerals (byte count, mm/module, capacity remaining) use **tabular figures**, set in WPF via `Typography.NumeralAlignment="Tabular"`. Proportional figures cause the readout to jitter horizontally as digits change on every 250 ms debounce, which is both distracting and makes it hard to see whether a number is rising or falling — the exact judgement P2 requires.

*Flagged: Bahnschrift's figure widths must be verified as tabular before it is used for live-updating numerals. If they are not, Bahnschrift is confined to static display text and the hero numeral falls back to IBM Plex Sans SemiBold with `tnum`. See §11 Q4.*

### 3.6 Type scale

Sizes in device-independent pixels (WPF DIP, 1/96 in). The scale is a custom ramp, not a modular ratio — an instrument panel needs a few precise stops, not a smooth geometric series.

| Token | Size / Line | Face & weight | Usage |
|---|---|---|---|
| `type.hero` | 40 / 40 | Bahnschrift 600, width 87.5 | **The mm/module readout.** One instance per screen. |
| `type.verdict` | 22 / 26 | Bahnschrift 600, tracked +2% | The verdict word: Safe / Marginal / Will fail |
| `type.title` | 20 / 28 | Plex Sans 600 | Window and dialog titles |
| `type.panel` | 15 / 22 | Plex Sans 600 | Panel headings — Contact, Branding, Export |
| `type.body` | 13 / 20 | Plex Sans 400 | **Base size.** Field values, list rows, prose. |
| `type.bodyStrong` | 13 / 20 | Plex Sans 500 | Emphasised values, remedy actions |
| `type.label` | 12 / 16 | Plex Sans 500 | Field labels, units, helper text |
| `type.eyebrow` | 11 / 14 | Bahnschrift 600, tracked +8%, uppercase | Section eyebrows only. **Never body text.** |
| `type.mono` | 12 / 18 | Plex Mono 400 | Payload viewer, paths, hex |
| `type.monoSmall` | 11 / 16 | Plex Mono 400 | Export-log rows, filenames |

**Why base 13, not 16.** This is a dense desktop instrument sitting beside Adobe applications that run 11–12 px UI. A 16 px base would force either scrolling in the form pane or a wider window than a 1366-wide laptop can give, and P1 requires the budget and the form to coexist without scrolling. 13 px is the floor at which Plex Sans stays comfortable, and **11 px is a hard minimum used only for tracked uppercase and mono metadata**. Instead of shrinking below that, §9 provides a Compact / Default / Large UI density setting and §10 honours the Windows text-scaling setting to 200%.

**Why the hero is 40 px.** It is roughly triple the base, which puts it in a different reading mode entirely — glanced, not read. That is P1 made typographic: the module size should register from across the desk while Nikhil is looking at the printed proof in his hand, not at the screen.

### 3.7 Spacing

4 px base unit.

| Token | px | Usage |
|---|---|---|
| `space.0` | 0 | Flush edges — canvas, control strip |
| `space.1` | 4 | Icon-to-label, chip padding |
| `space.2` | 8 | Label-to-input, intra-row |
| `space.3` | 12 | Between fields in a group |
| `space.4` | 16 | Panel padding, between groups |
| `space.5` | 24 | Between panel sections |
| `space.6` | 32 | Above a primary action, dialog padding |
| `space.8` | 48 | Around the canvas artwork |
| `space.12` | 64 | Empty-state vertical centring |

**Why 4 and not 8.** Windows DPI scaling is the deciding constraint, not taste. At 125% a 4 px unit yields 5 px, at 150% it yields 6, at 175% it yields 7 — integers at every standard scale factor. A 6 px unit gives 7.5 at 125% and 10.5 at 175%, and WPF's layout rounding then produces inconsistent hairlines and 1 px seams between panels. A 4 px grid at this density also gives the granularity to set a 12 px gap between fields and 16 px around a panel without the two collapsing into the same value.

### 3.8 Radius

| Token | px | Applies to |
|---|---|---|
| `radius.none` | **0** | **Preview canvas, the QR itself, crop marks, control strip, table cells** |
| `radius.control` | 2 | Buttons, inputs, chips, segmented controls |
| `radius.panel` | 4 | Dialogs, popovers, panel containers |

**Why so tight.** 8–12 px radii are the signature of consumer software, and this tool is accountable for a press run. But the real rule is `radius.none`: **nothing that displays or bounds the QR is ever rounded.** A rounded frame around a code implies the code is a decorative object whose edges are negotiable, when in fact module edges must read as sharp and the quiet zone must read as functional (P3, EC-27). The control strip is square-cornered for the same reason — it is a measuring device, and measuring devices do not have soft corners.

### 3.9 Elevation and shadow

| Token | Value | Applies to |
|---|---|---|
| `elevation.flat` | none — separation by 1 px `border.hairline` and a background-value step | **All panels, rails, inputs, list rows, the control strip** |
| `elevation.float` | `0 4 16 rgba(0,0,0,0.45)` dark / `0 4 16 rgba(0,0,0,0.14)` light | Dialogs, popovers, dropdowns, tooltips — genuinely floating layers only |
| `elevation.scrim` | `rgba(0,0,0,0.55)` dark / `rgba(0,0,0,0.35)` light | Modal backdrop |
| `elevation.forbidden` | — | **The QR preview. No shadow, ever, at any elevation.** |

**Why almost none.** Shadow-as-hierarchy is a Material metaphor that breaks down at instrument density — a dozen softly shadowed panels adjacent to each other read as mush, whereas a 1 px hairline plus a background-value step is exact and costs no pixels. Shadows are reserved for layers that genuinely float above the workspace, where the metaphor is true.

`elevation.forbidden` is a real token in the system, present so that a reviewer can point at it. Per P3, a drop shadow on the QR would suggest it can be dropped onto any background, which is precisely the transparent-background failure FR-5.9 exists to prevent.

### 3.10 Motion

| Token | Duration / curve | Usage |
|---|---|---|
| `motion.instant` | 0 ms | Verdict changes, byte counter, preview re-render |
| `motion.control` | 100 ms, ease-out | Hover, press, focus ring |
| `motion.panel` | 160 ms, ease-out | Popover and dropdown open, rail collapse |
| `motion.dialog` | 200 ms, ease-out | Modal enter; exit is `motion.control` |

**The verdict never animates.** No cross-fade, no colour transition, no count-up. A number that animates toward its value is unreadable during the transition and implies the reading is settling, which invites Nikhil to wait and see rather than trust it. Instruments snap.

All motion is suppressed when Windows **"Show animations in Windows"** is off — read from `SystemParameters.ClientAreaAnimation`, not from a private setting.

---

## 4. Screen inventory

One main window with three primary views, plus dialogs and panels. No wizard, no onboarding carousel.

### Primary views

| # | View | Purpose | Entered from |
|---|---|---|---|
| **V1** | **Library** | The home surface. Search, browse, create, duplicate and open client records. The list *is* the app's memory (F7) and the answer to the annual-reprint job. | App launch; `Ctrl+L` |
| **V2** | **Editor** | The workspace where all real work happens: contact form, live preview, Scannability Budget, branding. Three panes. Everything in P1–P3 lives here. | Opening or creating a client |
| **V3** | **Export log** | Every PNG ever produced, with the exact payload, verdict, module size and whether the gate was overridden. Answers "the QR you made me doesn't work" in thirty seconds (FR-7.7). | `Ctrl+H`; a client's row menu |

### Dialogs — modal, dismissible with `Esc` unless stated

| # | Dialog | Purpose |
|---|---|---|
| **D1** | **Export PNG** | Physical width in mm, DPI, quiet zone, path, filename. Runs the gate and the decode-back self-test before writing (FR-6.9). |
| **D2** | **Unsafe export confirmation** | The FR-4.5 hard gate. Acknowledgement is deliberate, not reflexive. `Esc` cancels; there is no default-focused confirm button. |
| **D3** | **Print test sheet** | Preview and print the QR at 20/25/30/40/50 mm for physical scan-testing (FR-4.6). |
| **D4** | **Settings** | Tabs: General · Defaults · Library · Offline & Privacy · About. |
| **D5** | **Library import — resolve conflicts** | Per-record keep / replace / skip on JSON import (FR-7.6). |
| **D6** | **Relink logo** | Re-point a dangling `logo_path` (EC-20). |

### Panels and drawers — non-modal, inside V2

| # | Panel | Purpose |
|---|---|---|
| **N1** | **Scannability Budget** | Right rail, always mounted, never collapsible. The verdict, the control strip, the remedies. |
| **N2** | **Branding** | Right rail, collapsible. Logo toggle and placement, colours, ECC, contrast readout. |
| **N3** | **Payload inspector** | Slide-over from the right showing the raw vCard, line-numbered and copyable (FR-2.9). |
| **N4** | **Logo placement overlay** | Appears on the canvas while adjusting a logo: size handle, keep-out zones, coverage percentage. |

### System states — not screens, but must be designed

| # | State | Purpose |
|---|---|---|
| **S1** | **First run / empty library** | Day one. An invitation to add the first client, not a marketing panel. |
| **S2** | **Degraded read-only** | Library unreadable or locked by another instance (EC-21, EC-22). A persistent banner, not a modal — Nikhil must still be able to read his data. |
| **S3** | **Sync-conflict notice** | OneDrive conflict copies detected (EC-19). |
| **S4** | **Offline indicator** | Persistent status-bar affordance. **Styled as confirmation, not warning** — see §8. |

---

## 5. User flows

### J1 — New client to first export *(target: ≤ 90 s, PRD M5)*

1. **Launch.** App opens on **V1 Library**, search field focused. Ready for typing immediately — no splash, no dashboard.
2. **New client.** `Ctrl+N`, or the **New client** button in the header. V1 gives way to **V2 Editor** with an unsaved record; focus lands in *Given name*.
3. **Enter the two required fields.** Given name, then mobile. `Tab` moves between them. On blur the phone normalises to E.164 and shows what it became (FR-1.4).
4. **Preview appears.** As soon as both required fields hold values, the canvas renders. The hero readout and verdict fill. Before this point the canvas shows a locked state naming exactly which field is missing (§8).
5. **Add optional fields.** Each populated field shows its byte cost inline as a chip (P2). The control strip fills toward its ceiling. The hero readout moves.
6. **Set print width.** Nikhil types `25` into *Print width* in the right rail. The hero updates. Verdict may change.
7. **Decide on branding.** Toggling *Logo* previews the cost before applying (P2, FR-5.0). If it would tip the verdict, the toggle says so *before* it is switched.
8. **Read the verdict.** Safe → continue. Marginal or Will fail → **J3**.
9. **Export.** `Ctrl+E` opens **D1**. Width, DPI and quiet zone carry defaults from the last-used preset. Filename is generated and shown before writing.
10. **Gate and self-test.** The gate re-runs, then the decode-back self-test on the final rendered bitmap (FR-4.4). A determinate progress state runs during this — it is the one operation slow enough to need one.
11. **Written.** Inline confirmation in D1's footer with the path and **Open containing folder**. No toast, no animation. D1 stays open so a second size can be exported immediately.
12. **Save to library.** Prompted once, non-blocking, dismissible — a walk-in job should not force a permanent record (FR-7.8).

### J2 — Returning client reprint *(target: ≤ 15 s, PRD M3)*

1. **Launch** → V1, search focused.
2. **Type three or four characters** of the client or company name. The list filters live.
3. **`Enter`** opens the top match in V2. All fields, branding and the last export preset are restored.
4. **Change the one detail** — usually a mobile number. The control strip, hero readout and verdict update.
5. **`Ctrl+E`, `Enter`.** D1 opens with the previous settings and the filename regenerated. Export.
6. **Overwrite prompt** if the filename collides (FR-6.8) — Overwrite / Rename / Cancel, never silent.

The 15-second budget is why steps 2–5 are four keystrokes and one edit, and why D1 must never reset to defaults on a returning client.

### J3 — Over budget: diagnose and resolve *(the differentiating flow)*

1. **Verdict reads Marginal or Will fail.** The hero readout turns to the verdict colour; the control strip shows fill past the ceiling tick.
2. **Remedies appear beneath the verdict**, ranked by bytes recovered — biggest lever first, which with a logo present is almost always *Remove logo* (FR-4.3).
3. **Each remedy is a row** stating the action, the bytes it recovers, and **the resulting module size and verdict**: `Remove logo → ECC H drops to M · +165 B · 0.34 mm · Marginal`.
4. **Hovering or keyboard-focusing a remedy previews it** — the canvas shows the resulting code ghosted, the control strip shows where the fill would land, the hero shows the resulting number. Nothing is committed.
5. **Activating applies it.** The remedy row becomes an applied state with **Undo**; the list re-ranks against the new state (EC-1a).
6. **Or change print width instead.** The width field carries a `Minimum safe: 34 mm` hint, and a one-click **Use minimum safe width**.
7. **Verdict resolves to Safe** → J1 step 9. Still Will fail → **J5**.

### J4 — Adding a logo, and discovering its cost

1. **Logo row in the Branding panel** shows, before any file is chosen, what adding one would cost: `Forces ECC H · capacity 362 → 196 B · payload 250 B → Will fail at 25 mm`.
2. **If it would tip the verdict, the toggle is not blocked** — Nikhil may still want it and change something else. But the consequence is stated in the same breath as the offer (P2).
3. **Choose a file.** Local file picker only.
4. **Placement overlay (N4)** appears on the canvas: the logo at 18% default width, a size handle, keep-out zones over the finder and alignment patterns rendered in `qr.logo.keepout` (FR-5.5).
5. **Resizing past 18%** warns; **past 25%** is blocked at the handle — the handle stops, with the reason stated inline (FR-5.3).
6. **Dragging into a keep-out zone** is prevented by the drag itself, not reported afterwards.
7. **Self-test re-runs** on every logo change (FR-5.10). A failure here blocks export and says so.
8. **Removing the logo restores the previous non-logo ECC level**, and says which one it restored (FR-5.2) — otherwise Nikhil keeps paying for a logo he removed.

### J5 — Blocked export: override or retreat

1. **`Ctrl+E` with a Will fail verdict.** D1 opens, but its export button is disabled with the reason inline. The gate is stated in D1, not sprung after a click.
2. **Two routes out**, given equal visual weight but not equal effort: **Fix it** returns to the remedies in N1; **Export anyway** opens **D2**.
3. **D2 states the specific consequence** in plain terms: `This code will be about 0.24 mm per module at 22 mm. Below roughly 0.30 mm, phone cameras stop reading reliably off print.`
4. **The acknowledgement is a checkbox with a literal sentence**, unchecked, and the confirm button stays disabled until it is checked (FR-4.5).
5. **The confirm button is not default-focused and `Enter` does not activate it.** `Esc` cancels. Deliberate friction — a gate that can be dismissed by muscle memory protects nobody, and M7 depends on this gate staying credible.
6. **On confirm** the file is written with the `_UNSAFE` suffix, and the export log records `unsafe_override = true` against the violet override token.

### J6 — Back up and restore the library

1. **Settings → Library → Export library to JSON.** File picker, defaulting outside any sync root.
2. **Confirmation states the record count and path.**
3. **Restore:** *Import library* → **D5** lists every incoming record against its local counterpart with keep / replace / skip, plus **Apply to all**.
4. **Import is transactional** — it either applies fully or not at all, and the pre-import database is retained until the next clean launch.

### J7 — Verifying the offline claim *(US-14, US-15)*

1. **The status bar always shows** `Offline · no network access` with a filled indicator.
2. **Clicking it opens Settings → Offline & Privacy**, which states in Nikhil's own language what the app does and does not do with client data (FR-8.6) — written to be repeated to a client, not to satisfy a lawyer.
3. **It names the verification method** used at release: packet capture, `netstat`, and an adapter-disabled functional run (FR-8.8), with the result recorded for the installed version.
4. **"Try it" instruction:** disable the network adapter and keep working. Nothing changes — no banner, no timeout, no degraded mode. That absence *is* the demonstration.

### J8 — Physical verification before a press run

1. **Print test sheet** from the Editor toolbar or `Ctrl+P` opens **D3**.
2. **D3 previews one page** with the QR at 20/25/30/40/50 mm, each labelled with its size, module size and verdict.
3. **Print** to any local printer.
4. **Scan each with a phone.** The sheet is captioned with the app's predicted threshold so Nikhil can compare prediction against reality — which is also how the M1b threshold calibration gets its field data.

---

## 6. Per-screen layout

### 6.1 V2 — Editor *(the screen the product lives or dies on)*

```
┌──────────────────────────────────────────────────────────────────────────────────┐
│ ‹ Library    Meera D'Souza — Sunrise Physiotherapy          ● Saved   [Settings]  │  56
├────────────────────┬──────────────────────────────────┬──────────────────────────┤
│ CONTACT            │                                  │ SCANNABILITY             │
│                    │      ┌ ┐                  ┌ ┐    │                          │
│ Given name    ●    │                                  │  ┌────────────────────┐  │
│ [Meera        ]    │        ▓▓▒░ QR PREVIEW ░▒▓▓      │  │  0.34              │  │
│                    │        ▓▒░  (crop marks) ░▒▓     │  │  MM PER MODULE     │  │
│ Family name        │                                  │  │                    │  │
│ [D'Souza      ]    │      └ ┘                  └ ┘    │  │  ! Marginal        │  │
│                    │                                  │  └────────────────────┘  │
│ Company      24 B  │  ▐█▐█▐█▐█▐█▐░░░░░░│░░░░╎          │                          │
│ [Sunrise Phys.]    │  CONTROL STRIP  ▲ceiling ╎ghost   │  Payload    250 / 362 B  │
│                    │                                  │  ECC        M (no logo)  │
│ Job title    18 B  ├──────────────────────────────────┤  Version    14 · 71 mod  │
│ [Physiothera..]    │ 1:1  ⌕ 100%  Q  ▦   25 mm × 300dpi│  Width      25 mm        │
│                    ├──────────────────────────────────┤  Min safe   34 mm  [use] │
│ ▾ REACH            │                                  │                          │
│ Mobile        ●    │                                  │ REMEDIES                 │
│ [+91 98765...]     │                                  │ Widen to 34 mm           │
│                    │                                  │   0.46 mm · ✓ Safe       │
│ Work phone   29 B  │                                  │ ─────────────────────    │
│ [+91 22 1234..]    │                                  │ Remove address  +78 B    │
│                    │                                  │   0.41 mm · ✓ Safe       │
│ Email        45 B  │                                  │ ─────────────────────    │
│ [meera@sun...]     │                                  │ Remove note     +47 B    │
│                    │                                  │   0.38 mm · ! Marginal   │
│ ▸ ADDRESS   78 B   │                                  │                          │
│ ▸ EXTRA     31 B   │                                  │ ▾ BRANDING               │
│                    │                                  │ ▾ EXPORT                 │
│                    │                                  │                          │
│                    │                                  │  [   Export PNG…   ⏎ ]   │
├────────────────────┴──────────────────────────────────┴──────────────────────────┤
│ ● Offline · no network access          library.db · %APPDATA%       250 B · v14  │  28
└──────────────────────────────────────────────────────────────────────────────────┘
     340 fixed              flexible ≥ 480                    340 fixed
```

#### Sections and hierarchy

| Region | Width | Contents | Scroll |
|---|---|---|---|
| **Header** | full, 56 | Back to Library · client identity · save state · Settings | never |
| **Form pane** | 340 fixed | Contact fields in four collapsible groups: Identity, Reach, Address, Extra | vertical |
| **Canvas** | flexible, min 480 | Preview with crop marks · **control strip** flush at its lower edge · canvas toolbar below | never |
| **Right rail** | 340 fixed | **N1 Scannability** (fixed, never collapses) · N2 Branding · Export · primary action | vertical, below N1 |
| **Status bar** | full, 28 | Offline indicator · library path · live payload and version | never |

#### Hierarchy, in the order the eye should take it

1. **The hero readout** — `0.34` at 40 px Bahnschrift, with `MM PER MODULE` beneath at 11 px tracked. Highest contrast, largest type, top of the rail.
2. **The verdict** — glyph, word, colour, immediately below the number, sharing its container.
3. **The preview** — largest *area*, but deliberately lower contrast than the hero because the mid-grey canvas is quiet by design.
4. **The control strip** — the one piece of visual signature, at the canvas edge.
5. **Remedies** — only present when relevant. When the verdict is Safe this region is empty and the rail is calm.
6. **Everything else** — form fields, branding, export settings, at body weight.

#### The primary action, and where it sits

**`Export PNG…` is the only filled `accent.base` button on the screen**, pinned to the bottom of the right rail — **directly below the Scannability panel and the remedies**.

This placement is the argument. **The pointer cannot travel to the export button without the eye crossing the verdict**, and the keyboard cannot reach it without tabbing through the budget region (§10). Putting it in the header, or bottom-left near the form, would let Nikhil export without ever looking at the number the app exists to compute. P1 is a layout constraint here, not a styling one.

When the verdict is **Will fail**, the button stays visible and enabled — it opens D1, where the gate is stated. It is never a dead control with no explanation.

#### Components used

`AppHeader` · `SaveStateChip` · `FieldGroup` · `FieldRow` · `ByteCostChip` · `PhoneField` · `PreviewCanvas` · `CropMarks` · `ControlStrip` · `CanvasToolbar` · `MeasurementReadout` · `VerdictBadge` · `StatRow` · `RemedyItem` · `CollapsibleSection` · `Button` · `StatusBar` · `OfflineIndicator`

---

### 6.2 V1 — Library

```
┌──────────────────────────────────────────────────────────────────────┐
│  ContactQR                                    [Settings]  [Ctrl+H]   │
├──────────────────────────────────────────────────────────────────────┤
│  ⌕ [Search clients, companies, email        ]        [+ New client]  │
├──────────────────────────────────────────────────────────────────────┤
│  CLIENT                    COMPANY              MODIFIED   EXPORTED  │
├──────────────────────────────────────────────────────────────────────┤
│  Meera D'Souza             Sunrise Physio       2 days     12 Aug ⋯  │
│  Rajesh Kumar              Kumar Electricals    1 week     —      ⋯  │
│  Anita Fernandes           AF Interiors         3 weeks    28 Jul ⋯  │
├──────────────────────────────────────────────────────────────────────┤
│  ● Offline · no network access      42 clients      library.db       │
└──────────────────────────────────────────────────────────────────────┘
```

**Purpose.** Get to a client fast, or start a new one. Nothing else.

**Hierarchy.** Search field first and auto-focused — J2's 15-second budget is spent almost entirely here. Then the list. Then, quietly, **New client** as a secondary button, because opening an existing client is the more frequent action by roughly four to one on a reprint-heavy book.

**Default sort:** last modified, descending (FR-7.4) — the returning-client case surfaces without a query.

**Row menu (⋯):** Duplicate · Export log · Reveal last export · Delete. **Duplicate** is placed first because FR-7.5 exists for a specific recurring job — three partners at one firm.

**No card grid.** A table with four columns is denser, sortable, and scannable by a person who knows what he is looking for. A card grid would show eight clients where a table shows thirty, and would imply the records are visual objects when they are contact data.

**Components:** `SearchField` · `DataTable` · `ClientRow` · `RowMenu` · `Button` · `StatusBar` · `EmptyState`

---

### 6.3 D1 — Export PNG

**Layout.** Single column, 480 wide. Fields top to bottom in the order Nikhil decides them: **Width (mm) → DPI → Quiet zone → Folder → Filename**. Physical width leads because it is the primary control (FR-6.2); pixel dimensions render beneath it as read-only derived text: `295 × 295 px · module 4 px`.

**A verdict strip repeats at the top of the dialog** — the same `VerdictBadge` and mm figure from N1. The dialog must not become a place where the gate is out of sight, and changing width inside D1 changes the verdict live.

**Primary action:** `Export PNG`. Disabled with an inline reason when the gate blocks. Beside it, `Print test sheet…` as a ghost button — because the moment before export is exactly when physical verification is worth offering.

**Footer confirmation, not a toast.** On success the dialog footer becomes the confirmation: path, file size, and **Open containing folder**. The dialog stays open so a second size can be exported immediately, which is common for a client who wants both a card and a window decal.

**Components:** `Dialog` · `VerdictBadge` · `MeasurementReadout` · `NumericField` · `SegmentedControl` · `PathField` · `FilenameField` · `Button` · `InlineConfirmation` · `ProgressRow`

---

### 6.4 D2 — Unsafe export confirmation

**Layout.** 440 wide, deliberately plain. No icon, no illustration.

1. **Title:** `Export a code that will probably fail?`
2. **The specific numbers**, not a generic warning: `0.24 mm per module at 22 mm. Below about 0.30 mm, phone cameras stop reading reliably off print.`
3. **What will happen if you continue:** the file is written with `_UNSAFE` in its name and the override is recorded in the export log.
4. **Acknowledgement checkbox**, unchecked: `I understand this code is likely to fail when printed at this size.`
5. **Actions:** `Cancel` (secondary, focused by default) · `Export unsafe code` (destructive styling, disabled until the box is checked).

**Three deliberate frictions.** The confirm button is not default-focused; `Enter` does not activate it; its label says `Export unsafe code`, not `Continue`. FR-4.5 and metric M7 both depend on this gate being believed, and a gate dismissible by muscle memory is equivalent to no gate.

**Components:** `Dialog` · `AcknowledgementCheckbox` · `Button (destructive)` · `StatList`

---

### 6.5 V3 — Export log

**Layout.** Full-width table, newest first, filterable by client. Columns: **Date · Client · File · Width · Module · ECC · Version · Verdict · Self-test**.

**Overridden rows carry the violet `verdict.override` marker** in a leading gutter column, so a scan down the page separates "I chose this" from "this was fine."

**Row expansion reveals the `vcard_snapshot`** in `type.mono` — the exact bytes encoded at the time (PRD §7). This is the single most useful field in the product for support, and it is two clicks from any client.

**Components:** `DataTable` · `VerdictBadge (compact)` · `PayloadViewer` · `FilterBar` · `EmptyState`

---

### 6.6 D4 — Settings

Five tabs, vertical on the left at 180 wide: **General · Defaults · Library · Offline & Privacy · About**.

- **General** — theme, UI density (Compact / Default / Large), default country code for phone normalisation.
- **Defaults** — export presets (CRUD), default preset, filename template with a live example rendered beneath it.
- **Library** — database location with a warning if it sits inside a sync root (EC-19), export/import JSON, hard-delete maintenance.
- **Offline & Privacy** — the plain-language statement (FR-8.6), the verification method and its recorded result for this build, and the "disable your adapter and keep working" instruction. **Written to be read aloud to a client.**
- **About** — version, bundled font licences, third-party library licences and their network audit result (FR-8.3).

---

## 7. Component library

Every component lists variants and the states it must implement. `focus` throughout means the §10 double-ring treatment.

### 7.1 Signature and measurement

#### `ControlStrip` — *the signature component*

A flush, square-cornered band of segmented patches at the canvas's lower edge, 20 px tall (28 in Large density), full canvas width.

- **Anatomy:** segments of 4 px with 1 px gaps, filling left to right in proportion to payload bytes against capacity. A **solid 2 px tick** marks the ceiling at the current ECC level. A **hollow tick** marks the ceiling that would apply with the logo removed. Below, in `type.eyebrow`: `250 / 362 B` at the left, `CEILING M` and `GHOST H` at their respective ticks.
- **Variants:** `full` (canvas) · `compact` (D1 header, 12 px, no labels)
- **States:**
  - `safe` — fill in `text.secondary`, ceiling tick in `border.strong`
  - `marginal` — fill in `verdict.marginal.text` for the final 15% of segments
  - `over` — fill continues **past** the ceiling tick in `verdict.fail.text`; the overflow segments are visually distinct so "how far over" is readable, not just "over"
  - `preview` — while a remedy is hover- or focus-previewed, a ghosted fill shows where the payload *would* land
  - `noLogo` — the ghost tick is hidden, since it would coincide with the ceiling
  - `empty` — no payload yet; strip renders as an unfilled track, not hidden

**Why this and not a progress bar.** A progress bar says how far along you are. This says how much room is left, where the wall is, and **where the wall would move if you dropped the logo** — three facts a bar cannot carry. It is also the artifact Nikhil already reads on a press sheet.

#### `MeasurementReadout`

Large figure + unit + label. Used for the hero mm/module and, at `sm`, in D1.

- **Variants:** `hero` (40 px Bahnschrift) · `md` (22 px) · `sm` (15 px inline)
- **States:** `default` · `safe` · `marginal` · `fail` (figure takes the verdict colour) · `stale` (dimmed to `text.tertiary` while a re-render is pending — visible only in the rare case that debounce is exceeded) · `unavailable` (`—` when required fields are missing)
- Tabular figures mandatory. Never animates (§3.10).

#### `VerdictBadge`

- **Variants:** `lg` (rail, glyph + word + wash background) · `compact` (table cell, glyph + word) · `inline` (remedy rows, glyph + word at `type.label`)
- **States:** `safe` · `marginal` · `fail` · `overridden` · `pending` (self-test running) · `unavailable`
- Always renders glyph **and** word. There is no icon-only variant, at any size. This is enforced at the component level rather than left to usage.

#### `ByteCostChip`

The inline cost marker beside a field label (P2).

- **Variants:** `field` (beside a label) · `group` (on a collapsed section header, summing its children) · `delta` (`+78 B` in remedies, showing recovery)
- **States:** `neutral` (< 10% of remaining capacity) · `costly` (10–25%, `text.primary`) · `critical` (> 25% or would exceed capacity, `verdict.marginal.text`) · `recovering` (green, used only in delta form)

#### `BudgetStatRow`

Label-value pair for Payload / ECC / Version / Width / Min safe. Values in tabular figures, right-aligned to a shared edge so the column reads as a spec sheet.

- **States:** `default` · `derived` (value is computed, rendered `text.secondary`) · `forced` (ECC when a logo locks it to H — carries a lock glyph and a `why?` info popover) · `actionable` (Min safe, carries an inline `use` button)

#### `RemedyItem`

- **Anatomy:** action text · `ByteCostChip (delta)` · resulting module size · `VerdictBadge (inline)`
- **Variants:** `field-removal` · `logo-removal` · `width-change` · `ecc-change` · `url-shorten`
- **States:** `default` · `hover/focus` (previews on canvas, strip and hero — commits nothing) · `applied` (struck action, `Undo` present) · `unavailable` (would not help; shown greyed with a reason rather than hidden, so the list does not reshuffle unpredictably)

### 7.2 Canvas

#### `PreviewCanvas`

- **Variants:** `fit` · `1:1` (true physical size, DPI-aware) · `zoom` (25–400%)
- **States:** `empty` (required field missing — names the field) · `rendering` · `ready` · `selfTestRunning` · `selfTestFailed` (canvas dims, error takes over — FR-4.4, EC-18) · `previewingRemedy` (ghosted alternate)
- **Overlays, each independently toggled:** quiet-zone boundary (`Q`) · module grid at high zoom (`G`) · logo keep-out (automatic while dragging)
- **Never:** shadow, rounded corners, transparency checkerboard, background other than `canvas.field`.

#### `CropMarks`

Four hairline L-shapes in `border.strong`, offset 8 px from the artwork's trim edge. Decorative in function but semantically correct — they mark the trim, which is what the QR's outer boundary is.

#### `CanvasToolbar`

Below the canvas: `1:1` toggle · zoom · quiet-zone toggle · grid toggle · a read-only summary `25 mm × 300 dpi`. Ghost buttons only — nothing here competes with the primary action.

### 7.3 Form

#### `FieldRow`

The workhorse. Label row (label · required dot · `ByteCostChip`), input, helper/error row.

- **Variants:** `text` · `phone` · `email` · `url` · `multiline` · `select`
- **States:** `empty` · `focused` · `filled` · `normalised` (shows what it became, e.g. E.164 — with an `undo` affordance, since FR-1.4 forbids silent guessing) · `advisory` (amber, non-blocking — FR-1.6) · `blocking` (red, required field empty) · `disabled` · `readOnly` (degraded mode)

#### `PhoneField`

`FieldRow` plus a country-code prefix control and the E.164 normalisation notice. Additional state: `ambiguous` — cannot infer a country code, prompts rather than guesses (EC-6).

#### `FieldGroup`

Collapsible section with an eyebrow header and a `ByteCostChip (group)` summing its children — so a collapsed **Address** group still shows `78 B`. Collapsing must never hide a cost.

- **States:** `expanded` · `collapsed` · `collapsedWithError` (cannot be collapsed while it contains a blocking error)

#### `AcknowledgementCheckbox`

Used only in D2. Larger hit target than a standard checkbox, label is a full sentence, and it gates a specific button. Never pre-checked, never remembered between sessions.

### 7.4 Branding

#### `ColorField`

Swatch · hex input · eyedropper (screen-picker, local only) · recent-colours row.

- **States:** `default` · `focused` · `invalidHex` · `lowContrast` (amber advisory) · `blocked` (red — below the FR-5.7 threshold, export gated) · `inverted` (red — light-on-dark, blocked outright per FR-5.8, with the light-patch workaround stated inline)

#### `ContrastReadout`

Measured ratio, threshold, verdict, and the nearest compliant colour as a one-click suggestion. Numeric first — this is the accessible answer for a colour-blind operator judging a client palette, and it is also simply the correct one.

#### `LogoControl`

- **States:** `off` (shows the cost of switching on, before switching on — J4 step 1) · `choosing` · `placed` · `oversize` (18–25%, amber) · `blocked` (> 25%, or intruding on a keep-out zone) · `missing` (dangling path — EC-20, offers **Relink**) · `unsupported` (EC-17, names the file)

#### `EccSelector`

Segmented control, L / M / Q / H.

- **States:** `default` · `forced` (locked to H by a logo, lock glyph, `why?` popover) · `warned` (L selected — unsuitable for handled or laminated stock)

### 7.5 Structure and system

- **`Button`** — variants `primary` (one per screen), `secondary`, `ghost`, `destructive`; states `default · hover · pressed · focus · disabled (with inline reason) · loading`. Disabled buttons **always** carry a reason nearby; a dead control with no explanation is forbidden.
- **`SegmentedControl`** — ECC, DPI, density. States `default · hover · selected · focus · disabled · partiallyDisabled` (an option unavailable in context carries its reason in a tooltip).
- **`NumericField`** — unit suffix (`mm`, `dpi`, `%`), spinner, scrub-to-adjust on the label. States `default · focused · outOfRange · derived (read-only) · warned`.
- **`DataTable` / `ClientRow`** — states `default · hover · selected · focused · contextMenuOpen · deleting (soft-delete undo window)`.
- **`SearchField`** — states `empty (placeholder names the searchable fields) · typing · results · noResults`.
- **`StatusBar`** + **`OfflineIndicator`** — see §8.
- **`Banner`** — variants `info · warning · error · conflict`; used for S2 and S3. Persistent, dismissible only where dismissal is safe.
- **`Dialog`** — sizes `sm 440 · md 480 · lg 720`; states `entering · open · busy (actions disabled, progress shown) · confirming`.
- **`PayloadViewer`** — line-numbered mono text, copy button, byte ruler in the gutter marking where the current line sits in the total. States `default · copied · truncated`.
- **`InlineConfirmation`** — replaces toasts throughout. Appears in the footer of the surface that performed the action, persists until dismissed or superseded. **There is no toast system in this product**: a toast that auto-dismisses is unreadable at a glance and trains dismissal, which §2 argues against directly.
- **`InfoPopover`** — the `why?` affordances on forced ECC, min-safe-width, and contrast. Keyboard-reachable, `Esc`-dismissible, never hover-only.
- **`EmptyState`** — title, one line of direction, one action. No illustration.
- **`ProgressRow`** — determinate where the total is known (batch, test sheet), indeterminate only for the self-test.

---

## 8. States

### 8.1 The offline state is inverted — read this first

Every other application in Nikhil's life treats "offline" as degradation: a grey cloud, a warning triangle, a retry button. **Here it is the product promise being kept**, and styling it as a fault would undermine the one claim we most need him to repeat to clients (FR-8.6, US-14).

| | Conventional | **ContactQR** |
|---|---|---|
| Glyph | Struck-through cloud | Filled dot, `verdict.safe.text` |
| Copy | "You're offline" | `Offline · no network access` |
| Tone | Warning | Confirmation |
| Action | Retry | Opens Settings → Offline & Privacy |
| When network is *available* | Normal | **Still reads exactly the same.** The app does not know or care. |

The indicator's value is that it never changes. If it could flicker to "online," it would imply a connection exists that we might use. It cannot, so it does not. **There is no offline empty state, no offline error state, and no reconnecting state anywhere in this product** — those states are absent by design, and their absence is the demonstration in J7.

### 8.2 V1 — Library

| State | Treatment |
|---|---|
| **Empty (first run, S1)** | Centred `EmptyState`: `No clients yet` / `Add your first client to generate a QR code.` / **[+ New client]**. No illustration, no tour, no sample data — sample records in a client list are a liability, not a welcome. |
| **Loading** | Local SQLite read on a warm cache is sub-frame. **No spinner.** If the read exceeds 200 ms (large library, cold disk, network share) a skeleton of six rows appears — matching row height exactly so nothing shifts when data lands. |
| **No search results** | In-list: `No clients match "kumr"` + `Clear search`. The list frame stays; only the body changes, so the search field never moves under the cursor. |
| **Error — library unreadable (S2, EC-21)** | Persistent red `Banner` above the list: what happened, where the file is, where the most recent JSON backup is, and **[Open backup folder]**. The app stays usable in read-only mode. **The damaged file is never auto-repaired or truncated.** |
| **Error — locked by another instance (EC-22)** | Amber `Banner`: `Another copy of ContactQR has this library open.` + **[Switch to it]** + **[Open read-only]**. Two writable views of one database are never presented. |
| **Sync conflict (S3, EC-19)** | Amber `Banner` naming the detected conflict copies, with **[Show files]**. Surfaced, never silently ignored. |
| **Success** | Deliberately silent. Saving a client returns to the list with the row updated and briefly held in `surface.selected`. No confirmation for an expected outcome. |
| **Offline** | Status bar only, per §8.1. |

### 8.3 V2 — Editor

| State | Treatment |
|---|---|
| **Empty — required fields missing** | Canvas shows a locked state naming the specific blocker: `Add a given name and a mobile number to generate a code.` The hero readout shows `—`, the verdict shows `unavailable`, the control strip renders as an unfilled track. **The panel structure never collapses or hides** — P1 holds even before there is a code, so the layout does not jump when the first valid state arrives. |
| **Loading — re-render** | Effectively instant. During the 250 ms debounce the canvas holds the *previous* code and the hero dims to `stale`. It never blanks — a flashing canvas on every keystroke would make the density judgement impossible. |
| **Loading — self-test** | `VerdictBadge` → `pending`, canvas → `selfTestRunning`, export disabled with `Verifying…`. Indeterminate progress; the only genuinely indeterminate operation in the app. |
| **Error — over capacity (EC-1)** | Generation blocked. Control strip shows overflow past the ceiling. Hero shows the module size it *would* be. Remedies populate, `Remove logo` ranked first when a logo is present. **Never silently drops a field to fit.** |
| **Error — self-test failed (EC-18)** | The most serious in-app error: we produced something we cannot verify. Canvas dims, red `Banner` takes the rail: `This code did not decode correctly after rendering. Export is blocked.` + diagnostic details + **[Copy diagnostics]**. Export is unavailable and **cannot be overridden** — FR-4.5's override applies to the *size* gate, never to a failed self-test. |
| **Error — colour blocked (EC-14, EC-15)** | `ColorField` → `blocked`, `ContrastReadout` shows measured ratio against threshold, nearest compliant colour offered one-click. For inversion, the light-patch workaround is stated in the same block. |
| **Warning — marginal** | Hero and verdict take `verdict.marginal`. Remedies present. Export permitted. Nothing is blocked and nothing is nagged twice. |
| **Success — Safe** | The rail goes quiet: remedies region empty, verdict green, hero green. **The reward for a good code is an absence of interface.** |
| **Success — exported** | `InlineConfirmation` in D1's footer with path, size and **[Open containing folder]**. No toast, no animation, no sound. |
| **Read-only (S2)** | Every input `readOnly`, primary action disabled with the reason, banner persists. Preview and export of already-valid records remain available where safe. |

### 8.4 D1 — Export

| State | Treatment |
|---|---|
| **Empty** | Not reachable — D1 cannot open without a generated code. |
| **Loading** | `busy`: fields disabled, `ProgressRow` shows `Rendering → Verifying → Writing` as three determinate steps. Naming the steps matters when the self-test adds a perceptible pause; an unlabelled spinner reads as a hang. |
| **Error — gate blocks (J5)** | Export button disabled, reason inline above it, **[Fix it]** and **[Export anyway…]** offered. The gate is stated on open, never sprung after a click. |
| **Error — path unwritable (EC-23)** | Distinct message per cause: read-only location, disconnected share, disk full. `Export failed` alone is not acceptable copy when the disk is full. |
| **Error — filename collision (EC-24, FR-6.8)** | Inline: Overwrite / Rename / Cancel. Never silent. |
| **Success** | Footer `InlineConfirmation`. Dialog stays open for a second size. |
| **Offline** | No difference. Export is a local file write. |

### 8.5 V3 — Export log

| State | Treatment |
|---|---|
| **Empty** | `No exports yet` / `Exported PNGs will be listed here with the exact data they encoded.` — states the value, which is what makes it worth opening later. |
| **Loading** | Skeleton rows past 200 ms. |
| **Error** | Log unreadable → banner; the log is append-only and never blocks the rest of the app. |
| **Success / offline** | No distinct treatment. |

---

## 9. Responsive behaviour

**The brief asked for mobile, tablet and desktop. This product has no mobile or tablet target** — PRD §3 lists macOS, Linux, web and mobile as explicit non-goals, and the acceptance user's phone runs a *stock camera*, not our software. Specifying phone layouts would design a product we have agreed not to build.

The real adaptive problem is a resizable desktop window across **window widths, Windows DPI scale factors, and Nikhil's two monitors**. That is what follows. Flagged in §11 Q1.

### 9.1 Window breakpoints

Minimum window **1100 × 720**. Below that, the window will not resize further — P1 cannot be honoured in less.

| Breakpoint | Width | Layout |
|---|---|---|
| **Compact** | 1100–1365 | Form pane 300 · canvas flexible · **right rail collapses to 280 and Branding/Export become a tabbed stack under Scannability**. The Scannability panel itself never collapses. |
| **Standard** | 1366–1919 | The common laptop. Form 340 · canvas flexible · rail 340. The reference layout in §6.1. |
| **Comfortable** | 1920–2559 | Form 380 · rail 360 · **all growth goes to the canvas**, because the preview is the only element that benefits from area. |
| **Wide** | 2560+ | Panes stop growing; the canvas caps at 1200 and centres, with `surface.window` gutters. An unboundedly large QR preview is not more informative and would break the 1:1 mental model. |

Below **900 px height**, the form pane's group headers become sticky so field labels never scroll out of reach of their inputs.

### 9.2 Windows DPI scaling — the real constraint

The app must be **per-monitor DPI aware v2**, not system-aware. Nikhil drags this window between two monitors that may run different scale factors, and a stale scale factor breaks P3 directly.

| Scale | Requirements |
|---|---|
| **100%** | Reference. Hairlines are exactly 1 device pixel. |
| **125% / 150% / 175%** | The 4 px spacing unit yields integers (5 / 6 / 7). Hairlines must snap to device pixels — `UseLayoutRounding` and `SnapsToDevicePixels` on every panel boundary, or the control strip's segment gaps blur and it stops reading as a measuring device. |
| **200%** | Density setting should default to Comfortable. All iconography is vector; no raster assets at any scale. |
| **Mixed-DPI drag** | On `DpiChanged`, **the 1:1 preview must recompute from the new monitor's actual DPI**. If it does not, dragging the window to the second monitor silently makes the 1:1 view a lie — the single worst P3 violation available, because it is invisible. |

**The QR preview never uses fractional module scaling.** Per FR-3.7, module size in the preview bitmap is a whole number of device pixels at every DPI; the canvas rounds the rendered size down to the nearest whole multiple rather than scaling a smaller bitmap up. Grey edge pixels in the preview would misrepresent print quality in exactly the direction that causes reprints.

### 9.3 UI density

Three settings, honouring the Windows text-scaling setting on top (§10):

| Density | Base type | Row height | Panel padding | For |
|---|---|---|---|---|
| **Compact** | 12 | 24 | 12 | 1366×768 laptops, or a large library |
| **Default** | 13 | 28 | 16 | Reference |
| **Large** | 15 | 34 | 20 | 200% scaling, or reduced vision |

Density changes type and spacing tokens only. **The hero readout stays at 40 px in every density** — P1 does not scale down.

### 9.4 Dual monitor

Grounded in the persona, not speculation: Nikhil has two monitors and works with InDesign on one.

- **Tear-off preview window.** The canvas can be undocked to a second monitor (`Ctrl+Shift+D`), leaving the form and rail on the primary. The undocked window carries **the hero readout, verdict and control strip with it** — P1 travels with the preview, because a preview without its verdict is precisely the comfortable lie P3 forbids.
- Window position, monitor assignment and dock state persist per machine.
- The tear-off window has no chrome beyond a title bar and the canvas toolbar, so it can sit beside an InDesign document without competing.

---

## 10. Accessibility

WPF exposes accessibility through **UI Automation**, not ARIA. Each ARIA need below is given its UIA equivalent, which is what will actually be implemented and tested with Narrator and NVDA.

### 10.1 Contrast — measured, not asserted

All ratios in §3.1–3.3 are computed against their stated background and are build requirements, verified in CI by a token-level contrast test rather than by eye.

| Requirement | Target | Status |
|---|---|---|
| Body text (`text.primary` on `surface.panel`) | ≥ 4.5:1 | **13.0:1** ✓ AAA |
| Labels and units (`text.secondary`) | ≥ 4.5:1 | **6.7:1** ✓ |
| Placeholders (`text.tertiary`) | ≥ 4.5:1 | **4.8:1** ✓ |
| Verdict text, all three | ≥ 4.5:1 | 6.2 / 8.0 / 5.2:1 ✓ |
| White on `verdict.fail.fill` | ≥ 4.5:1 | **4.7:1** ✓ |
| Accent text and primary button | ≥ 4.5:1 | **6.1:1** ✓ |
| Focus ring (non-text) | ≥ 3:1 | **8.9:1** ✓ |
| Control borders (non-text) | ≥ 3:1 | 3.1:1 ✓ |
| Hero readout at 40 px | ≥ 3:1 (large text) | ≥ 6.2:1 in every verdict state ✓ |

**Deliberately excluded from the contrast contract: the client's own QR colours.** Those are the client's brand, and the app's job is to *measure* them (`ContrastReadout`) and block what will not scan (FR-5.7, FR-5.8) — not to restyle them. Our contrast obligations end at our chrome.

### 10.2 Colour independence

No information is carried by colour alone, anywhere.

- Verdicts render **glyph + word + colour**, enforced in the component (§7.1) rather than left to each usage.
- The control strip encodes overflow by **position past the ceiling tick**, not only by hue.
- Overridden export-log rows carry a **gutter marker and the word `Overridden`**, not just violet.
- `ByteCostChip` severity is carried by the **number itself**, which is the actual information.
- `ContrastReadout` leads with a **numeric ratio** — the accessible answer for an operator judging a palette he may not perceive as we do.

### 10.3 Focus order

Focus order in the Editor mirrors the visual hierarchy, and this is a P1 requirement rather than a convention: **the budget region sits in the tab path before the export button**, so a keyboard user's route to export crosses the verdict exactly as a mouse user's eye does.

**V2 Editor tab ring:**

1. Header — back to Library, client name, Settings
2. **Form pane** — groups in order, fields within groups; a collapsed group is one stop, expanding on `Enter`/`Space`
3. **Canvas toolbar** — 1:1, zoom, quiet zone, grid
4. **Scannability panel** — hero readout (focusable, exposes its full reading to screen readers), verdict, stat rows, `use` on Min safe
5. **Remedies** — each row one stop; focus previews, `Enter` applies
6. **Branding** — logo, colours, ECC
7. **Export settings**
8. **`Export PNG…`** — last
9. Status bar — offline indicator (focusable, opens Settings)

- `F6` / `Shift+F6` cycles panes (Form → Canvas → Rail), the Windows convention for multi-pane applications.
- **Dialogs trap focus** and restore it to the invoking control on close.
- **D2 focuses `Cancel`**, never the confirm button (§6.4).

### 10.4 Keyboard

Every action is reachable without a mouse — including logo placement, which is the one place a mouse feels mandatory.

| Key | Action |
|---|---|
| `Ctrl+N` / `Ctrl+L` / `Ctrl+H` | New client · Library · Export log |
| `Ctrl+F` | Focus search |
| `Ctrl+S` | Save client |
| `Ctrl+E` | Export |
| `Ctrl+P` | Print test sheet |
| `Ctrl+Shift+P` | Payload inspector |
| `Ctrl+Shift+D` | Dock / undock preview |
| `Ctrl+1/2/3` | Focus form / canvas / rail |
| `F6` | Cycle panes |
| `1` | Toggle 1:1 view |
| `Q` / `G` | Quiet-zone / module-grid overlay |
| `Ctrl+Z` | Undo, including applied remedies |
| `Esc` | Close dialog, clear search, cancel logo drag |
| `Enter` (search) | Open top match |

**Logo placement by keyboard:** arrow keys nudge 1 px (`Shift+Arrow` 10 px), `+`/`-` resize by 1% within the 25% cap, and a keep-out collision is announced rather than silently refused.

**No keyboard trap anywhere.** The payload viewer uses `Ctrl+A`/`Ctrl+C` and releases `Tab`.

### 10.5 UI Automation — the ARIA equivalents

| Need | ARIA (web) | **WPF implementation** |
|---|---|---|
| Name an icon-only control | `aria-label` | `AutomationProperties.Name` on every ghost/icon button |
| Associate label with input | `aria-labelledby` | `AutomationProperties.LabeledBy` on every `FieldRow` input |
| Describe a control | `aria-describedby` | `AutomationProperties.HelpText` — carries byte cost and the mm/module explanation |
| Announce live change | `aria-live="polite"` | `AutomationProperties.LiveSetting="Polite"` on the byte counter and hero readout |
| Announce urgent change | `aria-live="assertive"` | `LiveSetting="Assertive"` on the verdict — **only on transition into `fail`** |
| Expose a meter | `role="meter"` | Custom `AutomationPeer` on `ControlStrip` implementing **`IRangeValueProvider`**: `Minimum=0`, `Maximum=capacity`, `Value=payloadBytes`, `IsReadOnly=true` |
| Group related fields | `role="group"` | `AutomationProperties.Name` on each `FieldGroup` container |
| Modal semantics | `aria-modal` | `Window.ShowDialog` + `AutomationProperties.IsDialog` |
| Invalid input | `aria-invalid` | `Validation.HasError` + `HelpText` naming the fix |
| Required field | `aria-required` | `AutomationProperties.IsRequiredForForm` |

**Announcement discipline — the specific risk here.** The byte counter changes on every 250 ms debounce. Announcing each change would make the Editor unusable with a screen reader. Therefore:

- The byte counter is `Polite` and **announces only when the tab focus is inside the budget region**, not while typing in the form.
- The verdict announces **only on transition** (Safe → Marginal → Will fail), never on every recompute.
- On entering `fail`, the assertive announcement is a full sentence: *"Will fail. 0.24 millimetres per module at 22 millimetres. Three remedies available."*
- The hero readout exposes a spoken form distinct from its visual one: `AutomationProperties.Name = "0.34 millimetres per module"`, not `"0.34"`.

### 10.6 Windows platform accessibility

- **High Contrast themes.** When Windows HC is active, all tokens remap to `SystemColors` brushes. Verdicts fall back to `SystemColors.HotTrack` / `GrayText` / `SystemColors.Highlight` **plus their glyph and word**, which is exactly why §10.2 forbids colour-only encoding — the design survives HC without a special case.
- **Windows text scaling** honoured to **200%**. Layout reflows; it must not clip or truncate. Verified at 1366×768 @ 200% and 175%, the worst case.
- **Reduced motion** — `SystemParameters.ClientAreaAnimation` suppresses all motion tokens (§3.10). The verdict already never animates.
- **Narrator and NVDA** are both part of the acceptance pass. Testing only with Narrator misses the majority of real screen-reader users.
- **Focus ring: double-ring.** A 2 px `accent.focus` stroke plus a 1 px `surface.window` offset stroke, so focus stays visible on graphite chrome, on the mid-grey canvas, and on a user's own light-coloured QR background alike. A single ring would vanish against at least one of the three.
- **Minimum hit target 28 × 28** at Default density, **32 × 32** at Large. Below Windows' nominal guidance in places, deliberately, for instrument density — but never for a destructive or gating control, where 40 × 40 is the floor. `AcknowledgementCheckbox` in D2 is the largest interactive target in the product.

### 10.7 Colour-vision consideration for the operator's actual job

A distinct problem from UI accessibility: **a colour-blind operator choosing a client's brand colours cannot judge QR contrast by eye**, and this app's whole purpose is to remove that kind of guesswork.

- `ContrastReadout` is numeric and always visible — not on hover, not in a tooltip.
- Blocked colours state the measured ratio, the threshold, and the nearest compliant colour as a one-click fix.
- *Proposed for v2, flagged rather than assumed:* a colour-vision-deficiency simulation toggle on the canvas (deuteranopia / protanopia / tritanopia). Genuinely useful for judging whether a client's palette holds up, but it is a new rendering path with its own correctness surface and it does not belong in v1. See §11 Q5.

---

## 11. Open questions and flagged assumptions

Ordered by cost of a late answer.

| # | Question | Why it matters | Owner |
|---|---|---|---|
| **Q1** | **§9 was requested as mobile / tablet / desktop; I have delivered window breakpoints, DPI scaling and dual-monitor instead.** Is that the right reading? | The PRD makes mobile and web explicit non-goals, and the acceptance user scans with a *stock camera*, not our software. Designing phone layouts would specify a product we agreed not to build. If a companion mobile app is actually wanted, that is a PRD change first, not a design decision. | Product |
| **Q2** | **§10 was requested as ARIA; I have specified UI Automation.** Confirm. | WPF has no ARIA. Specifying `aria-*` attributes would produce a document that cannot be implemented. UIA is the real equivalent and is what Narrator and NVDA consume. | Engineering |
| **Q3** | **Graphite-dark default, or light default?** | I have argued dark from Adobe-CS context and eye fatigue during density judgement. But if Nikhil proofs against white stock on screen, light may be the honest default. The canvas stays `#8C8C8C` either way — that part is not negotiable. Worth ten minutes of watching him work. | Operator |
| **Q4** | **Does Bahnschrift have true tabular figures?** | It is the display face for all measurement readouts (§3.5), chosen partly because it ships with Windows and needs no bundling. If its figures are proportional, live-updating numerals will jitter and Bahnschrift must be confined to static display text, with the hero falling back to Plex Sans SemiBold + `tnum`. Verify before any UI work. | Engineering |
| **Q5** | **Colour-vision-deficiency simulation on the canvas — v2 or never?** | Useful for judging a client palette, and arguably more useful to this operator than to most. But it is a second rendering path with its own correctness surface, adjacent to a product whose central claim is that its rendering is verified. Proposed for v2, not v1. | Product |
| **Q6** | **Does the tear-off preview window justify its cost in v1?** | Grounded in the persona's two monitors and it makes P1 travel with the preview. But it is real windowing complexity — per-monitor DPI, state persistence, focus management. Reasonable to defer to v1.1 if the schedule tightens. | Product + engineering |
| **Q7** | **Is 1100 × 720 the right minimum window?** | It is derived from fitting form + canvas + rail without violating P1, not measured. If Nikhil's laptop is 1366 × 768 in practice, the Compact breakpoint is the one that needs the real design attention, not Standard. Answer alongside PRD Q5. | Operator |
| **Q8** | **`type.body` at 13 px with an 11 px floor.** | Deliberate, for instrument density beside Adobe applications, and mitigated by three density settings plus 200% Windows text scaling. But it is below the comfortable default for a 36-year-old and further below it for a 46-year-old. If the Compact density is never used, the base should probably be 14. Revisit after two weeks of real use. | Operator |
