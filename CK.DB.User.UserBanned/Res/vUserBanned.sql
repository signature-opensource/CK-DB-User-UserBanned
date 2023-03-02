-- SetupConfig: {}
--
create view CK.vUserBanned
as
    select ub.UserId
          ,u.UserName
    	  ,ub.BanStartDate
    	  ,ub.BanEndDate
    	  ,IsBannedNow = case when ub.BanStartDate <= convert(datetime2(2), sysutcdatetime()) and ub.BanEndDate > convert(datetime2(2), sysutcdatetime()) then 1 else 0 end
    	  ,ub.Reason
    from CK.tUserBanned ub
    inner join CK.tUser u
    	on ub.UserId = u.UserId
