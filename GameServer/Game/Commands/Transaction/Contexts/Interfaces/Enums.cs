namespace GameServer.Game.Commands.Transaction.Contexts.Interfaces
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
        Exception,

        // Login
        DuplicateAuth,
        OnlyAdult,
        OnlyAdmin,
        BannedAccount,
        InvalidToken,
        TryAgainLater,
        FailedDummyAuth, // dummyClient only

        // Db
        FailedLoadWorldInfoList,
        FailedLoadPlayerInfoList,
        BadPlayerIndex,
        FailedFindPlayer,
        BadPlayerDbId,
        FailedLoadPlayer,

        // Enter
        NotRoutedPlayer,
        DuplicateRuntimeId,
        DuplicateNickname,
        FailedOnEnter,

        FailedAttach,
        FailedDetach,
        Disconnected,

    }
}
