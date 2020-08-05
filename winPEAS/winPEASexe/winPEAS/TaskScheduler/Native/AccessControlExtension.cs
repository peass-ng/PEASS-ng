using System.Linq;

namespace System.Security.AccessControl
{
	/// <summary>Extensions for classes in the System.Security.AccessControl namespace.</summary>
	public static class AccessControlExtension
	{
		/// <summary>Canonicalizes the specified Access Control List.</summary>
		/// <param name="acl">The Access Control List.</param>
		public static void Canonicalize(this RawAcl acl)
		{
			if (acl == null) throw new ArgumentNullException(nameof(acl));

			// Extract aces to list
			var aces = new Collections.Generic.List<GenericAce>(acl.Cast<GenericAce>());

			// Sort aces based on canonical order
			aces.Sort((a, b) => Collections.Generic.Comparer<byte>.Default.Compare(GetComparisonValue(a), GetComparisonValue(b)));

			// Add sorted aces back to ACL
			while (acl.Count > 0) acl.RemoveAce(0);
			var aceIndex = 0;
			aces.ForEach(ace => acl.InsertAce(aceIndex++, ace));
		}

		/// <summary>Sort ACEs according to canonical form for this <see cref="ObjectSecurity"/>.</summary>
		/// <param name="objectSecurity">The object security whose DiscretionaryAcl will be made canonical.</param>
		public static void CanonicalizeAccessRules(this ObjectSecurity objectSecurity)
		{
			if (objectSecurity == null) throw new ArgumentNullException(nameof(objectSecurity));
			if (objectSecurity.AreAccessRulesCanonical) return;

			// Get raw SD from objectSecurity and canonicalize DACL
			var sd = new RawSecurityDescriptor(objectSecurity.GetSecurityDescriptorBinaryForm(), 0);
			sd.DiscretionaryAcl.Canonicalize();

			// Convert SD back into objectSecurity
			objectSecurity.SetSecurityDescriptorBinaryForm(sd.GetBinaryForm());
		}

		/// <summary>Returns an array of byte values that represents the information contained in this <see cref="GenericSecurityDescriptor"/> object.</summary>
		/// <param name="sd">The <see cref="GenericSecurityDescriptor"/> object.</param>
		/// <returns>The byte array into which the contents of the <see cref="GenericSecurityDescriptor"/> is marshaled.</returns>
		public static byte[] GetBinaryForm(this GenericSecurityDescriptor sd)
		{
			if (sd == null) throw new ArgumentNullException(nameof(sd));
			var bin = new byte[sd.BinaryLength];
			sd.GetBinaryForm(bin, 0);
			return bin;
		}

		// A canonical ACL must have ACES sorted according to the following order:
		// 1. Access-denied on the object
		// 2. Access-denied on a child or property
		// 3. Access-allowed on the object
		// 4. Access-allowed on a child or property
		// 5. All inherited ACEs
		private static byte GetComparisonValue(GenericAce ace)
		{
			if ((ace.AceFlags & AceFlags.Inherited) != 0)
				return 5;
			switch (ace.AceType)
			{
				case AceType.AccessDenied:
				case AceType.AccessDeniedCallback:
				case AceType.SystemAudit:
				case AceType.SystemAlarm:
				case AceType.SystemAuditCallback:
				case AceType.SystemAlarmCallback:
					return 0;
				case AceType.AccessDeniedObject:
				case AceType.AccessDeniedCallbackObject:
				case AceType.SystemAuditObject:
				case AceType.SystemAlarmObject:
				case AceType.SystemAuditCallbackObject:
				case AceType.SystemAlarmCallbackObject:
					return 1;
				case AceType.AccessAllowed:
				case AceType.AccessAllowedCallback:
					return 2;
				case AceType.AccessAllowedObject:
				case AceType.AccessAllowedCallbackObject:
					return 3;
				default:
					return 4;
			}
		}
	}
}