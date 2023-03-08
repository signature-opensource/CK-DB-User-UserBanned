-- SetupConfig: {}
--
create view CK.vUserCurrentlyBanned
as
    select ub.UserId
    	  ,ub.KeyReason
          ,u.UserName
    	  ,ub.BanStartDate
    	  ,ub.BanEndDate
    from CK.fUserBannedViewAt( convert(datetime2(2), sysutcdatetime()) ) ub
    inner join CK.tUser u
    	on ub.UserId = u.UserId
