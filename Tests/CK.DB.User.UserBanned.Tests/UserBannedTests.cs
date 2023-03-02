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
                userBanned.Invoking( table => table.SetUserBanned( ctx, 0, 1, "Test" ) )
                          .Should().Throw<Exception>()
                          .WithInnerException<Exception>()
                          .WithMessage( "Security.AnonymousNotAllowed" );

                userBanned.Invoking( table => table.SetUserBanned( ctx, 1, 0, "Test" ) )
                          .Should().Throw<Exception>()
                          .WithInnerException<Exception>()
                          .WithMessage( "Security.InvalidUserId" );
            }
        }

        [Test]
        public void actor_0_or_user_0_cannot_destroy_ban()
        {
            var userBanned = ObtainSqlPackage<UserBannedTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                userBanned.Invoking( table => table.DestroyUserBanned( ctx, 0, 1 ) )
                          .Should().Throw<Exception>()
                          .WithInnerException<Exception>()
                          .WithMessage( "Security.AnonymousNotAllowed" );

                userBanned.Invoking( table => table.DestroyUserBanned( ctx, 1, 0 ) )
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

                userBanned.SetUserBanned( ctx, 1, userId, "Test ban user" );

                userBanned.GetUserBanned( ctx, userId ).Should().NotBeNull();
            }
        }

        [Test]
        public void system_can_destroy_ban()
        {
            var user = ObtainSqlPackage<UserTable>();
            var userBanned = ObtainSqlPackage<UserBannedTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                int userId = user.CreateUser( ctx, 1, Guid.NewGuid().ToString() );

                userBanned.SetUserBanned( ctx, 1, userId, "Test ban user" );

                userBanned.GetUserBanned( ctx, userId ).Should().NotBeNull();

                userBanned.DestroyUserBanned( ctx, 1, userId );

                userBanned.GetUserBanned( ctx, userId ).Should().BeNull();
            }
        }

        [Test]
        public void group_1_member_cannot_be_ban()
        {
            var userBanned = ObtainSqlPackage<UserBannedTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                userBanned.Invoking( table => table.SetUserBanned( ctx, 1, 1, "Test bas system" ) )
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

                userBanned.Invoking( table => table.SetUserBanned( ctx, userId, userId, "I ban myself" ) )
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

                userBanned.SetUserBanned( ctx, 1, userId, "Ban a member" );

                userBanned.Invoking( table => table.DestroyUserBanned( ctx, userId, userId ) )
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
                userBanned.Invoking( table => table.SetUserBanned( ctx, 1, 1, "", DateTime.UtcNow, new TimeSpan( -3712L ) ) )
                          .Should().ThrowExactly<ArgumentOutOfRangeException>()
                          .WithMessage( "Banishment duration cannot be negative. (Parameter 'duration')" );

                userBanned.Invoking( table => table.SetUserBanned( ctx, 1, 1, "", DateTime.UtcNow, new TimeSpan( -3712L ) ) )
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
                int userId = user.CreateUser( ctx, 1, Guid.NewGuid().ToString() );

                userBanned.SetUserBanned( ctx, 1, userId, "Test ban user" );

                Assert.IsNotNull(
                    userBanned.GetUserBanned( ctx, userId ),
                    $"Banned user {userId} must be in the UserBanned table." );

                user.DestroyUser( ctx, 1, userId );

                userBanned.GetUserBanned( ctx, userId ).Should().BeNull();
            }
        }

        [Test]
        public void ban_user_many_times_with_same_values_is_idempotent()
        {
            var user = ObtainSqlPackage<UserTable>();
            var userBanned = ObtainSqlPackage<UserBannedTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                int userId = user.CreateUser( ctx, 1, Guid.NewGuid().ToString() );

                Assert.IsNull(
                    userBanned.GetUserBanned( ctx, userId ),
                    $"User {userId} must not be banned." );

                userBanned.SetUserBanned( ctx, 1, userId, "Repeat ban user" );

                var banned = userBanned.GetUserBanned( ctx, userId );

                for( int i = 0; i < 10; i++ )
                {
                    userBanned.SetUserBanned( ctx, 1, userId, "Repeat ban user" );

                    var banned2 = userBanned.GetUserBanned( ctx, userId );

                    banned2.Should().NotBeNull();

                    if( banned is not null )
                    {
                        banned2.Should().BeEquivalentTo( banned );
                    }
                    banned = banned2;
                }
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

                userBanned.SetUserBanned( ctx, 1, userId, "Test ban user in the future", DateTime.UtcNow.AddYears( 3712 ) );

                var banned = userBanned.GetUserBanned( ctx, userId );

                banned.Should().NotBeNull();
                banned!.IsBannedNow.Should().BeFalse();
            }
        }

        [Test]
        public void user_who_is_no_longer_banned_is_not_banned_now()
        {
            var user = ObtainSqlPackage<UserTable>();
            var userBanned = ObtainSqlPackage<UserBannedTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                int userId = user.CreateUser( ctx, 1, Guid.NewGuid().ToString() );

                userBanned.SetUserBanned( ctx, 1, userId, "Test ban user in the future", DateTime.UtcNow.AddYears( -1 ), new TimeSpan( 3712L ) );

                var banned = userBanned.GetUserBanned( ctx, userId );

                banned.Should().NotBeNull();
                banned!.IsBannedNow.Should().BeFalse();
            }
        }

        [Test]
        public void user_banned_forever_is_ban_now()
        {
            var user = ObtainSqlPackage<UserTable>();
            var userBanned = ObtainSqlPackage<UserBannedTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                int userId = user.CreateUser( ctx, 1, Guid.NewGuid().ToString() );

                userBanned.SetUserBanned( ctx, 1, userId, "Test ban user in the future" );

                var banned = userBanned.GetUserBanned( ctx, userId );

                banned.Should().NotBeNull();
                banned!.IsBannedNow.Should().BeTrue();
            }
        }

        [Test]
        public void ban_informations_can_be_updated()
        {
            var user = ObtainSqlPackage<UserTable>();
            var userBanned = ObtainSqlPackage<UserBannedTable>();

            using( SqlStandardCallContext ctx = new() )
            {
                int userId = user.CreateUser( ctx, 1, Guid.NewGuid().ToString() );

                DateTime banStartDate = DateTime.UtcNow;
                TimeSpan duration = TimeSpan.FromDays( 1 );
                string reason = "User banned";

                userBanned.SetUserBanned( ctx, 1, userId, reason, banStartDate, duration );

                var banned = userBanned.GetUserBanned( ctx, userId );
                banned.Should().NotBeNull();
                banned!.UserId.Should().Be( userId );
                banned!.BanStartDate.Should().BeCloseTo( banStartDate, new TimeSpan( 0, 0, 0, 0, 10 ) );
                banned!.BanEndDate.Should().BeCloseTo( banStartDate + duration, new TimeSpan( 0, 0, 0, 0, 10 ) );
                banned!.Reason.Should().Be( reason );

                banStartDate = DateTime.UtcNow.AddDays( 1 );
                duration = TimeSpan.FromDays( 10 );
                reason = "Test update ban informations";

                userBanned.SetUserBanned( ctx, 1, userId, reason, banStartDate, duration );

                banned = userBanned.GetUserBanned( ctx, userId );
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
                var firstBanStartDate = new DateTime( 3712, 01, 01 );
                var firstBanEndDate = firstBanStartDate.AddDays( 1 );

                userBanned.SetUserBanned( ctx, 1, userId, "Temporary ban user", firstBanStartDate, firstBanEndDate );

                var banned = userBanned.GetUserBanned( ctx, userId );

                banned.Should().NotBeNull();
                banned!.BanStartDate.Should().Be( firstBanStartDate );
                banned!.BanEndDate.Should().Be( firstBanEndDate );

                userBanned.SetUserBanned( ctx, 1, userId, "Update ban user" );

                banned = userBanned.GetUserBanned( ctx, userId );

                banned.Should().NotBeNull();
                banned!.BanStartDate.Should().Be( firstBanStartDate );
                banned!.BanEndDate.Should().Be( new DateTime( 9999, 12, 31 ) );
            }
        }

        static T ObtainSqlPackage<T>() where T : SqlPackage
        {
            return TestHelper.StObjMap.StObjs.Obtain<T>()
                ?? throw new NullReferenceException( $"Cannot obtain {typeof( T ).Name} table." );
        }
    }
}
