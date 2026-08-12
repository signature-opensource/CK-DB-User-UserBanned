using CK.Core;
using CK.Cris;
using CK.DB.Actor;
using CK.IO.User.UserBanned;
using CK.SqlServer;
using CK.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

namespace CK.DB.User.UserBanned.Tests
{
    [TestFixture]
    public class UserBannedCrisTests
    {
        /// <summary>
        /// If <see langword="true"/>, not System group member are allow to ban/unban user.<br/>
        /// Test <see cref="not_system_actor_cannot_ban_and_the_error_is_reported_Async"/> will not be run.<br/>
        /// Default value is <see langword="false"/>.
        /// </summary>
        static protected bool AssumeNotSystemCanBan = false;

        /// <summary>The eternal ban end date set by CK.sUserBannedSet when no end date is provided.</summary>
        static readonly DateTime EternalBanEndDate = new DateTime( 9999, 12, 31 );

        [Test]
        public async Task set_user_banned_command_bans_the_user_Async()
        {
            using var scope = SharedEngine.AutomaticServices.CreateScope();
            var services = scope.ServiceProvider;
            var pocoDirectory = services.GetRequiredService<PocoDirectory>();
            var executor = services.GetRequiredService<CrisExecutionContext>();
            var user = services.GetRequiredService<UserTable>();
            var userBanned = services.GetRequiredService<UserBannedTable>();

            using SqlStandardCallContext ctx = new();
            string keyReason = "Cris.SetBan";
            int userId = await user.CreateUserAsync( ctx, 1, Guid.NewGuid().ToString() );

            var cmd = pocoDirectory.Create<ISetUserBannedCommand>( c =>
            {
                c.ActorId = 1;
                c.UserId = userId;
                c.KeyReason = keyReason;
            } );
            var res = await ExecuteAsync( executor, cmd );

            res.Success.ShouldBeTrue();
            var banned = userBanned.GetCurrentlyBannedUser( ctx, userId, keyReason );
            banned.ShouldNotBeNull();
            banned!.KeyReason.ShouldBe( keyReason );
            banned!.BanEndDate.ShouldBe( EternalBanEndDate );
        }

        [Test]
        public async Task set_user_banned_command_honors_start_and_end_dates_Async()
        {
            using var scope = SharedEngine.AutomaticServices.CreateScope();
            var services = scope.ServiceProvider;
            var pocoDirectory = services.GetRequiredService<PocoDirectory>();
            var executor = services.GetRequiredService<CrisExecutionContext>();
            var user = services.GetRequiredService<UserTable>();
            var userBanned = services.GetRequiredService<UserBannedTable>();

            using SqlStandardCallContext ctx = new();
            string keyReason = "Cris.SetBanWithDates";
            int userId = await user.CreateUserAsync( ctx, 1, Guid.NewGuid().ToString() );
            var banStartDate = new DateTime( 2020, 01, 01, 0, 0, 0, DateTimeKind.Utc );
            var banEndDate = new DateTime( 2999, 12, 31, 0, 0, 0, DateTimeKind.Utc );

            var cmd = pocoDirectory.Create<ISetUserBannedCommand>( c =>
            {
                c.ActorId = 1;
                c.UserId = userId;
                c.KeyReason = keyReason;
                c.BanStartDate = banStartDate;
                c.BanEndDate = banEndDate;
            } );
            var res = await ExecuteAsync( executor, cmd );

            res.Success.ShouldBeTrue();
            var banned = userBanned.GetCurrentlyBannedUser( ctx, userId, keyReason );
            banned.ShouldNotBeNull();
            banned!.BanStartDate.ShouldBe( banStartDate );
            banned!.BanEndDate.ShouldBe( banEndDate );
        }

