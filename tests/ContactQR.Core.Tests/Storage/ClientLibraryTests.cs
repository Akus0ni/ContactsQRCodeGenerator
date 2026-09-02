using ContactQR.Core.Contacts;
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

        library.RecordExport(id);

        library.All().Single().LastExportedAt.Should().NotBeNull();
    }

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
