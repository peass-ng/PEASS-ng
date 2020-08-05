using System;
using System.Runtime;
using System.Security;
using System.Security.AccessControl;
using System.Security.Principal;
using JetBrains.Annotations;

namespace Microsoft.Win32.TaskScheduler
{
	/// <summary>
	/// Specifies the access control rights that can be applied to Task Scheduler tasks.
	/// </summary>
	[Flags]
	public enum TaskRights
	{
		/// <summary>Specifies the right to exert full control over a task folder or task, and to modify access control and audit rules. This value represents the right to do anything with a task and is the combination of all rights in this enumeration.</summary>
		FullControl = 0x1f01ff,
		/// <summary>Specifies the right to create tasks and folders, and to add or remove data from tasks. This right includes the following rights: .</summary>
		Write = 0x120116,
		/// <summary>Specifies the right to open and copy folders or tasks as read-only. This right includes the following rights: .</summary>
		Read = 0x120089,
		/// <summary>Specifies the right run tasks. This right includes the following rights: .</summary>
		Execute = 0x120089,
		/// <summary>The right to wait on a task.</summary>
		Synchronize = 0x100000,
		/// <summary>The right to change the owner of a task.</summary>
		TakeOwnership = 0x80000,
		/// <summary>Specifies the right to change the security and audit rules associated with a task or folder.</summary>
		ChangePermissions = 0x40000,
		/// <summary>The right to open and copy the access rules and audit rules for a task.</summary>
		ReadPermissions = 0x20000,
		/// <summary>The right to delete a folder or task.</summary>
		Delete = 0x10000,
		/// <summary>Specifies the right to open and write file system attributes to a folder or file. This does not include the ability to write data, extended attributes, or access and audit rules.</summary>
		WriteAttributes = 0x100,
		/// <summary>Specifies the right to open and copy file system attributes from a folder or task. For example, this value specifies the right to view the file creation or modified date. This does not include the right to read data, extended file system attributes, or access and audit rules.</summary>
		ReadAttributes = 0x80,
		/// <summary>Specifies the right to delete a folder and any tasks contained within that folder.</summary>
		DeleteChild = 0x40,
		/// <summary>Specifies the right to run a task.</summary>
		ExecuteFile = 0x20,
		/// <summary>Specifies the right to open and write extended file system attributes to a folder or file. This does not include the ability to write data, attributes, or access and audit rules.</summary>
		WriteExtendedAttributes = 0x10,
		/// <summary>Specifies the right to open and copy extended system attributes from a folder or task. For example, this value specifies the right to view author and content information. This does not include the right to read data, system attributes, or access and audit rules.</summary>
		ReadExtendedAttributes = 8,
		/// <summary>Specifies the right to append data to the end of a file.</summary>
		AppendData = 4,
		/// <summary>Specifies the right to open and write to a file or folder. This does not include the right to open and write file system attributes, extended file system attributes, or access and audit rules.</summary>
		WriteData = 2,
		/// <summary>Specifies the right to open and copy a task or folder. This does not include the right to read file system attributes, extended file system attributes, or access and audit rules.</summary>
		ReadData = 1,
	}

	/// <summary>
	/// Represents a set of access rights allowed or denied for a user or group. This class cannot be inherited.
	/// </summary>
	public sealed class TaskAccessRule : AccessRule
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TaskAccessRule"/> class, specifying the user or group the rule applies to, the access rights, and whether the specified access rights are allowed or denied.
		/// </summary>
		/// <param name="identity">The user or group the rule applies to. Must be of type <see cref="SecurityIdentifier"/> or a type such as <see cref="NTAccount"/> that can be converted to type <see cref="SecurityIdentifier"/>.</param>
		/// <param name="eventRights">A bitwise combination of <see cref="TaskRights"/> values specifying the rights allowed or denied.</param>
		/// <param name="type">One of the <see cref="AccessControlType"/> values specifying whether the rights are allowed or denied.</param>
		public TaskAccessRule([NotNull] IdentityReference identity, TaskRights eventRights, AccessControlType type)
			: this(identity, (int)eventRights, false, InheritanceFlags.None, PropagationFlags.None, type)
		{
		}

