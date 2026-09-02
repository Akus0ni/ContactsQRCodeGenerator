# ContactQR

Offline Windows desktop tool that generates vCard QR codes for printed business cards.
A solo print-and-design operator enters a client's contact details and exports a PNG that,
when scanned with a stock phone camera, opens the native Add Contact sheet with the fields
already filled in.

**Read `PRD.md` and `DESIGN.md` before making product or UI decisions.** They are the
specification, not background reading, and most "why is it like this" questions are answered
there. Requirements are referenced throughout the code as `FR-x.y`, `EC-n` and `M-n`.

## The two rules that are not negotiable

**1. This application makes zero outbound network connections.** No HTTP, no sockets, no DNS,
no update check, no telemetry, no crash reporting, no font or asset fetching. This is the
product's central promise to the operator's clients (PRD FR-8.1).

It is enforced mechanically, not by review: `BannedSymbols.txt` bans the networking surface
at compile time via `Microsoft.CodeAnalysis.BannedApiAnalyzers`, and RS0030 is escalated to an
error in `Directory.Build.props`. **If a build fails on a banned symbol, the fix is never to
remove the ban.** An in-app update checker is the most likely accidental violation, because it
is the default posture of most desktop frameworks and installers.

**2. Never ship a QR the app has not verified.** The product exists because competing tools
render codes that look correct on screen and fail off the printed card. Any change that makes
a code exportable without passing both the module-size gate and the decode-back self-test is
a defect, however convenient it is.

## Layout

| Path | What it is |
|---|---|
| `PRD.md` | Product requirements. The specification. |
| `DESIGN.md` | Design brief — principles, tokens, screens, components, accessibility. |
| `src/ContactQR.Core/` | Pure domain logic. No UI, no I/O, no network. See its `CLAUDE.md`. |
| `src/ContactQR.Rendering/` | QR encoding, drawing, PNG export, decode-back self-test. |
| `src/ContactQR.Storage/` | The client library. One SQLite file. |
| `src/ContactQR.App/` | The WPF application. Presentation only. |
| `tests/ContactQR.Core.Tests/` | xUnit + FluentAssertions. See its `CLAUDE.md`. |
| `tests/ContactQR.Rendering.Tests/` | Rendering, PNG metadata, capacity cross-verification. |
| `BannedSymbols.txt` | The compile-time offline guarantee. |
| `Directory.Build.props` | Nullable, warnings-as-errors, analyzers, the RS0030 escalation. |

`ContactQR.Core` targets `net10.0`, not `net10.0-windows`, so a UI dependency cannot be added
without the target framework change making it obvious in review. Keep it that way.

## Build and test

```bash
dotnet build          # must be warning-free; warnings are errors
dotnet test           # 222 tests, all must pass
dotnet run --project src/ContactQR.App    # launch the Editor
```

.NET 10 SDK. The solution file is `ContactQR.slnx`, the newer XML format — `dotnet build`
finds it automatically, but `-p ContactQR.sln` will not work.

## Conventions

- Follow the `clean-code-csharp` skill: intention-revealing names, small methods, no
  `Manager`/`Helper`/`Processor` class names, exceptions over error codes, never return `null`.
- File-scoped namespaces, nullable reference types on, XML docs on public API in `src/`.
- Comments explain **why**, never what. Most comments here cite the PRD requirement that
  forced the decision, because that is the context a future reader will lack.
- Test names are `Method_Scenario_ExpectedBehaviour`. CA1707 is switched off under `tests/`
  for exactly this reason; it stays on in `src/`.

## Things that will bite you

- **vCard needs CRLF, not LF.** A bare LF produces intermittent, device-specific failures that
  are very hard to diagnose after cards are printed (FR-2.3).
- **Count UTF-8 bytes, never characters.** A Devanagari or CJK name costs roughly three bytes
  per character and will push a code over budget invisibly (FR-2.5, EC-2).
- **The capacity table is now cross-verified** against QRCoder across all 40 versions and 4
  correction levels, in `QrCapacityCrossVerificationTests`. Do not delete that test.
- **`InvariantGlobalization` must stay `false` in the WPF app.** WPF cannot bind under
  invariant globalization; it throws before the first window appears.
- **The 0.40 / 0.30 mm-per-module thresholds are an uncalibrated hypothesis**, not measured
  fact. PRD M1b requires calibration against the physical device matrix.
- **FluentAssertions is pinned to 7.2.0** deliberately. Version 8 and later require a paid
  licence for commercial use. Do not upgrade without a licensing decision.
