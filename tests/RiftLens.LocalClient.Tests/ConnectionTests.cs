using System.Net.Security;
using System.Text.Json;
using RiftLens.LocalClient.Connection;

namespace RiftLens.LocalClient.Tests;

public class ConnectionTests
{
    [Fact]
    public void Lockfile_est_analyse()
    {
        Assert.True(LcuCredentials.TryParseLockfile("LeagueClient:18324:52437:Xq3vT9-bZpL0aRk2w:https\n", out var c));
        Assert.Equal((52437, "Xq3vT9-bZpL0aRk2w", "https"), (c.Port, c.Password, c.Protocol));
        Assert.Equal("https://127.0.0.1:52437/", c.BaseUri.ToString());
        Assert.Equal("wss://127.0.0.1:52437/", c.WebSocketUri.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("LeagueClient:1:abc:pw:https")]
    [InlineData("LeagueClient:1:52437::https")]
    [InlineData("pas un lockfile")]
    public void Lockfile_invalide_est_rejete(string content) => Assert.False(LcuCredentials.TryParseLockfile(content, out _));

    [Fact]
    public void Arguments_de_LeagueClientUx_en_repli()
    {
        const string cmd = "\"C:/Riot Games/League of Legends/LeagueClientUx.exe\" \"--riotclient-auth-token=AAA\" \"--app-port=52437\" \"--remoting-auth-token=Xq3vT9-bZpL0aRk2w\" \"--region=EUW\"";
        Assert.True(LcuCredentials.TryParseCommandLine(cmd, out var c));
        Assert.Equal((52437, "Xq3vT9-bZpL0aRk2w"), (c.Port, c.Password));
    }

    [Fact]
    public void Authentification_basic_riot()
    {
        var c = new LcuCredentials(1, "pw");
        Assert.Equal("Basic cmlvdDpwdw==", c.AuthorizationHeader.ToString()); // riot:pw
        Assert.DoesNotContain("pw", c.ToString(), StringComparison.Ordinal); // jamais de mot de passe dans les logs
    }

    [Theory]
    [InlineData("https://127.0.0.1:52437/", SslPolicyErrors.RemoteCertificateChainErrors, true)]
    [InlineData("https://127.0.0.1:2999/", SslPolicyErrors.RemoteCertificateNameMismatch, true)]
    [InlineData("https://europe.api.riotgames.com/", SslPolicyErrors.RemoteCertificateChainErrors, false)]
    [InlineData("https://192.168.1.10:52437/", SslPolicyErrors.RemoteCertificateChainErrors, false)]
    [InlineData("https://localhost.evil.com/", SslPolicyErrors.RemoteCertificateChainErrors, false)]
    [InlineData("https://europe.api.riotgames.com/", SslPolicyErrors.None, true)]
    public void Certificat_auto_signe_accepte_uniquement_en_loopback(string uri, SslPolicyErrors errors, bool expected) =>
        Assert.Equal(expected, LoopbackCertificatePolicy.IsAcceptable(new Uri(uri), errors));

    [Fact]
    public void Message_WAMP_d_evenement()
    {
        Assert.True(WampMessage.TryParseEvent(Fixture.Read("wamp-event.json"), out var evt));
        Assert.Equal(WampMessage.ChampSelectTopic, evt.Topic);
        Assert.Equal("Update", evt.EventType);
        Assert.Equal(JsonValueKind.Object, evt.Data.ValueKind);
        Assert.Equal("[5,\"OnJsonApiEvent_lol-champ-select_v1_session\"]", WampMessage.SubscribeTo(WampMessage.ChampSelectTopic));
    }

    [Theory]
    [InlineData("[0,\"session\",1,\"server\"]")]
    [InlineData("pas du json")]
    [InlineData("[8,\"topic\",\"pas un objet\"]")]
    public void Messages_WAMP_ignores(string message) => Assert.False(WampMessage.TryParseEvent(message, out _));
}
