using CK.Core;
using CK.DB.Actor;
using CK.SqlServer;
using FluentAssertions;
using NUnit.Framework;
using System;
using static CK.Testing.DBSetupTestHelper;

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
                userBanned.Invoking( table => table.SetUserBanned( ctx, 0, "test", 1 ) )
                          .Should().Throw<Exception>()
                          .WithInnerException<Exception>()
                          .WithMessage( "Security.AnonymousNotAllowed" );

                userBanned.Invoking( table => table.SetUserBanned( ctx, 1, "test", 0 ) )
                          .Should().Throw<Exception>()
                          .WithInnerException<Exception>()
                          .WithMessage( "Security.InvalidUserId" );
            }
        }

        [Test]
        public void null_ro_empty_reason_is_invalid()
        {
            var userBanned = ObtainSqlPackage<UserBannedTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                userBanned.Invoking( table => table.SetUserBanned( ctx, 1, null!, 3712 ) )
                    .Should().Throw<Exception>()
                    .WithInnerException<Exception>()
                    .WithMessage( "Security.InvalidNullOrEmptyReason" );

                userBanned.Invoking( table => table.SetUserBanned( ctx, 1, "", 3712 ) )
                    .Should().Throw<Exception>()
                    .WithInnerException<Exception>()
                    .WithMessage( "Security.InvalidNullOrEmptyReason" );
            }
        }

        [Test]
        public void actor_0_or_user_0_cannot_destroy_ban()
        {
            var userBanned = ObtainSqlPackage<UserBannedTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                userBanned.Invoking( table => table.DestroyUserBanned( ctx, 0, "test", 1 ) )
                          .Should().Throw<Exception>()
                          .WithInnerException<Exception>()
                          .WithMessage( "Security.AnonymousNotAllowed" );

                userBanned.Invoking( table => table.DestroyUserBanned( ctx, 1, "test", 0 ) )
                          .Should().Throw<Exception>()
                          .WithInnerException<Exception>()
                          .WithMessage( "Security.InvalidUserId" );
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

                userBanned.GetCurrentlyBannedUser( ctx, userId ).Should().HaveCount( 1 );
            }
        }

        [Test]
        public void system_can_destroy_ban()
        {
            var user = ObtainSqlPackage<UserTable>();
            var userBanned = ObtainSqlPackage<UserBannedTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                string reason = "test";
                int userId = user.CreateUser( ctx, 1, Guid.NewGuid().ToString() );

                userBanned.SetUserBanned( ctx, 1, reason, userId );

                userBanned.GetCurrentlyBannedUser( ctx, userId, reason ).Should().NotBeNull();

                userBanned.DestroyUserBanned( ctx, 1, reason, userId );

                userBanned.GetCurrentlyBannedUser( ctx, userId, reason ).Should().BeNull();
            }
        }

        [Test]
        public void group_1_member_cannot_be_ban()
        {
            var userBanned = ObtainSqlPackage<UserBannedTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                userBanned.Invoking( table => table.SetUserBanned( ctx, 1, "test", 1 ) )
                    .Should().Throw<Exception>()
                    .WithInnerException<Exception>()
                    .WithMessage( "ck:CK.sUserBannedSet-{*}-[Security.CannotBanSystemGroupMember]" );
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

                userBanned.Invoking( table => table.SetUserBanned( ctx, userId, "auto-ban", userId ) )
                          .Should().Throw<Exception>()
                          .WithInnerException<Exception>()
                          .WithMessage( "ck:CK.sUserBannedSet-{*}-[Security.SystemLevelOnly]" );
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

                userBanned.Invoking( table => table.DestroyUserBanned( ctx, userId, "test", userId ) )
                          .Should().Throw<Exception>()
                          .WithInnerException<Exception>()
                          .WithMessage( "ck:CK.sUserBannedDestroy-{*}-[Security.SystemLevelOnly]" );
            }
        }

        [Test]
        public void cannot_set_negative_ban_duration()
        {
            var userBanned = ObtainSqlPackage<UserBannedTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                userBanned.Invoking( table => table.SetUserBanned( ctx, 1, "test", 3712, DateTime.UtcNow, new TimeSpan( -3712L ) ) )
                          .Should().ThrowExactly<ArgumentOutOfRangeException>()
                          .WithMessage( "Banishment duration cannot be negative. (Parameter 'duration')" );

                userBanned.Invoking( table => table.SetUserBanned( ctx, 1, "test", 3712, DateTime.UtcNow, new TimeSpan( -3712L ) ) )
                          .Should().ThrowExactly<ArgumentOutOfRangeException>()
                          .WithMessage( "Banishment duration cannot be negative. (Parameter 'duration')" );
            }
        }

        [Test]
        public void destroy_user_destroy_user_ban()
        {
            var user = ObtainSqlPackage<UserTable>();
            var userBanned = ObtainSqlPackage<UserBannedTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                string reason = "test";
                int userId = user.CreateUser( ctx, 1, Guid.NewGuid().ToString() );

                userBanned.SetUserBanned( ctx, 1, reason, userId );

                Assert.IsNotNull(
                    userBanned.GetCurrentlyBannedUser( ctx, userId, reason ),
                    $"Banned user {userId} must be in the UserBanned table." );

                user.DestroyUser( ctx, 1, userId );

                userBanned.GetCurrentlyBannedUser( ctx, userId, reason ).Should().BeNull();
            }
        }

        [Test]
        public void ban_user_many_times_with_same_values_is_idempotent()
        {
            var user = ObtainSqlPackage<UserTable>();
            var userBanned = ObtainSqlPackage<UserBannedTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                string reason = "repeat-user-ban";
                int userId = user.CreateUser( ctx, 1, Guid.NewGuid().ToString() );

                Assert.IsNull(
                    userBanned.GetCurrentlyBannedUser( ctx, userId, reason ),
                    $"User {userId} must not be banned." );

                userBanned.SetUserBanned( ctx, 1, reason, userId );

                var banned = userBanned.GetCurrentlyBannedUser( ctx, userId, reason );

                for( int i = 0; i < 10; i++ )
                {
                    userBanned.SetUserBanned( ctx, 1, reason, userId );

                    var banned2 = userBanned.GetCurrentlyBannedUser( ctx, userId, reason );

                    banned2.Should().NotBeNull();

                    if( banned is not null )
                    {
                        banned2.Should().BeEquivalentTo( banned );
                    }
                    banned = banned2;
                }

                userBanned.GetCurrentlyBannedUser( ctx, userId ).Should().HaveCount( 1 );
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

                userBanned.GetBannedUser( ctx, userId, "test" ).Should().NotBeNull();

                userBanned.GetCurrentlyBannedUser( ctx, userId, "test" ).Should().BeNull();
            }
        }

        [Test]
        public void user_who_is_no_longer_banned_is_not_banned_now()
        {
            var user = ObtainSqlPackage<UserTable>();
            var userBanned = ObtainSqlPackage<UserBannedTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                string reason = "futur-ban";
                int userId = user.CreateUser( ctx, 1, Guid.NewGuid().ToString() );

                userBanned.SetUserBanned( ctx, 1, reason, userId, DateTime.UtcNow.AddYears( -1 ), new TimeSpan( 3712L ) );

                userBanned.GetBannedUser( ctx, userId, reason ).Should().NotBeNull();

                userBanned.GetCurrentlyBannedUser( ctx, userId, reason ).Should().BeNull();
            }
        }

        [Test]
        public void user_banned_forever_is_ban_now()
        {
            var user = ObtainSqlPackage<UserTable>();
            var userBanned = ObtainSqlPackage<UserBannedTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                string reason = "ban-forever";
                int userId = user.CreateUser( ctx, 1, Guid.NewGuid().ToString() );

                userBanned.SetUserBanned( ctx, 1, reason, userId );

                userBanned.GetCurrentlyBannedUser( ctx, userId, reason ).Should().NotBeNull();
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
                string reason = "test";

                userBanned.SetUserBanned( ctx, 1, reason, userId, banStartDate, duration );

                var banned = userBanned.GetCurrentlyBannedUser( ctx, userId, reason );
                banned.Should().NotBeNull();
                banned!.UserId.Should().Be( userId );
                banned!.BanStartDate.Should().BeCloseTo( banStartDate, new TimeSpan( 0, 0, 0, 0, 10 ) );
                banned!.BanEndDate.Should().BeCloseTo( banStartDate + duration, new TimeSpan( 0, 0, 0, 0, 10 ) );
                banned!.Reason.Should().Be( reason );

                banStartDate = DateTime.UtcNow.AddDays( -1 );
                duration = TimeSpan.FromDays( 10 );

                userBanned.SetUserBanned( ctx, 1, reason, userId, banStartDate, duration );

                banned = userBanned.GetCurrentlyBannedUser( ctx, userId, reason );
                banned.Should().NotBeNull();
                banned!.UserId.Should().Be( userId );
                banned!.BanStartDate.Should().BeCloseTo( banStartDate, new TimeSpan( 0, 0, 0, 0, 10 ) );
                banned!.BanEndDate.Should().BeCloseTo( banStartDate + duration, new TimeSpan( 0, 0, 0, 0, 10 ) );
                banned!.Reason.Should().Be( reason );
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
                string reason = "test";
                var firstBanStartDate = new DateTime( 3712, 01, 01 );
                var firstBanEndDate = firstBanStartDate.AddDays( 1 );

                userBanned.SetUserBanned( ctx, 1, reason, userId, firstBanStartDate, firstBanEndDate );

                var banned = userBanned.GetBannedUser( ctx, userId, reason );

                banned.Should().NotBeNull();
                banned!.BanStartDate.Should().Be( firstBanStartDate );
                banned!.BanEndDate.Should().Be( firstBanEndDate );

                userBanned.SetUserBanned( ctx, 1, reason, userId );

                banned = userBanned.GetBannedUser( ctx, userId, reason );
                banned.Should().NotBeNull();
                banned!.BanStartDate.Should().Be( firstBanStartDate );
                banned!.BanEndDate.Should().Be( new DateTime( 9999, 12, 31 ) );
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

                string reason1 = "first-reason";
                userBanned.SetUserBanned( ctx, 1, reason1, userId );
                string reason2 = "second-reason";
                userBanned.SetUserBanned( ctx, 1, reason2, userId );
                string reason3 = "tird-reason";
                userBanned.SetUserBanned( ctx, 1, reason3, userId );

                var banns = userBanned.GetCurrentlyBannedUser( ctx, userId );

                banns.Should().HaveCount( 3 );
                banns.Should().ContainSingle( ban => ban.Reason == reason1 );
                banns.Should().ContainSingle( ban => ban.Reason == reason2 );
                banns.Should().ContainSingle( ban => ban.Reason == reason3 );
            }
        }

        static T ObtainSqlPackage<T>() where T : SqlPackage
        {
            return TestHelper.StObjMap.StObjs.Obtain<T>()
                ?? throw new NullReferenceException( $"Cannot obtain {typeof( T ).Name} table." );
        }
    }
}
