# PRD — ContactQR (working title)

**Offline Windows desktop generator for vCard QR codes on printed business cards**

| | |
|---|---|
| Status | Draft for review |
| Author | Product |
| Date | 2 September 2026 |
| Target platform | Windows 10 21H2+ / Windows 11, x64 |
| Stack | .NET 10 (stable), WPF, signed MSI/MSIX installer |
| Network posture | Zero outbound connections, by architecture, verifiable |

> **Naming is unresolved.** "ContactQR" is a placeholder used for readability. See Open Questions Q1.

---

## 1. Problem statement — who hurts and why

A solo print-and-design operator produces visiting cards for a rolling book of small-business clients — clinics, contractors, consultants, boutiques. Clients increasingly ask for "a QR code on the card that saves my number to the phone." Every route currently available to that operator is bad:

**Online QR generators are a liability, not a tool.** The operator has to paste a client's full name, mobile number, email, and business address into a stranger's web form. That is the client's data, not the operator's, being handed to an ad-funded service with an unread privacy policy. It creates a data-protection exposure the operator cannot answer for when a client asks where their details went.

**"Dynamic" QR codes are a trap dressed as a feature.** The dominant free generators encode a short URL that redirects through the vendor's server. This produces three failures the operator only discovers after the cards are printed and delivered: the vendor gates the QR behind a signup wall after a trial period and the client's printed card dies; scanning requires the recipient to have working mobile data, so it fails in a basement clinic or a rural site office; and every scan is logged and monetised by a third party the client never agreed to.

**Free static generators produce codes that don't survive print.** They emit a low-resolution PNG at screen DPI with no control over quiet zone, module size, or error correction. Placed on a card at 20mm and sent to a commercial press, the result is a QR that reads fine on the operator's monitor and fails intermittently off the printed piece — under fluorescent light, on uncoated stock, at an angle. The operator finds out when the client calls, after 500 cards are boxed.

**There is no reusable record.** Each generator session starts from an empty form. When a client changes their mobile number or moves office — routine, several times a year — the operator retypes all eleven fields from scratch and hopes for no transcription error in a phone number.

**Underlying all of it: nobody tells the operator the code is too dense to work.** This is the failure that costs real money, and it is a budget problem the operator is never shown. A visiting card is 85×55mm; a QR on it realistically gets 20–25mm of width. That width, divided by the smallest module a phone camera can resolve off paper, sets a hard ceiling on how many bytes the card can carry — and a fully-populated business vCard runs about 400 bytes, comfortably over it.

Three decisions spend that same budget, and the operator currently sees none of them priced: **how many fields** they include, **whether the client's logo goes in the centre** — which forces error-correction level H and costs roughly a third of the capacity — and **how wide the QR is printed**. Any one of the three can be traded against the others. Combine the expensive end of all three and each module lands near 0.2mm, well under what a stock camera resolves off uncoated stock at arm's length.

Existing tools render all of this without complaint. It looks perfect on screen, at 400 pixels, on a 27-inch monitor. It fails on the card. The operator eats the reprint.

**The pain, stated plainly:** the operator cannot promise a client that the QR on their card will work, cannot promise their data stayed private, and cannot cheaply fix either promise after the cards are printed.

---

## 2. Target user + 2 personas

**Target user (primary, and the only one who opens the app):** an independent designer / small print-shop owner in a one-person business, producing visiting cards for 15–30 SMB clients a month. Windows-native. Comfortable with Illustrator, InDesign, and Canva; not a developer. Bills per job and absorbs the cost of any reprint.

Persona 2 is deliberately **not** a second operator. The product's entire success condition sits with someone who never launches the app — the person holding the printed card. Specifying only the operator would let us ship something that feels good and doesn't work.

### Persona 1 — Nikhil Rao, 36. The operator. *Primary user.*

Runs a one-person design and print-brokering studio from a spare room. Windows 11 desktop, two monitors, Adobe CS, a colour laser for proofs, a trade press for volume runs. About 20 card jobs a month at ₹2,500–8,000 each; a 500-card reprint costs him roughly ₹3,000 plus a damaged client relationship.

- **Job to be done:** add a QR to a card layout, in under two minutes, that he can personally guarantee will scan off the printed piece.
- **Behaviour:** works client-by-client from a job folder. Never batch-processes; each card is a separate design conversation. Keeps a mental list of repeat clients who come back annually for reprints with one changed detail.
- **What he says:** *"I need to hand the client a card and know it works. I can't have them ringing me in three weeks saying half their customers can't scan it."*
- **Frustration:** he does not know what makes a QR fail. He has no way to distinguish a code that works from one that is one bad print run away from failing. He wants the tool to tell him.
- **Non-negotiable:** client contact details must not leave his machine. He has been asked this directly by a client and had no good answer.
- **Success looks like:** exports a print-ready PNG, drops it into InDesign, and moves on without thinking about it again.

### Persona 2 — Meera D'Souza, 52. The scanner. *Never opens the app. Defines acceptance.*

A physiotherapy patient handed her therapist's card at reception. iPhone 13, iOS 18, mid-range eyesight, indoor fluorescent lighting, standing at a desk holding the card at arm's length. Has never deliberately scanned a QR code and does not know she needs a special app.

- **Job to be done:** get the clinic's number into her phone without typing it.
- **Behaviour:** opens the stock Camera app because that is what she has been shown works. Holds the phone roughly 20cm from the card, hand not entirely steady. Waits about **three seconds**. If no banner appears, she puts the phone away and the card goes in a drawer.
- **Constraints she imposes on us, whether we like it or not:** stock camera app only — no Lens, no third-party scanner. No mobile data assumed. One attempt. Zero tolerance for a "sign up to view this contact" interstitial.
- **Success looks like:** a yellow contact banner appears, she taps it, the native Add Contact sheet opens with the clinic name and number already filled, she taps Done.
- **Why she is in this document:** every requirement below that constrains payload size, error correction, module size, and quiet zone exists because of Meera's three seconds. She is the acceptance test.

---

## 3. Goals and non-goals

### Goals

| # | Goal | Measured by |
|---|---|---|
| G1 | A generated QR opens the native Add Contact sheet with fields pre-filled, on stock iOS Camera and stock Android camera, scanned off print | Device matrix, §9 M1 |
| G2 | The app never makes a network connection, and this is provable, not asserted | §9 M4 — packet-level verification |
| G3 | The operator is warned *before export* when a code is too dense to survive print at the intended size | §6 F4 — no export path bypasses the check silently |
| G4 | Exported PNG is genuinely print-ready: correct DPI metadata, correct quiet zone, correct physical dimensions | §9 M2 |
| G5 | Regenerating an existing client's QR after a detail change takes under 15 seconds | §9 M3 |
| G6 | Client contact data is stored locally, under the operator's control, and is backup-able and portable | §6 F7 |

### Non-goals — explicitly out of scope, v1 through v3

