using ContactQR.Core.Contacts;
using ContactQR.Core.Scannability;
using ContactQR.Core.Tests.Contacts;
using ContactQR.Storage;
using FluentAssertions;

namespace ContactQR.Core.Tests.Storage;

public sealed class ClientLibraryTests : IDisposable
{
    private readonly ClientLibrary library = new(":memory:");

    public void Dispose() => library.Dispose();

    [Fact]
    public void Save_ThenAll_ReturnsTheStoredClient()
    {
        library.Save(new ClientRecordBuilder().WithCompany("Sunrise Physiotherapy").Build());

        library.All().Should().ContainSingle()
            .Which.Record.Company.Should().Be("Sunrise Physiotherapy");
    }

    [Fact]
    public void Save_RoundTripsEveryField()
    {
        var original = new ClientRecordBuilder()
            .WithCompany("Acme Interiors Pvt Ltd, Mumbai")
            .WithJobTitle("Managing Director")
            .WithNote("Turnkey fit-out")
            .WithAddress(new PostalAddress { Street = "12 MG Road", City = "Mumbai" })
            .Build();

        library.Save(original);

        library.All().Single().Record.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void Save_WithAnExistingId_UpdatesRatherThanDuplicating()
    {
        var id = library.Save(new ClientRecordBuilder().WithCompany("Old name").Build());

        library.Save(new ClientRecordBuilder().WithCompany("New name").Build(), id);

        library.All().Should().ContainSingle()
            .Which.Record.Company.Should().Be("New name");
    }

    [Fact]
    public void Search_MatchesOnCompany()
    {
        library.Save(new ClientRecordBuilder().WithCompany("Sunrise Physiotherapy").Build());
        library.Save(new ClientRecordBuilder().WithGivenName("Rajesh").WithCompany("Kumar Electricals").Build());

        library.Search("kumar").Should().ContainSingle()
            .Which.Record.Company.Should().Be("Kumar Electricals");
    }

    [Fact]
    public void Search_IsCaseInsensitive()
    {
        library.Save(new ClientRecordBuilder().WithCompany("Sunrise Physiotherapy").Build());

        library.Search("SUNRISE").Should().ContainSingle();
    }

    [Fact]
    public void Search_WithEmptyText_ReturnsEverything()
    {
        library.Save(new ClientRecordBuilder().Build());
        library.Save(new ClientRecordBuilder().WithGivenName("Rajesh").Build());

        library.Search("   ").Should().HaveCount(2);
    }

    [Fact]
    public void Delete_HidesTheClient_ButIsSoft()
    {
        var id = library.Save(new ClientRecordBuilder().Build());

        library.Delete(id);

        library.All().Should().BeEmpty();
    }

    [Fact]
    public void Save_AfterDelete_RestoresTheClient()
    {
        var id = library.Save(new ClientRecordBuilder().Build());
        library.Delete(id);

        library.Save(new ClientRecordBuilder().WithCompany("Restored").Build(), id);

        library.All().Should().ContainSingle()
            .Which.Record.Company.Should().Be("Restored");
    }

    [Fact]
    public void RecordExport_StampsTheClient()
    {
        var id = library.Save(new ClientRecordBuilder().Build());

        library.RecordExport(EntryFor(id));

        library.All().Single().LastExportedAt.Should().NotBeNull();
    }

    [Fact]
    public void RecordExport_KeepsTheExactPayloadEncoded()
    {
        var id = library.Save(new ClientRecordBuilder().Build());

        library.RecordExport(EntryFor(id) with { VCardSnapshot = "BEGIN:VCARD\r\nORIGINAL\r\nEND:VCARD\r\n" });

        // The snapshot must survive later edits to the client, so a card printed months ago
        // can still be reconstructed exactly (PRD FR-7.7).
        library.Save(new ClientRecordBuilder().WithCompany("Renamed since").Build(), id);

        library.ExportHistory(id).Single().VCardSnapshot.Should().Contain("ORIGINAL");
    }

    [Fact]
    public void RecordExport_DistinguishesAnOverrideFromANormalExport()
    {
        var id = library.Save(new ClientRecordBuilder().Build());

        library.RecordExport(EntryFor(id));
        library.RecordExport(EntryFor(id) with
        {
            UnsafeOverride = true,
            Verdict = ScannabilityVerdict.WillFail,
        });

        var history = library.ExportHistory(id);

        history.Should().HaveCount(2);
        history.Count(entry => entry.UnsafeOverride).Should().Be(1);
    }

    [Fact]
    public void ExportHistory_ReturnsNewestFirst()
    {
        var id = library.Save(new ClientRecordBuilder().Build());

        library.RecordExport(EntryFor(id) with
        {
            FilePath = "older.png",
            ExportedAt = DateTimeOffset.UtcNow.AddHours(-2),
        });
        library.RecordExport(EntryFor(id) with { FilePath = "newer.png" });

        library.ExportHistory(id)[0].FilePath.Should().Be("newer.png");
    }

    [Fact]
    public void ExportHistory_WithoutAClient_ReturnsEverything()
    {
        var first = library.Save(new ClientRecordBuilder().Build());
        var second = library.Save(new ClientRecordBuilder().WithGivenName("Rajesh").Build());

        library.RecordExport(EntryFor(first));
        library.RecordExport(EntryFor(second));

        library.ExportHistory().Should().HaveCount(2);
    }

    [Fact]
    public void ExportHistory_IsEmpty_BeforeAnyExport()
    {
        library.Save(new ClientRecordBuilder().Build());

        library.ExportHistory().Should().BeEmpty();
    }

    private static ExportLogEntry EntryFor(Guid clientId) => new()
    {
        ClientId = clientId,
        FilePath = @"C:\exports\card.png",
        VCardSnapshot = "BEGIN:VCARD\r\nEND:VCARD\r\n",
        PayloadBytes = 151,
        ErrorCorrection = ErrorCorrectionLevel.M,
        Version = 8,
        WidthMillimetres = 25m,
        ModuleSizeMillimetres = 0.44m,
        Verdict = ScannabilityVerdict.Safe,
        SelfTestPassed = true,
    };

    [Fact]
    public void LastExportedAt_IsNull_BeforeAnyExport()
    {
        library.Save(new ClientRecordBuilder().Build());

        library.All().Single().LastExportedAt.Should().BeNull();
    }

    [Fact]
    public void ExportJson_ProducesReadableBackup_ThatSurvivesWithoutTheApplication()
    {
        library.Save(new ClientRecordBuilder().WithCompany("Sunrise Physiotherapy").Build());

        library.ExportJson().Should().Contain("Sunrise Physiotherapy");
    }
}
