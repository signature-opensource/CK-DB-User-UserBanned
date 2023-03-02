using CK.SqlServer;
using Dapper;
using System;

namespace CK.DB.User.UserBanned.Tests
{
    internal static class UserBannedTableExtensions
    {
        internal sealed class UserBanned
        {
            public int UserId { get; set; }

            public string UserName { get; set; } = string.Empty;

            public DateTime BanStartDate { get; set; }

            public DateTime BanEndDate { get; set; }

            public bool IsBannedNow { get; set; }

            public string Reason { get; set; } = string.Empty;
        }

        internal static UserBanned? GetUserBanned( this UserBannedTable @this, ISqlCallContext ctx, int userId )
        {
            return ctx.GetConnectionController( @this ).QuerySingleOrDefault<UserBanned?>(
                @"select UserId
                        ,UserName
                        ,BanStartDate
                        ,BanEndDate
                        ,IsBannedNow
                        ,Reason
                  from CK.vUserBanned
                  where UserId = @UserId;",
                new { UserId = userId } );
        }
    }
}