| Not doing | Why |
|---|---|
| **Dynamic / trackable / editable-after-print QR codes** | Structurally requires a hosted redirect. Incompatible with the offline requirement. This is a *rejected feature*, not a deferred one — it is the thing we are defined against. Do not revisit. |
| **Scan analytics** | Same reason. Requires a server. Also a privacy commitment we are making to the client. |
| **Cloud sync of the client library** | Same reason. Local file + manual export is the sync story. |
| **Any online lookup** — address autocomplete, company enrichment, URL validation, logo fetch, update checks, crash telemetry | Any one of these breaks G2. An in-app update checker is the most likely accidental violation; it is banned. |
| **macOS, Linux, web, mobile** | Windows desktop only. |
| **Being a card design tool** | We output a PNG. Layout happens in InDesign/Illustrator/Canva. We do not do card templates. |
| **Bulk / CSV import** | Confirmed out of MVP. Deferred to v2 — see §5. |
| **Formats other than vCard 3.0** (MECARD, vCard 2.1, vCard 4.0, wifi/URL/text QR) | vCard 3.0 has the broadest native camera support across both platforms. Supporting alternates doubles the encoder surface for no user-visible gain. See §10 Q3. |
| **Reading / decoding arbitrary third-party QR codes** | We decode only our own output, as a self-test (F4). Not a general scanner. |
| **Multi-user, accounts, licensing, DRM** | Single-operator tool. |

---

## 4. User stories

Format: `As a <role>, I want <capability>, so that <outcome>.` Each carries the MVP feature it maps to.

### Operator — core loop

- **US-1** — As the operator, I want to enter a client's business contact details in a single form where only name and phone are required, so that I can produce a QR for a one-man contractor and a full-detail clinic with the same workflow. → *F1*
- **US-2** — As the operator, I want the QR preview to update as I type, so that I can see the code getting denser as I add fields and stop before it becomes unprintable. → *F3*
- **US-3** — As the operator, I want the app to tell me the smallest physical width this specific QR can be printed at and still scan reliably, so that I can decide whether it fits the card layout before I commit to the design. → *F4*
- **US-4** — As the operator, I want to be blocked with a clear explanation — not a silent success — when my chosen print size is below that minimum, so that I never hand a press a code that will fail. → *F4*
- **US-5** — As the operator, I want the app to decode its own generated PNG and confirm the contact data round-trips exactly, so that I have machine verification and not just a picture that looks like a QR code. → *F4*
- **US-6** — As the operator, I want to export a PNG at a chosen physical size and DPI with the DPI written into the file, so that InDesign places it at the correct size instead of guessing at 72dpi. → *F6*

### Operator — branding

- **US-7** — As the operator, I want to optionally drop the client's logo into the centre of the QR, so that the code looks like a designed element of the card rather than a sticker. → *F5*
- **US-7a** — As the operator, I want to see what adding the logo costs me in capacity *before* I add it, so that I can tell the client "the logo means we drop the address" as a design conversation rather than discovering it after the code fails. → *F4, F5*
- **US-8** — As the operator, I want to set the QR's foreground and background colours to the client's brand palette, so that it matches the card. → *F5*
- **US-9** — As the operator, I want to be stopped when my colour choice or logo size will break scanning — insufficient contrast, inverted light-on-dark, logo too large — so that brand consistency never silently costs the client a working code. → *F5*

### Operator — library

- **US-10** — As the operator, I want to save each client's details to a local library, so that next year's reprint doesn't mean retyping eleven fields and risking a typo in a phone number. → *F7*
- **US-11** — As the operator, I want to search my library by client or company name, so that I can find a returning client in seconds. → *F7*
- **US-12** — As the operator, I want to duplicate an existing client record as the starting point for a new one, so that I can produce cards for three partners at the same firm without re-entering the shared company details. → *F7*
- **US-13** — As the operator, I want to export my entire library to a single file and import it back, so that I can back it up and survive a machine rebuild. → *F7*

### Operator — trust

- **US-14** — As the operator, I want to be able to demonstrate that this app makes no network connections, so that I can answer a client who asks where their data went. → *F8*
- **US-15** — As the operator, I want the app to work identically with the machine's network adapter disabled, so that the offline guarantee is something I can verify myself in ten seconds. → *F8*

### Scanner — acceptance conditions (not implemented features; they constrain the above)

- **US-16** — As a person handed a printed card, I want to point my stock phone camera at the QR and get a contact prompt within three seconds without installing anything, so that I actually save the number instead of giving up. → *constrains F2, F4, F6*
- **US-17** — As a person with no mobile signal, I want the scan to work anyway, so that the card is useful in a basement clinic or on a site. → *satisfied by static vCard payload; the reason dynamic QR is a non-goal*
- **US-18** — As a person saving the contact, I want the company name, job title, and address to land in the correct fields of my phone's contact record rather than mashed into the name, so that the entry is actually usable later. → *constrains F2 — correct vCard field mapping and escaping*

---

## 5. Feature list — MVP / v2 / later

### MVP — v1.0

| ID | Feature | One-line justification |
|---|---|---|
| **F1** | Contact editor — full business field set, name + primary phone mandatory, everything else optional | The core input. Confirmed scope. |
| **F2** | vCard 3.0 encoder — spec-correct escaping, CRLF, UTF-8, E.164 phone normalisation | Correctness here determines whether US-18 works. Most-commonly-broken part of every competing tool. |
| **F3** | QR generation + live preview | The core output. |
| **F4** | **Scannability Budget** — live byte count, QR version, module size at target print width, minimum-safe-size advisor, hard export gate, and decode-back self-test | The differentiator, and the direct answer to the problem in §1. Nothing else on the market does this. |
| **F5** | Branding — *optional* centre logo + custom foreground/background colours, with contrast and coverage guardrails | Confirmed must-have. Logo is off by default and priced at the toggle (FR-5.0). Guardrails are not optional extras; without them this feature actively breaks G1. |
| **F6** | PNG export — physical size in mm, DPI selection, embedded DPI metadata, configurable quiet zone | Confirmed must-have. |
| **F7** | Local client library — create, search, edit, duplicate, delete; JSON export/import for backup | Confirmed must-have. Directly serves the annual-reprint workflow. |
| **F8** | Offline guarantee — architectural, with an in-app statement and a documented verification procedure | Confirmed must-have and part of the definition of done. |

### v2 — next release

Ordered by expected value to the operator.

1. **CSV import + batch export** — one PNG per row, filename from a template. Deferred from MVP by decision; the single-record path must be proven first. The column-mapping UI and per-row error report are the real cost here, not the loop.
2. **Vector export — SVG and PDF** — removes the DPI question entirely and is what a commercial press actually prefers. Strong candidate for promotion into v1.1 rather than waiting for v2.
3. **`.vcf` file export** — same encoder, written to disk instead of a QR. Near-zero marginal cost; useful for email signatures.
4. **Branding presets per client** — saved logo + colour pair attached to a library record, so a repeat client's card is visually identical to last year's.
5. **Compact vCard mode** — an aggressive encoder variant that strips `TYPE` parameters and redundant fields to buy roughly 60–90 bytes, for the case where the operator needs the logo *and* a small print size. See §6 F4 for why this is needed.
6. **N-up proof sheet** — a printable page of the QR at several sizes for physical scan-testing before committing to a press run.
7. **Undo/redo and change history** on library records.

### Later — v3 and beyond, unscheduled

- Contact photo embedded in the vCard *(payload cost is severe — likely never viable at card size; include only alongside vector export and large-format use cases)*
- Non-card form factors: window decals, signage, vehicle livery — different size regimes where dense codes are fine
- NFC tag writing as a companion to the printed QR
- Localised UI
- Multiple QRs per client — separate "personal" and "reception desk" codes
- Adobe / Canva plugin
- macOS port

