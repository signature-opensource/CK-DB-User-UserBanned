using CK.Core;
using CK.DB.Actor;
using CK.DB.Auth;
using CK.DB.User.UserBanned;
using CK.DB.User.UserBanned.Tests;
using CK.SqlServer;
using static CK.Testing.DBSetupTestHelper;
using FluentAssertions;
using NUnit.Framework;
using System;

namespace CK.DB.User.UserPassword.Banned.Tests
{
    [TestFixture]
    public class FromUserPassword : CK.DB.User.UserPassword.Tests.UserPasswordTests
    {
        [Test]
        public void success_login_not_ban_user()
        {
            var user = ObtainSqlPackage<UserTable>();
            var userPassword = ObtainSqlPackage<UserPasswordTable>();
            var userBanned = ObtainSqlPackage<UserBannedTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                int userId = user.CreateUser( ctx, 1, Guid.NewGuid().ToString() );
                string password = Guid.NewGuid().ToString();
                userPassword.CreateOrUpdatePasswordUser( ctx, 1, userId, password );

                LoginResult result = userPassword.LoginUser( ctx, userId, password );

                result.IsSuccess.Should().BeTrue();
                result.FailureCode.Should().Be( 0 );
                userPassword.GetFailedAttemptCount( ctx, userId ).Should().Be( 0 );
                userBanned.GetBannedUser( ctx, userId ).Should().BeNull();
            }
        }

