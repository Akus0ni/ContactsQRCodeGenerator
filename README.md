<div align="center">

<img src=".github/readme-icon.png" width="96" alt="ContactQR">

# ContactQR

**Offline vCard QR codes for printed business cards — that you can prove will scan.**

[![Download the MSI](https://img.shields.io/badge/Download-ContactQR.msi-0095AD?style=for-the-badge&logo=windows&logoColor=white)](https://github.com/Akus0ni/ContactsQRCodeGenerator/releases/latest/download/ContactQR.msi)

[![Latest release](https://img.shields.io/github/v/release/Akus0ni/ContactsQRCodeGenerator?logo=github&label=latest)](https://github.com/Akus0ni/ContactsQRCodeGenerator/releases/latest)
[![CI](https://github.com/Akus0ni/ContactsQRCodeGenerator/actions/workflows/ci.yml/badge.svg)](https://github.com/Akus0ni/ContactsQRCodeGenerator/actions/workflows/ci.yml)
[![Platform](https://img.shields.io/badge/Windows_10%2F11-x64-0078D4?logo=windows&logoColor=white)](#requirements)
[![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Network calls](https://img.shields.io/badge/network_calls-zero-2EA043?logo=shieldsdotio&logoColor=white)](#-two-promises)

</div>

---

## What it is

A Windows desktop tool for a solo print-and-design operator. Enter a client's contact details,
export a PNG, drop it into InDesign. Someone handed the printed card points a stock phone
camera at it and gets the native **Add Contact** sheet, already filled in.

It exists because the alternatives fail in ways you only discover after 500 cards are boxed:

| The usual tool | What goes wrong |
|---|---|
| Online QR generators | Your client's name, mobile and address get pasted into a stranger's web form |
| "Dynamic" QR codes | The code is a redirect through a vendor's server — it dies behind a signup wall, needs mobile data, and logs every scan |
| Free static generators | Screen-resolution PNGs with no control over module size or quiet zone. Perfect on a 27" monitor, intermittent off uncoated stock |
| All of them | **None tells you the code is too dense to survive print at the size you're placing it** |

---

## 🔒 Two promises

> [!IMPORTANT]
> **1 — Zero outbound network connections.** No HTTP, no sockets, no DNS, no update check, no
> telemetry, no crash reporting, no font fetching. Your client's contact data never leaves the
> machine, and the app behaves identically with every network adapter disabled.
>
> This is enforced at compile time, not by code review. `BannedSymbols.txt` bans the entire
> networking surface via `Microsoft.CodeAnalysis.BannedApiAnalyzers`, and `RS0030` is escalated
> to a build error. Adding an update checker breaks the build.

> [!IMPORTANT]
> **2 — Nothing is exported that the app has not verified.** Every export passes two independent
> gates: a **module-size check** against the physical print width, and a **decode-back
> self-test** that re-decodes the finished bitmap — logo composited, colours applied, at export
> resolution — and asserts it round-trips to the exact source vCard.

---

## The Scannability Budget

The feature the other tools don't have, and the reason this one exists.

A visiting card gives a QR about 20–25 mm. That width divided by the module count sets a hard
ceiling on how many bytes the card can carry. Three decisions spend that budget — **how many
fields**, **whether the client's logo goes in the middle**, and **how wide it prints** — and
you are normally shown the price of none of them.

ContactQR prices all three, live, before you export:

```
Payload        251 bytes          ECC  M          Version 10
Print width    22.0 mm            Module size     0.29 mm
Verdict        ✕ WILL FAIL — export blocked
```

...and then ranks the ways out by how many bytes each one actually recovers:

```
Remove logo → ECC H drops to M      recovers 165 B → 0.34 mm → Marginal
Widen 22 mm → 30 mm                                 → 0.41 mm → Safe
Remove postal address               saves    78 B → 0.36 mm
Remove Note                         saves    47 B → 0.33 mm
```

> [!TIP]
> **The logo is the most expensive single decision on the card** — more than any field. It forces
> error correction to level H, roughly halving capacity. When a code is over budget, "drop the
> logo" is almost always the cheapest fix in design terms and the largest in bytes.

| Verdict | Module size | Export |
|:--|:--|:--|
| 🟢 **Safe** | ≥ 0.40 mm | Yes |
| 🟡 **Marginal** | 0.30 – 0.40 mm | Yes, with an acknowledgement |
| 🔴 **Will fail** | < 0.30 mm | **Blocked.** Overriding requires a ticked confirmation, writes `_UNSAFE` into the filename, and is recorded in the export log |

> [!WARNING]
> The 0.40 / 0.30 mm thresholds are an **uncalibrated hypothesis** drawn from published print-QR
> guidance — not this product's own measurement. Use **Print test sheet** to output the same code
> at 20/25/30/40/50 mm, print it at 100%, and scan each one with a real phone. That is the
> calibration, and it is still outstanding.

---

## Install

<div align="center">

### [⬇️ Download ContactQR.msi](https://github.com/Akus0ni/ContactsQRCodeGenerator/releases/latest/download/ContactQR.msi)

*That link always resolves to the newest release. [Browse all releases →](https://github.com/Akus0ni/ContactsQRCodeGenerator/releases)*

</div>

The MSI installs, upgrades an existing copy in place, and uninstalls from Programs and Features.

> [!CAUTION]
> **The installer is not code-signed yet**, so SmartScreen will warn on first run. Choose
> **More info → Run anyway**. Signing is pending a certificate — see
> [PRD](PRD.md) FR-8.7. Until then, download only from the Releases page above.

### Requirements

- Windows 10 21H2 or later, or Windows 11 — **x64**
- [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)

> [!NOTE]
> There is deliberately **no in-app update check** — an updater is a network client, and that
> would break promise 1. New versions are a new MSI, downloaded by you, from the link above.

---

## Where your data lives

Everything stays in one folder on your machine:

```
%APPDATA%\ContactQR\
├── library.db                      SQLite — clients, contact points, export history
└── logs\
    └── contactqr-2026-09-02.log    Diagnostic log, one file per day, kept 14 days
```

> [!NOTE]
> **Uninstalling does not delete `library.db`.** It is your client book, built up over years, and
> an upgrade that reinstalled would otherwise wipe it. Removing it is a deliberate, manual act.
>
> The location sits outside OneDrive on purpose — a database inside a sync root can be locked
> mid-write or produce conflict copies. Back it up with **Export library** to JSON, which stays
> readable without this app installed.

Deleting `library.db` while the app is closed is safe: a new empty one is created on next
launch. Your clients and export history are gone permanently, though.

---

## Something went wrong?

Check `%APPDATA%\ContactQR\logs\`. Every session records its version, paths, runtime and
culture, then every export with its full budget line, plus any crash with a complete stack
trace. Because the app may not phone home, this file is the only diagnostic channel there is —
attach it to a bug report.

```
2026-09-02 16:34:57.852 +05:30  INFO   --- session start ---
    version    1.0.2.0
    directory  C:\Program Files\ContactQR\
    runtime    10.0.11
2026-09-02 16:35:20.114 +05:30  INFO   Exported C:\Jobs\Meridian_Nikhil_QR_25mm.png —
    251 bytes, M, version 10, 25.0 mm at 300 dpi, 0.410 mm per module, verdict Safe
```

---

## Build from source

```bash
git clone https://github.com/Akus0ni/ContactsQRCodeGenerator.git
cd ContactsQRCodeGenerator

dotnet build                              # must be warning-free — warnings are errors
dotnet test                               # 244 tests, all must pass
dotnet run --project src/ContactQR.App    # launch the Editor
```

Needs the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0). The solution file is
`ContactQR.slnx` — the newer XML format, which `dotnet build` finds automatically.

### Layout

| Path | What it is |
|---|---|
| [`PRD.md`](PRD.md) | Product requirements. The specification, not background reading. |
| [`DESIGN.md`](DESIGN.md) | Design brief — principles, tokens, screens, accessibility. |
| `src/ContactQR.Core/` | Pure domain logic. vCard encoder, capacity table, budget maths. |
| `src/ContactQR.Rendering/` | QR encoding, drawing, PNG export, decode-back self-test. |
| `src/ContactQR.Storage/` | The client library. One SQLite file. |
| `src/ContactQR.App/` | The WPF application. Presentation only. |
| `installer/` | WiX 6 MSI — install, upgrade, uninstall. |
| `BannedSymbols.txt` | The compile-time offline guarantee. |

`ContactQR.Core` targets `net10.0`, not `net10.0-windows`, so a UI dependency cannot sneak in
without the target framework change making it obvious in review.

Requirements are cited throughout the code as `FR-x.y`, `EC-n` and `M-n`, referring to
[`PRD.md`](PRD.md).

---

## Contributing

`main` is protected. Changes arrive by pull request, `build-and-test` must pass, and force
pushes and deletions are blocked.

1. Fork, branch, and open a PR against `main`
2. CI runs build, 244 tests, and a smoke test that the published app actually starts
3. [@Akus0ni](https://github.com/Akus0ni) is a required reviewer on every PR and the only
   person who can merge

> [!NOTE]
> CI is the gate, not a formality — the offline guarantee is one of the things it enforces. A
> merge that does not bump `<Version>` is normal and does not fail; most merges are not releases.

**To cut a release:** bump `<Version>` in `Directory.Build.props` — the single source of truth
for the assembly version, the MSI `ProductVersion`, and the release tag — then merge to `main`.
CI builds the MSI and publishes a tagged release automatically.

---

## Status

Version **1.0.2**. Working end to end: contact editor, live preview, the Scannability Budget
with ranked remedies, both export gates, PNG export with correct `pHYs` DPI metadata, the print
test sheet, and the client library.

Known gaps, tracked in [`PRD.md`](PRD.md):

- 🔴 **The MSI is unsigned** — needs a code-signing certificate (FR-8.7, Q2)
- 🟡 **Module-size thresholds are uncalibrated** — needs the physical device matrix (M1b)
- 🟡 Not yet built: export dialog with DPI and quiet-zone controls, logo picking, colour
  pickers, payload inspector, export-log view
- 🟡 No licence file yet — see below

---

## Licence

Not yet chosen. Until one is added, no permissions are granted beyond viewing the source.
