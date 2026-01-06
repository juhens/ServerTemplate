namespace GameServer.Game.Contexts.Interfaces
{
    public enum TransactionState
    {
        Idle,
        Busy,
        Failed,
        Logout,
    }

    public enum TransactionResult
    {
        Success,

        // Login
        DuplicateAuth,
        OnlyAdult,
        OnlyAdmin,
        BannedAccount,
        InvalidToken,
        TryAgainLater,
        DummyAuthFailed, // dummyClient only

        LoadWorldInfoListFailed,
        LoadPlayerInfoListFailed,
        LoadPlayerFailed,
    }
}
