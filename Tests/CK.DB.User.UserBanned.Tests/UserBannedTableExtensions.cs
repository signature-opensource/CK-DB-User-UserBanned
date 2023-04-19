using CK.SqlServer;
using Dapper;
using System;
using System.Collections.Generic;

namespace CK.DB.User.UserBanned.Tests
{
    public static class UserBannedTableExtensions
    {
        public sealed class UserBanned
        {
            public int UserId { get; set; }

            public string KeyReason { get; set; } = string.Empty;

            DateTime _banStartDate;
            public DateTime BanStartDate
            {
                get => _banStartDate;
                // Must specify kind because Dapper maps datetime2(2) in DateTime with DateTimeKind.Unspecified.
                set => _banStartDate = DateTime.SpecifyKind( value, DateTimeKind.Utc );
            }

            DateTime _banEndDate;
            public DateTime BanEndDate
            {
                get => _banEndDate;
                // Must specify kind because Dapper maps datetime2(2) in DateTime with DateTimeKind.Unspecified.
                set => _banEndDate = DateTime.SpecifyKind( value, DateTimeKind.Utc );
            }
        }

        public static IEnumerable<UserBanned> GetCurrentlyBannedUser( this UserBannedTable @this, ISqlCallContext ctx, int userId )
        {
            return ctx.GetConnectionController( @this ).Query<UserBanned>(
                @"select UserId, KeyReason, BanStartDate, BanEndDate
                  from CK.vUserCurrentlyBanned
                  where UserId = @UserId;",
                new { UserId = userId } );
        }

        public static UserBanned? GetCurrentlyBannedUser( this UserBannedTable @this, ISqlCallContext ctx, int userId, string keyReason )
        {
            return ctx.GetConnectionController( @this ).QuerySingleOrDefault<UserBanned?>(
                @"select UserId, KeyReason, BanStartDate, BanEndDate
                  from CK.vUserCurrentlyBanned
                  where UserId = @UserId
                      and KeyReason like @KeyReason;",
                new { UserId = userId, KeyReason = keyReason } );
        }

        public static UserBanned? GetBannedUser( this UserBannedTable @this, ISqlCallContext ctx, int userId )
        {
            return ctx.GetConnectionController( @this ).QuerySingleOrDefault<UserBanned?>(
                @"select UserId, KeyReason, BanStartDate, BanEndDate
                  from CK.tUserBanned
                  where UserId = @UserId;",
                new { UserId = userId } );
        }
    }
}
