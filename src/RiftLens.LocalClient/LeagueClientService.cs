using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RiftLens.LocalClient.ChampSelect;
using RiftLens.LocalClient.Connection;
using RiftLens.LocalClient.Import;

namespace RiftLens.LocalClient;

/// <summary>
/// Détecte le client LoL, s'abonne aux événements du champ select via le WebSocket WAMP (pas de polling)
/// et expose l'état courant. Se reconnecte automatiquement quand le client redémarre.
/// </summary>
public sealed partial class LeagueClientService(
    ILockfileLocator locator,
    IOptions<LocalClientOptions> options,
    TimeProvider time,
    ILogger<LeagueClientService> logger) : BackgroundService, ILeagueClientService
{
    private LcuHttpClient? _http;

    public LeagueClientStatus Status { get; private set; }

    public ChampSelectState? ChampSelect { get; private set; }

    public event Action? Changed;

    public async Task<ImportResult> ImportRunePageAsync(RunePageRequest request, CancellationToken ct = default)
    {
        if (_http is not { } http)
        {
            return ImportResult.Fail("Client League of Legends non détecté.");
        }

        try { return await RunePageImporter.ImportAsync(http, request, ct); }
        catch (Exception ex) when (ex is LcuRequestException or HttpRequestException)
        {
            LogImportFailed(logger, ex, "runes");
            return ImportResult.Fail("Le client a refusé la page de runes.");
        }
    }

    public async Task<ImportResult> ImportItemSetAsync(ItemSetRequest request, CancellationToken ct = default)
    {
        if (_http is not { } http)
        {
            return ImportResult.Fail("Client League of Legends non détecté.");
        }

        try { return await ItemSetImporter.ImportAsync(http, request, time, ct); }
        catch (Exception ex) when (ex is LcuRequestException or HttpRequestException)
        {
            LogImportFailed(logger, ex, "item set");
            return ImportResult.Fail("Le client a refusé l'item set.");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var credentials = locator.Locate();
            if (credentials is null)
            {
                SetState(LeagueClientStatus.NotRunning, null);
                await DelayAsync(options.Value.DetectionInterval, stoppingToken);
                continue;
            }

            SetState(LeagueClientStatus.Connecting, null);
            try
            {
                await RunSessionAsync(credentials, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex) when (ex is WebSocketException or HttpRequestException or LcuRequestException or IOException)
            {
                // Client en cours de démarrage ou fermé : on réessaie.
                LogSessionEnded(logger, ex.Message);
            }
            finally
            {
                _http?.Dispose();
                _http = null;
            }

            SetState(LeagueClientStatus.NotRunning, null);
            await DelayAsync(options.Value.DetectionInterval, stoppingToken);
        }
    }

    private async Task RunSessionAsync(LcuCredentials credentials, CancellationToken ct)
    {
        using var socket = new ClientWebSocket();
        socket.Options.SetRequestHeader("Authorization", credentials.AuthorizationHeader.ToString());
        socket.Options.RemoteCertificateValidationCallback = LoopbackCertificatePolicy.ForUri(credentials.WebSocketUri);
        socket.Options.AddSubProtocol("wamp");
        await socket.ConnectAsync(credentials.WebSocketUri, ct);
        await SendAsync(socket, WampMessage.SubscribeTo(WampMessage.ChampSelectTopic), ct);

        _http = new LcuHttpClient(credentials);
        var initial = await _http.GetElementAsync("lol-champ-select/v1/session", ct);
        SetState(LeagueClientStatus.Connected, initial is { } session ? ChampSelectMapper.Map(session) : null);
        LogConnected(logger, credentials.Port);

        var buffer = new byte[64 * 1024];
        using var message = new MemoryStream();
        while (socket.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            var result = await socket.ReceiveAsync(buffer, ct);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                break;
            }

            message.Write(buffer, 0, result.Count);
            if (!result.EndOfMessage)
            {
                continue;
            }

            var text = Encoding.UTF8.GetString(message.GetBuffer(), 0, (int)message.Length);
            message.SetLength(0);
            if (text.Length > 0 && WampMessage.TryParseEvent(text, out var evt))
            {
                OnEvent(evt);
            }
        }
    }

    internal void OnEvent(LcuEvent evt)
    {
        if (evt.Topic != WampMessage.ChampSelectTopic)
        {
            return;
        }

        var state = evt.EventType == "Delete" || evt.Data.ValueKind != JsonValueKind.Object ? null : ChampSelectMapper.Map(evt.Data);
        SetState(LeagueClientStatus.Connected, state);
    }

    private void SetState(LeagueClientStatus status, ChampSelectState? champSelect)
    {
        if (Status == status && Equals(ChampSelect, champSelect))
        {
            return;
        }

        Status = status;
        ChampSelect = champSelect;
        Changed?.Invoke();
    }

    private static Task SendAsync(ClientWebSocket socket, string text, CancellationToken ct) =>
        socket.SendAsync(Encoding.UTF8.GetBytes(text), WebSocketMessageType.Text, endOfMessage: true, ct);

    private async Task DelayAsync(TimeSpan delay, CancellationToken ct)
    {
        try { await Task.Delay(delay, time, ct); }
        catch (OperationCanceledException) { /* arrêt demandé */ }
    }

    public override void Dispose()
    {
        _http?.Dispose();
        base.Dispose();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Connecté au client LoL (port {Port})")]
    private static partial void LogConnected(ILogger logger, int port);

    [LoggerMessage(Level = LogLevel.Information, Message = "Session client LoL terminée : {Reason}")]
    private static partial void LogSessionEnded(ILogger logger, string reason);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Import {Kind} refusé par le client LoL")]
    private static partial void LogImportFailed(ILogger logger, Exception ex, string kind);
}
