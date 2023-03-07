using CK.Core;

namespace CK.DB.User.UserPassword.Banned
{
    [SqlPackage( Schema = "CK", ResourcePath = "Res" )]
    [Versions( "1.0.0" )]
    [SqlObjectItem( "transform:CK.sAuthUserOnLogin" )]
    public abstract class Package : SqlPackage
    {
        void StObjConstruct( CK.DB.User.UserPassword.Package userPassword, CK.DB.User.UserBanned.Package bannedPackage )
        {
        }
    }
}
