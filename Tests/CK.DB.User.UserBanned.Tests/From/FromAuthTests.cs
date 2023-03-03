using CK.Core;
using CK.DB.Actor;
using CK.SqlServer;
using static CK.Testing.DBSetupTestHelper;
using NUnit.Framework;
using System;
using CK.DB.Auth;
using FluentAssertions;

namespace CK.DB.User.UserBanned.Tests
{
    [TestFixture]
    public class FromAuthTests : CK.DB.Auth.Tests.AuthTests
    {
        [Test]
        public void not_banned_user_can_login()
        {
            var user = ObtainSqlPackage<UserTable>();
            var auth = ObtainSqlPackage<CK.DB.Auth.Package>();

            using( SqlStandardCallContext ctx = new() )
            {
                int userId = user.CreateUser( ctx, 1, Guid.NewGuid().ToString() );

                LoginResult result = auth.OnUserLogin( ctx, "", Util.UtcMinValue, userId, actualLogin: false, DateTime.UtcNow );

                result.UserId.Should().Be( userId );
                result.IsSuccess.Should().BeTrue();
                result.FailureCode.Should().Be( 0 );
                result.FailureReason.Should().BeNullOrEmpty();
            }
        }

            [Test]
        public void no_longer_banned_user_can_login()
        {
            var user = ObtainSqlPackage<UserTable>();
            var userBanned = ObtainSqlPackage<UserBannedTable>();
            var auth = ObtainSqlPackage<CK.DB.Auth.Package>();

            using( SqlStandardCallContext ctx = new() )
            {
                int userId = user.CreateUser( ctx, 1, Guid.NewGuid().ToString() );

                userBanned.SetUserBanned( ctx, 1, "test", userId, DateTime.UtcNow.AddYears( -1 ), new TimeSpan( 3712 ) );

                LoginResult result = auth.OnUserLogin( ctx, "", Util.UtcMinValue, userId, actualLogin: false, DateTime.UtcNow );

                result.UserId.Should().Be( userId );
                result.IsSuccess.Should().BeTrue();
                result.FailureCode.Should().Be( 0 );
                result.FailureReason.Should().BeNullOrEmpty();
            }
        }

        [Test]
        public void user_ban_on_the_futur_can_login()
        {
            var user = ObtainSqlPackage<UserTable>();
            var userBanned = ObtainSqlPackage<UserBannedTable>();
            var auth = ObtainSqlPackage<CK.DB.Auth.Package>();

            using( SqlStandardCallContext ctx = new() )
            {
                int userId = user.CreateUser( ctx, 1, Guid.NewGuid().ToString() );

                userBanned.SetUserBanned( ctx, 1, "test", userId, DateTime.UtcNow.AddYears( 1 ) );

                LoginResult result = auth.OnUserLogin( ctx, "", Util.UtcMinValue, userId, actualLogin: false, DateTime.UtcNow );

                result.UserId.Should().Be( userId );
                result.IsSuccess.Should().BeTrue();
                result.FailureCode.Should().Be( 0 );
                result.FailureReason.Should().BeNullOrEmpty();
            }
        }

        [Test]
        public void banned_user_cannot_successfully_login()
        {
            var user = ObtainSqlPackage<UserTable>();
            var userBanned = ObtainSqlPackage<UserBannedTable>();
            var auth = ObtainSqlPackage<CK.DB.Auth.Package>();

            using( SqlStandardCallContext ctx = new() )
            {
                string keyReason = Guid.NewGuid().ToString();
                int userId = user.CreateUser( ctx, 1, Guid.NewGuid().ToString() );

                userBanned.SetUserBanned( ctx, 1, keyReason, userId );

                LoginResult result = auth.OnUserLogin( ctx, "", Util.UtcMinValue, userId, actualLogin: false, DateTime.UtcNow );

                result.FailureCode.Should().Be( 6 );
                result.FailureReason.Should().Be( keyReason );
                result.IsSuccess.Should().BeFalse();
            }
        }

        static T ObtainSqlPackage<T>() where T : SqlPackage
        {
            return TestHelper.StObjMap.StObjs.Obtain<T>()
                ?? throw new NullReferenceException( $"Cannot obtain {typeof( T ).Name} table." );
        }
    }
}
