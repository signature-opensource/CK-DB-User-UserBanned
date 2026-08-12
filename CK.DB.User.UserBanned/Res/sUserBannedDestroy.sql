create procedure CK.sUserBannedDestroy (
    @ActorId int,
    @KeyReason varchar(128),
    @UserId int
)
as
begin
    if @ActorId <= 0 throw 50000, 'Security.AnonymousNotAllowed', 1;
    if IsNull(@KeyReason, '') = '' throw 50000, 'Security.InvalidNullOrEmptyKeyReason', 1;
    if @UserId <= 0 throw 50000, 'Security.InvalidUserId', 1;

    --[beginsp]

    declare @CanContinue bit = 0;
    if exists( select 1 from CK.tActorProfile where GroupId = 1 and ActorId = @ActorId )
        set @CanContinue = 1;

    -- Extension point: a consuming package can transform this procedure to inject SQL here
    -- that adjusts @CanContinue (to re-open or to harden the access) before the final check.
    --<DestroySecurityCheck revert />

    if @CanContinue = 0 throw 50000, 'Security.AdminOnly', 1;

    --<PreDestroy revert />
    delete from CK.tUserBanned where KeyReason = @KeyReason and UserId = @UserId;
    --<PostDestroy />

    --[endsp]
end
