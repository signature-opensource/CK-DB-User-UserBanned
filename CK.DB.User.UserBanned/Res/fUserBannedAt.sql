create function CK.fUserBannedAt
(
    @Date datetime2(2)
)
returns table
as
return
(
    select *
    from CK.tUserBanned
    where BanStartDate <= @Date and @Date < BanEndDate
);
