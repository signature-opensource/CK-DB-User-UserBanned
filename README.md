# CK-DB-User-UserBanned

[![Licence](https://img.shields.io/github/license/signature-opensource/CK-DB-User-UserBanned.svg)](LICENSE)

Adds user banishment to **CK.DB.Auth**: a user present in the UserBanned table has been banned, and
the authentication procedure itself refuses to log them in.

| Package | Description | Latest stable |
|---------|-------------|---------------|
| [CK.DB.User.UserBanned](CK.DB.User.UserBanned/README.md) | The `CK.tUserBanned` table, the date-based function and view, the set/destroy procedures, and the transformations of `CK.sAuthUserOnLogin` and `CK.sUserDestroy`. | [![nuget](https://img.shields.io/nuget/v/CK.DB.User.UserBanned.svg?label=CK.DB.User.UserBanned)](https://www.nuget.org/packages/CK.DB.User.UserBanned/) |
| [CK.IO.User.UserBanned](CK.IO.User.UserBanned/README.md) | The Cris command contract - `ISetUserBannedCommand` and `IDestroyUserBannedCommand` - with no dependency on the database layer. | [![nuget](https://img.shields.io/nuget/v/CK.IO.User.UserBanned.svg?label=CK.IO.User.UserBanned)](https://www.nuget.org/packages/CK.IO.User.UserBanned/) |
| [CK.DB.User.UserPassword.Banned](CK.DB.User.UserPassword.Banned/README.md) | Automatic banishment after repeated basic-authentication failures. | [![nuget](https://img.shields.io/nuget/v/CK.DB.User.UserPassword.Banned.svg?label=CK.DB.User.UserPassword.Banned)](https://www.nuget.org/packages/CK.DB.User.UserPassword.Banned/) |

## Test projects are not published

Both test projects of this repository declare `<IsPackable>false</IsPackable>`:

- `Tests/CK.DB.User.UserBanned.Tests`
- `Tests/CK.DB.User.UserPassword.Banned.Tests`

They used to be packable, following the `CK.DB.*.Tests` convention where a test package exposes reusable base
fixtures that a downstream repository inherits from — the way this repository itself consumes `CK.DB.Auth.Tests`
and `CK.DB.User.UserPassword.Tests`. Nothing consumes them that way today: the only reference to
`CK.DB.User.UserBanned.Tests` is the `ProjectReference` from `CK.DB.User.UserPassword.Banned.Tests`, inside this
repository. Publishing them would therefore mean maintaining a public surface with no consumer, so we stopped.

If a downstream repository ever needs to inherit these fixtures (for instance to derive from `UserBannedTests` and
set `AssumeNotSystemCanBan`, which exists precisely for that purpose), set `IsPackable` back to `true` in the
relevant project. Nothing else is required — keep in mind that doing so turns the test classes into a published
API, and renaming or reordering them then becomes a breaking change for the consumers.
