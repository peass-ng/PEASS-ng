using System;
using System.Security.Principal;
using winPEAS.TaskScheduler.TaskEditor.Native;

namespace winPEAS.TaskScheduler
{
    /// <summary>Represents a system account.</summary>
    internal class User : IEquatable<User>, IDisposable
    {
        private static readonly WindowsIdentity cur = WindowsIdentity.GetCurrent();
        private SecurityIdentifier sid;

        /// <summary>Initializes a new instance of the <see cref="User"/> class.</summary>
        /// <param name="userName">
        /// Name of the user. This can be in the format <c>DOMAIN\username</c> or <c>username@domain.com</c> or <c>username</c> or
        /// <c>null</c> (for current user).
        /// </param>
        public User(string userName = null)
        {
            if (string.IsNullOrEmpty(userName)) userName = null;
            // 2018-03-02: Hopefully not a breaking change, but by adding in the comparison of an account name without a domain and the
            // current user, there is a chance that current implementations will break given the condition that a local account with the same
            // name as a domain account exists and the intention was to prefer the local account. In such a case, the developer should
            // prepend the user name in TaskDefinition.Principal.UserId with the machine name of the local machine.
            if (userName == null || cur.Name.Equals(userName, StringComparison.InvariantCultureIgnoreCase) || GetUser(cur.Name).Equals(userName, StringComparison.InvariantCultureIgnoreCase))
            {
                Identity = cur;
                sid = Identity.User;
            }
            else if (userName.Contains("\\") && !userName.StartsWith(@"NT AUTHORITY\"))
            {
                try
                {
                    using (var ds = new NativeMethods.DomainService())
                    {
                        Identity = new WindowsIdentity(ds.CrackName(userName));
                        sid = Identity.User;
                    }
                }
                catch { }
            }

            if (Identity == null)
            {
                if (userName != null && userName.Contains("@"))
                {
                    Identity = new WindowsIdentity(userName);
                    sid = Identity.User;
                }

                if (Identity == null && userName != null)
                {
                    var ntacct = new NTAccount(userName);
                    try { sid = (SecurityIdentifier)ntacct.Translate(typeof(SecurityIdentifier)); } catch { }
                }
            }

            string GetUser(string domUser)
            {
                var split = domUser.Split('\\');
                return split.Length == 2 ? split[1] : domUser;
            }
        }

        /// <summary>Initializes a new instance of the <see cref="User"/> class.</summary>
        /// <param name="wid">The <see cref="WindowsIdentity"/>.</param>
        internal User(WindowsIdentity wid) { Identity = wid; sid = wid.User; }

        /// <summary>Gets the current user.</summary>
        /// <value>The current user.</value>
        public static User Current => new User(cur);

        /// <summary>Gets the identity.</summary>
        /// <value>The identity.</value>
        public WindowsIdentity Identity { get; private set; }

        /// <summary>Gets a value indicating whether this instance is in an administrator role.</summary>
        /// <value><c>true</c> if this instance is an admin; otherwise, <c>false</c>.</value>
        public bool IsAdmin => Identity != null ? new WindowsPrincipal(Identity).IsInRole(WindowsBuiltInRole.Administrator) : false;

        /// <summary>Gets a value indicating whether this instance is the interactive user.</summary>
        /// <value><c>true</c> if this instance is the current user; otherwise, <c>false</c>.</value>
        public bool IsCurrent => Identity?.User.Equals(cur.User) ?? false;

        /// <summary>Gets a value indicating whether this instance is a service account.</summary>
        /// <value><c>true</c> if this instance is a service account; otherwise, <c>false</c>.</value>
        public bool IsServiceAccount
        {
            get
            {
                try
                {
                    return (sid != null && (sid.IsWellKnown(WellKnownSidType.LocalSystemSid) || sid.IsWellKnown(WellKnownSidType.NetworkServiceSid) || sid.IsWellKnown(WellKnownSidType.LocalServiceSid)));
                }
                catch { }
                return false;
            }
        }

        /// <summary>Gets a value indicating whether this instance is the SYSTEM account.</summary>
        /// <value><c>true</c> if this instance is the SYSTEM account; otherwise, <c>false</c>.</value>
        public bool IsSystem => sid != null && sid.IsWellKnown(WellKnownSidType.LocalSystemSid);

        /// <summary>Gets the SID string.</summary>
        /// <value>The SID string.</value>
        public string SidString => sid?.ToString();

        /// <summary>Gets the NT name (DOMAIN\username).</summary>
        /// <value>The name of the user.</value>
        public string Name => Identity?.Name ?? ((NTAccount)sid?.Translate(typeof(NTAccount)))?.Value;

        /// <summary>Create a <see cref="User"/> instance from a SID string.</summary>
        /// <param name="sid">The SID string.</param>
        /// <returns>A <see cref="User"/> instance.</returns>
        public static User FromSidString(string sid) => new User(((NTAccount)new SecurityIdentifier(sid).Translate(typeof(NTAccount))).Value);

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose() => Identity?.Dispose();

        /// <summary>Determines whether the specified <see cref="System.Object"/>, is equal to this instance.</summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            if (obj is User user)
                return Equals(user);
            if (obj is WindowsIdentity wid && sid != null)
                return sid.Equals(wid.User);
            try
            {
                if (obj is string un)
                    return Equals(new User(un));
            }
            catch { }
            return base.Equals(obj);
        }

        /// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.</returns>
        public bool Equals(User other) => (other != null && sid != null) ? sid.Equals(other.sid) : false;

        /// <summary>Returns a hash code for this instance.</summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode() => sid?.GetHashCode() ?? 0;
    }
}
