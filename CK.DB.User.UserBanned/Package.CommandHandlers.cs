using CK.Core;
using CK.Cris;
using CK.IO.User.UserBanned;
using CK.SqlServer;
using System;
using System.Threading.Tasks;

namespace CK.DB.User.UserBanned
{
    public abstract partial class Package
    {
        /// <summary>
        /// Handles <see cref="ISetUserBannedCommand"/>: creates or updates the banishment of the user.
        /// The security check (System group membership by default, see the BannedSecurityCheck injection
        /// point) is enforced by CK.sUserBannedSet: a failure is reported as a user message.
        /// </summary>
        [CommandHandler]
        public async Task<ICrisBasicCommandResult> HandleSetUserBannedCommandAsync( ISqlCallContext ctx,
                                                                                    UserMessageCollector collector,
                                                                                    ISetUserBannedCommand cmd )
        {
            int actorId = cmd.ActorId.GetValueOrDefault();
            using( ctx.Monitor.OpenInfo( $"Handling {nameof( ISetUserBannedCommand )}. (ActorId: {actorId}, UserId: {cmd.UserId}, KeyReason: {cmd.KeyReason})" ) )
            {
                var res = cmd.CreateResult();
                if( !CheckUserIdAndKeyReason( collector, cmd.UserId, cmd.KeyReason ) )
                {
                    res.SetUserMessages( collector );
                    return res;
                }
                try
                {
                    await UserBannedTable.SetUserBannedAsync( ctx, actorId, cmd.KeyReason, cmd.UserId,
                                                              cmd.BanStartDate, cmd.BanEndDate );
                    collector.Info( "User successfully banned.", "UserBanned.UserBanned" );
                }
                catch( SqlDetailedException ex ) when( ex.InnerSqlException is not null )
                {
                    ctx.Monitor.Error( $"Error while handling {nameof( ISetUserBannedCommand )}.", ex );
                    collector.Error( "User could not be banned.", "UserBanned.SetFailed" );
                }
                catch( Exception ex )
                {
                    ctx.Monitor.Error( ex );
                    collector.Error( "An error occurred while banning the user.", "UserBanned.SetFailed" );
                }
                res.SetUserMessages( collector );
                return res;
            }
        }

        /// <summary>
        /// Handles <see cref="IDestroyUserBannedCommand"/>: destroys the banishment of the user for the
        /// given reason. The security check (System group membership by default, see the
        /// BannedSecurityCheck injection point) is enforced by CK.sUserBannedDestroy: a failure is
        /// reported as a user message.
        /// </summary>
        [CommandHandler]
        public async Task<ICrisBasicCommandResult> HandleDestroyUserBannedCommandAsync( ISqlCallContext ctx,
                                                                                        UserMessageCollector collector,
                                                                                        IDestroyUserBannedCommand cmd )
        {
            int actorId = cmd.ActorId.GetValueOrDefault();
            using( ctx.Monitor.OpenInfo( $"Handling {nameof( IDestroyUserBannedCommand )}. (ActorId: {actorId}, UserId: {cmd.UserId}, KeyReason: {cmd.KeyReason})" ) )
            {
                var res = cmd.CreateResult();
                if( !CheckUserIdAndKeyReason( collector, cmd.UserId, cmd.KeyReason ) )
                {
                    res.SetUserMessages( collector );
                    return res;
                }
                try
                {
                    await UserBannedTable.DestroyUserBannedAsync( ctx, actorId, cmd.KeyReason, cmd.UserId );
                    collector.Info( "User banishment successfully destroyed.", "UserBanned.BanDestroyed" );
                }
                catch( SqlDetailedException ex ) when( ex.InnerSqlException is not null )
                {
                    ctx.Monitor.Error( $"Error while handling {nameof( IDestroyUserBannedCommand )}.", ex );
                    collector.Error( "User banishment could not be destroyed.", "UserBanned.DestroyFailed" );
                }
                catch( Exception ex )
                {
                    ctx.Monitor.Error( ex );
                    collector.Error( "An error occurred while destroying the user banishment.", "UserBanned.DestroyFailed" );
                }
                res.SetUserMessages( collector );
                return res;
            }
        }

        /// <summary>
        /// Validates the command parameters before any SQL round trip: the stored procedures would throw
        /// (Security.InvalidUserId / Security.InvalidNullOrEmptyKeyReason) but a user message is nicer.
        /// </summary>
        /// <returns>True if the parameters are valid.</returns>
        static bool CheckUserIdAndKeyReason( UserMessageCollector collector, int userId, string keyReason )
        {
            bool valid = true;
            if( userId <= 0 )
            {
                collector.Error( "Invalid user identifier.", "UserBanned.InvalidUserId" );
                valid = false;
            }
            if( string.IsNullOrWhiteSpace( keyReason ) )
            {
                collector.Error( "A ban reason is required.", "UserBanned.NoKeyReason" );
                valid = false;
            }
            return valid;
        }
    }
}
