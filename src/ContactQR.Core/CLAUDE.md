# ContactQR.Core

Pure domain logic: contact records, vCard encoding, and the scannability maths.

**This library has no UI, no file I/O, no database access and no network.** It targets
`net10.0` rather than `net10.0-windows` specifically so that a Windows-only or UI dependency
cannot be added without the target framework change making it obvious in review. Keep it that
way — everything here should be callable from a test with no fixture and no setup.

## What is here

| Namespace | Contents |
|---|---|
| `Contacts` | `ClientRecord`, `ContactPoint`, `PostalAddress`. Immutable records. |
| `VCard` | `VCardEncoder`, `VCardTextEscaper`. Record in, string out. |
| `Scannability` | `QrCapacityTable`, `ScannabilityCalculator`, `ScannabilityAssessment`. |
| `Contacts` (guidance) | `ContactField`, `ContactFieldGuidance`, `ContactFieldGuidanceCatalogue` — tooltip content. |

## VCard — the highest-risk code in the product

This is the part every free online generator gets wrong, and a defect here is invisible until
a client's customer cannot save a number. Treat the following as fixed:

- **CRLF line terminators.** Not LF (FR-2.3).
- **Escape `\`, `;`, `,` and newlines in values** — and escape the backslash first, or you
  double-escape the ones you just introduced. Un-escaped commas are the most common vCard
  defect in the wild: `Acme Interiors Pvt Ltd, Mumbai` parses as two `ORG` components (FR-2.4).
- **Structural delimiters are not escaped.** The comma in `TYPE=WORK,VOICE` is a parameter
  delimiter, not a value. Only property values pass through the escaper.
- **`N` and `FN` are both mandatory** in vCard 3.0. Without `FN`, iOS shows a blank contact
  name (FR-2.2).
- **`ADR` always emits all seven components**, empty ones preserved as consecutive semicolons.
  Dropping a leading empty component shifts every later value into the wrong field (FR-2.7).
- **Empty optional fields are omitted entirely**, never emitted bare. `TITLE:` with no value
  causes visible blank fields in some Android contact apps (FR-1.2).
- **Property order is fixed** so identical input yields a byte-identical QR, which is what
  makes a reprint reproducible (FR-2.8).
- **Lines are deliberately not folded.** This is a conscious deviation from RFC 2426 to save
  scarce bytes; it is believed safe on modern parsers but is unconfirmed on the device matrix
  (FR-2.6, PRD Q9).

`MeasureBytes` counts UTF-8 bytes. Never count characters anywhere in this library.

## Scannability

`ScannabilityCalculator` answers the question the product exists to answer: will this code
survive being scanned off print at this physical size?

```
modules per side  = 17 + 4 × version
total             = modules + 2 × quiet zone
module size (mm)  = printed width ÷ total
```

Two things to know before changing it:

- **Exceeding capacity is a verdict, not an exception.** The interface has to show *how far*
  over budget a payload is, so `ScannabilityVerdict.ExceedsCapacity` plus `OverflowBytes` is
  data. The four verdict members map one-to-one onto the four rendering states of the control
  strip in `DESIGN.md`.
- **Thresholds are injected, not constant.** `ScannabilityThresholds` exists so that the
  calibration PRD M1b requires is a configuration change rather than a code change. The
  current 0.40 / 0.30 values are a published-guidance hypothesis, not this product's own
  measurement.

`QrCapacityTable` holds 160 hand-entered ISO/IEC 18004 values. Its tests check internal
consistency, which catches transcription error but cannot prove correctness — **cross-verify
against the QR encoder before this table gates a real export** (FR-3.3).

## Field guidance

`ContactFieldGuidanceCatalogue` holds the tooltip content for every editor field: what the
field becomes on the recipient's phone, the vCard property it is encoded as, and any known
platform caveat. It lives here rather than in XAML so the mapping is stated once, beside the
encoder that implements it.

**`ConfirmedOnDeviceMatrix` is `false` for every entry and must stay that way until the
physical device matrix has run.** PRD EC-30 and M1a require per-platform behaviour to be
established by measurement, not by reading the vCard specification. The interface presents
unconfirmed guidance as expected rather than measured behaviour. Flipping an entry to `true`
is a deliberate act that follows a device test, never a guess.
