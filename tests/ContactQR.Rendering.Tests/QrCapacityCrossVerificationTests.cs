using ContactQR.Core.Scannability;
using ContactQR.Rendering;
using FluentAssertions;

namespace ContactQR.Rendering.Tests;

/// <summary>
/// Cross-verifies the hand-entered capacity table against the QR encoder that actually builds
/// the symbols.
/// </summary>
/// <remarks>
/// PRD FR-3.3 makes the encoder the authority on capacity, and the table in
/// <see cref="QrCapacityTable"/> was 160 numbers entered by hand from a published reference.
/// Until this test existed, nothing proved the two agreed. This is the check that lets the
/// table gate a real export.
/// </remarks>
public sealed class QrCapacityCrossVerificationTests
{
    public static TheoryData<ErrorCorrectionLevel> AllLevels { get; } =
    [
        ErrorCorrectionLevel.L,
        ErrorCorrectionLevel.M,
        ErrorCorrectionLevel.Q,
        ErrorCorrectionLevel.H,
    ];

    [Theory]
    [MemberData(nameof(AllLevels))]
    public void TableCapacity_IsAcceptedByTheEncoder_AtEveryVersion(ErrorCorrectionLevel level)
    {
        for (var version = 1; version <= QrCapacityTable.MaximumVersion; version++)
        {
            var capacity = QrCapacityTable.CapacityFor(version, level);
            var payload = new string('A', capacity);

            var symbol = QrEncoder.Encode(payload, level);

            symbol.Version.Should().BeLessThanOrEqualTo(
                version,
                "a payload of exactly the tabulated capacity for version {0} at level {1} must fit within it",
                version,
                level);
        }
    }

    [Theory]
    [MemberData(nameof(AllLevels))]
    public void OneByteOverTableCapacity_ForcesTheEncoderToALargerVersion(ErrorCorrectionLevel level)
    {
        // Checking a spread rather than all 40, because each miss here indicates the same
        // class of transcription error and the run time is not free.
        foreach (var version in new[] { 1, 5, 10, 15, 20, 25, 30, 35, 39 })
        {
            var capacity = QrCapacityTable.CapacityFor(version, level);
            var payload = new string('A', capacity + 1);

            var symbol = QrEncoder.Encode(payload, level);

            symbol.Version.Should().BeGreaterThan(
                version,
                "a payload one byte over the tabulated capacity for version {0} at level {1} must not fit",
                version,
                level);
        }
    }

    [Fact]
    public void EncoderVersion_MatchesTheVersionTheCalculatorPredicts()
    {
        var calculator = new ScannabilityCalculator();

        foreach (var payloadBytes in new[] { 20, 60, 131, 250, 400, 800, 1_500 })
        {
            var payload = new string('A', payloadBytes);
            var predicted = calculator.Assess(payloadBytes, ErrorCorrectionLevel.M, 30m);
            var actual = QrEncoder.Encode(payload, ErrorCorrectionLevel.M);

            actual.Version.Should().Be(
                predicted.Version,
                "the budget panel must predict the version the encoder will actually produce for {0} bytes",
                payloadBytes);
        }
    }

    [Fact]
    public void ModulesPerSide_MatchesTheEncoderOutput()
    {
        foreach (var payloadBytes in new[] { 20, 131, 400, 1_000 })
        {
            var symbol = QrEncoder.Encode(new string('A', payloadBytes), ErrorCorrectionLevel.M);

            symbol.ModulesPerSide.Should().Be(QrCapacityTable.ModulesPerSide(symbol.Version));
            symbol.TotalModulesPerSide.Should().Be(symbol.ModulesPerSide + 8);
        }
    }
}
