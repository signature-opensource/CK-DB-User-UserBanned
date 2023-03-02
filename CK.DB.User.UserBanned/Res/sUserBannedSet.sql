create procedure CK.sUserBannedSet (
    @ActorId int,
    @UserId int,
    @Reason nvarchar(max),
    @BanStartDate datetime2(2),
    @BanEndDate datetime2(2)
)
as
begin
    if @ActorId <= 0 throw 50000, 'Security.AnonymousNotAllowed', 1;
    if @UserId <= 0 throw 50000, 'Security.InvalidUserId', 1;

    --[beginsp]

    if exists( select 1 from CK.tActorProfile where GroupId = 1 and ActorId = @UserId )
        throw 50000, 'Security.CannotBanSystemGroupMember', 1;
    
    -- Preconditions

    if not exists( select 1 from CK.tActorProfile where GroupId = 1 and ActorId = @ActorId )
        throw 50000, 'Security.SystemLevelOnly', 1;
        
    -- Action

    if not exists( select 1 from CK.tUserBanned where UserId = @UserId )
	begin
		--<PreCreate revert />

        insert into CK.tUserBanned( UserId, Reason, BanStartDate, BanEndDate ) values
		(
			@UserId,
			@Reason,
			IsNull(@BanStartDate, sysutcdatetime()),
			IsNull(@BanEndDate, '9999-12-31')
		);

		--<PostCreate />
	end
    else
	begin
   		--<PreUpdate revert />

		update CK.tUserBanned
		set Reason = @Reason,
			BanStartDate = IsNull(@BanStartDate, BanStartDate),
			BanEndDate = IsNull(@BanEndDate, '9999-12-31')
		where UserId = @UserId;

		--<PostUpdate />
	end

    --[endsp]
end
