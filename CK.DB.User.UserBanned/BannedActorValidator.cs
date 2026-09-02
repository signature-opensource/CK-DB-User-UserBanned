using CK.Auth;
using CK.Core;
using CK.Cris;
using CK.IO.Actor;
using CK.SqlServer;
using Dapper;
using System.Threading.Tasks;

namespace CK.DB.User.UserBanned
{
    /// <summary>
    /// Refuses every authenticated command sent by a currently banished user.
    /// <para>
    /// This is the mechanism that actually makes a banishment effective on an already opened session: the
    /// login is refused by <c>CK.sAuthUserOnLogin</c>, but a token obtained before the ban stays valid
    /// until it expires. The actor channel push and the navigation guard are only there to eject the user
    /// quickly and politely; this validator is what guarantees that a banished user cannot do anything,
    /// whatever the client does or fails to do.
    /// </para>
    /// <para>
    /// It deliberately targets <see cref="ICommandAuthNormal"/>, the marker of "requires an authenticated
    /// actor", rather than a dedicated marker: every authenticated command is covered without having to
    /// mark them one by one, including the actor channel registration.
    /// </para>
    /// <para>
    /// Being an <see cref="IAutoService"/>, it is active as soon as this package is referenced: a
    /// banishment is not something an application should have to opt into.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This costs one SQL round trip per authenticated command. It is negligible for a back office; if it
    /// ever weighs, the remedy is a short-lived per-actor cache in the request scope, not a narrower set
    /// of guarded commands.
    /// </remarks>
    public class BannedActorValidator : IAutoService
    {
        readonly CK.DB.Actor.UserTable _userTable;

        /// <summary>
        /// Initializes a new <see cref="BannedActorValidator"/>.
        /// </summary>
        /// <param name="userTable">The user table, used only as the SQL call context anchor.</param>
        public BannedActorValidator( CK.DB.Actor.UserTable userTable )
        {
            _userTable = userTable;
        }

        /// <summary>
        /// Rejects the command with a user message when a banishment is currently active for its actor.
        /// </summary>
        /// <param name="ctx">The call context.</param>
        /// <param name="collector">The user message collector.</param>
        /// <param name="cmd">The authenticated command being validated.</param>
        [CommandHandlingValidator]
        public async Task ValidateNotBannedAsync( ISqlCallContext ctx, UserMessageCollector collector, ICommandAuthNormal cmd )
        {
            // The profile query is the one command a banished user must still be allowed to run. The
            // client learns that it is banished by reading IsBanned on its own profile; refusing that read
            // would leave the navigation guard blind and turn the flag into dead code. It is read-only and
            // about the caller itself, and the caller is being logged out anyway.
            if( cmd is IGetUserProfileQCommand ) return;

            int actorId = cmd.ActorId.GetValueOrDefault();
            // Anonymous (0) is not our business: commands that require an actor are rejected upstream.
            if( actorId <= 0 ) return;

            bool banned = await ctx[_userTable].QuerySingleOrDefaultAsync<int>(
                """
                select top 1 1
                from CK.fUserBannedViewAt( sysutcdatetime() )
                where UserId = @UserId;
                """,
                new { UserId = actorId } ) == 1;

            if( !banned ) return;

            ctx.Monitor.Error( $"Refusing a command from a banished user. (ActorId: {actorId}, Command: {cmd.CrisPocoModel.PocoName})" );
            collector.Error( "Your account has been disabled.", "UserBanned.ActorBanned" );
        }
    }
}