		private TaskAccessRule([NotNull] IdentityReference identity, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type)
			: base(identity, accessMask, isInherited, inheritanceFlags, propagationFlags, type)
		{
		}

		/// <summary>
		/// Gets the rights allowed or denied by the access rule.
		/// </summary>
		/// <value>
		/// A bitwise combination of <see cref="TaskRights"/> values indicating the rights allowed or denied by the access rule.
		/// </value>
		public TaskRights TaskRights => (TaskRights)AccessMask;
	}

	/// <summary>
	/// Represents a set of access rights to be audited for a user or group. This class cannot be inherited.
	/// </summary>
	public sealed class TaskAuditRule : AuditRule
	{
		internal TaskAuditRule([NotNull] IdentityReference identity, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags)
			: base(identity, accessMask, isInherited, inheritanceFlags, propagationFlags, flags)
		{
		}

		/// <summary>
		/// Gets the access rights affected by the audit rule.
		/// </summary>
		/// <value>
		/// A bitwise combination of <see cref="TaskRights"/> values that indicates the rights affected by the audit rule.
		/// </value>
		/// <remarks><see cref="TaskAuditRule"/> objects are immutable. You can create a new audit rule representing a different user, different rights, or a different combination of AuditFlags values, but you cannot modify an existing audit rule.</remarks>
		public TaskRights TaskRights => (TaskRights)AccessMask;
	}

	/// <summary>
	/// Represents the Windows access control security for a Task Scheduler task. This class cannot be inherited.
	/// </summary>
	/// <remarks>
	/// <para>A TaskSecurity object specifies access rights for a Task Scheduler task, and also specifies how access attempts are audited. Access rights to the task are expressed as rules, with each access rule represented by a <see cref="TaskAccessRule"/> object. Each auditing rule is represented by a <see cref="TaskAuditRule"/> object.</para>
	/// <para>This mirrors the underlying Windows security system, in which each securable object has at most one discretionary access control list (DACL) that controls access to the secured object, and at most one system access control list (SACL) that specifies which access attempts are audited. The DACL and SACL are ordered lists of access control entries (ACE) that specify access and auditing for users and groups. A <see cref="TaskAccessRule"/> or <see cref="TaskAuditRule"/> object might represent more than one ACE.</para>
	/// <para>Note</para>
	/// <para>A <see cref="Task"/> object can represent a local task or a Task Scheduler task. Windows access control security is meaningful only for Task Scheduler tasks.</para>
	/// <para>The TaskSecurity, <see cref="TaskAccessRule"/>, and <see cref="TaskAuditRule"/> classes hide the implementation details of ACLs and ACEs. They allow you to ignore the seventeen different ACE types and the complexity of correctly maintaining inheritance and propagation of access rights. These objects are also designed to prevent the following common access control errors:</para>
	/// <list type="bullet">
	/// <item><description>Creating a security descriptor with a null DACL. A null reference to a DACL allows any user to add access rules to an object, potentially creating a denial-of-service attack. A new TaskSecurity object always starts with an empty DACL, which denies all access for all users.</description></item>
	/// <item><description>Violating the canonical ordering of ACEs. If the ACE list in the DACL is not kept in the canonical order, users might inadvertently be given access to the secured object. For example, denied access rights must always appear before allowed access rights. TaskSecurity objects maintain the correct order internally. </description></item>
	/// <item><description>Manipulating security descriptor flags, which should be under resource manager control only.</description></item>
	/// <item><description>Creating invalid combinations of ACE flags.</description></item>
	/// <item><description>Manipulating inherited ACEs. Inheritance and propagation are handled by the resource manager, in response to changes you make to access and audit rules.</description></item>
	/// <item><description>Inserting meaningless ACEs into ACLs.</description></item>
	/// </list>
	/// <para>The only capabilities not supported by the .NET security objects are dangerous activities that should be avoided by the majority of application developers, such as the following:</para>
	/// <list type="bullet">
	/// <item><description>Low-level tasks that are normally performed by the resource manager.</description></item>
	/// <item><description>Adding or removing access control entries in ways that do not maintain the canonical ordering.</description></item>
	/// </list>
	/// <para>To modify Windows access control security for a task, use the <see cref="Task.GetAccessControl()"/> method to get the TaskSecurity object. Modify the security object by adding and removing rules, and then use the <see cref="Task.SetAccessControl"/> method to reattach it. </para>
	/// <para>Important: Changes you make to a TaskSecurity object do not affect the access levels of the task until you call the <see cref="Task.SetAccessControl"/> method to assign the altered security object to the task.</para>
	/// <para>To copy access control security from one task to another, use the <see cref="Task.GetAccessControl()"/> method to get a TaskSecurity object representing the access and audit rules for the first task, then use the <see cref="Task.SetAccessControl"/> method, or a constructor that accepts a TaskSecurity object, to assign those rules to the second task.</para>
	/// <para>Users with an investment in the security descriptor definition language (SDDL) can use the <see cref="Task.SetSecurityDescriptorSddlForm"/> method to set access rules for a task, and the <see cref="Task.GetSecurityDescriptorSddlForm"/> method to obtain a string that represents the access rules in SDDL format. This is not recommended for new development.</para>
	/// </remarks>
	public sealed class TaskSecurity : CommonObjectSecurity
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TaskSecurity"/> class with default values.
		/// </summary>
		public TaskSecurity()
			: base(false)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TaskSecurity" /> class with the specified sections of the access control security rules from the specified task.
		/// </summary>
		/// <param name="task">The task.</param>
		/// <param name="sections">The sections of the ACL to retrieve.</param>
		public TaskSecurity([NotNull] Task task, AccessControlSections sections = Task.defaultAccessControlSections)
			: base(false)
		{
			SetSecurityDescriptorSddlForm(task.GetSecurityDescriptorSddlForm(Convert(sections)), sections);
			this.CanonicalizeAccessRules();
		}



		/// <summary>
		/// Gets the enumeration that the <see cref="TaskSecurity"/> class uses to represent access rights.
		/// </summary>
		/// <returns>A <see cref="Type"/> object representing the <see cref="TaskRights"/> enumeration.</returns>
		public override Type AccessRightType => typeof(TaskRights);

		/// <summary>
		/// Gets the type that the TaskSecurity class uses to represent access rules.
		/// </summary>
		/// <returns>A <see cref="Type"/> object representing the <see cref="TaskAccessRule"/> class.</returns>
		public override Type AccessRuleType => typeof(TaskAccessRule);

		/// <summary>
		/// Gets the type that the TaskSecurity class uses to represent audit rules.
		/// </summary>
		/// <returns>A <see cref="Type"/> object representing the <see cref="TaskAuditRule"/> class.</returns>
		public override Type AuditRuleType => typeof(TaskAuditRule);

        /// <summary>
		/// Creates a new access control rule for the specified user, with the specified access rights, access control, and flags.
		/// </summary>
		/// <param name="identityReference">An <see cref="IdentityReference"/> that identifies the user or group the rule applies to.</param>
		/// <param name="accessMask">A bitwise combination of <see cref="TaskRights"/> values specifying the access rights to allow or deny, cast to an integer.</param>
		/// <param name="isInherited">Meaningless for tasks, because they have no hierarchy.</param>
		/// <param name="inheritanceFlags">Meaningless for tasks, because they have no hierarchy.</param>
		/// <param name="propagationFlags">Meaningless for tasks, because they have no hierarchy.</param>
		/// <param name="type">One of the <see cref="AccessControlType"/> values specifying whether the rights are allowed or denied.</param>
		/// <returns>
		/// The <see cref="T:System.Security.AccessControl.AccessRule" /> object that this method creates.
		/// </returns>
		public override AccessRule AccessRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type) => new TaskAccessRule(identityReference, (TaskRights)accessMask, type);

		/// <summary>
		/// Creates a new audit rule, specifying the user the rule applies to, the access rights to audit, and the outcome that triggers the audit rule.
		/// </summary>
		/// <param name="identityReference">An <see cref="IdentityReference"/> that identifies the user or group the rule applies to.</param>
		/// <param name="accessMask">A bitwise combination of <see cref="TaskRights"/> values specifying the access rights to audit, cast to an integer.</param>
		/// <param name="isInherited">Meaningless for tasks, because they have no hierarchy.</param>
		/// <param name="inheritanceFlags">Meaningless for tasks, because they have no hierarchy.</param>
		/// <param name="propagationFlags">Meaningless for tasks, because they have no hierarchy.</param>
		/// <param name="flags">One of the <see cref="AuditFlags"/> values specifying whether to audit successful access, failed access, or both.</param>
		/// <returns>
		/// A <see cref="TaskAuditRule"/> object representing the specified audit rule for the specified user. The return type of the method is the base class, <see cref="AuditRule"/>, but the return value can be cast safely to the derived class.
		/// </returns>
		public override AuditRule AuditRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags) => new TaskAuditRule(identityReference, accessMask, isInherited, inheritanceFlags, propagationFlags, flags);

		/// <summary>
		/// Returns a <see cref="System.String" /> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String" /> that represents this instance.
		/// </returns>
		public override string ToString() => GetSecurityDescriptorSddlForm(Task.defaultAccessControlSections);

		private static SecurityInfos Convert(AccessControlSections si)
		{
			SecurityInfos ret = 0;
			if ((si & AccessControlSections.Audit) != 0)
				ret |= SecurityInfos.SystemAcl;
			if ((si & AccessControlSections.Access) != 0)
				ret |= SecurityInfos.DiscretionaryAcl;
			if ((si & AccessControlSections.Group) != 0)
				ret |= SecurityInfos.Group;
			if ((si & AccessControlSections.Owner) != 0)
				ret |= SecurityInfos.Owner;
			return ret;
		}

		private AccessControlSections GetAccessControlSectionsFromChanges()
		{
			AccessControlSections none = AccessControlSections.None;
			if (AccessRulesModified)
			{
				none = AccessControlSections.Access;
			}
			if (AuditRulesModified)
			{
				none |= AccessControlSections.Audit;
			}
			if (OwnerModified)
			{
				none |= AccessControlSections.Owner;
			}
			if (GroupModified)
			{
				none |= AccessControlSections.Group;
			}
			return none;
		}

		/// <summary>
		/// Saves the specified sections of the security descriptor associated with this <see cref="TaskSecurity"/> object to permanent storage. We recommend that the values of the <paramref name="includeSections"/> parameters passed to the constructor and persist methods be identical.
		/// </summary>
		/// <param name="task">The task used to retrieve the persisted information.</param>
		/// <param name="includeSections">One of the <see cref="AccessControlSections"/> enumeration values that specifies the sections of the security descriptor (access rules, audit rules, owner, primary group) of the securable object to save.</param>
		[SecurityCritical]
		internal void Persist([NotNull] Task task, AccessControlSections includeSections = Task.defaultAccessControlSections)
		{
			WriteLock();
			try
			{
				AccessControlSections accessControlSectionsFromChanges = GetAccessControlSectionsFromChanges();
				if (accessControlSectionsFromChanges != AccessControlSections.None)
				{
					task.SetSecurityDescriptorSddlForm(GetSecurityDescriptorSddlForm(accessControlSectionsFromChanges));
					OwnerModified = GroupModified = AccessRulesModified = AuditRulesModified = false;
				}
			}
			finally
			{
				WriteUnlock();
			}
		}
    }
}