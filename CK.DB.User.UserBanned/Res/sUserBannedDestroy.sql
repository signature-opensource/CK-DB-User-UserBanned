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
    
    -- Preconditions
    if not exists( select 1 from CK.tActorProfile where GroupId = 1 and ActorId = @ActorId )
        throw 50000, 'Security.SystemLevelOnly', 1;

    -- Action

    --<PreDestroy revert />
    delete from CK.tUserBanned where KeyReason = @KeyReason and UserId = @UserId;
    --<PostDestroy />

    --[endsp]
end
