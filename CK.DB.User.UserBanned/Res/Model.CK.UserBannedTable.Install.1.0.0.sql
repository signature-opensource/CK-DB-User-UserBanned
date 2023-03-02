--[beginscript]

create table CK.tUserBanned (
    UserId int not null,
    Reason varchar(128) not null,
    BanStartDate datetime2(2) not null,
    BanEndDate datetime2(2) not null,

    constraint PK_CK_UserBanned_UserId primary key ( UserId, Reason ),
    constraint FK_CK_UserBanned_UserId foreign key ( UserId ) references CK.tUser( UserId ),
	constraint CK_CK_UserBanned_BanStartDate_BanEndDate check ( BanStartDate <= BanEndDate )
);

--[endscript]
