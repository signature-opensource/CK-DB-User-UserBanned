# CK.IO.User.UserBanned

The Cris command surface of user banishment, and the profile extension that carries the state. This
package holds definitions only - no implementation - so that a client (a TypeScript front-end, a remote
agent) can depend on the contract without dragging the database layer in.

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
    /// ... (member summary elided)
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

## Sending one.

A command interface has no implementation to instantiate: you ask the `PocoDirectory` for one and
configure it in a lambda.

```csharp
var pocoDirectory = services.GetRequiredService<PocoDirectory>();
var executor = services.GetRequiredService<CrisExecutionContext>();

var cmd = pocoDirectory.Create<ISetUserBannedCommand>( c =>
{
    c.ActorId = 1;
    c.UserId = userId;
    c.KeyReason = "Cris.SetBan";
} );

var executing = await executor.ExecuteRootCommandAsync( cmd );
var res = executing.WithResult<ICrisBasicCommandResult>().Result;

res.Success.ShouldBeTrue();
```

Three things to read off it:

- **`ActorId` is assigned here, which does not contradict the section above.** It is declared
  `[AmbientServiceValue] int? ActorId { get; set; }`, so in an authenticated pipeline it is supplied
  from the ambient authentication. The example runs outside one, so it names the actor explicitly -
  user `1` being System. In application code you leave it alone.
- **The dates are omitted**, and the resulting ban is eternal. The test asserts
  `banned!.BanEndDate.ShouldBe( EternalBanEndDate )` against its own
  `static readonly DateTime EternalBanEndDate = new DateTime( 9999, 12, 31 )`, which is the `BanEndDate`
  summary's *"when null, the banishment is eternal (9999-12-31)"* made observable.
- **`res.Success` is the only thing to branch on**, per the section below. The two-step
  `ExecuteRootCommandAsync` then `WithResult<T>().Result` is the Cris shape, not something specific to
  this package.

Destroying a ban is the same call with [`IDestroyUserBannedCommand`](IDestroyUserBannedCommand.cs) and
the same `UserId` / `KeyReason` pair - the reason is part of the identity of a ban, so destroying
requires naming it.

From [`UserBannedCrisTests`](../Tests/CK.DB.User.UserBanned.Tests/UserBannedCrisTests.cs).

## The profile extension.

Beside the two commands, this package declares the one boolean the client reads:

```csharp
public interface IUserProfile : Actor.IUserProfile
{
    /// <summary>
    /// Gets or sets whether a banishment is currently active for this user, that is whether now falls
    /// inside one of its [BanStartDate, BanEndDate[ windows.
    /// </summary>
    public bool IsBanned { get; set; }
}
```

Deliberately a single boolean, and the declaration says why: *"the profile answers "may this user use
the application", not "why and until when". The reason and the end date belong to the administration
screens, which read `CK.tUserBanned` through the workspace user list."*

Note the half-open window `[BanStartDate, BanEndDate[` - the same interval the SQL side evaluates, so
the flag is a projection of that evaluation and never stored state.

See [`IUserProfile`](IUserProfile.cs).


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
| `UserBanned.ActorBanned` | not from these two handlers: raised by the validator that refuses every authenticated command of a banished actor, with the text *"Your account has been disabled."* |

Note that the handlers catch `SqlDetailedException` as a whole: a security refusal
(`Security.AdminOnly`, `Security.CannotBanSystemGroupMember`) reaches the caller as the same
`UserBanned.SetFailed` message as any other SQL error. The distinction is only in the monitor, where the
exception is logged. So a caller cannot tell a refusal from a failure - which also means it cannot tell
whether the target is a System group member.
