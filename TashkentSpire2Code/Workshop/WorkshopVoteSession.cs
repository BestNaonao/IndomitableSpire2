namespace TashkentSpire2.TashkentSpire2Code.Workshop;

internal enum WorkshopVoteState
{
    Unknown,
    Checking,
    Available,
    Submitting,
    VotedUp,
    Unavailable
}

internal enum WorkshopUserVote
{
    None,
    Down,
    Up
}

internal interface IWorkshopVoteClient
{
    bool TryGetCurrentUser(out ulong userId);
    Task<WorkshopUserVote> GetVoteAsync(ulong userId, CancellationToken cancellationToken);
    Task VoteUpAsync(ulong userId, CancellationToken cancellationToken);
}

internal sealed class WorkshopVoteSession(IWorkshopVoteClient client, Action<string> logWarning)
{
    private CancellationTokenSource? _operation;
    private long _generation;

    public WorkshopVoteState State { get; private set; }
    public ulong UserId { get; private set; }
    public bool WasVotedDown { get; private set; }
    public event Action? Changed;

    public void Reset()
    {
        _generation++;
        var previous = _operation;
        _operation = null;
        previous?.Cancel();
        UserId = 0;
        WasVotedDown = false;
        SetState(WorkshopVoteState.Unknown);
    }

    public async Task RefreshAsync()
    {
        if (State != WorkshopVoteState.Unknown || _operation != null)
            return;

        if (!client.TryGetCurrentUser(out ulong userId) || userId == 0)
        {
            SetState(WorkshopVoteState.Unavailable);
            return;
        }

        UserId = userId;
        long generation = ++_generation;
        using var operation = new CancellationTokenSource();
        _operation = operation;
        SetState(WorkshopVoteState.Checking);
        try
        {
            operation.Token.ThrowIfCancellationRequested();
            WorkshopUserVote vote = await client.GetVoteAsync(userId, operation.Token);
            if (!IsCurrent(generation, userId))
                return;

            WasVotedDown = vote == WorkshopUserVote.Down;
            SetState(vote == WorkshopUserVote.Up ? WorkshopVoteState.VotedUp : WorkshopVoteState.Available);
        }
        catch (OperationCanceledException)
        {
            if (generation == _generation)
                SetState(WorkshopVoteState.Unavailable);
        }
        catch (Exception exception)
        {
            if (generation == _generation)
            {
                SetState(WorkshopVoteState.Unavailable);
                logWarning($"Workshop vote query failed: {exception.Message}");
            }
        }
        finally
        {
            if (ReferenceEquals(_operation, operation))
                _operation = null;
        }
    }

    public async Task<bool> VoteUpAsync(ulong confirmedUserId)
    {
        if (State != WorkshopVoteState.Available || _operation != null || UserId != confirmedUserId)
            return false;

        if (!client.TryGetCurrentUser(out ulong currentUserId) || currentUserId != confirmedUserId)
        {
            Reset();
            SetState(WorkshopVoteState.Unavailable);
            return false;
        }

        long generation = ++_generation;
        using var operation = new CancellationTokenSource();
        _operation = operation;
        SetState(WorkshopVoteState.Submitting);
        try
        {
            operation.Token.ThrowIfCancellationRequested();
            await client.VoteUpAsync(confirmedUserId, operation.Token);
            if (!IsCurrent(generation, confirmedUserId))
                return false;

            SetState(WorkshopVoteState.VotedUp);
            return true;
        }
        catch (OperationCanceledException)
        {
            if (generation == _generation)
                SetState(WorkshopVoteState.Unavailable);
            return false;
        }
        catch (Exception exception)
        {
            if (generation == _generation)
            {
                SetState(WorkshopVoteState.Unavailable);
                logWarning($"Workshop vote submission failed: {exception.Message}");
            }
            return false;
        }
        finally
        {
            if (ReferenceEquals(_operation, operation))
                _operation = null;
        }
    }

    private bool IsCurrent(long generation, ulong userId)
    {
        if (generation != _generation)
            return false;
        if (client.TryGetCurrentUser(out ulong currentUserId) && currentUserId == userId)
            return true;

        Reset();
        SetState(WorkshopVoteState.Unavailable);
        return false;
    }

    private void SetState(WorkshopVoteState state)
    {
        State = state;
        Changed?.Invoke();
    }
}
