using System.Globalization;
using System.Windows;
using ContactQR.Core.Scannability;

namespace ContactQR.App.Views;

/// <summary>
/// The hard gate on exporting a code that will not survive print (PRD FR-4.5).
/// </summary>
public partial class UnsafeExportDialog : Window
{
    private UnsafeExportDialog(ScannabilityAssessment assessment)
    {
        InitializeComponent();

        Explanation.Text = string.Format(
            CultureInfo.InvariantCulture,
            "This code will be about {0:0.00} mm per module at {1:0.#} mm. Below roughly {2:0.00} mm, "
            + "phone cameras stop reading reliably off print — it may work on screen and still fail on the card.",
            assessment.ModuleSizeMillimetres,
            assessment.PrintWidthMillimetres,
            ScannabilityThresholds.Default.FloorMillimetresPerModule);

        Consequences.Text = string.Format(
            CultureInfo.InvariantCulture,
            "The file is written with _UNSAFE in its name, and the override is recorded in the export log "
            + "so you can find it later. Widening to {0:0.0} mm would make it safe.",
            assessment.MinimumSafeWidthMillimetres);
    }

    /// <summary>
    /// Asks the operator to confirm an unsafe export.
    /// </summary>
    /// <param name="owner">The window to centre on.</param>
    /// <param name="assessment">The assessment that blocked the export.</param>
    /// <returns><see langword="true"/> only when the operator deliberately acknowledged and confirmed.</returns>
    public static bool Confirm(Window owner, ScannabilityAssessment assessment)
    {
        ArgumentNullException.ThrowIfNull(assessment);

        var dialog = new UnsafeExportDialog(assessment) { Owner = owner };

        // Focus lands on Cancel, never on the confirm button.
        dialog.Loaded += (_, _) => dialog.CancelButton.Focus();

        return dialog.ShowDialog() is true;
    }

    private void OnAcknowledgementChanged(object sender, RoutedEventArgs e) =>
        ConfirmButton.IsEnabled = Acknowledgement.IsChecked is true;

    private void OnConfirm(object sender, RoutedEventArgs e)
    {
        // Belt and braces: the button is disabled until the box is ticked, but the decision is
        // consequential enough that the handler re-checks rather than trusting enablement.
        if (Acknowledgement.IsChecked is not true)
        {
            return;
        }

        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;
}
