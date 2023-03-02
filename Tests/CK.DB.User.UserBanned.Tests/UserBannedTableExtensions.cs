using CK.SqlServer;
using Dapper;
using System;
using System.Collections.Generic;

namespace CK.DB.User.UserBanned.Tests
{
    internal static class UserBannedTableExtensions
    {
        internal sealed class UserBanned
        {
            public int UserId { get; set; }

            public string Reason { get; set; } = string.Empty;

            public DateTime BanStartDate { get; set; }

            public DateTime BanEndDate { get; set; }
        }

        internal static IEnumerable<UserBanned> GetCurrentlyBannedUser( this UserBannedTable @this, ISqlCallContext ctx, int userId )
        {
            return ctx.GetConnectionController( @this ).Query<UserBanned>(
                @"select UserId, Reason, BanStartDate, BanEndDate
                  from CK.vUserCurrentlyBanned
                  where UserId = @UserId;",
                new { UserId = userId } );
        }

        internal static UserBanned? GetCurrentlyBannedUser( this UserBannedTable @this, ISqlCallContext ctx, int userId, string reason )
        {
            return ctx.GetConnectionController( @this ).QuerySingleOrDefault<UserBanned?>(
                @"select UserId, Reason, BanStartDate, BanEndDate
                  from CK.vUserCurrentlyBanned
                  where UserId = @UserId
                      and Reason like @Reason;",
                new { UserId = userId, Reason = reason } );
        }

        internal static UserBanned? GetBannedUser( this UserBannedTable @this, ISqlCallContext ctx, int userId, string reason )
        {
            return ctx.GetConnectionController( @this ).QuerySingleOrDefault<UserBanned?>(
                @"select UserId, Reason, BanStartDate, BanEndDate
                  from CK.tUserBanned
                  where UserId = @UserId;",
                new { UserId = userId } );
        }
    }
}
