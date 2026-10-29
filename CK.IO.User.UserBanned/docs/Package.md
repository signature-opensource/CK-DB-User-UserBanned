The Cris command contract for user banishment.

Defines `ISetUserBannedCommand` and `IDestroyUserBannedCommand` - both `ICommandAuthCritical` (a recent login), both
returning an `ICrisBasicCommandResult` - so a client can drive banning from an endpoint without
depending on the database layer.

Contains definitions only. The handlers ship with the database implementation, and they report security
or SQL failures as user messages rather than exceptions.
