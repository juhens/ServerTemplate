namespace ServerCore.Infrastructure
{
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
        NotRouted,
        DuplicateRuntimeId,
        DuplicateNickname,
        FailedOnEnter,

        FailedAttach,
        FailedDetach,
        Disconnected,
        DuplicateDbId,
    }
}
