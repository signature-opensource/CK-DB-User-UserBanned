create procedure CK.sUserBannedSet
(
    @ActorId int,
    @Reason varchar(128),
    @UserId int,
    @BanStartDate datetime2(2),
    @BanEndDate datetime2(2)
)
as
begin
    if @ActorId <= 0 throw 50000, 'Security.AnonymousNotAllowed', 1;
    if IsNull(@Reason, '') = '' throw 50000, 'Security.InvalidNullOrEmptyReason', 1;
    if @UserId <= 0 throw 50000, 'Security.InvalidUserId', 1;

    --[beginsp]

    if exists( select 1 from CK.tActorProfile where GroupId = 1 and ActorId = @UserId )
        throw 50000, 'Security.CannotBanSystemGroupMember', 1;
    
    -- Preconditions

    if not exists( select 1 from CK.tActorProfile where GroupId = 1 and ActorId = @ActorId )
        throw 50000, 'Security.SystemLevelOnly', 1;
        
    -- Action

    if not exists( select 1 from CK.tUserBanned where Reason like @Reason and UserId = @UserId )
	begin
		--<PreCreate revert />

        insert into CK.tUserBanned( Reason, UserId, BanStartDate, BanEndDate ) values
		(
			@Reason,
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
		where Reason like @Reason and UserId = @UserId;

		--<PostUpdate />
	end

    --[endsp]
end
