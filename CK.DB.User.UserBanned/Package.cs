using CK.Core;

namespace CK.DB.User.UserBanned
{
    [SqlPackage( Schema = "CK", ResourcePath = "Res" )]
    [Versions( "1.0.0" )]
    [SqlObjectItem( "transform:CK.sUserDestroy, transform:CK.sAuthUserOnLogin" )]
    public abstract class Package : SqlPackage
    {
        void StObjConstruct( CK.DB.Auth.Package auth )
        {
        }

        [InjectObject]
        public UserBannedTable UserBannedTable { get; private set; }
    }
}
