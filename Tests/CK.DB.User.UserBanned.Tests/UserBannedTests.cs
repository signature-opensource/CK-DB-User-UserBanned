using CK.Core;
using CK.DB.Actor;
using CK.Testing;
using CK.SqlServer;
using NUnit.Framework;
using Shouldly;
using System;
using System.Linq;
using static CK.Testing.MonitorTestHelper;

namespace CK.DB.User.UserBanned.Tests
{
    [TestFixture]
    public class UserBannedTests
    {
        /// <summary>
        /// If <see langword="true"/>, not System group member are allow to ban/unban user.<br/>
        /// Tests <see cref="not_system_member_cannot_ban_user"/> and <see cref="not_system_member_cannot_destroy_ban"/> will not be run.<br/>
        /// Default value is <see langword="false"/>.
        /// </summary>
        static protected bool AssumeNotSystemCanBan = false;

        [Test]
        public void actor_0_or_user_0_cannot_ban()
        {
            var userBanned = ObtainSqlPackage<UserBannedTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                Util.Invokable( () => userBanned.SetUserBanned( ctx, 0, "test", 1 ) )
                    .ShouldThrow<Exception>()
                    .InnerException!.Message.ShouldBe( "Security.AnonymousNotAllowed" );

                Util.Invokable( () => userBanned.SetUserBanned( ctx, 1, "test", 0 ) )
                    .ShouldThrow<Exception>()
                    .InnerException!.Message.ShouldBe( "Security.InvalidUserId" );
            }
        }