        [Test]
        public async Task destroy_user_banned_command_removes_the_ban_Async()
        {
            using var scope = SharedEngine.AutomaticServices.CreateScope();
            var services = scope.ServiceProvider;
            var pocoDirectory = services.GetRequiredService<PocoDirectory>();
            var executor = services.GetRequiredService<CrisExecutionContext>();
            var user = services.GetRequiredService<UserTable>();
            var userBanned = services.GetRequiredService<UserBannedTable>();

            using SqlStandardCallContext ctx = new();
            string keyReason = "Cris.DestroyBan";
            int userId = await user.CreateUserAsync( ctx, 1, Guid.NewGuid().ToString() );

            await userBanned.SetUserBannedAsync( ctx, 1, keyReason, userId );
            userBanned.GetCurrentlyBannedUser( ctx, userId, keyReason ).ShouldNotBeNull();

            var cmd = pocoDirectory.Create<IDestroyUserBannedCommand>( c =>
            {
                c.ActorId = 1;
                c.UserId = userId;
                c.KeyReason = keyReason;
            } );
            var res = await ExecuteAsync( executor, cmd );

            res.Success.ShouldBeTrue();
            userBanned.GetBannedUser( ctx, userId ).ShouldBeNull();
        }

        [Test]
        public async Task not_system_actor_cannot_ban_and_the_error_is_reported_Async()
        {
            Assume.That( AssumeNotSystemCanBan is false );

            using var scope = SharedEngine.AutomaticServices.CreateScope();
            var services = scope.ServiceProvider;
            var pocoDirectory = services.GetRequiredService<PocoDirectory>();
            var executor = services.GetRequiredService<CrisExecutionContext>();
            var user = services.GetRequiredService<UserTable>();
            var userBanned = services.GetRequiredService<UserBannedTable>();

            using SqlStandardCallContext ctx = new();
            string keyReason = "Cris.NotSystem";
            int notSystemId = await user.CreateUserAsync( ctx, 1, Guid.NewGuid().ToString() );
            int userId = await user.CreateUserAsync( ctx, 1, Guid.NewGuid().ToString() );

            var cmd = pocoDirectory.Create<ISetUserBannedCommand>( c =>
            {
                c.ActorId = notSystemId;
                c.UserId = userId;
                c.KeyReason = keyReason;
            } );
            var res = await ExecuteAsync( executor, cmd );

            // CK.sUserBannedSet throws Security.AdminOnly: the handler catches it and reports an error.
            res.Success.ShouldBeFalse();
            userBanned.GetBannedUser( ctx, userId ).ShouldBeNull();
        }

        [Test]
        public async Task commands_with_invalid_userId_or_empty_keyReason_report_an_error_Async()
        {
            using var scope = SharedEngine.AutomaticServices.CreateScope();
            var services = scope.ServiceProvider;
            var pocoDirectory = services.GetRequiredService<PocoDirectory>();
            var executor = services.GetRequiredService<CrisExecutionContext>();
            var user = services.GetRequiredService<UserTable>();
            var userBanned = services.GetRequiredService<UserBannedTable>();

            using SqlStandardCallContext ctx = new();
            int userId = await user.CreateUserAsync( ctx, 1, Guid.NewGuid().ToString() );

            var emptyReason = pocoDirectory.Create<ISetUserBannedCommand>( c =>
            {
                c.ActorId = 1;
                c.UserId = userId;
                c.KeyReason = string.Empty;
            } );
            ( await ExecuteAsync( executor, emptyReason ) ).Success.ShouldBeFalse();

            var invalidUserId = pocoDirectory.Create<ISetUserBannedCommand>( c =>
            {
                c.ActorId = 1;
                c.UserId = 0;
                c.KeyReason = "Cris.InvalidUserId";
            } );
            ( await ExecuteAsync( executor, invalidUserId ) ).Success.ShouldBeFalse();

            var destroyEmptyReason = pocoDirectory.Create<IDestroyUserBannedCommand>( c =>
            {
                c.ActorId = 1;
                c.UserId = userId;
                c.KeyReason = string.Empty;
            } );
            ( await ExecuteAsync( executor, destroyEmptyReason ) ).Success.ShouldBeFalse();

            // Nothing has been written.
            userBanned.GetCurrentlyBannedUser( ctx, userId ).Count().ShouldBe( 0 );
        }

        static async Task<ICrisBasicCommandResult> ExecuteAsync( CrisExecutionContext executor,
                                                                 ICommand<ICrisBasicCommandResult> command )
        {
            var executing = await executor.ExecuteRootCommandAsync( command );
            return executing.WithResult<ICrisBasicCommandResult>().Result;
        }
    }
}
