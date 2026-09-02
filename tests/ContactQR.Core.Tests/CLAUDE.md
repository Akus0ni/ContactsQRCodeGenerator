# ContactQR.Core.Tests

xUnit + FluentAssertions over `ContactQR.Core`. 85 tests, all must pass, and the build is
warning-free with warnings-as-errors.

```bash
dotnet test
```

## Conventions

- **`Method_Scenario_ExpectedBehaviour`.** CA1707 forbids underscores in member names and is
  switched off under `tests/` in `.editorconfig` for this reason alone. It stays on in `src/`.
- **One logical assertion per test.** A test that checks three things reports one failure and
  hides two.
- **The name states the behaviour; the body states the case.** Where a test encodes a
  non-obvious *reason*, put it in the name — `Encode_UsesCarriageReturnLineFeed_BecauseBare-
  LineFeedFailsIntermittentlyOnIos` is doing real work that a comment would not.
- `GenerateDocumentationFile` is off here (`tests/Directory.Build.props`); test methods do not
  get XML docs.

## Layout

| File | Covers |
|---|---|
| `Contacts/ClientRecordBuilder.cs` | Test builder. Defaults to the minimum valid record. |
| `VCard/VCardTextEscaperTests.cs` | Escaping, including backslash-first ordering. |
| `VCard/VCardEncoderTests.cs` | Property order, CRLF, omission, address components, guards. |
| `Scannability/QrCapacityTableTests.cs` | Table consistency and known reference values. |
| `Scannability/ScannabilityCalculatorTests.cs` | Version selection, module size, verdicts. |
| `Scannability/PrdReferenceScenarioTests.cs` | The PRD's three reference payloads, pinned. |

## `ClientRecordBuilder`

Defaults to a valid minimal record — given name, family name, one primary mobile — so each
test declares only the field it is actually about. Use `WithoutPrimaryPhone()` to exercise the
required-field guards.

## `PrdReferenceScenarioTests` is load-bearing

It pins the Minimal / Typical / Full payloads the PRD reasons about, and the verdicts they
reach at real card widths. These are the numbers the product's central claim rests on, so they
live in assertions rather than only in prose where they can drift.

**It has already caught one error in the design brief.** The brief projected a version 7 symbol
for a minimal payload and therefore called 22mm safe. The real minimal vCard is 131 bytes,
which needs version 8 at ECC M — 57 modules including the quiet zone, giving 0.386mm at 22mm,
which is Marginal. Even name, company and one phone number is not comfortably safe on a 22mm
card; it needs about 22.8mm.

When these numbers change, change them because a measurement said so, and update `PRD.md` and
`DESIGN.md` in the same commit.
