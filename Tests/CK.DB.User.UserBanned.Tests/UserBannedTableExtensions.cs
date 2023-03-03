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

            public string KeyReason { get; set; } = string.Empty;

            public DateTime BanStartDate { get; set; }

            public DateTime BanEndDate { get; set; }
        }

        internal static IEnumerable<UserBanned> GetCurrentlyBannedUser( this UserBannedTable @this, ISqlCallContext ctx, int userId )
        {
            return ctx.GetConnectionController( @this ).Query<UserBanned>(
                @"select UserId, KeyReason, BanStartDate, BanEndDate
                  from CK.vUserCurrentlyBanned
                  where UserId = @UserId;",
                new { UserId = userId } );
        }

        internal static UserBanned? GetCurrentlyBannedUser( this UserBannedTable @this, ISqlCallContext ctx, int userId, string keyReason )
        {
            return ctx.GetConnectionController( @this ).QuerySingleOrDefault<UserBanned?>(
                @"select UserId, KeyReason, BanStartDate, BanEndDate
                  from CK.vUserCurrentlyBanned
                  where UserId = @UserId
                      and KeyReason like @KeyReason;",
                new { UserId = userId, KeyReason = keyReason } );
        }

        internal static UserBanned? GetBannedUser( this UserBannedTable @this, ISqlCallContext ctx, int userId )
        {
            return ctx.GetConnectionController( @this ).QuerySingleOrDefault<UserBanned?>(
                @"select UserId, KeyReason, BanStartDate, BanEndDate
                  from CK.tUserBanned
                  where UserId = @UserId;",
                new { UserId = userId } );
        }
    }
}
