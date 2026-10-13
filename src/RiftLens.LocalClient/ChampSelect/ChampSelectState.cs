namespace RiftLens.LocalClient.ChampSelect;

/// <summary>
/// Vue du champ select limitée aux champions, rôles et bans.
/// <para>
/// Volontairement, ce modèle ne contient <b>aucune</b> information d'identité (nom, Riot ID, PUUID, summonerId) :
/// en classé, Riot masque les noms des coéquipiers pendant la sélection et RiftLens ne doit pas les révéler.
/// </para>
/// </summary>
public sealed record ChampSelectState(
    long GameId,
    int LocalCellId,
    string Phase,
    TimeSpan TimeLeft,
    IReadOnlyList<ChampSelectSlot> MyTeam,
    IReadOnlyList<ChampSelectSlot> TheirTeam,
    IReadOnlyList<int> MyBans,
    IReadOnlyList<int> TheirBans)
{
    public ChampSelectSlot? Local => MyTeam.FirstOrDefault(s => s.IsLocal);

    /// <summary>Champion verrouillé ou survolé par le joueur local (0 si aucun).</summary>
    public int LocalChampionId => Local is { } l ? l.DisplayedChampionId : 0;
}

/// <param name="Role">Top, Jungle, Middle, Bottom, Support ou null (inconnu, toujours le cas côté adverse).</param>
public sealed record ChampSelectSlot(int CellId, int ChampionId, int PickIntentId, string? Role, bool IsLocal, bool IsLocked)
{
    public int DisplayedChampionId => ChampionId > 0 ? ChampionId : PickIntentId;
}
