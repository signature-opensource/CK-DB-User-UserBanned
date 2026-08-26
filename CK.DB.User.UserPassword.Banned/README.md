# CK.DB.User.UserPassword.Banned

This package is based on `CK.DB.User.UserPassword` and
[CK.DB.User.UserBanned](../CK.DB.User.UserBanned/README.md).

It adds banishment rules for basic authentication.

This `CK.DB.User.UserPassword.Banned.Package` injects code into `sAuthUserOnLogin` procedure (from
CK.DB.Auth package). When a login failed and after incrementation of the FailedAttemptCount from the
CK.tUserPassword table, it applies the followings banishment rules:

| `FailedAttemptCount` after the increment | Ban duration |
|---|---|
| 1 to 3 | none |
| 4 or 5 (`> 3`) | 15 minutes |
| 6 to 8 (`> 5`) | 2 hours |
| 9 and above (`> 8`) | 24 hours |

The thresholds are strict comparisons, so **the first ban is created by the 4th failed attempt**, not
the 3rd. `test_general_ban_flow_Async` in `Tests/CK.DB.User.UserPassword.Banned.Tests` walks the whole
ladder and is the authority on these boundaries.

## It brings no schema of its own.

This package contains a single file besides its `Package.cs`:
[Res/sAuthUserOnLogin.tql](Res/sAuthUserOnLogin.tql). No table, no view, no procedure. All it does is
inject SQL into the `OnBasicAttemptCountIncremented` section of `CK.sAuthUserOnLogin`, which computes
an end date from the failed attempt count and calls the existing procedure:

```sql
exec CK.sUserBannedSet 1, 'UserPassword.TooManyAttempt', @UserId, @Now, @EndBanned;
```

That section is itself injected by `CK.DB.User.UserPassword` into the `LoginFailed` part of
`sAuthUserOnLogin`, inside a guard: it only runs when `@Scheme = 'Basic'` **and** the failure code is
`InvalidCredentials` (4) or `UnregisteredUser` (2). So this package only ever reacts to a password
login, never to an external provider.

Two consequences follow from that one `exec` line:

- The acting actor is hard-coded to `1` - the `System` user seeded by `CK.DB.Actor`, which
  `CK.tActorProfile` also makes a member of the System group (row `(1, 1)`). The ban is issued by the
  system, not by the failing caller, and that membership is what satisfies the System-group check of
  [`sUserBannedSet`](../CK.DB.User.UserBanned/Res/sUserBannedSet.sql).
- The reason is the fixed key `'UserPassword.TooManyAttempt'`. Since a banishment is unique per
  (UserId, KeyReason), successive failures **update** the same row instead of accumulating: the
  automatic ban is one moving interval per user, and it never collides with a ban set manually under
  a different reason.

## The ban surfaces one attempt later, and the counter freezes.

Both halves of the flow live in the same procedure, but not in the same order. The banishment check
injected by [CK.DB.User.UserBanned](../CK.DB.User.UserBanned/README.md) sits at `CheckLoginFailure`,
*before* the increment section. So on the attempt that creates the ban, the login has already been
evaluated - the failure is still a plain credentials failure:

- **4th failure**: the count reaches 4, a 15-minute ban is created, and the returned failure code is
  still `4` (`InvalidCredentials`).
- **5th attempt**: the ban is now active, the check refuses the login with code `6`
  (`GloballyDisabledUser`) and the reason `"UserPassword.TooManyAttempt"`.
- Because that code is neither 4 nor 2, the increment block is skipped: `FailedAttemptCount` **stays at
  4** for as long as the ban holds.

The practical effect is that a user cannot climb the ladder by hammering the form - each rung costs one
attempt *after* the previous ban has expired. `ban_KeyReason_must_be_too_many_attempts` asserts exactly
this: code not 6 for the first four attempts, then code 6 with the count frozen at 4.

## A System group member cannot be banned - and that raises.

`sUserBannedSet` throws `Security.CannotBanSystemGroupMember` when its target belongs to the System
group (`GroupId = 1`), and there is no guard around the `exec` above. Following the code paths, the 4th
failed basic login of a System group member therefore does not produce a plain login failure: the
`throw` propagates out of `sAuthUserOnLogin`.

This is worth knowing before adding a real administrator to the System group. Two caveats on the
statement itself:

- it is read from the code, **not** covered by any test in this repository - the throw is only tested
  directly on `sUserBannedSet` (`UserBannedTests`, which asserts the
  `[Security.CannotBanSystemGroupMember]` message on a `SqlDetailedException`);
- the seeded `System` user (`UserId` 1) has no `CK.tUserPassword` row by default, so it is not reachable
  through the basic scheme out of the box. The case needs a System group member that actually has a
  password.

## Setup dependencies.

```csharp
[SqlPackage( Schema = "CK", ResourcePath = "Res" )]
[Versions( "1.0.0" )]
[SqlObjectItem( "transform:CK.sAuthUserOnLogin" )]
public abstract class Package : SqlPackage
{
    void StObjConstruct( CK.DB.User.UserPassword.Package userPassword, CK.DB.User.UserBanned.Package bannedPackage )
    {
    }
}
```

Both parents are required: `CK.tUserPassword` must exist for `@FailedAttemptCount` to be there, and
`CK.sUserBannedSet` must exist to be called. Note that this package and
[CK.DB.User.UserBanned](../CK.DB.User.UserBanned/README.md) both transform `CK.sAuthUserOnLogin` - one
to refuse a banned user at login, the other to create the ban - and they inject into different sections
of it. The setup engine orders the two transformations from this dependency.
