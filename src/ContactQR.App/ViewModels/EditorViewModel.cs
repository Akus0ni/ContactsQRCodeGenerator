using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ContactQR.Core.Contacts;
using ContactQR.Core.Scannability;
using ContactQR.Core.VCard;
using ContactQR.App.Diagnostics;
using ContactQR.App.Views;
using ContactQR.Rendering;
using ContactQR.Storage;
using SkiaSharp;

namespace ContactQR.App.ViewModels;

/// <summary>
/// The Editor: contact form, live preview and the Scannability Budget.
/// </summary>
public sealed partial class EditorViewModel : ObservableObject
{
    private readonly ScannabilityCalculator calculator = new();
    private readonly QrExporter exporter = new();
    private readonly DispatcherTimer debounce;
    private readonly ClientLibrary library;

    private ScannabilityAssessment? lastAssessment;

    [ObservableProperty]
    private string givenName = string.Empty;

    [ObservableProperty]
    private string familyName = string.Empty;

    [ObservableProperty]
    private string company = string.Empty;

    [ObservableProperty]
    private string jobTitle = string.Empty;

    [ObservableProperty]
    private string mobile = string.Empty;

    [ObservableProperty]
    private string workPhone = string.Empty;

    [ObservableProperty]
    private string workEmail = string.Empty;

    [ObservableProperty]
    private string website = string.Empty;

    [ObservableProperty]
    private string street = string.Empty;

    [ObservableProperty]
    private string city = string.Empty;

    [ObservableProperty]
    private string postalCode = string.Empty;

    [ObservableProperty]
    private string note = string.Empty;

    [ObservableProperty]
    private bool hasLogo;

    [ObservableProperty]
    private decimal printWidthMillimetres = 25m;

    [ObservableProperty]
    private BitmapSource? preview;

    [ObservableProperty]
    private string moduleSizeReadout = "—";

    [ObservableProperty]
    private string verdictGlyph = string.Empty;

    [ObservableProperty]
    private string verdictWord = "Enter a name and mobile";

    [ObservableProperty]
    private string verdictBrushKey = "TextTertiary";

    [ObservableProperty]
    private string payloadReadout = "0 B";

    [ObservableProperty]
    private string eccReadout = "—";

    [ObservableProperty]
    private string versionReadout = "—";

    [ObservableProperty]
    private string minimumSafeWidthReadout = "—";

    [ObservableProperty]
    private double budgetFraction;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool canExport;

    [ObservableProperty]
    private string vCardPayload = string.Empty;

    /// <summary>The library record being edited, or null for an unsaved client.</summary>
    [ObservableProperty]
    private Guid? clientId;

    [ObservableProperty]
    private string saveState = "Not saved";

    public EditorViewModel(ClientLibrary library)
    {
        ArgumentNullException.ThrowIfNull(library);
        this.library = library;

        // 250ms debounce. Re-encoding per keystroke is wasteful and makes the byte counter
        // flicker distractingly (DESIGN FR-3.5).
        debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        debounce.Tick += (_, _) =>
        {
            debounce.Stop();
            Regenerate();
        };

        foreach (var guidance in ContactFieldGuidanceCatalogue.All)
        {
            FieldGuidance.Add(guidance);
        }

        // The locked state has to be correct before the first keystroke, not only after the
        // first debounce. Without this the panel opens blank and says nothing about what is
        // missing, which is the opposite of principle P1.
        Regenerate();
    }

    /// <summary>Tooltip content for every field, bound directly from the domain catalogue.</summary>
    public ObservableCollection<ContactFieldGuidance> FieldGuidance { get; } = [];

