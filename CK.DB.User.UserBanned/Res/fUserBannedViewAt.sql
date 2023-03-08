create function CK.fUserBannedViewAt
(
    @Date datetime2(2)
)
returns table
as
return
(
    select UserId, KeyReason, BanStartDate, BanEndDate
    from CK.tUserBanned
    where BanStartDate <= @Date and @Date < BanEndDate
);
