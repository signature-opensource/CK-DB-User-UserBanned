using CK.Auth;
using CK.Cris;

namespace CK.IO.User.UserBanned;

/// <summary>
/// Creates or updates the banishment of a user for a given <see cref="KeyReason"/>.
/// The acting <see cref="ICommandAuthNormal.ActorId"/> must be allowed to ban (by default a member
/// of the System group, enforced by CK.sUserBannedSet).
/// </summary>
public interface ISetUserBannedCommand : ICommand<ICrisBasicCommandResult>, ICommandAuthNormal
{
    /// <summary>
    /// Gets or sets the identifier of the user to ban.
    /// </summary>
    int UserId { get; set; }

    /// <summary>
    /// Gets or sets the reason of the banishment. Must not be null or empty (128 characters at most).
    /// </summary>
    string KeyReason { get; set; }

    /// <summary>
    /// Gets or sets the start date (UTC) of the banishment. When null, the existing start date is
    /// kept for an already banned user, or utc now is used for a new banishment.
    /// </summary>
    DateTime? BanStartDate { get; set; }

    /// <summary>
    /// Gets or sets the end date (UTC) of the banishment. When null, the banishment is eternal
    /// (9999-12-31).
    /// </summary>
    DateTime? BanEndDate { get; set; }
}