    /// <summary>The remedies offered when a code is over budget, ranked by bytes recovered.</summary>
    public ObservableCollection<string> Remedies { get; } = [];

    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName is null || !AffectsTheCode(e.PropertyName))
        {
            return;
        }

        SaveState = ClientId is null ? "Not saved" : "Unsaved changes";

        debounce.Stop();
        debounce.Start();
    }

    private static bool AffectsTheCode(string propertyName) => propertyName is
        nameof(GivenName) or nameof(FamilyName) or nameof(Company) or nameof(JobTitle)
        or nameof(Mobile) or nameof(WorkPhone) or nameof(WorkEmail) or nameof(Website)
        or nameof(Street) or nameof(City) or nameof(PostalCode) or nameof(Note)
        or nameof(HasLogo) or nameof(PrintWidthMillimetres);

    private void Regenerate()
    {
        Remedies.Clear();

        if (string.IsNullOrWhiteSpace(GivenName) || string.IsNullOrWhiteSpace(Mobile))
        {
            ShowLockedState();
            return;
        }

        var client = BuildClient();
        var payload = VCardEncoder.Encode(client);
        var payloadBytes = System.Text.Encoding.UTF8.GetByteCount(payload);
        var level = ScannabilityCalculator.EffectiveErrorCorrection(ErrorCorrectionLevel.M, HasLogo);
        var assessment = calculator.Assess(payloadBytes, level, PrintWidthMillimetres);

        VCardPayload = payload;
        lastAssessment = assessment;
        ApplyAssessment(assessment);
        RenderPreview(payload, level);
        BuildRemedies(client, assessment, level);
    }

    private void ShowLockedState()
    {
        Preview = null;
        CanExport = false;
        ModuleSizeReadout = "—";
        VerdictGlyph = string.Empty;
        VerdictBrushKey = "TextTertiary";
        PayloadReadout = "0 B";
        EccReadout = "—";
        VersionReadout = "—";
        MinimumSafeWidthReadout = "—";
        BudgetFraction = 0;
        VCardPayload = string.Empty;
        lastAssessment = null;

        VerdictWord = string.IsNullOrWhiteSpace(GivenName)
            ? "Add a given name"
            : "Add a mobile number";

        StatusMessage = "A given name and a mobile number are the only fields a code cannot be built without.";
    }

    private void ApplyAssessment(ScannabilityAssessment assessment)
    {
        PayloadReadout = assessment.Verdict is ScannabilityVerdict.ExceedsCapacity
            ? $"{assessment.PayloadBytes} B · {assessment.OverflowBytes} B over"
            : $"{assessment.PayloadBytes} / {assessment.CapacityBytes} B";

        EccReadout = HasLogo
            ? $"{assessment.ErrorCorrection} — forced by logo"
            : assessment.ErrorCorrection.ToString();

        VersionReadout = assessment.Version is 0
            ? "—"
            : $"{assessment.Version} · {assessment.TotalModulesPerSide} modules";

        ModuleSizeReadout = assessment.Version is 0
            ? "—"
            : assessment.ModuleSizeMillimetres.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

        MinimumSafeWidthReadout = assessment.Version is 0
            ? "—"
            : $"{assessment.MinimumSafeWidthMillimetres:0.0} mm";

        BudgetFraction = assessment.CapacityBytes is 0
            ? 0
            : Math.Min(1.0, assessment.PayloadBytes / (double)assessment.CapacityBytes);

        (VerdictGlyph, VerdictWord, VerdictBrushKey) = assessment.Verdict switch
        {
            ScannabilityVerdict.Safe => ("✓", "Safe", "VerdictSafeText"),
            ScannabilityVerdict.Marginal => ("!", "Marginal", "VerdictMarginalText"),
            ScannabilityVerdict.WillFail => ("✕", "Will fail", "VerdictFailText"),
            _ => ("✕", "Too much data", "VerdictFailText"),
        };

        CanExport = assessment.Verdict is not ScannabilityVerdict.ExceedsCapacity;

        StatusMessage = assessment.Verdict switch
        {
            ScannabilityVerdict.Safe =>
                $"Prints reliably at {PrintWidthMillimetres:0.#} mm.",
            ScannabilityVerdict.Marginal =>
                $"Readable, but with little margin. {assessment.MinimumSafeWidthMillimetres:0.0} mm would be safe.",
            ScannabilityVerdict.WillFail =>
                $"Below the size a phone camera resolves off print. Widen to {assessment.MinimumSafeWidthMillimetres:0.0} mm or remove a field.",
            _ =>
                $"{assessment.OverflowBytes} bytes too many for any QR code at this correction level.",
        };
    }

    private void RenderPreview(string payload, ErrorCorrectionLevel level)
    {
        try
        {
            var symbol = QrEncoder.Encode(payload, level);
            var options = new QrRenderOptions();

            using var bitmap = QrImageRenderer.Render(symbol, modulePixels: 8, options);
            var selfTest = QrSelfTest.Verify(bitmap, payload);

            if (!selfTest.Passed)
            {
                CanExport = false;
                StatusMessage = selfTest.Diagnostics ?? "The rendered code failed verification.";
            }

            Preview = ToBitmapSource(bitmap);
        }
        catch (ArgumentException)
        {
            Preview = null;
            CanExport = false;
        }
    }

    private void BuildRemedies(ClientRecord client, ScannabilityAssessment assessment, ErrorCorrectionLevel level)
    {
        if (assessment.Verdict is ScannabilityVerdict.Safe)
        {
            return;
        }

        // Ranked by bytes recovered, so the largest lever is always first. With a logo present
        // that is almost always the logo itself (PRD FR-4.3).
        if (HasLogo)
        {
            var without = calculator.Assess(
                assessment.PayloadBytes,
                ErrorCorrectionLevel.M,
                PrintWidthMillimetres);

            Remedies.Add(Describe(
                "Remove logo — error correction drops H to M",
                assessment.PayloadBytes,
                without));
        }

        AddFieldRemedy(client, "Remove postal address", client with { Address = null }, level);
        AddFieldRemedy(client, "Remove note", client with { Note = null }, level);

        Remedies.Add(
            $"Widen to {assessment.MinimumSafeWidthMillimetres:0.0} mm  →  reaches Safe");
    }

    private void AddFieldRemedy(ClientRecord original, string action, ClientRecord reduced, ErrorCorrectionLevel level)
    {
        var originalBytes = VCardEncoder.MeasureBytes(original);
        var reducedBytes = VCardEncoder.MeasureBytes(reduced);

        if (reducedBytes >= originalBytes)
        {
            return;
        }

        var assessment = calculator.Assess(reducedBytes, level, PrintWidthMillimetres);

        Remedies.Add($"{action}  ·  +{originalBytes - reducedBytes} B  →  "
            + $"{assessment.ModuleSizeMillimetres:0.00} mm · {Word(assessment.Verdict)}");
    }

    private static string Describe(string action, int payloadBytes, ScannabilityAssessment after) =>
        $"{action}  →  {after.ModuleSizeMillimetres:0.00} mm · {Word(after.Verdict)}";

    private static string Word(ScannabilityVerdict verdict) => verdict switch
    {
        ScannabilityVerdict.Safe => "Safe",
        ScannabilityVerdict.Marginal => "Marginal",
        ScannabilityVerdict.WillFail => "Will fail",
        _ => "Too much data",
    };

    private ClientRecord BuildClient()
    {
        var points = new List<ContactPoint>
        {
            new()
            {
                Kind = ContactPointKind.Phone,
                Subtype = ContactPointSubtype.Mobile,
                RawValue = Mobile.Trim(),
                IsPrimary = true,
            },
        };

        AddIfPresent(points, WorkPhone, ContactPointKind.Phone, ContactPointSubtype.Work, 1);
        AddIfPresent(points, WorkEmail, ContactPointKind.Email, ContactPointSubtype.Work, 2);
        AddIfPresent(points, Website, ContactPointKind.Url, ContactPointSubtype.Social, 3);

        var address = new PostalAddress
        {
            Street = NullIfBlank(Street),
            City = NullIfBlank(City),
            PostalCode = NullIfBlank(PostalCode),
        };

        return new ClientRecord
        {
            GivenName = GivenName.Trim(),
            FamilyName = NullIfBlank(FamilyName),
            Company = NullIfBlank(Company),
            JobTitle = NullIfBlank(JobTitle),
            Note = NullIfBlank(Note),
            Address = address.IsEmpty ? null : address,
            ContactPoints = points,
        };
    }

    private static void AddIfPresent(
        List<ContactPoint> points,
        string value,
        ContactPointKind kind,
        ContactPointSubtype subtype,
        int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        points.Add(new ContactPoint
        {
            Kind = kind,
            Subtype = subtype,
            RawValue = value.Trim(),
            SortOrder = sortOrder,
        });
    }

    private static string? NullIfBlank(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static BitmapImage ToBitmapSource(SKBitmap bitmap)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = new MemoryStream(encoded.ToArray());

        var source = new BitmapImage();
        source.BeginInit();
        source.CacheOption = BitmapCacheOption.OnLoad;
        source.StreamSource = stream;
        source.EndInit();
        source.Freeze();

        return source;
    }

    /// <summary>
    /// Exports a PNG, enforcing both gates.
    /// </summary>
    /// <remarks>
    /// A blocking verdict routes through <see cref="UnsafeExportDialog"/>, which requires a
    /// deliberate acknowledgement and records the override (PRD FR-4.5). A failed self-test is
    /// fatal and has no override at all — that gate protects against having drawn something we
    /// cannot verify, which is never a judgement call to hand the operator.
    /// </remarks>
    [RelayCommand]
    private void ExportPng()
    {
        if (string.IsNullOrWhiteSpace(VCardPayload) || lastAssessment is null)
        {
            return;
        }

        var assessment = lastAssessment;
        var overridden = false;

        if (assessment.BlocksExport)
        {
            if (assessment.Verdict is ScannabilityVerdict.ExceedsCapacity)
            {
                DiagnosticLog.Information(
                    $"Export refused: payload is {assessment.OverflowBytes} bytes over capacity "
                        + $"at {assessment.ErrorCorrection}.");

                StatusMessage = $"{assessment.OverflowBytes} bytes too many to encode at all. "
                    + "Remove a field before exporting.";
                return;
            }

            if (!UnsafeExportDialog.Confirm(Application.Current.MainWindow, assessment))
            {
                DiagnosticLog.Information("Export cancelled at the unsafe-export dialog.");
                StatusMessage = "Export cancelled. Nothing was written.";
                return;
            }

            overridden = true;

            // The override is already written to the export log in the database (PRD FR-4.5).
            // It is repeated here because this is the file the operator can actually read when a
            // printed card comes back failing.
            DiagnosticLog.Warning(
                "Unsafe export override accepted. "
                    + $"Verdict {assessment.Verdict}, "
                    + $"{assessment.ModuleSizeMillimetres:0.000} mm per module "
                    + $"at {PrintWidthMillimetres:0.0} mm.");
        }

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "PNG image (*.png)|*.png",
            FileName = SuggestedFileName(overridden),
            Title = "Export QR code",
        };

        if (dialog.ShowDialog() is not true)
        {
            return;
        }

        var level = ScannabilityCalculator.EffectiveErrorCorrection(ErrorCorrectionLevel.M, HasLogo);
        var result = exporter.Export(new QrExportRequest
        {
            Payload = VCardPayload,
            ErrorCorrection = level,
            WidthMillimetres = PrintWidthMillimetres,
            DotsPerInch = 300,
        });

        if (!result.SelfTest.Passed)
        {
            // A code that fails its own decode-back is a defect in this application, not an
            // operator mistake (PRD EC-18). It needs to leave a trace that can be reported.
            DiagnosticLog.Warning(
                "Decode-back self-test FAILED at export; nothing was written. "
                    + $"{result.SelfTest.Diagnostics}");

            StatusMessage = result.SelfTest.Diagnostics ?? "Verification failed. Nothing was written.";
            return;
        }

        if (!TryWrite(dialog.FileName, result.Png))
        {
            return;
        }

        RecordExport(dialog.FileName, result, overridden);

        DiagnosticLog.Information(
            $"Exported {dialog.FileName} — "
                + $"{result.Assessment.PayloadBytes} bytes, "
                + $"{result.Assessment.ErrorCorrection}, "
                + $"version {result.Assessment.Version}, "
                + $"{result.ActualWidthMillimetres:0.0} mm at 300 dpi, "
                + $"{result.Assessment.ModuleSizeMillimetres:0.000} mm per module, "
                + $"verdict {result.Assessment.Verdict}"
                + (overridden ? ", UNSAFE OVERRIDE" : string.Empty));

        StatusMessage = overridden
            ? $"Exported with an override to {Path.GetFileName(dialog.FileName)}. "
                + "Test it on a printed proof before sending the card to press."
            : $"Exported {result.SidePixels} px square "
                + $"({result.ActualWidthMillimetres:0.0} mm at 300 dpi) to {Path.GetFileName(dialog.FileName)}.";
    }

    private void RecordExport(string filePath, QrExportResult result, bool overridden)
    {
        // An unsaved walk-in job has nothing to attach the log entry to, and saving it here
        // would force a library record the operator did not ask for (PRD FR-7.8).
        if (ClientId is not { } id)
        {
            return;
        }

        library.RecordExport(new ExportLogEntry
        {
            ClientId = id,
            FilePath = filePath,
            VCardSnapshot = VCardPayload,
            PayloadBytes = result.Assessment.PayloadBytes,
            ErrorCorrection = result.Assessment.ErrorCorrection,
            Version = result.Assessment.Version,
            WidthMillimetres = PrintWidthMillimetres,
            ModuleSizeMillimetres = result.Assessment.ModuleSizeMillimetres,
            Verdict = result.Assessment.Verdict,
            UnsafeOverride = overridden,
            SelfTestPassed = result.SelfTest.Passed,
        });
    }

    /// <summary>
    /// Writes a printable sheet carrying this code at several sizes, for physical scan-testing
    /// before a press run (PRD FR-4.6).
    /// </summary>
    /// <remarks>
    /// This is also how the module-size thresholds get calibrated. They are currently an
    /// estimate from published guidance rather than this product's own measurement, and every
    /// printed sheet is one run of that experiment (PRD M1b).
    /// </remarks>
    [RelayCommand]
    private void PrintTestSheet()
    {
        if (string.IsNullOrWhiteSpace(VCardPayload))
        {
            return;
        }

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "PNG image (*.png)|*.png",
            FileName = $"{(string.IsNullOrWhiteSpace(Company) ? GivenName : Company)}_scan_test_sheet.png",
            Title = "Save scan test sheet",
        };

        if (dialog.ShowDialog() is not true)
        {
            return;
        }

        var sheet = new TestSheetComposer().Compose(new TestSheetRequest
        {
            Payload = VCardPayload,
            ErrorCorrection = ScannabilityCalculator.EffectiveErrorCorrection(ErrorCorrectionLevel.M, HasLogo),
            ClientName = string.IsNullOrWhiteSpace(Company) ? GivenName : Company,
        });

        if (!TryWrite(dialog.FileName, sheet.Png))
        {
            return;
        }

        DiagnosticLog.Information(
            $"Saved scan test sheet to {dialog.FileName} with {sheet.Tiles.Count} sizes.");

        StatusMessage = $"Test sheet saved with {sheet.Tiles.Count} sizes. "
            + "Print it at 100% with no scaling, then scan each code with a phone.";
    }

    /// <summary>
    /// Writes an exported file, reporting a refused path rather than failing the application.
    /// </summary>
    /// <remarks>
    /// A read-only folder, a disconnected share or a full disk (PRD EC-23) are ordinary operator
    /// conditions, not crashes, and each needs a message that names its own cause.
    /// </remarks>
    private bool TryWrite(string filePath, byte[] contents)
    {
        try
        {
            File.WriteAllBytes(filePath, contents);
            return true;
        }
        catch (Exception writeFailure) when (
            writeFailure is IOException
                or UnauthorizedAccessException
                or DirectoryNotFoundException)
        {
            DiagnosticLog.Failure($"Could not write {filePath}.", writeFailure);
            StatusMessage = $"Could not write the file: {writeFailure.Message}";
            return false;
        }
    }

    [RelayCommand]
    private void SaveClient()
    {
        if (string.IsNullOrWhiteSpace(GivenName) || string.IsNullOrWhiteSpace(Mobile))
        {
            StatusMessage = "A given name and a mobile number are needed before a client can be saved.";
            return;
        }

        var isNew = ClientId is null;

        ClientId = library.Save(BuildClient(), ClientId);
        SaveState = "Saved";

        DiagnosticLog.Information(
            $"{(isNew ? "Created" : "Updated")} client {ClientId} in the library.");

        StatusMessage = $"Saved {GivenName} to the library.";
    }

    /// <summary>Loads a stored client into the form.</summary>
    /// <param name="stored">The record to edit.</param>
    public void Load(StoredClient stored)
    {
        ArgumentNullException.ThrowIfNull(stored);

        var record = stored.Record;

        ClientId = stored.Id;
        GivenName = record.GivenName;
        FamilyName = record.FamilyName ?? string.Empty;
        Company = record.Company ?? string.Empty;
        JobTitle = record.JobTitle ?? string.Empty;
        Note = record.Note ?? string.Empty;
        Street = record.Address?.Street ?? string.Empty;
        City = record.Address?.City ?? string.Empty;
        PostalCode = record.Address?.PostalCode ?? string.Empty;

        Mobile = record.PrimaryPhone?.ValueToEncode ?? string.Empty;
        WorkPhone = ValueOf(record, ContactPointKind.Phone, ContactPointSubtype.Work);
        WorkEmail = ValueOf(record, ContactPointKind.Email, ContactPointSubtype.Work);
        Website = ValueOf(record, ContactPointKind.Url, ContactPointSubtype.Social);

        SaveState = "Saved";
        Regenerate();
    }

    /// <summary>Clears the form for a new client.</summary>
    public void StartNew()
    {
        ClientId = null;
        GivenName = string.Empty;
        FamilyName = string.Empty;
        Company = string.Empty;
        JobTitle = string.Empty;
        Mobile = string.Empty;
        WorkPhone = string.Empty;
        WorkEmail = string.Empty;
        Website = string.Empty;
        Street = string.Empty;
        City = string.Empty;
        PostalCode = string.Empty;
        Note = string.Empty;
        HasLogo = false;
        SaveState = "Not saved";
        Regenerate();
    }

    private static string ValueOf(ClientRecord record, ContactPointKind kind, ContactPointSubtype subtype) =>
        record.ContactPoints
            .FirstOrDefault(point => point.Kind == kind && point.Subtype == subtype)
            ?.ValueToEncode
        ?? string.Empty;

    private string SuggestedFileName(bool overridden)
    {
        var parts = new[] { Company, GivenName }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => string.Concat(part.Trim().Split(Path.GetInvalidFileNameChars())));

        var stem = string.Join("_", parts);

        if (string.IsNullOrWhiteSpace(stem))
        {
            stem = "contact";
        }

        // The minimum safe width travels with the file into the layout application, which is
        // where the scaling mistake actually happens (PRD EC-28).
        var safeWidth = lastAssessment is null || lastAssessment.Version is 0
            ? string.Empty
            : $"_min{lastAssessment.MinimumSafeWidthMillimetres:0}mm";

        var unsafeSuffix = overridden ? "_UNSAFE" : string.Empty;

        return $"{stem}_QR_{PrintWidthMillimetres:0}mm{safeWidth}{unsafeSuffix}.png";
    }
}
