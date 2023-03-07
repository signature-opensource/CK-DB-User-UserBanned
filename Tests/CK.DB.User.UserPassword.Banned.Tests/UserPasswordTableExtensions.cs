using CK.SqlServer;
using Dapper;

namespace CK.DB.User.UserPassword.Banned.Tests
{
    internal static class UserPasswordTableExtensions
    {
        public static int GetFailedAttemptCount( this UserPasswordTable @this, ISqlCallContext ctx, int userId  )
        {
            return ctx.GetConnectionController( @this ).QuerySingleOrDefault<int>(
                "select FailedAttemptCount from CK.tUserPassword where UserId = @UserId;",
                new { UserId = userId } );
        }
    }
}
