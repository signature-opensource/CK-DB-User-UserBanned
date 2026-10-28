using CK.Auth;
using CK.Cris;

namespace CK.IO.User.UserBanned;

/// <summary>
/// Destroys the banishment of a user for a given <see cref="KeyReason"/>.
/// The acting actor must be allowed to unban (by default a member of the System group, enforced by CK.sUserBannedDestroy).
/// <para>
/// This is administration, a high-value action: it requires the <see cref="ICommandAuthCritical">Critical</see> level
/// (a recent login), never reachable by a calling party alone.
/// </para>
/// </summary>
public interface IDestroyUserBannedCommand : ICommand<ICrisBasicCommandResult>, ICommandAuthCritical
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
