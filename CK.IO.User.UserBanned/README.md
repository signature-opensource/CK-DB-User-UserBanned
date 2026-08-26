# CK.IO.User.UserBanned

The Cris command surface of user banishment. This package holds only the command definitions - no
implementation - so that a client (a TypeScript front-end, a remote agent) can depend on the contract
without dragging the database layer in.

## The two commands.

Both are `ICommand<ICrisBasicCommandResult>` and `ICommandAuthNormal`. Two guarantees come with that,
and they are worth separating:

- from `ICommandAuthUnsafe`, which it extends: **the acting `ActorId` is the current
  `IAuthenticationInfo.UnsafeUser`** - it comes from the authentication, not from the payload, and it is
  the one the stored procedures check;
- from `ICommandAuthNormal` itself: **the authentication level must be `Normal` or `Critical`**. A
  remembered-but-unsafe session is refused before the handler runs.

The second one is the whole difference with `ICommandAuthUnsafe`, and it is what stops a stale session
from banning anybody.

```csharp
/// <summary>
/// Creates or updates the banishment of a user for a given <see cref="KeyReason"/>.
/// The acting <see cref="ICommandAuthNormal.ActorId"/> must be allowed to ban (by default a member
/// of the System group, enforced by CK.sUserBannedSet).
/// </summary>
public interface ISetUserBannedCommand : ICommand<ICrisBasicCommandResult>, ICommandAuthNormal
{
    int UserId { get; set; }

    /// <summary>
    /// Gets or sets the reason of the banishment. Must not be null or empty (128 characters at most).
    /// </summary>
    string KeyReason { get; set; }

    /// <summary>
    /// Gets or sets the start date (UTC) of the banishment. When null, the existing start date is
    /// kept for an already banned user, or utc now is used for a new banishment.
    /// </summary>
    DateTime? BanStartDate { get; set; }

    /// <summary>
    /// Gets or sets the end date (UTC) of the banishment. When null, the banishment is eternal
    /// (9999-12-31).
    /// </summary>
    DateTime? BanEndDate { get; set; }
}
```

```csharp
/// <summary>
/// Destroys the banishment of a user for a given <see cref="KeyReason"/>.
/// The acting <see cref="ICommandAuthNormal.ActorId"/> must be allowed to unban (by default a member
/// of the System group, enforced by CK.sUserBannedDestroy).
/// </summary>
public interface IDestroyUserBannedCommand : ICommand<ICrisBasicCommandResult>, ICommandAuthNormal
{
    int UserId { get; set; }
    string KeyReason { get; set; }
}
```

See [ISetUserBannedCommand.cs](ISetUserBannedCommand.cs) and
[IDestroyUserBannedCommand.cs](IDestroyUserBannedCommand.cs).

## Failures are results, not exceptions.

The handlers live with the database implementation, not here. They validate `UserId` and `KeyReason`
before any SQL round trip, then catch `SqlDetailedException` and any other exception. In every failing
case the exception is logged and turned into a `UserMessageCollector` error, so the caller gets an
unsuccessful `ICrisBasicCommandResult` with a displayable message and nothing is written.

The message codes are stable and can be matched on the client:

| Code | Meaning |
|------|---------|
| `UserBanned.InvalidUserId` | `UserId` is not positive. |
| `UserBanned.NoKeyReason` | `KeyReason` is null, empty or whitespace. |
| `UserBanned.UserBanned` | success (info). |
| `UserBanned.SetFailed` | the set failed - typically a security refusal from `CK.sUserBannedSet`. |
| `UserBanned.BanDestroyed` | success (info). |
| `UserBanned.DestroyFailed` | the destroy failed - typically a security refusal. |

Note that the handlers catch `SqlDetailedException` as a whole: a security refusal
(`Security.AdminOnly`, `Security.CannotBanSystemGroupMember`) reaches the caller as the same
`UserBanned.SetFailed` message as any other SQL error. The distinction is only in the monitor, where the
exception is logged. So a caller cannot tell a refusal from a failure - which also means it cannot tell
whether the target is a System group member.
