using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RiftLens.Infrastructure.Local;
using RiftLens.Infrastructure.Riot;

namespace RiftLens.Infrastructure.Tests.Riot;

public sealed class RiotKeyStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "riftlens-tests-" + Guid.NewGuid().ToString("N"));

    private RiotKeyStore Create(string configKey = "") =>
        new(Options.Create(new RiotApiOptions { ApiKey = configKey }), Options.Create(new LocalModeOptions { DataDirectory = _dir }), NullLogger<RiotKeyStore>.Instance);

    [Fact]
    public async Task La_cle_est_persistee_et_relue_par_une_nouvelle_instance()
    {
        var store = Create();
        Assert.Equal(RiotKeyState.Missing, store.State);
        var changed = false;
        store.KeyChanged += () => changed = true;

        await store.SetKeyAsync("  RGAPI-00000000-0000-0000-0000-000000000000 ");

        Assert.True(changed);
        Assert.Equal(RiotKeyState.Valid, store.State);
        Assert.Equal("RGAPI-00000000-0000-0000-0000-000000000000", Create().GetKey());
        if (!OperatingSystem.IsWindows())
        {
            Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(Path.Combine(_dir, LocalModeOptions.RiotKeyFileName)));
        }
    }

    [Fact]
    public void La_configuration_est_prioritaire() => Assert.Equal("RGAPI-config", Create("RGAPI-config").GetKey());

    [Fact]
    public async Task Etat_refuse_puis_accepte()
    {
        var store = Create();
        await store.SetKeyAsync("RGAPI-x");
        store.ReportRejected();
        Assert.Equal(RiotKeyState.Rejected, store.State);
        store.ReportAccepted();
        Assert.Equal(RiotKeyState.Valid, store.State);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }
}
