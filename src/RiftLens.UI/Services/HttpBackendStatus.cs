using System.Net;
using System.Net.Http.Json;
using RiftLens.Contracts;

namespace RiftLens.UI.Services;

/// <summary>Interroge <c>/api/meta</c> pour connaître l'état du backend.</summary>
public sealed class HttpBackendStatus(HttpClient http) : IBackendStatus
{
    public BackendState State { get; private set; }

    public ServerInfoDto? ServerInfo { get; private set; }

    public event Action? Changed;

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        BackendState next;
        var previousInfo = ServerInfo;
        try
        {
            using var response = await http.GetAsync(new Uri("api/meta", UriKind.Relative), cancellationToken);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                next = BackendState.Unauthorized;
            }
            else
            {
                response.EnsureSuccessStatusCode();
                ServerInfo = await response.Content.ReadFromJsonAsync<ServerInfoDto>(cancellationToken);
                next = BackendState.Online;
            }
        }
        catch (HttpRequestException)
        {
            next = BackendState.Offline;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            next = BackendState.Offline; // timeout
        }

        if (next != State || previousInfo != ServerInfo)
        {
            State = next;
            Changed?.Invoke();
        }
    }
}
