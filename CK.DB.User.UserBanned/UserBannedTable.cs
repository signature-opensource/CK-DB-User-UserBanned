using CK.Core;
using CK.SqlServer;
using System;
using System.Threading.Tasks;

namespace CK.DB.User.UserBanned
{
    [SqlTable( "tUserBanned", Package = typeof( Package ), ResourcePath = "Res" )]
    [Versions( "1.0.0" )]
    [SqlObjectItem( "vUserBanned" )]
    public abstract class UserBannedTable : SqlTable
    {
        void StObjConstruct( CK.DB.Actor.UserTable userTable )
        {
        }

        #region Stored procedures

        /// <summary>
        /// Create or update a user banishment between the specified dates.<br/>
        /// If <paramref name="banStartDate"/> is <see langword="null"/> and the user is alerady ban then the start date will be the same, else it will be utc now.<br/>
        /// If <paramref name="banEndDate"/> is <see langword="null"/> it will be eternal (9999-12-31).<br/>
        /// </summary>
        /// <param name="ctx">The call context.</param>
        /// <param name="actorId">The identifier of the actor who bans the user.</param>
        /// <param name="userId">The identifier of the user to ban.</param>
        /// <param name="reason">The reason of the banishment.</param>
        /// <param name="banStartDate">The start date of the banishment, default is utc now.</param>
        /// <param name="banEndDate">The end date of the banishment, default is eternal.</param>
        [SqlProcedure( "sUserBannedSet" )]
        public abstract void SetUserBanned( ISqlCallContext ctx, int actorId, int userId, string reason, DateTime? banStartDate = null, DateTime? banEndDate = null );

        /// <inheritdoc cref="SetUserBanned(ISqlCallContext, int, int, string, DateTime?, DateTime?)"/>
        [SqlProcedure( "sUserBannedSet" )]
        public abstract Task SetUserBannedAsync( ISqlCallContext ctx, int actorId, int userId, string reason, DateTime? banStartDate = null, DateTime? banEndDate = null );

        /// <summary>
        /// Destroy the user banishment.
        /// </summary>
        /// <param name="ctx">The call context.</param>
        /// <param name="actorId">The identifier of the actor who destroy the banishment.</param>
        /// <param name="userId">The identifier of the banned user.</param>
        [SqlProcedure( "sUserBannedDestroy" )]
        public abstract void DestroyUserBanned( ISqlCallContext ctx, int actorId, int userId );

        /// <inheritdoc cref="DestroyUserBanned(ISqlCallContext, int, int)"/>
        [SqlProcedure( "sUserBannedDestroy" )]
        public abstract Task DestroyUserBannedAsync( ISqlCallContext ctx, int actorId, int userId );

        #endregion

        /// <summary>
        /// Create or update a user banishment from the specified date and for the specified duration.<br/>
        /// </summary>
        /// <param name="ctx">The call context.</param>
        /// <param name="actorId">The identifier of the actor who bans the user.</param>
        /// <param name="userId">The identifier of the user to ban.</param>
        /// <param name="reason">The reason of the banishment.</param>
        /// <param name="banStartDate">The start date of the banishment, default is now.</param>
        /// <param name="duration">The duration of the banishment.</param>
        public void SetUserBanned( ISqlCallContext ctx, int actorId, int userId, string reason, DateTime banStartDate, TimeSpan duration )
        {
            CheckDurationValidity( duration );
            SetUserBanned( ctx, actorId, userId, reason, banStartDate, banStartDate + duration );
        }

        /// <inheritdoc cref="SetUserBanned(ISqlCallContext, int, int, string, DateTime, TimeSpan)"/>
        public async Task SetUserBannedAsync( ISqlCallContext ctx, int actorId, int userId, string reason, DateTime banStartDate, TimeSpan duration )
        {
            CheckDurationValidity( duration );
            await SetUserBannedAsync( ctx, actorId, userId, reason, banStartDate, banStartDate + duration );
        }

        static void CheckDurationValidity( TimeSpan duration )
        {
            if( duration < TimeSpan.Zero )
            {
                throw new ArgumentOutOfRangeException( nameof( duration ), "Banishment duration cannot be negative." );
            }
        }
    }
}