### Explicitly rejected — will not build

- Dynamic/editable QR, scan analytics, cloud sync, online update checks, crash telemetry. See §3 non-goals. Each requires a network connection and breaks the product's central promise.

---

## 6. Detailed functional requirements per MVP feature

---

### F1 — Contact editor

**FR-1.1** The editor presents these fields in four groups. Only the two marked **required** block generation; every other field, and the logo, is optional.

| Group | Field | Required | vCard target | Notes |
|---|---|---|---|---|
| Identity | Given name | **Yes** | `N` field 2, `FN` | |
| | Family name | No | `N` field 1, `FN` | |
| | Company | No | `ORG` | |
| | Job title | No | `TITLE` | |
| Reach | Mobile | **Yes** | `TEL;TYPE=CELL` | Primary. E.164 normalised. |
| | Mobile 2 | No | `TEL;TYPE=CELL` | |
| | Work phone | No | `TEL;TYPE=WORK,VOICE` | |
| | Fax | No | `TEL;TYPE=WORK,FAX` | Rarely used; costs ~30 bytes |
| | Work email | No | `EMAIL;TYPE=INTERNET,WORK` | |
| | Personal email | No | `EMAIL;TYPE=INTERNET,HOME` | |
| | Website | No | `URL` | |
| Address | Street | No | `ADR` field 3 | |
| | City | No | `ADR` field 4 | |
| | State / region | No | `ADR` field 5 | |
| | Postal code | No | `ADR` field 6 | |
| | Country | No | `ADR` field 7 | |
| Extra | Social / profile URLs | No | `URL` (repeated) | Up to 3. LinkedIn, Instagram, WhatsApp. |
| | Note / tagline | No | `NOTE` | Free text. Highest payload risk per character. |

**FR-1.2** Given name and mobile are the only fields that block generation. All others may be left empty and MUST be omitted from the vCard entirely — never emitted as an empty property (`TITLE:` with no value causes visible empty fields on some Android contact apps).

**FR-1.3** Every optional field displays its live byte cost when populated, e.g. `Note — 47 bytes`. This makes the density trade-off visible at the point of decision rather than at export. It is the primary mechanism by which the operator learns what makes a code dense.

**FR-1.4** Phone number handling. Input is accepted in any common form. On blur the field is normalised to E.164 (`+919876543210`) and the normalised value displayed. If no country code can be inferred, the app prompts for a default country (set once in Settings, remembered). **Rationale:** a number stored without a country code fails when the scanner's phone is roaming or registered in another country — a silent, delayed failure. Ambiguous input is flagged for confirmation, never silently guessed.

**FR-1.5** Website and social URLs missing a scheme are prefixed `https://` on blur, displayed as modified. No network request is made to validate any URL — see F8.

**FR-1.6** Validation is advisory except for the two required fields. An email that fails a format check produces a warning, not a block; the operator may know something the regex does not.

**FR-1.7** *(Ambiguity flagged — see §10 Q4)* Whether name input should be a single "Full name" box or split given/family is unresolved. This document assumes **split**, because vCard 3.0's `N` property is structured and a single box forces us to guess where the surname is — which breaks for mononyms, South Indian initial-first names, and CJK ordering. The cost is a marginally slower form. Confirm before build.

---

### F2 — vCard 3.0 encoder

This is the highest-correctness-risk component in the product. It is also the part every free online generator gets wrong. Treat these as hard requirements with unit tests.

**FR-2.1** Output conforms to vCard 3.0 (RFC 2426). `BEGIN:VCARD`, `VERSION:3.0`, `END:VCARD`. `VERSION` MUST immediately follow `BEGIN`.

**FR-2.2** Both `N` and `FN` MUST be emitted. `FN` is mandatory in vCard 3.0 and its absence causes iOS to display a blank contact name. `N` is emitted as the structured 5-part form: `N:Family;Given;;;`.

**FR-2.3** **Line terminators MUST be CRLF (`\r\n`), not LF.** Some parsers tolerate LF; iOS behaviour is inconsistent. This is a one-line bug that produces intermittent, device-specific failures and is very hard to diagnose after the fact. Assert it in tests.

**FR-2.4** **Value escaping.** Within any property value, these characters MUST be escaped: backslash → `\\`, semicolon → `\;`, comma → `\,`, newline → `\n`. Un-escaped commas and semicolons are the single most common vCard defect in the wild — a company name like `Acme Interiors Pvt Ltd, Mumbai` will, unescaped, be parsed as two `ORG` components and display wrongly on the scanner's phone. Structural delimiters in `N` and `ADR` are of course not escaped.

**FR-2.5** **Encoding is UTF-8, emitted as raw bytes in QR byte mode.** No quoted-printable, no `CHARSET=` parameter (a vCard 2.1 idiom that confuses 3.0 parsers). Non-ASCII characters cost 2–4 bytes each; the byte counter in F4 MUST count encoded bytes, never characters. A Devanagari or CJK name roughly triples that field's cost and can push a code over budget invisibly if we count characters.

**FR-2.6** **Line folding is disabled.** RFC 2426 permits folding long lines at 75 octets. We do not fold. Rationale: folding adds bytes to an already-constrained payload, and a meaningful minority of mobile parsers mishandle folded lines. Modern iOS and Android parsers handle long unfolded lines correctly. *This is a deliberate spec deviation and MUST be included in the device test matrix (§9 M1) rather than assumed.*

**FR-2.7** `ADR` is emitted with the full 7-component structure, empty components preserved as consecutive semicolons: `ADR;TYPE=WORK:;;Street;City;State;Postcode;Country`. Dropping the leading empty components shifts every subsequent value into the wrong field.

**FR-2.8** Properties are emitted in a fixed, deterministic order: `BEGIN`, `VERSION`, `N`, `FN`, `ORG`, `TITLE`, `TEL`(×n), `EMAIL`(×n), `URL`(×n), `ADR`, `NOTE`, `END`. Deterministic output means identical input always produces a byte-identical QR — necessary for reproducible reprints and for meaningful diffing during debugging.

**FR-2.9** The raw vCard text MUST be viewable in the UI (a collapsible "show payload" panel) and copyable to the clipboard. This is the operator's escape hatch when a scan fails and is essential for supporting the product.

**FR-2.10** The encoder is pure: contact record in, string out. No I/O, no globals, no network. Fully unit-testable, with a golden-file test corpus covering each escaping case, empty-field omission, non-ASCII input, and the full-population maximum case.

---

### F3 — QR generation and live preview

