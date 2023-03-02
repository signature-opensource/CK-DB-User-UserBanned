--[beginscript]

create table CK.tUserBanned (
    UserId int,
    BanStartDate datetime2(2) not null,
    BanEndDate datetime2(2) not null,
    Reason nvarchar(max) not null,

    constraint PK_CK_UserBanned_UserId primary key ( UserId ),
    constraint FK_CK_UserBanned_UserId foreign key ( UserId ) references CK.tUser( UserId ),
	constraint CK_CK_UserBanned_BanStartDate_BanEndDate check ( BanStartDate <= BanEndDate )
);

--[endscript]
