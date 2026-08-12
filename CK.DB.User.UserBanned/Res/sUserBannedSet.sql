create procedure CK.sUserBannedSet
(
    @ActorId int,
    @KeyReason varchar(128),
    @UserId int,
    @BanStartDate datetime2(2),
    @BanEndDate datetime2(2)
)
as
begin
    if @ActorId <= 0 throw 50000, 'Security.AnonymousNotAllowed', 1;
    if IsNull(@KeyReason, '') = '' throw 50000, 'Security.InvalidNullOrEmptyKeyReason', 1;
    if @UserId <= 0 throw 50000, 'Security.InvalidUserId', 1;

    --[beginsp]

    if exists( select 1 from CK.tActorProfile where GroupId = 1 and ActorId = @UserId )
        throw 50000, 'Security.CannotBanSystemGroupMember', 1;
    
    declare @CanContinue bit = 0;
    if exists( select 1 from CK.tActorProfile where GroupId = 1 and ActorId = @ActorId )
        set @CanContinue = 1;

    -- Extension point: a consuming package can transform this procedure to inject SQL here
    -- that adjusts @CanContinue (to re-open or to harden the access) before the final check.
    --<BannedSecurityCheck revert />

    if @CanContinue = 0 throw 50000, 'Security.AdminOnly', 1;
        
    -- Action

    if not exists( select 1 from CK.tUserBanned where KeyReason like @KeyReason and UserId = @UserId )
	begin
		--<PreCreate revert />

        insert into CK.tUserBanned( KeyReason, UserId, BanStartDate, BanEndDate ) values
		(
			@KeyReason,
			@UserId,
			IsNull(@BanStartDate, sysutcdatetime()),
			IsNull(@BanEndDate, '9999-12-31')
		);

		--<PostCreate />
	end
    else
	begin
   		--<PreUpdate revert />

		update CK.tUserBanned
		set BanStartDate = IsNull(@BanStartDate, BanStartDate),
			BanEndDate = IsNull(@BanEndDate, '9999-12-31')
		where KeyReason like @KeyReason and UserId = @UserId;

		--<PostUpdate />
	end

    --[endsp]
end
