using ContactQR.Core.Scannability;
using FluentAssertions;

namespace ContactQR.Core.Tests.Scannability;

/// <summary>
/// Consistency tests for the capacity table. These cannot prove the table matches ISO/IEC
/// 18004 — only cross-verification against the QR encoder can do that, and PRD FR-3.3 requires
/// it before the table gates any export. What they do catch is transcription error, which is
/// the realistic failure mode for 160 hand-entered numbers.
/// </summary>
public sealed class QrCapacityTableTests
{
    public static TheoryData<ErrorCorrectionLevel> AllLevels =>
    [
        ErrorCorrectionLevel.L,
        ErrorCorrectionLevel.M,
        ErrorCorrectionLevel.Q,
        ErrorCorrectionLevel.H,
    ];

    [Theory]
    [MemberData(nameof(AllLevels))]
    public void Capacity_IncreasesWithVersion_AtEveryCorrectionLevel(ErrorCorrectionLevel level)
    {
        for (var version = QrCapacityTable.MinimumVersion + 1; version <= QrCapacityTable.MaximumVersion; version++)
        {
            QrCapacityTable.CapacityFor(version, level)
                .Should().BeGreaterThan(
                    QrCapacityTable.CapacityFor(version - 1, level),
                    "capacity must grow with version at level {0}", level);
        }
    }

    [Fact]
    public void Capacity_DecreasesAsRedundancyIncreases_AtEveryVersion()
    {
        for (var version = QrCapacityTable.MinimumVersion; version <= QrCapacityTable.MaximumVersion; version++)
        {
            var l = QrCapacityTable.CapacityFor(version, ErrorCorrectionLevel.L);
            var m = QrCapacityTable.CapacityFor(version, ErrorCorrectionLevel.M);
            var q = QrCapacityTable.CapacityFor(version, ErrorCorrectionLevel.Q);
            var h = QrCapacityTable.CapacityFor(version, ErrorCorrectionLevel.H);

            l.Should().BeGreaterThan(m, "L holds more than M at version {0}", version);
            m.Should().BeGreaterThan(q, "M holds more than Q at version {0}", version);
            q.Should().BeGreaterThan(h, "Q holds more than H at version {0}", version);
        }
    }

    [Theory]
    [InlineData(1, ErrorCorrectionLevel.L, 17)]
    [InlineData(10, ErrorCorrectionLevel.H, 119)]
    [InlineData(12, ErrorCorrectionLevel.M, 287)]
    [InlineData(14, ErrorCorrectionLevel.M, 362)]
    [InlineData(15, ErrorCorrectionLevel.H, 220)]
    [InlineData(40, ErrorCorrectionLevel.L, 2_953)]
    [InlineData(40, ErrorCorrectionLevel.H, 1_273)]
    public void Capacity_MatchesKnownReferenceValues(int version, ErrorCorrectionLevel level, int expected)
    {
        QrCapacityTable.CapacityFor(version, level).Should().Be(expected);
    }

    [Theory]
    [InlineData(1, 21)]
    [InlineData(7, 45)]
    [InlineData(12, 65)]
    [InlineData(40, 177)]
    public void ModulesPerSide_FollowsTheSpecificationFormula(int version, int expected)
    {
        QrCapacityTable.ModulesPerSide(version).Should().Be(expected);
    }

    [Fact]
    public void TryFindSmallestVersion_ReturnsTheFirstVersionThatFits()
    {
        QrCapacityTable.TryFindSmallestVersion(287, ErrorCorrectionLevel.M, out var version)
            .Should().BeTrue();

        version.Should().Be(12);
    }

    [Fact]
    public void TryFindSmallestVersion_StepsUp_WhenAPayloadExceedsAVersionByOneByte()
    {
        QrCapacityTable.TryFindSmallestVersion(288, ErrorCorrectionLevel.M, out var version)
            .Should().BeTrue();

        version.Should().Be(13);
    }

    [Fact]
    public void TryFindSmallestVersion_Fails_WhenThePayloadExceedsVersion40()
    {
        QrCapacityTable.TryFindSmallestVersion(1_274, ErrorCorrectionLevel.H, out var version)
            .Should().BeFalse();

        version.Should().Be(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(41)]
    public void CapacityFor_Throws_ForVersionsOutsideTheSpecification(int version)
    {
        var capacity = () => QrCapacityTable.CapacityFor(version, ErrorCorrectionLevel.M);

        capacity.Should().Throw<ArgumentOutOfRangeException>();
    }
}
