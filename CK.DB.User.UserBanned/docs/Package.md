Adds user banishment to CK.DB.Auth.

Implements the `CK.tUserBanned` table, where a banishment is an interval identified by the couple
(UserId, KeyReason). A user can carry several bans for different reasons.

Nothing stores an is-banned flag: `CK.fUserBannedViewAt` answers for any date, `CK.vUserCurrentlyBanned`
for now. Enforcement is in SQL: this package transforms `CK.sAuthUserOnLogin` so a banned user cannot
log in, and `CK.sUserDestroy` so bans do not outlive their user.

Banning is reserved to the System group by default, System members can never be banned, and both rules
are extension points.
