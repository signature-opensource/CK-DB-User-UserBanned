using CK.Core;
using CK.DB.Actor;
using CK.DB.Auth;
using CK.SqlServer;
using CK.Testing;
using Shouldly;
using NUnit.Framework;
using System;
using static CK.Testing.MonitorTestHelper;

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

                result.UserId.ShouldBe( userId );
                result.IsSuccess.ShouldBeTrue();
                result.FailureCode.ShouldBe( 0 );
                result.FailureReason.ShouldBeNullOrEmpty();
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

                result.UserId.ShouldBe( userId );
                result.IsSuccess.ShouldBeTrue();
                result.FailureCode.ShouldBe( 0 );
                result.FailureReason.ShouldBeNullOrEmpty();
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

                result.UserId.ShouldBe( userId );
                result.IsSuccess.ShouldBeTrue();
                result.FailureCode.ShouldBe( 0 );
                result.FailureReason.ShouldBeNullOrEmpty();
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

                result.FailureCode.ShouldBe( 6 );
                result.FailureReason.ShouldBe( keyReason );
                result.IsSuccess.ShouldBeFalse();
            }
        }

        [Test]
        public void can_login_after_ban_duration()
        {
            // Reduce a pricision of a DateTime to 0,01 second, because the sql ban date works with datetim2(2).
            static DateTime ReducePrecision( DateTime dateTime )
                => new DateTime( dateTime.Ticks - (dateTime.Ticks % (TimeSpan.TicksPerMillisecond * 10)), dateTime.Kind );

            var user = ObtainSqlPackage<UserTable>();
            var userBanned = ObtainSqlPackage<UserBannedTable>();
            var auth = ObtainSqlPackage<CK.DB.Auth.Package>();

            using( SqlStandardCallContext ctx = new() )
            {
                string keyReason = Guid.NewGuid().ToString();
                int userId = user.CreateUser( ctx, 1, Guid.NewGuid().ToString() );

                var banStartDate = ReducePrecision( DateTime.UtcNow );
                var banEndDate = ReducePrecision( banStartDate.AddSeconds( 5 ) );
                userBanned.SetUserBanned( ctx, 1, keyReason, userId, banStartDate, banEndDate );

                DateTime now;
                LoginResult result;

                while( (now = ReducePrecision( DateTime.UtcNow )) < banEndDate )
                {
                    result = auth.OnUserLogin( ctx, "", Util.UtcMinValue, userId, actualLogin: false, now );

                    result.FailureCode.ShouldBe( 6 );
                    result.FailureReason.ShouldBe( keyReason );
                    result.IsSuccess.ShouldBeFalse();
                }

                result = auth.OnUserLogin( ctx, "", Util.UtcMinValue, userId, actualLogin: false, DateTime.UtcNow );

                result.FailureCode.ShouldBe( 0 );
                result.IsSuccess.ShouldBeTrue();
            }
        }

        static T ObtainSqlPackage<T>() where T : SqlPackage
        {
            return SharedEngine.Map.StObjs.Obtain<T>()
                ?? throw new NullReferenceException( $"Cannot obtain {typeof( T ).Name} table." );
        }
    }
}