        [Test]
        public void null_ro_empty_keyReason_is_invalid()
        {
            var userBanned = ObtainSqlPackage<UserBannedTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                Util.Invokable( () => userBanned.SetUserBanned( ctx, 1, null!, 3712 ) )
                    .ShouldThrow<Exception>()
                    .InnerException!.Message.ShouldBe( "Security.InvalidNullOrEmptyKeyReason" );

                Util.Invokable( () => userBanned.SetUserBanned( ctx, 1, "", 3712 ) )
                    .ShouldThrow<Exception>()
                    .InnerException!.Message.ShouldBe( "Security.InvalidNullOrEmptyKeyReason" );
            }
        }

        [Test]
        public void actor_0_or_user_0_cannot_destroy_ban()
        {
            var userBanned = ObtainSqlPackage<UserBannedTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                Util.Invokable( () => userBanned.DestroyUserBanned( ctx, 0, "test", 1 ) )
                    .ShouldThrow<Exception>()
                    .InnerException!.Message.ShouldBe( "Security.AnonymousNotAllowed" );

                Util.Invokable( () => userBanned.DestroyUserBanned( ctx, 1, "test", 0 ) )
                    .ShouldThrow<Exception>()
                    .InnerException!.Message.ShouldBe( "Security.InvalidUserId" );
            }
        }

        [Test]
        public void system_can_ban_user()
        {
            var user = ObtainSqlPackage<UserTable>();
            var userBanned = ObtainSqlPackage<UserBannedTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                int userId = user.CreateUser( ctx, 1, Guid.NewGuid().ToString() );

                userBanned.SetUserBanned( ctx, 1, "test", userId );

                userBanned.GetCurrentlyBannedUser( ctx, userId ).Count().ShouldBe( 1 );
            }
        }

        [Test]
        public void system_can_destroy_ban()
        {
            var user = ObtainSqlPackage<UserTable>();
            var userBanned = ObtainSqlPackage<UserBannedTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                string keyReason = "test";
                int userId = user.CreateUser( ctx, 1, Guid.NewGuid().ToString() );

                userBanned.SetUserBanned( ctx, 1, keyReason, userId );

                userBanned.GetCurrentlyBannedUser( ctx, userId, keyReason ).ShouldNotBeNull();

                userBanned.DestroyUserBanned( ctx, 1, keyReason, userId );

                userBanned.GetCurrentlyBannedUser( ctx, userId, keyReason ).ShouldBeNull();
            }
        }

        [Test]
        public void group_1_member_cannot_be_ban()
        {
            var userBanned = ObtainSqlPackage<UserBannedTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                Util.Invokable( () => userBanned.SetUserBanned( ctx, 1, "test", 1 ) )
                    .ShouldThrow<Exception>()
                    .InnerException!.Message.ShouldMatch( @"^ck:CK\.sUserBannedSet-\{.*\}-\[Security\.CannotBanSystemGroupMember\]" );
            }
        }

        [Test]
        public void not_system_member_cannot_ban_user()
        {
            Assume.That( AssumeNotSystemCanBan is false );

            var user = ObtainSqlPackage<UserTable>();
            var userBanned = ObtainSqlPackage<UserBannedTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                int userId = user.CreateUser( ctx, 1, Guid.NewGuid().ToString() );

                Util.Invokable( () => userBanned.SetUserBanned( ctx, userId, "auto-ban", userId ) )
                    .ShouldThrow<Exception>()
                    .InnerException!.Message.ShouldMatch( @"^ck:CK\.sUserBannedSet-\{.*\}-\[Security\.SystemLevelOnly\]" );
            }
        }

        [Test]
        public void not_system_member_cannot_destroy_ban()
        {
            Assume.That( AssumeNotSystemCanBan is false );

            var user = ObtainSqlPackage<UserTable>();
            var userBanned = ObtainSqlPackage<UserBannedTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                int userId = user.CreateUser( ctx, 1, Guid.NewGuid().ToString() );

                userBanned.SetUserBanned( ctx, 1, "test", userId );

                Util.Invokable( () => userBanned.DestroyUserBanned( ctx, userId, "test", userId ) )
                    .ShouldThrow<Exception>()
                    .InnerException!.Message.ShouldMatch( @"^ck:CK\.sUserBannedDestroy-\{.*\}-\[Security\.SystemLevelOnly\]" );
            }
        }

        [Test]
        public void cannot_set_negative_ban_duration()
        {
            var userBanned = ObtainSqlPackage<UserBannedTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                Util.Invokable( () => userBanned.SetUserBanned( ctx, 1, "test", 3712, DateTime.UtcNow, new TimeSpan( -3712L ) ) )
                    .ShouldThrowExactly<ArgumentOutOfRangeException>()
                    .Message.ShouldBe( "Banishment duration cannot be negative. (Parameter 'duration')" );

                Util.Invokable( () => userBanned.SetUserBanned( ctx, 1, "test", 3712, DateTime.UtcNow, new TimeSpan( -3712L ) ) )
                    .ShouldThrowExactly<ArgumentOutOfRangeException>()
                    .Message.ShouldBe( "Banishment duration cannot be negative. (Parameter 'duration')" );
            }
        }

        [Test]
        public void destroy_user_destroy_user_ban()
        {
            var user = ObtainSqlPackage<UserTable>();
            var userBanned = ObtainSqlPackage<UserBannedTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                string keyReason = "test";
                int userId = user.CreateUser( ctx, 1, Guid.NewGuid().ToString() );

                userBanned.SetUserBanned( ctx, 1, keyReason, userId );

                userBanned.GetCurrentlyBannedUser( ctx, userId, keyReason )
                          .ShouldNotBeNull( $"Banned user {userId} must be in the UserBanned table." );

                user.DestroyUser( ctx, 1, userId );

                userBanned.GetCurrentlyBannedUser( ctx, userId, keyReason ).ShouldBeNull();
            }
        }

        [Test]
        public void ban_user_many_times_with_same_values_is_idempotent()
        {
            var user = ObtainSqlPackage<UserTable>();
            var userBanned = ObtainSqlPackage<UserBannedTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                string keyReason = "repeat-user-ban";
                int userId = user.CreateUser( ctx, 1, Guid.NewGuid().ToString() );

                userBanned.GetCurrentlyBannedUser( ctx, userId, keyReason )
                          .ShouldBeNull( $"User {userId} must not be banned." );

                userBanned.SetUserBanned( ctx, 1, keyReason, userId );

                var banned = userBanned.GetCurrentlyBannedUser( ctx, userId, keyReason );

                for( int i = 0; i < 10; i++ )
                {
                    userBanned.SetUserBanned( ctx, 1, keyReason, userId );

                    var banned2 = userBanned.GetCurrentlyBannedUser( ctx, userId, keyReason );

                    banned2.ShouldNotBeNull();

                    if( banned is not null )
                    {
                        banned2.ShouldBeEquivalentTo( banned );
                    }
                    banned = banned2;
                }

                userBanned.GetCurrentlyBannedUser( ctx, userId ).Count().ShouldBe( 1 );
            }
        }

        [Test]
        public void user_banned_in_the_future_is_not_banned_now()
        {
            var user = ObtainSqlPackage<UserTable>();
            var userBanned = ObtainSqlPackage<UserBannedTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                int userId = user.CreateUser( ctx, 1, Guid.NewGuid().ToString() );

                userBanned.SetUserBanned( ctx, 1, "test", userId, DateTime.UtcNow.AddYears( 3712 ) );

                userBanned.GetBannedUser( ctx, userId ).ShouldNotBeNull();

                userBanned.GetCurrentlyBannedUser( ctx, userId, "test" ).ShouldBeNull();
            }
        }

        [Test]
        public void user_who_is_no_longer_banned_is_not_banned_now()
        {
            var user = ObtainSqlPackage<UserTable>();
            var userBanned = ObtainSqlPackage<UserBannedTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                string keyReason = "futur-ban";
                int userId = user.CreateUser( ctx, 1, Guid.NewGuid().ToString() );

                userBanned.SetUserBanned( ctx, 1, keyReason, userId, DateTime.UtcNow.AddYears( -1 ), new TimeSpan( 3712L ) );

                userBanned.GetBannedUser( ctx, userId ).ShouldNotBeNull();

                userBanned.GetCurrentlyBannedUser( ctx, userId, keyReason ).ShouldBeNull();
            }
        }

        [Test]
        public void user_banned_forever_is_ban_now()
        {
            var user = ObtainSqlPackage<UserTable>();
            var userBanned = ObtainSqlPackage<UserBannedTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                string keyReason = "ban-forever";
                int userId = user.CreateUser( ctx, 1, Guid.NewGuid().ToString() );

                userBanned.SetUserBanned( ctx, 1, keyReason, userId );

                userBanned.GetCurrentlyBannedUser( ctx, userId, keyReason ).ShouldNotBeNull();
            }
        }

        [Test]
        public void ban_dates_can_be_updated()
        {
            var user = ObtainSqlPackage<UserTable>();
            var userBanned = ObtainSqlPackage<UserBannedTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                int userId = user.CreateUser( ctx, 1, Guid.NewGuid().ToString() );

                DateTime banStartDate = DateTime.UtcNow;
                TimeSpan duration = TimeSpan.FromDays( 1 );
                string keyReason = "test";

                userBanned.SetUserBanned( ctx, 1, keyReason, userId, banStartDate, duration );

                var banned = userBanned.GetCurrentlyBannedUser( ctx, userId, keyReason );
                banned.ShouldNotBeNull();
                banned!.UserId.ShouldBe( userId );
                banned!.BanStartDate.ShouldBe( banStartDate, new TimeSpan( 0, 0, 0, 0, 10 ) );
                banned!.BanEndDate.ShouldBe( banStartDate + duration, new TimeSpan( 0, 0, 0, 0, 10 ) );
                banned!.KeyReason.ShouldBe( keyReason );

                banStartDate = DateTime.UtcNow.AddDays( -1 );
                duration = TimeSpan.FromDays( 10 );

                userBanned.SetUserBanned( ctx, 1, keyReason, userId, banStartDate, duration );

                banned = userBanned.GetCurrentlyBannedUser( ctx, userId, keyReason );
                banned.ShouldNotBeNull();
                banned!.UserId.ShouldBe( userId );
                banned!.BanStartDate.ShouldBe( banStartDate, new TimeSpan( 0, 0, 0, 0, 10 ) );
                banned!.BanEndDate.ShouldBe( banStartDate + duration, new TimeSpan( 0, 0, 0, 0, 10 ) );
                banned!.KeyReason.ShouldBe( keyReason );
            }
        }

        [Test]
        public void update_ban_without_date_let_same_banStartDate_and_set_eternal_ban()
        {
            var user = ObtainSqlPackage<UserTable>();
            var userBanned = ObtainSqlPackage<UserBannedTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                int userId = user.CreateUser( ctx, 1, Guid.NewGuid().ToString() );
                string keyReason = "test";
                var firstBanStartDate = new DateTime( 3712, 01, 01 );
                var firstBanEndDate = firstBanStartDate.AddDays( 1 );

                userBanned.SetUserBanned( ctx, 1, keyReason, userId, firstBanStartDate, firstBanEndDate );

                var banned = userBanned.GetBannedUser( ctx, userId );

                banned.ShouldNotBeNull();
                banned!.BanStartDate.ShouldBe( firstBanStartDate );
                banned!.BanEndDate.ShouldBe( firstBanEndDate );

                userBanned.SetUserBanned( ctx, 1, keyReason, userId );

                banned = userBanned.GetBannedUser( ctx, userId );
                banned.ShouldNotBeNull();
                banned!.BanStartDate.ShouldBe( firstBanStartDate );
                banned!.BanEndDate.ShouldBe( new DateTime( 9999, 12, 31 ) );
            }
        }

        [Test]
        public void user_can_have_multiple_bans()
        {
            var user = ObtainSqlPackage<UserTable>();
            var userBanned = ObtainSqlPackage<UserBannedTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                int userId = user.CreateUser( ctx, 1, Guid.NewGuid().ToString() );

                string keyReason1 = "first-reason";
                userBanned.SetUserBanned( ctx, 1, keyReason1, userId );
                string keyReason2 = "second-reason";
                userBanned.SetUserBanned( ctx, 1, keyReason2, userId );
                string keyReason3 = "tird-reason";
                userBanned.SetUserBanned( ctx, 1, keyReason3, userId );

                var banns = userBanned.GetCurrentlyBannedUser( ctx, userId );

                banns.Count().ShouldBe( 3 );
                banns.ShouldContain( ban => ban.KeyReason == keyReason1, 1 );
                banns.ShouldContain( ban => ban.KeyReason == keyReason2, 1 );
                banns.ShouldContain( ban => ban.KeyReason == keyReason3, 1 );
            }
        }

        static T ObtainSqlPackage<T>() where T : SqlPackage
        {
            return SharedEngine.Map.StObjs.Obtain<T>()
                ?? throw new NullReferenceException( $"Cannot obtain {typeof( T ).Name} table." );
        }
    }
}