**FR-3.1** Encoding uses a fully-offline managed library. **Recommendation: QRCoder** (MIT licence, pure managed C#, no native or network dependencies) to produce the module matrix, with rendering handled separately — see FR-3.4. Any candidate library MUST be auditable for the absence of network calls (F8).

**FR-3.2** QR mode is **byte mode** with UTF-8. Alphanumeric mode is unavailable because vCard payloads contain lowercase letters, `@`, and `:`. Do not let an encoder library silently choose a mode that mangles the payload.

**FR-3.3** The encoder selects the **smallest QR version** that holds the payload at the selected error-correction level. Version is never fixed by the user; it is a computed consequence of payload and ECC, and is surfaced read-only in the Scannability Budget panel.

**FR-3.4** Rendering is separated from encoding. The encoder yields a boolean module matrix; a renderer draws it. **Recommendation: SkiaSharp** for rendering (colour, logo compositing, scaling). *Implementation gotcha to plan for: SkiaSharp does not write the PNG `pHYs` chunk, so DPI metadata (FR-6.4) requires either post-hoc chunk injection or using `System.Drawing.Common` — Windows-only, which is acceptable here — via `Bitmap.SetResolution`. Decide this at design time; it is a known trap that gets discovered late.*

**FR-3.5** The preview updates on a **250ms debounce** after the last keystroke, not per-character. Re-encoding on every keypress is wasteful and makes the byte counter flicker distractingly.

**FR-3.6** The preview renders at the *actual configured physical size* against an on-screen reference, and additionally offers a 1:1 physical-size view that accounts for Windows display scaling. **Rationale:** a QR that looks crisp at 400px on a 27" monitor tells the operator nothing about how it behaves at 22mm on paper. Misleading previews are how bad codes get approved.

**FR-3.7** Modules are rendered with **sharp edges and no anti-aliasing at module boundaries**. Module dimensions in the output bitmap MUST be whole pixels; the renderer rounds the output size to the nearest whole multiple of the module count rather than scaling a smaller bitmap up. Fractional module widths produce grey edge pixels that degrade scan reliability off print.

**FR-3.8** Generation is blocked, with the reason shown inline, while either required field is empty.

---

### F4 — Scannability Budget *(the differentiating feature)*

This exists because of the budget problem described in §1. It is not a nice-to-have; without it the product is another generator that produces confident-looking failures.

**The constraint, stated numerically.** A QR of version *V* has `17 + 4V` modules per side. Adding the mandatory 4-module quiet zone on each edge gives `25 + 4V` total. Printed at physical width *W* mm, module size is `W / (25 + 4V)` mm. Below roughly **0.30mm per module**, a stock phone camera stops reliably resolving the code off print in one attempt under ordinary indoor lighting; **0.40mm** is a comfortable working target.

Approximate QR byte-mode capacities *(illustrative only — these MUST be read from the encoder library at runtime, never hardcoded from this table; published capacity tables differ slightly and the encoder is the authority)*:

| ECC level | ~Redundancy | v7 | v10 | v14 | v18 | v24 |
|---|---|---|---|---|---|---|
| L | 7% | 154 | 271 | 461 | 718 | 1213 |
| **M** | **15%** | **122** | **213** | **362** | **560** | **955** |
| Q | 25% | 87 | 151 | 259 | 394 | 680 |
| **H** | **30%** | **67** | **119** | **196** | **291** | **511** |

#### The byte budget, by print width and logo choice

Since the logo is optional, ECC is not fixed: **no logo permits M** (recommended default), **logo forces H** (FR-5.1). This is the single largest lever the operator has. Approximate bytes available:

| QR width on card | No logo (ECC M) — Safe / Floor | With logo (ECC H) — Safe / Floor |
|---|---|---|
| 22 mm | ~122 B / ~287 B | ~67 B / ~155 B |
| 25 mm | ~192 B / ~362 B | ~98 B / ~196 B |
| 30 mm | ~287 B / ~560 B | ~155 B / ~291 B |
| 40 mm | ~560 B / ~1000 B+ | ~291 B / ~511 B |

Against three reference payloads:

| Payload | Contents | Size |
|---|---|---|
| **Minimal** | Name, company, one mobile | ~122 B |
| **Typical** | + job title, work phone, email, website | ~250 B |
| **Full** | + postal address, note, second phone, social URLs | ~400–450 B |

**Three conclusions the operator needs, and no existing tool provides:**

1. **The logo is the most expensive single decision on the card — more expensive than any field.** Removing it roughly doubles the available bytes at every size. When a code is over budget, "drop the logo" is almost always the cheapest fix in design terms and the largest in byte terms.
2. **At 22mm without a logo, a Typical payload is Marginal and a Full payload fails.** Name, company, and one phone number is what a visiting-card-sized QR comfortably carries. This is a real constraint on the client conversation, not a software limitation to engineer around.
3. **A logo'd QR wants ~30mm and a Minimal-to-Typical payload.** Below that it is a layout problem, and the honest answer to the client is "the logo costs you the address," not a code that fails after printing.

The operator must be shown this trade-off live, priced, before export — not discover it from a client three weeks later.

**FR-4.1** A persistent Scannability Budget panel displays, live:
- Payload size in **UTF-8 bytes** (not characters), with capacity remaining at the current ECC level
- Selected ECC level, and whether it is operator-chosen or forced (logo present → H)
- Resulting QR version and module count
- Target print width in mm (operator-set)
- **Computed module size in mm** — the number that actually decides success
- **Minimum safe print width in mm** for this exact payload and ECC
- A status verdict: **Safe / Marginal / Will fail**

**FR-4.2** Verdict thresholds, applied to computed module size:
- **Safe** — ≥ 0.40 mm/module
- **Marginal** — 0.30 to 0.40 mm/module. Export permitted with an explicit acknowledgement.
- **Will fail** — < 0.30 mm/module. **Export blocked.** See FR-4.5.
- *These thresholds MUST be empirically calibrated against the §9 M1 device matrix during v1 development and corrected if measurement disagrees. They are a starting hypothesis drawn from published print-QR guidance, not this product's own evidence. Setting them too conservatively is not the safe choice — it blocks codes that would have worked and trains the operator to override the gate (see M7).*

**FR-4.3** When the verdict is Marginal or Will fail, the panel presents **ranked, specific, actionable remedies with byte savings computed from the current record** — not generic advice. Ranked by bytes recovered, so the biggest lever is always first:
- `Remove logo → ECC H drops to M — recovers ~165 bytes → 0.34mm → Marginal`
- `Increase print width 22mm → 30mm — 0.41mm → Safe`
- `Remove postal address — saves 78 bytes → 0.36mm`
- `Remove Note — saves 47 bytes → 0.33mm`
- `Remove fax number — saves 31 bytes`
- `Shorten website URL — saves 18 bytes`

Each remedy shows its resulting module size and verdict. Applying one is a single click, and reversible. The operator picks the trade-off; the app prices it.

**FR-4.4** **Decode-back self-test.** Before any export completes, the app decodes the rendered bitmap — logo composited, colours applied, at final export resolution — using an independent offline decoder (**recommendation: ZXing.Net**, MIT, fully managed) and asserts that the decoded string is byte-identical to the source vCard. Export fails loudly if it does not match.

This is cheap and it is the difference between "we drew a QR" and "we verified a QR." It catches the entire class of bugs where the logo overlay, a colour choice, or a rounding error corrupted the code — bugs that are otherwise found by a client three weeks after printing. *Note the honest limitation: passing this test proves the symbol is structurally decodable, not that it survives print at the chosen size. It complements the module-size gate; it does not replace it.*

**FR-4.5** The **Will fail** export block is a hard gate with **no silent bypass**. An override is available, but it requires an explicit checkbox reading approximately *"I understand this code is likely to fail when printed at this size"*, the override event is written to the export log (F7), and the exported filename is suffixed `_UNSAFE`. **Rationale:** the whole product is the promise in G3. A soft warning that can be clicked past reflexively is functionally identical to no warning, and this warning is the one that saves a reprint.

**FR-4.6** A "Print test sheet" action outputs a single page containing the QR at 20/25/30/40/50mm with each size labelled, for physical scan-testing before a press run. Low cost, and it converts the module-size argument from theory into something the operator can verify with their own phone in thirty seconds.

---

### F5 — Branding: logo and colours

Guardrails in this feature are load-bearing. Unconstrained, this is the feature most likely to break G1.

**FR-5.0** **The logo is optional and off by default.** A code with no logo is the default output and the recommended one at visiting-card sizes. The logo control makes its cost explicit at the point of choice — adding one shows, inline, the byte capacity it will consume and the resulting verdict change, e.g. *"Adding a logo forces ECC H: capacity drops 362 → 196 bytes. Current payload 250 bytes → **Will fail** at 25mm."* **Rationale:** this is the most expensive decision the operator makes (F4), and today it is presented everywhere as a free cosmetic toggle. Pricing it at the toggle is where the reprint gets prevented.

**FR-5.1** **Error-correction level selection.**
- **No logo:** ECC defaults to **M** (~15% redundancy) and is operator-adjustable to L or Q. M is the default because it is the standard trade-off point for print — L saves bytes but leaves almost no margin for ink spread and card wear; Q costs capacity that a card-sized code cannot spare. Level L is available but warns that it is unsuitable for anything that will be handled, folded, or laminated.
- **With logo:** ECC is **forced to H** (~30% redundancy) and the control is locked, with the reason shown inline. H is what makes a centre occlusion survivable at all. Any lower level with a logo produces a code that decodes cleanly on a screen render and fails on paper — the worst failure mode, because it passes casual testing.

**FR-5.2** Logo formats: PNG (with alpha), JPEG, SVG. All loaded from local disk only. Removing the logo restores the previously selected non-logo ECC level rather than leaving the code at H — otherwise the operator silently keeps paying for a logo they removed.

**FR-5.3** **Logo coverage is capped.** Default width **18%** of QR width (≈3.2% of area). Hard maximum **25%** of width (≈6.25% of area). Beyond the default, a warning; beyond the maximum, blocked. **Rationale:** ECC H's 30% tolerance is a *total* damage budget shared with print defects, ink spread, lighting, camera angle, and card wear. Consuming most of it with a logo leaves nothing for the real world. The 25% cap is deliberately more conservative than the "up to 30%" figure quoted by online generators, because they are not accountable for the reprint.

**FR-5.4** The logo is composited over the modules it covers with an opaque background pad — typically the QR's background colour — plus a small margin. Alpha-blending a logo over modules produces mid-tone pixels the decoder must resolve as light or dark, which is worse than a clean occlusion the error correction can simply repair.

**FR-5.5** The logo is **never** permitted to overlap the three finder patterns or the alignment patterns. Occluding a finder pattern is unrecoverable at any ECC level. The renderer computes their positions and rejects any placement that intrudes.

**FR-5.6** Foreground and background colours are freely selectable, subject to FR-5.7 and FR-5.8.

**FR-5.7** **Contrast gate.** The foreground/background pair must meet a minimum luminance contrast ratio. **Recommendation: 7:1 as the block threshold, 10:1 for a clean pass**, calibrated against the device matrix. Below threshold, export is blocked. *Note that WCAG text-contrast ratios are the wrong instrument here — camera sensor response under mixed lighting is the actual constraint. Calibrate empirically in M1 and record the resulting threshold.*

**FR-5.8** **Inverted codes — light modules on a dark background — are blocked outright, not warned.** A significant share of decoders, including some stock camera implementations, assume dark-on-light and simply do not attempt inversion. There is no partial credit here: the code either scans on the recipient's phone or it does not. If the operator needs a dark card, the correct answer is a light patch behind the QR, and the app should say so in the block message.

**FR-5.9** Fully transparent PNG backgrounds are not offered in v1. Placed onto a dark or mid-tone card in InDesign, a transparent-background QR silently becomes an inverted or low-contrast code — the exact failure FR-5.7 and FR-5.8 exist to prevent, reintroduced downstream where we cannot see it. The background colour must be explicit. *(Revisit alongside vector export in v2, where the placement context can be reasoned about.)*

**FR-5.10** Every change to logo or colour re-triggers the F4 decode-back self-test. Branding changes are precisely what breaks decodability, so they must never bypass verification.

---

### F6 — PNG export

**FR-6.1** Export dialog fields: physical width in **mm** (primary), DPI, quiet zone in modules, output path, filename.

**FR-6.2** Physical width in mm is the primary control; pixel dimensions are derived and shown read-only. **Rationale:** the operator thinks in card layout, and every size-related failure in this product is a physical-size failure. Leading with pixels invites the classic mistake of exporting 300×300px for a 25mm print.

**FR-6.3** DPI presets: 300 (press standard), 600 (small sizes and fine detail), 1200 (available, rarely needed). **72 and 96 DPI are not offered** — they exist only to produce unprintable files. Custom DPI is accepted with a warning below 300.

**FR-6.4** **The exported PNG MUST carry correct DPI metadata in its `pHYs` chunk.** Without it, InDesign and Illustrator assume 72dpi and place the image at roughly four times its intended size, and the operator scales it down by eye — introducing resampling that destroys module edges. See the FR-3.4 implementation note; this is a real trap.

**FR-6.5** Quiet zone defaults to **4 modules** on all sides, per spec, and cannot be set below 4. It may be increased. *The temptation to trim the quiet zone to fit a tight layout is exactly why the floor is enforced: it is invisible on screen and a common cause of print scan failure.*

**FR-6.6** Module size in the output must be a whole number of pixels (FR-3.7). The renderer adjusts final pixel dimensions to the nearest whole multiple and reports the tiny resulting deviation from the requested mm width.

**FR-6.7** Default filename template: `{Company}_{GivenName}_QR_{Width}mm.png`, sanitised — characters illegal in Windows filenames removed, length capped, reserved device names (`CON`, `PRN`, `AUX`, `NUL`, `COM1`–`COM9`, `LPT1`–`LPT9`) avoided. The template is editable in Settings.

**FR-6.8** Existing-file collisions prompt: Overwrite / Rename / Cancel. Never silently overwrite — an operator re-exporting after a fix and an operator about to destroy a delivered asset look identical from inside the app.

**FR-6.9** Export runs the F4 gate (FR-4.5) and the decode-back self-test (FR-4.4). Neither can be skipped without the explicit override.

**FR-6.10** On success, the app confirms with the file path and offers "Open containing folder."

---

### F7 — Local client library

**FR-7.1** Storage is a **single SQLite database file** via `Microsoft.Data.Sqlite` — fully managed, offline, no server. Default location `%APPDATA%\ContactQR\library.db`, relocatable in Settings.

*Rationale for SQLite over loose JSON: it gives transactional writes (no half-saved record after a crash), indexed search, and a single file to back up. The relocation setting matters because this machine's Documents folder is OneDrive-synced — a database file inside a sync root can be locked mid-write or produce sync-conflict duplicates. See §8 EC-19; the default location deliberately sits outside the sync root.*

**FR-7.2** Operations: create, read, update, delete, duplicate. Delete is soft (`deleted_at`) with an undo window; hard-delete is available from a Settings maintenance action.

**FR-7.3** Search is a live substring match across given name, family name, company, and email. No fuzzy matching in v1 — a couple of hundred records do not need it, and fuzzy match surfacing the wrong client is worse than no match.

**FR-7.4** The list shows: display name, company, last modified, last exported. Sortable by each. Default sort is last modified, descending — the returning-client case.

**FR-7.5** **Duplicate** copies all fields into a new unsaved record with the name fields cleared and focus in Given name. This directly serves US-12, three partners at one firm.

**FR-7.6** Export the whole library to a single **JSON** file; import merges with a per-record conflict prompt. JSON rather than a database copy because it is human-readable, diffable, and future-proof against schema change — this is the backup story and it must not depend on the app being installed to be readable.

**FR-7.7** An **export log** records every generated PNG: client, timestamp, output path, payload byte count, ECC level, QR version, print width, module size, verdict, and whether an unsafe override was used. This is how a "the QR on the card you made me doesn't work" call gets answered in thirty seconds instead of by guessing.

**FR-7.8** Saving a record while a QR is generated is optional and non-blocking. A one-off code for a walk-in job should not require creating a permanent library entry.

**FR-7.9** Records store contact data only. No logo binaries in v1 — the library holds a file *path* to the logo. *(Deliberate: embedding binaries bloats the database and the JSON backup. The cost is a broken path if the operator reorganises their folders — handled as EC-20. Per-client branding presets in v2 should revisit whether to embed.)*

---

### F8 — Offline guarantee

Confirmed as part of the definition of done. This must be architectural and verifiable, not a claim in the About box.

**FR-8.1** The application makes **zero outbound network connections** under all conditions, including error paths and first run.

**FR-8.2** No update checker, no telemetry, no crash reporting, no analytics, no licence activation, no font or asset fetching, no online help. **The in-app update check is the most likely accidental violation of this requirement** — the default posture of most desktop frameworks and installers. It is explicitly banned; updates are manual installer downloads by the operator.

**FR-8.3** Build-time enforcement: a CI/build step fails the build on any reference to `System.Net.Http`, `HttpClient`, `WebClient`, `System.Net.Sockets`, or equivalent, in the app assembly or any transitive dependency. Every third-party package is licence- and network-audited before inclusion and the audit result recorded in the repository. **Rationale:** the guarantee will otherwise decay the first time someone adds a convenient library. A human promise is not a control.

**FR-8.4** The MSI/MSIX manifest declares no network capabilities. For MSIX specifically, `internetClient` and related capabilities are omitted, so the OS itself enforces the boundary.

**FR-8.5** The app functions identically with all network adapters disabled. No timeout, no degraded mode, no warning banner, no difference in behaviour whatsoever. This is US-15 and it is the operator's own ten-second verification.

**FR-8.6** An "Offline & Privacy" panel states plainly what the app does and does not do with client data, in language the operator can repeat to a client. This is a sales asset as much as a technical statement — it is the answer to the question Nikhil currently cannot answer.

**FR-8.7** The installer is **code-signed** with a valid certificate. *This is a hard external dependency with real cost and lead time — an OV or EV certificate, purchased and validated. Unsigned, Windows SmartScreen will warn on every install, which materially undermines a product whose entire pitch is trustworthiness. Flagging it as a dependency to resolve early, not a build-time detail.*

**FR-8.8** The release process produces a **documented verification procedure and its recorded results** — Wireshark or Fiddler capture over a full session covering every feature, plus `netstat` observation, plus an adapter-disabled functional run. This is a shipping gate, not a suggestion. See §9 M4.

---

## 7. Data model sketch

Conceptual. Field names indicative, not final schema.

### `Client`
| Field | Type | Notes |
|---|---|---|
| `id` | GUID | PK |
| `given_name` | text | Required |
| `family_name` | text | |
| `company` | text | |
| `job_title` | text | |
| `street`, `city`, `state`, `postal_code`, `country` | text | `ADR` components |
| `note` | text | `NOTE` |
| `logo_path` | text | Absolute path; may dangle — see EC-20 |
| `created_at`, `updated_at`, `deleted_at` | datetime | Soft delete |
| `last_exported_at` | datetime | Denormalised for list sort |

### `ContactPoint` — 1..n per Client
Separate table rather than fixed columns, so the count of phones/emails/URLs is open-ended without schema change.

| Field | Type | Notes |
|---|---|---|
| `id` | GUID | PK |
| `client_id` | GUID | FK → Client |
| `kind` | enum | `Phone` / `Email` / `Url` |
| `subtype` | enum | `Cell`, `Work`, `Fax`, `Home`, `Personal`, `Social` |
| `raw_value` | text | As the operator typed it |
| `normalised_value` | text | E.164 for phones, scheme-prefixed for URLs |
| `is_primary` | bool | Exactly one primary phone required per client |
| `sort_order` | int | Controls vCard emission order (FR-2.8) |

*Storing both raw and normalised is deliberate: the operator must be able to see what they typed and what we transformed it into, and a normalisation bug must be recoverable without data loss.*

### `BrandingProfile` — 0..1 per Client *(persisted in v2; transient in MVP)*
| Field | Type | Notes |
|---|---|---|
| `id` | GUID | PK |
| `client_id` | GUID | FK, nullable — allows a reusable house default |
| `foreground_color` | text | Hex |
| `background_color` | text | Hex |
| `logo_path` | text | **Nullable — logo is optional and null by default** |
| `logo_width_pct` | int | Default 18, max 25 (FR-5.3) |
| `ecc_level` | enum | Operator's chosen no-logo level; default `M`. Persisted separately from the effective level so that removing a logo restores it (FR-5.2). |
| `effective_ecc_level` | enum | Derived, not stored: `H` if `logo_path` is set, else `ecc_level`. Everything in F4 computes against this. |

### `ExportPreset` — application-level, not per-client
| Field | Type | Notes |
|---|---|---|
| `id` | GUID | PK |
| `name` | text | e.g. "Visiting card 25mm", "Window decal 120mm" |
| `width_mm` | decimal | |
| `dpi` | int | |
| `quiet_zone_modules` | int | ≥ 4 (FR-6.5) |
| `filename_template` | text | |
| `is_default` | bool | |

### `ExportRecord` — append-only audit trail (FR-7.7)
| Field | Type | Notes |
|---|---|---|
| `id` | GUID | PK |
| `client_id` | GUID | FK |
| `exported_at` | datetime | |
| `file_path` | text | |
| `payload_bytes` | int | |
| `vcard_snapshot` | text | Exact payload encoded. Enables reproducing a delivered code even after the client record changes. |
| `ecc_level` | enum | |
| `qr_version` | int | |
| `width_mm`, `dpi` | decimal, int | |
| `module_size_mm` | decimal | The number that matters |
| `verdict` | enum | `Safe` / `Marginal` / `WillFail` |
| `unsafe_override` | bool | Was FR-4.5 overridden |
| `self_test_passed` | bool | FR-4.4 result |

*The `vcard_snapshot` is the single most useful field in the model for support. When a client rings about a card printed eight months ago, it reconstructs exactly what was encoded — after the client record has since been edited.*

### `AppSettings` — singleton
`default_country_code`, `library_path`, `default_export_preset_id`, `theme`, `name_input_mode` *(pending §10 Q4)*.

---

## 8. Edge cases and failure states

Grouped by origin. Each states the required behaviour, not just the risk.

### Payload and encoding

- **EC-1 — Payload exceeds capacity at the current ECC level.** Most commonly a fully-populated record plus a logo (forced H). **Behaviour:** blocked at generation with the specific overflow amount and the ranked remedies of FR-4.3, with "remove logo" ranked first when a logo is present because it recovers the most bytes. Never silently drop a field to make it fit.
- **EC-1a — Operator removes the logo but the code is still over budget.** **Behaviour:** ECC reverts to the previous non-logo level (FR-5.2), the budget is recomputed, and the remedy list re-ranks against the new state. The panel must never show remedies computed against a stale ECC level.
- **EC-2 — Non-ASCII names inflate byte count invisibly.** Devanagari, CJK, accented Latin. **Behaviour:** the counter reports UTF-8 bytes (FR-2.5); the per-field byte display makes the cost visible at the field. A CJK name field showing 3× the expected cost is information, not a bug.
- **EC-3 — Emoji in the Note field.** 4 bytes each and a plausible parser break on the receiving device. **Behaviour:** warn on detection in `NOTE`; do not block. Flag for device-matrix testing.
- **EC-4 — Company name containing a comma or semicolon.** `Acme Interiors Pvt Ltd, Mumbai`. **Behaviour:** escaped per FR-2.4. This has a dedicated golden-file test; it is the most common defect in competing tools.
- **EC-5 — Multi-line Note.** **Behaviour:** newlines escaped as literal `\n` (FR-2.4), not emitted as raw line breaks, which would terminate the property and corrupt everything after it.
- **EC-6 — Phone number with no country code.** **Behaviour:** FR-1.4 prompts for the default country. Never guess silently; a wrong country code is a number that dials nowhere.
- **EC-7 — Phone number containing extension or letters** (`+91 22 1234 5678 x204`, `1-800-FLOWERS`). **Behaviour:** flag as unnormalisable, ask the operator to confirm or correct. Emit as typed if confirmed.
- **EC-8 — Very long website or social URL** (tracking-parameter-laden). **Behaviour:** flag with its byte cost and suggest shortening at the source. No online shortener — that would be a network call and a third-party dependency.
- **EC-9 — Whitespace-only "required" field.** **Behaviour:** trimmed, then treated as empty and blocked. A space is not a name.
- **EC-10 — Mononym / single-name client** (common in parts of India and Indonesia). **Behaviour:** family name may be empty; `N:;Given;;;` and `FN:Given` are emitted correctly. Do not require a surname.
- **EC-11 — Identical `TYPE` for two contact points** (two `TYPE=CELL` numbers). **Behaviour:** legal in vCard 3.0 and correctly parsed by both platforms. Emit both. Verify display behaviour on the device matrix.

### Branding and rendering

- **EC-12 — Logo overlaps a finder pattern.** **Behaviour:** geometrically prevented (FR-5.5). Unrecoverable damage, not a warning case.
- **EC-13 — Logo with a transparent background over the modules.** **Behaviour:** opaque pad applied automatically (FR-5.4); the operator is told it was applied.
- **EC-14 — Low-contrast brand colours** — e.g. mid-grey on light grey. **Behaviour:** blocked below threshold (FR-5.7), with the measured ratio, the threshold, and the nearest compliant colour suggested.
- **EC-15 — Inverted colours, light-on-dark.** **Behaviour:** blocked (FR-5.8), with the light-patch-behind-the-QR workaround explained.
- **EC-16 — Extremely large logo file** (a 40MP client photo). **Behaviour:** downsample on import to the needed resolution; do not attempt to render at source size.
- **EC-17 — Logo file is corrupt or an unsupported format masquerading as PNG.** **Behaviour:** caught and reported by filename; does not crash the render pipeline.
- **EC-18 — Decode-back self-test fails despite a Safe verdict.** A genuine defect, most likely in logo compositing or colour handling. **Behaviour:** export blocked, diagnostic details shown, operator invited to report it. Never export a code that failed its own verification.

### Storage and filesystem

- **EC-19 — Library database inside a OneDrive-synced folder.** Highly likely on this machine — the project itself lives under `OneDrive\Documents`. Sync can lock the file mid-write or create `library-DESKTOP-XXX.db` conflict copies. **Behaviour:** default location is outside the sync root (FR-7.1); if the operator relocates it into one, warn once with the specific risk. Detect conflict-copy filenames on startup and surface them rather than silently ignoring them.
- **EC-20 — Dangling logo path** after the operator reorganises client folders. **Behaviour:** flagged when the record is opened, with a re-link prompt. The record remains fully usable without the logo.
- **EC-21 — Library file corrupt or unreadable.** **Behaviour:** app starts in a degraded read-only mode, does not overwrite the damaged file, and points to the most recent JSON backup. Never auto-repair by truncation.
- **EC-22 — Library locked by another instance of the app.** **Behaviour:** detect and either focus the existing instance or open read-only. Do not present two writable views of one database.
- **EC-23 — Export target path is read-only, a disconnected UNC share, or full.** **Behaviour:** distinct, accurate messages per cause. "Export failed" is not an acceptable message when the disk is full.
- **EC-24 — Company name produces an illegal or reserved Windows filename.** `A/B Traders`, or a company literally named `CON`. **Behaviour:** sanitised per FR-6.7 and the resulting filename shown before writing.
- **EC-25 — App installed to, or run from, a read-only location.** **Behaviour:** all writes target `%APPDATA%`; nothing is written beside the executable.
- **EC-26 — Windows display scaling at 150% or 200%.** **Behaviour:** the 1:1 physical preview (FR-3.6) accounts for scaling. An unscaled preview lies about physical size, which is the one thing the preview exists to communicate.

### Downstream, outside the app

- **EC-27 — Operator crops the quiet zone in InDesign.** Outside our control. **Behaviour:** the exported PNG includes the quiet zone as part of the image, and the export confirmation states that the surrounding white space is functional and must not be cropped. Mitigation by communication is the only lever available.
- **EC-28 — QR scaled down after placement in the layout.** **Behaviour:** the same confirmation states the minimum safe width in mm for that specific file. *Consider encoding the safe width into the filename — e.g. `..._min25mm.png` — so the constraint travels with the asset into the layout application. Cheap and effective; recommended.*
- **EC-29 — Recipient's OEM camera does not scan QR codes by default.** Some older Xiaomi, Oppo, and Vivo builds. Outside our control and not fixable by us. **Behaviour:** document it in the device matrix results as a known-affected population so the operator can set client expectations honestly rather than believing the code is broken.
- **EC-30 — Recipient's phone parses the vCard but drops or misplaces fields.** Platform variation in handling `TITLE`, multiple `URL`s, and `NOTE`. **Behaviour:** characterise per-platform in the M1 matrix and document the actual observed behaviour rather than the spec's promise.

---

## 9. Success metrics

This is a single-operator internal tool. Adoption, retention, and DAU are meaningless here and would be theatre. The only metrics that matter are whether the codes work, whether the privacy claim holds, and whether the tool is faster than the alternative.

### Primary — G1, correctness

**M1 — Print scan success rate ≥ 99%, off printed media, one attempt, three seconds.**

The acceptance gate. Measured against a fixed matrix run before every release:

- **Devices (minimum 8):** iPhone current iOS, iPhone one major version back, iPhone ~5 years old on its terminal iOS; Pixel current Android, Samsung mid-range current, Samsung ~4 years old, one budget OEM device (Xiaomi/Realme/Oppo), one tablet.
- **Scanner app:** stock camera only. Google Lens is a *secondary* record, never a pass condition — Meera does not use it.
- **Media:** printed at 300dpi on coated and uncoated card stock. Screen scans do not count toward this metric.
- **Conditions:** indoor fluorescent, warm domestic light, dim; head-on and at ~30°; 15cm and 30cm distance.
- **Payloads:** minimal (name + phone), typical (~8 fields), maximum (all fields populated); each with and without a logo; each at its computed minimum-safe width and at Safe width.
- **Pass condition:** native Add Contact sheet opens with fields correctly populated — not merely "the code decoded."

**M1a — Field fidelity: 100%.** Across the matrix, every populated field lands in the correct field of the recipient's contact record. Zero instances of a comma-containing company name splitting, an address shifting components, or a title landing in the name.

**M1b — Threshold calibration.** M1 results MUST be used to confirm or correct the 0.40/0.50 mm/module thresholds (FR-4.2) and the 7:1 contrast threshold (FR-5.7). Shipping unvalidated thresholds means the product's central claim is a guess.

### Primary — G2, privacy

**M4 — Zero outbound network connections. Absolute; any non-zero result blocks release.**

Verified by packet capture (Wireshark/Fiddler) over a full session exercising every feature including error paths and first run, plus `netstat` observation, plus a complete functional run with all adapters disabled. Result recorded per release. Confirmed as part of the definition of done.

### Secondary — G4, G5, workflow

- **M2 — Print-readiness:** 100% of exported PNGs carry correct `pHYs` DPI metadata and place at the intended physical size in InDesign without manual scaling. Verified by direct placement test, not by inspecting our own code.
- **M3 — Time to regenerate an existing client** after a single detail change: **≤ 15 seconds**, open-app to exported-file. Directly measures whether the library (F7) is worth its cost.
- **M5 — Time to first export for a brand-new client:** **≤ 90 seconds**, including entering a full field set.
- **M6 — Self-test pass rate ≥ 99.9%** across the internal render-and-decode corpus. Failures indicate rendering defects (FR-4.4).
- **M7 — Warning precision.** Of codes the app rated **Safe**, ≥ 99% pass M1. Of codes it rated **Will fail**, ≥ 90% actually fail M1. *The second half matters as much as the first: a gate that cries wolf gets overridden reflexively, and then FR-4.5 protects nobody.*

### Outcome — the metric that actually pays

- **M8 — Zero reprints attributable to QR failure, per 1,000 cards shipped.** Tracked manually by the operator. Slow, lagging, and the only measurement that reflects real money. Baseline it against the current tooling before v1 ships, or the comparison is unavailable forever.

### Deliberately not measured

DAU/MAU, session length, feature engagement, NPS, funnels. One user. Instrumenting any of this would also require telemetry, which is banned by FR-8.2.

---

## 10. Open questions

Ordered by how much a late answer costs.

| # | Question | Why it matters | Owner | Needed by |
|---|---|---|---|---|
| **Q1** | **Product name.** "ContactQR" is a placeholder. | Blocks the code-signing certificate (Q2), the installer identity, and the MSIX package identity — none of which can be renamed cheaply after first release. | Operator | Before installer work |
| **Q2** | **Code-signing certificate — which, who buys it, what lead time?** OV vs EV. | FR-8.7. Real money and real validation lead time. Unsigned, SmartScreen warns on every install, which is corrosive for a product selling trustworthiness. This is the most likely schedule risk in the project. | Operator | Immediately — lead time is external |
| **Q3** | **MSI or MSIX?** The stack answer specified "MSI/MSIX installer" without choosing. | MSIX gives OS-enforced capability restrictions that strengthen the offline guarantee (FR-8.4) and cleaner install/uninstall, but adds packaging complexity and a Store-style identity model. MSI is more familiar and more flexible. **Recommendation: MSIX**, specifically because the declarative absence of network capabilities converts FR-8.1 from a code review promise into an OS-level control. | Product + operator | Before installer work |
| **Q4** | **Split given/family name, or a single full-name field?** | FR-1.7. This document assumes split, to avoid guessing surname position for mononyms, initial-first South Indian names, and CJK ordering — which would corrupt the `N` property. But it is a real ergonomic cost on every entry. **Flagged rather than decided.** | Operator | Before F1 build |
| **Q5** | **What is the actual QR width in your typical card layouts, and how often do clients actually want the logo in the code?** | The entire Scannability Budget calibration (F4) hinges on the real target. At 30mm without a logo, a Typical payload is comfortably Safe and F4 is mostly reassurance; at 22mm with a logo, almost everything is Marginal and F4 becomes the primary interaction the operator has with the app. That difference changes the UI's centre of gravity. Answer with three or four real recent layouts and a rough logo hit-rate. | Operator | Before F4 build |
| **Q6** | **Should minimum-safe-width be encoded into the exported filename?** EC-28 recommends yes. | The constraint then travels with the asset into InDesign, where the scaling mistake actually happens. Cost is uglier filenames. **Recommendation: yes, as an option, default on.** | Operator | Before F6 build |
| **Q7** | **Should vector export (SVG/PDF) be promoted from v2 into v1?** | It eliminates the entire DPI and module-rounding problem class (FR-3.7, FR-6.4, FR-6.6) rather than managing it, and it is what a commercial press prefers. The counter-argument is that logo compositing and the decode-back self-test are both meaningfully harder against vector output. **Genuinely uncertain — worth an explicit decision rather than a default.** | Product + operator | Before F6 build |
| **Q8** | **Where do the 0.40mm/module and 7:1 contrast thresholds actually land?** | Stated as hypotheses (FR-4.2, FR-5.7), to be corrected by M1b measurement. Until measured, the product's core claim rests on published rules of thumb rather than this product's own evidence. Not a blocker to building — a blocker to *claiming*. | Engineering | During M1 |
| **Q9** | **Is the unfolded-long-lines deviation (FR-2.6) safe across the full device matrix?** | A deliberate departure from RFC 2426. Believed safe on modern parsers and it saves scarce bytes, but if any matrix device fails on it, folding becomes mandatory and every payload gets larger — which changes F4's arithmetic. | Engineering | During M1 |
| **Q10** | **Does the operator ever need to hand a client a `.vcf` file rather than a QR?** | Currently v2 item 3, at near-zero marginal cost since the encoder already exists. If it comes up in practice, it is a trivial v1 addition. | Operator | Any time |
| **Q11** | **Backup discipline for the library.** FR-7.6 provides manual JSON export. Is manual sufficient, or is a scheduled local auto-backup needed? | The library becomes the operator's client record of value within a year. Losing it is a serious business event. Auto-backup to a local folder is cheap; auto-backup into OneDrive reintroduces EC-19. | Operator | Before v1.1 |

---

*End of PRD.*
