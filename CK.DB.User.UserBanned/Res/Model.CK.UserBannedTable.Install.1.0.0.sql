--[beginscript]

create table CK.tUserBanned (
    UserId int not null,
    KeyReason varchar(128) collate Latin1_General_100_BIN2 not null,
    BanStartDate datetime2(2) not null,
    BanEndDate datetime2(2) not null,

    constraint PK_CK_UserBanned_UserId_KeyReason primary key ( UserId, KeyReason ),
    constraint FK_CK_UserBanned_UserId foreign key ( UserId ) references CK.tUser( UserId ),
	constraint CK_CK_UserBanned_BanStartDate_BanEndDate check ( BanStartDate <= BanEndDate )
);

--[endscript]
