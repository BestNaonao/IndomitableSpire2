using MegaCrit.Sts2.Core.TestSupport;
#if !DISABLESTEAMWORKS
using MegaCrit.Sts2.Core.Multiplayer.Transport.Steam;
using MegaCrit.Sts2.Core.Platform.Steam;
using Steamworks;
#endif

namespace TashkentSpire2.TashkentSpire2Code.Workshop;

internal sealed class SteamWorkshopVoteClient : IWorkshopVoteClient
{
    internal const ulong WorkshopId = 3747818292UL;
#if !DISABLESTEAMWORKS
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(15);
#endif

    public bool TryGetCurrentUser(out ulong userId)
    {
        userId = 0;
        if (!MainFile.EnableWorkshopVote)
            return false;
#if !DISABLESTEAMWORKS
        try
        {
            if (!TestMode.IsOn && SteamInitializer.Initialized && SteamUser.BLoggedOn())
                userId = SteamUser.GetSteamID().m_SteamID;
        }
        catch (Exception)
        {
        }
#endif
        return userId != 0;
    }

    public async Task<WorkshopUserVote> GetVoteAsync(ulong userId, CancellationToken cancellationToken)
    {
#if !DISABLESTEAMWORKS
        VerifyUser(userId, cancellationToken);
        SteamAPICall_t call = SteamUGC.GetUserItemVote(new PublishedFileId_t(WorkshopId));
        if (call == SteamAPICall_t.Invalid)
            throw new InvalidOperationException("Steam rejected the workshop vote query.");

        using var result = new SteamCallResult<GetUserItemVoteResult_t>(call);
        GetUserItemVoteResult_t response = await result.Task.WaitAsync(RequestTimeout, cancellationToken);
        VerifyUser(userId, cancellationToken);
        if (response.m_nPublishedFileId.m_PublishedFileId != WorkshopId || response.m_eResult != EResult.k_EResultOK
            || (response.m_bVotedUp && response.m_bVotedDown))
            throw new InvalidOperationException($"Unexpected workshop vote response: {response.m_eResult}.");

        return response.m_bVotedUp ? WorkshopUserVote.Up : response.m_bVotedDown ? WorkshopUserVote.Down : WorkshopUserVote.None;
#else
        await Task.CompletedTask;
        throw new PlatformNotSupportedException("Steamworks is disabled.");
#endif
    }

    public async Task VoteUpAsync(ulong userId, CancellationToken cancellationToken)
    {
#if !DISABLESTEAMWORKS
        VerifyUser(userId, cancellationToken);
        SteamAPICall_t call = SteamUGC.SetUserItemVote(new PublishedFileId_t(WorkshopId), true);
        if (call == SteamAPICall_t.Invalid)
            throw new InvalidOperationException("Steam rejected the workshop vote submission.");

        using var result = new SteamCallResult<SetUserItemVoteResult_t>(call);
        SetUserItemVoteResult_t response = await result.Task.WaitAsync(RequestTimeout, cancellationToken);
        VerifyUser(userId, cancellationToken);
        if (response.m_nPublishedFileId.m_PublishedFileId != WorkshopId || response.m_eResult != EResult.k_EResultOK || !response.m_bVoteUp)
            throw new InvalidOperationException($"Workshop vote was not confirmed: {response.m_eResult}.");
#else
        await Task.CompletedTask;
        throw new PlatformNotSupportedException("Steamworks is disabled.");
#endif
    }

    private void VerifyUser(ulong userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (userId == 0 || !TryGetCurrentUser(out ulong currentUserId) || userId != currentUserId)
            throw new InvalidOperationException("The Steam account is no longer available.");
    }
}
