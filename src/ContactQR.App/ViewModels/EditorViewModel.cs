using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ContactQR.Core.Contacts;
using ContactQR.Core.Scannability;
using ContactQR.Core.VCard;
using ContactQR.Rendering;
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

    public EditorViewModel()
    {
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

    [RelayCommand]
    private void ExportPng()
    {
        if (string.IsNullOrWhiteSpace(VCardPayload))
        {
            return;
        }

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "PNG image (*.png)|*.png",
            FileName = SuggestedFileName(),
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
            StatusMessage = result.SelfTest.Diagnostics ?? "Verification failed. Nothing was written.";
            return;
        }

        File.WriteAllBytes(dialog.FileName, result.Png);

        StatusMessage = $"Exported {result.SidePixels} × {result.SidePixels} px "
            + $"({result.ActualWidthMillimetres:0.0} mm at 300 dpi) to {Path.GetFileName(dialog.FileName)}.";
    }

    private string SuggestedFileName()
    {
        var parts = new[] { Company, GivenName }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => string.Concat(part.Trim().Split(Path.GetInvalidFileNameChars())));

        var stem = string.Join('_', parts);

        if (string.IsNullOrWhiteSpace(stem))
        {
            stem = "contact";
        }

        return $"{stem}_QR_{PrintWidthMillimetres:0}mm.png";
    }
}
