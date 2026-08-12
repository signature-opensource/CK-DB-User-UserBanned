using CK.Auth;
using CK.Cris;

namespace CK.IO.User.UserBanned;

/// <summary>
/// Destroys the banishment of a user for a given <see cref="KeyReason"/>.
/// The acting <see cref="ICommandAuthNormal.ActorId"/> must be allowed to unban (by default a member
/// of the System group, enforced by CK.sUserBannedDestroy).
/// </summary>
public interface IDestroyUserBannedCommand : ICommand<ICrisBasicCommandResult>, ICommandAuthNormal
{
    /// <summary>
    /// Gets or sets the identifier of the user to unban.
    /// </summary>
    int UserId { get; set; }

    /// <summary>
    /// Gets or sets the reason of the banishment to destroy. Must not be null or empty.
    /// </summary>
    string KeyReason { get; set; }
}