        [Test]
        public void ban_KeyReason_must_be_too_many_attempts()
        {
            var user = ObtainSqlPackage<UserTable>();
            var userPassword = ObtainSqlPackage<UserPasswordTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                int userId = user.CreateUser( ctx, 1, Guid.NewGuid().ToString() );
                userPassword.CreateOrUpdatePasswordUser( ctx, 1, userId, Guid.NewGuid().ToString(), UCLMode.CreateOnly );

                LoginResult result;
                int attempt;

                for( attempt = 1; attempt <= 4; attempt++ )
                {
                    result = userPassword.LoginUser( ctx, userId, "fail-password" );
                    result.FailureCode.Should().NotBe( 6 );
                    result.IsSuccess.Should().BeFalse();
                    userPassword.GetFailedAttemptCount( ctx, userId ).Should().Be( attempt );
                }

                result = userPassword.LoginUser( ctx, userId, "fail-password" );
                userPassword.GetFailedAttemptCount( ctx, userId ).Should().Be( 4 );
                result.IsSuccess.Should().BeFalse();
                result.FailureCode.Should().Be( 6 );
                result.FailureReason.Should().Be( "UserPassword.TooManyAttempt" );
            }
        }

        [Test]
        public void fail_login_under_3_times_is_not_ban()
        {
            var user = ObtainSqlPackage<UserTable>();
            var userPassword = ObtainSqlPackage<UserPasswordTable>();
            var userBanned = ObtainSqlPackage<UserBannedTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                int userId = user.CreateUser( ctx, 1, Guid.NewGuid().ToString() );
                string password = Guid.NewGuid().ToString();
                userPassword.CreateOrUpdatePasswordUser( ctx, 1, userId, password );

                LoginResult result = userPassword.LoginUser( ctx, userId, "fail-password" );
                result.IsSuccess.Should().BeFalse();
                userPassword.GetFailedAttemptCount( ctx, userId).Should().Be( 1 );
                userBanned.GetBannedUser( ctx, userId ).Should().BeNull();

                result = userPassword.LoginUser( ctx, userId, "fail-password" );
                result.IsSuccess.Should().BeFalse();
                userPassword.GetFailedAttemptCount( ctx, userId ).Should().Be( 2 );
                userBanned.GetBannedUser( ctx, userId ).Should().BeNull();
            }
        }

        [Test]
        public void test_general_ban_flow()
        {
            var user = ObtainSqlPackage<UserTable>();
            var userPassword = ObtainSqlPackage<UserPasswordTable>();
            var userBanned = ObtainSqlPackage<UserBannedTable>();

            using( SqlStandardCallContext ctx = new() )
            using( userBanned.Database.TemporaryTransform( @"
                create transformer on CK.sAuthUserOnLogin
                as
                begin
                    replace single {dateadd(hour, 24, @Now)} with ""dateadd(second, 6, @Now)""
                    replace single {dateadd(hour, 2, @Now)} with ""dateadd(second, 4, @Now)""
                    replace single {dateadd(minute, 15, @Now)} with ""dateadd(second, 2, @Now)""
                end" ) )
            {
                int userId = user.CreateUser( ctx, 1, Guid.NewGuid().ToString() );
                string password = Guid.NewGuid().ToString();
                userPassword.CreateOrUpdatePasswordUser( ctx, 1, userId, password, UCLMode.CreateOnly );

                int attempt;
                UserBannedTableExtensions.UserBanned? ban;

                // FailedAttemptCount 1 to 3
                for( attempt = 1; attempt <= 3; attempt++ )
                {
                    userPassword.LoginUser( ctx, userId, "fail-password" ).IsSuccess.Should().BeFalse();
                    userPassword.GetFailedAttemptCount( ctx, userId ).Should().Be( attempt );
                    userBanned.GetBannedUser( ctx, userId ).Should().BeNull();
                }

                // FailedAttemptCount 4 to 5
                for( ; attempt <= 5; attempt++ )
                {
                    userPassword.LoginUser( ctx, userId, "fail-password" ).IsSuccess.Should().BeFalse();
                    ban = userBanned.GetBannedUser( ctx, userId );
                    ban.Should().NotBeNull();
                    ban!.KeyReason.Should().Be( "UserPassword.TooManyAttempt" );
                    ban!.BanEndDate.Should().BeCloseTo( ban!.BanStartDate.AddSeconds( 2 ), precision: new TimeSpan( 0, 0, 0, 0, 10 ) );
                    userBanned.GetCurrentlyBannedUser( ctx, userId, "UserPassword.TooManyAttempt" ).Should().NotBeNull();

                    while( DateTime.UtcNow < ban!.BanEndDate ) ;

                    userBanned.GetCurrentlyBannedUser( ctx, userId, "UserPassword.TooManyAttempt" ).Should().BeNull();
                }

                // FailedAttemptCount 6 to 8
                for( ; attempt <= 8; attempt++ )
                {
                    userPassword.LoginUser( ctx, userId, "fail-password" ).IsSuccess.Should().BeFalse();
                    ban = userBanned.GetBannedUser( ctx, userId );
                    ban.Should().NotBeNull();
                    ban!.KeyReason.Should().Be( "UserPassword.TooManyAttempt" );
                    ban!.BanEndDate.Should().BeCloseTo( ban!.BanStartDate.AddSeconds( 4 ), precision: new TimeSpan( 0, 0, 0, 0, 10 ) );
                    userBanned.GetCurrentlyBannedUser( ctx, userId, "UserPassword.TooManyAttempt" ).Should().NotBeNull();

                    while( DateTime.UtcNow < ban!.BanEndDate ) ;

                    userBanned.GetCurrentlyBannedUser( ctx, userId, "UserPassword.TooManyAttempt" ).Should().BeNull();
                }

                // FailedAttemptCount 9
                userPassword.LoginUser( ctx, userId, "fail-password" ).IsSuccess.Should().BeFalse();
                ban = userBanned.GetBannedUser(ctx, userId);
                ban.Should().NotBeNull();
                ban!.KeyReason.Should().Be( "UserPassword.TooManyAttempt" );
                ban!.BanEndDate.Should().BeCloseTo( ban!.BanStartDate.AddSeconds( 6 ), precision: new TimeSpan( 0, 0, 0, 0, 10 ) );
                userBanned.GetCurrentlyBannedUser( ctx, userId, "UserPassword.TooManyAttempt" ).Should().NotBeNull();

                while( DateTime.UtcNow < ban!.BanEndDate ) ;

                userBanned.GetCurrentlyBannedUser( ctx, userId, "UserPassword.TooManyAttempt" ).Should().BeNull();
            }
        }

        static T ObtainSqlPackage<T>() where T : SqlPackage
        {
            return TestHelper.StObjMap.StObjs.Obtain<T>()
                ?? throw new NullReferenceException( $"Cannot obtain {typeof( T ).Name} table." );
        }
    }
}
