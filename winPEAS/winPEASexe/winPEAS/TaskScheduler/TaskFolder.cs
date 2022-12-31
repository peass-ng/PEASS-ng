using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.AccessControl;
using System.Text.RegularExpressions;
using winPEAS.TaskScheduler.V1;
using winPEAS.TaskScheduler.V2;

namespace winPEAS.TaskScheduler
{
    /// <summary>
    /// Provides the methods that are used to register (create) tasks in the folder, remove tasks from the folder, and create or remove subfolders from the folder.
    /// </summary>
    [PublicAPI]
    public sealed class TaskFolder : IDisposable, IComparable<TaskFolder>
    {
        private ITaskScheduler v1List;
        private readonly ITaskFolder v2Folder;

        internal const string rootString = @"\";

        internal TaskFolder([NotNull] TaskService svc)
        {
            TaskService = svc;
            v1List = svc.v1TaskScheduler;
        }

        internal TaskFolder([NotNull] TaskService svc, [NotNull] ITaskFolder iFldr)
        {
            TaskService = svc;
            v2Folder = iFldr;
        }

        /// <summary>
        /// Releases all resources used by this class.
        /// </summary>
        public void Dispose()
        {
            if (v2Folder != null)
                Marshal.ReleaseComObject(v2Folder);
            v1List = null;
        }

        /// <summary>
        /// Gets a <see cref="System.Collections.Generic.IEnumerator{Task}"/> which enumerates all the tasks in this and all subfolders.
        /// </summary>
        /// <value>
        /// A <see cref="System.Collections.Generic.IEnumerator{Task}"/> for all <see cref="Task"/> instances.
        /// </value>
        [NotNull, ItemNotNull]
        public IEnumerable<Task> AllTasks => EnumerateFolderTasks(this);

        /// <summary>
        /// Gets the name that is used to identify the folder that contains a task.
        /// </summary>
        [NotNull]
        public string Name => (v2Folder == null) ? rootString : v2Folder.Name;

        /// <summary>
        /// Gets the parent folder of this folder.
        /// </summary>
        /// <value>
        /// The parent folder, or <c>null</c> if this folder is the root folder.
        /// </value>
        public TaskFolder Parent
        {
            get
            {
                // V1 only has the root folder
                if (v2Folder == null)
                    return null;

                string path = v2Folder.Path;
                string parentPath = System.IO.Path.GetDirectoryName(path);
                if (string.IsNullOrEmpty(parentPath))
                    return null;
                return TaskService.GetFolder(parentPath);
            }
        }

        /// <summary>
        /// Gets the path to where the folder is stored.
        /// </summary>
        [NotNull]
        public string Path => (v2Folder == null) ? rootString : v2Folder.Path;

        [NotNull]
        internal TaskFolder GetFolder([NotNull] string path)
        {
            if (v2Folder != null)
                return new TaskFolder(TaskService, v2Folder.GetFolder(path));
            throw new NotV1SupportedException();
        }

        /// <summary>
        /// Gets or sets the security descriptor of the task.
        /// </summary>
        /// <value>The security descriptor.</value>
        [Obsolete("This property will be removed in deference to the GetAccessControl, GetSecurityDescriptorSddlForm, SetAccessControl and SetSecurityDescriptorSddlForm methods.")]
        public GenericSecurityDescriptor SecurityDescriptor
        {
#pragma warning disable 0618
            get { return GetSecurityDescriptor(); }
            set { SetSecurityDescriptor(value); }
#pragma warning restore 0618
        }

        /// <summary>
        /// Gets all the subfolders in the folder.
        /// </summary>
        [NotNull, ItemNotNull]
        public TaskFolderCollection SubFolders
        {
            get
            {
                try
                {
                    if (v2Folder != null)
                        return new TaskFolderCollection(this, v2Folder.GetFolders(0));
                }
                catch { }
                return new TaskFolderCollection();
            }
        }

        /// <summary>
        /// Gets a collection of all the tasks in the folder.
        /// </summary>
        [NotNull, ItemNotNull]
        public TaskCollection Tasks => GetTasks();

        /// <summary>
        /// Gets or sets the <see cref="TaskService"/> that manages this task.
        /// </summary>
        /// <value>The task service.</value>
        public TaskService TaskService { get; }

        /// <summary>
        /// Compares the current object with another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// A value that indicates the relative order of the objects being compared. The return value has the following meanings: Value Meaning Less than zero This object is less than the <paramref name="other" /> parameter.Zero This object is equal to <paramref name="other" />. Greater than zero This object is greater than <paramref name="other" />.
        /// </returns>
        int IComparable<TaskFolder>.CompareTo(TaskFolder other) => string.Compare(Path, other.Path, true);

        /// <summary>
        /// Creates a folder for related tasks. Not available to Task Scheduler 1.0.
        /// </summary>
        /// <param name="subFolderName">The name used to identify the folder. If "FolderName\SubFolder1\SubFolder2" is specified, the entire folder tree will be created if the folders do not exist. This parameter can be a relative path to the current <see cref="TaskFolder"/> instance. The root task folder is specified with a backslash (\). An example of a task folder path, under the root task folder, is \MyTaskFolder. The '.' character cannot be used to specify the current task folder and the '..' characters cannot be used to specify the parent task folder in the path.</param>
        /// <param name="sd">The security descriptor associated with the folder.</param>
        /// <returns>A <see cref="TaskFolder"/> instance that represents the new subfolder.</returns>
        [Obsolete("This method will be removed in deference to the CreateFolder(string, TaskSecurity) method.")]
        public TaskFolder CreateFolder([NotNull] string subFolderName, GenericSecurityDescriptor sd) => CreateFolder(subFolderName, sd == null ? null : sd.GetSddlForm(Task.defaultAccessControlSections));

        /// <summary>
        /// Creates a folder for related tasks. Not available to Task Scheduler 1.0.
        /// </summary>
        /// <param name="subFolderName">The name used to identify the folder. If "FolderName\SubFolder1\SubFolder2" is specified, the entire folder tree will be created if the folders do not exist. This parameter can be a relative path to the current <see cref="TaskFolder"/> instance. The root task folder is specified with a backslash (\). An example of a task folder path, under the root task folder, is \MyTaskFolder. The '.' character cannot be used to specify the current task folder and the '..' characters cannot be used to specify the parent task folder in the path.</param>
        /// <param name="folderSecurity">The task security associated with the folder.</param>
        /// <returns>A <see cref="TaskFolder"/> instance that represents the new subfolder.</returns>
        public TaskFolder CreateFolder([NotNull] string subFolderName, [NotNull] TaskSecurity folderSecurity)
        {
            if (folderSecurity == null)
                throw new ArgumentNullException(nameof(folderSecurity));
            return CreateFolder(subFolderName, folderSecurity.GetSecurityDescriptorSddlForm(Task.defaultAccessControlSections));
        }

        /// <summary>
        /// Creates a folder for related tasks. Not available to Task Scheduler 1.0.
        /// </summary>
        /// <param name="subFolderName">The name used to identify the folder. If "FolderName\SubFolder1\SubFolder2" is specified, the entire folder tree will be created if the folders do not exist. This parameter can be a relative path to the current <see cref="TaskFolder" /> instance. The root task folder is specified with a backslash (\). An example of a task folder path, under the root task folder, is \MyTaskFolder. The '.' character cannot be used to specify the current task folder and the '..' characters cannot be used to specify the parent task folder in the path.</param>
        /// <param name="sddlForm">The security descriptor associated with the folder.</param>
        /// <param name="exceptionOnExists">Set this value to false to avoid having an exception called if the folder already exists.</param>
        /// <returns>A <see cref="TaskFolder" /> instance that represents the new subfolder.</returns>
        /// <exception cref="System.Security.SecurityException">Security descriptor mismatch between specified credentials and credentials on existing folder by same name.</exception>
        /// <exception cref="System.ArgumentException">Invalid SDDL form.</exception>
        /// <exception cref="Microsoft.Win32.TaskScheduler.NotV1SupportedException">Not supported under Task Scheduler 1.0.</exception>
        public TaskFolder CreateFolder([NotNull] string subFolderName, string sddlForm = null, bool exceptionOnExists = true)
        {
            if (v2Folder == null) throw new NotV1SupportedException();
            ITaskFolder ifld = null;
            try { ifld = v2Folder.CreateFolder(subFolderName, sddlForm); }
            catch (COMException ce)
            {
                int serr = ce.ErrorCode & 0x0000FFFF;
                if (serr == 0xb7) // ERROR_ALREADY_EXISTS
                {
                    if (exceptionOnExists) throw;
                    try
                    {
                        ifld = v2Folder.GetFolder(subFolderName);
                        if (ifld != null && sddlForm != null && sddlForm.Trim().Length > 0)
                        {
                            string sd = ifld.GetSecurityDescriptor((int)Task.defaultSecurityInfosSections);
                            if (string.Compare(sddlForm, sd, StringComparison.OrdinalIgnoreCase) != 0)
                                throw new SecurityException("Security descriptor mismatch between specified credentials and credentials on existing folder by same name.");
                        }
                    }
                    catch
                    {
                        if (ifld != null)
                            Marshal.ReleaseComObject(ifld);
                        throw;
                    }
                }
                else if (serr == 0x534 || serr == 0x538 || serr == 0x539 || serr == 0x53A || serr == 0x519 || serr == 0x57)
                    throw new ArgumentException(@"Invalid SDDL form", nameof(sddlForm), ce);
                else
                    throw;
            }
            return new TaskFolder(TaskService, ifld);
        }

        /// <summary>
        /// Deletes a subfolder from the parent folder. Not available to Task Scheduler 1.0.
        /// </summary>
        /// <param name="subFolderName">The name of the subfolder to be removed. The root task folder is specified with a backslash (\). This parameter can be a relative path to the folder you want to delete. An example of a task folder path, under the root task folder, is \MyTaskFolder. The '.' character cannot be used to specify the current task folder and the '..' characters cannot be used to specify the parent task folder in the path.</param>
        /// <param name="exceptionOnNotExists">Set this value to false to avoid having an exception called if the folder does not exist.</param>
        /// <exception cref="Microsoft.Win32.TaskScheduler.NotV1SupportedException">Not supported under Task Scheduler 1.0.</exception>
        public void DeleteFolder([NotNull] string subFolderName, bool exceptionOnNotExists = true)
        {
            if (v2Folder != null)
            {
                try
                {
                    v2Folder.DeleteFolder(subFolderName, 0);
                }
                catch (Exception e)
                {
                    if (!(e is FileNotFoundException || e is DirectoryNotFoundException) || exceptionOnNotExists)
                        throw;
                }
            }
            else
                throw new NotV1SupportedException();
        }

        /// <summary>Deletes a task from the folder.</summary>
        /// <param name="name">
        /// The name of the task that is specified when the task was registered. The '.' character cannot be used to specify the current task folder and the '..'
        /// characters cannot be used to specify the parent task folder in the path.
        /// </param>
        /// <param name="exceptionOnNotExists">Set this value to false to avoid having an exception called if the task does not exist.</param>
        public void DeleteTask([NotNull] string name, bool exceptionOnNotExists = true)
        {
            try
            {
                if (v2Folder != null)
                    v2Folder.DeleteTask(name, 0);
                else
                {
                    if (!name.EndsWith(".job", StringComparison.CurrentCultureIgnoreCase))
                        name += ".job";
                    v1List.Delete(name);
                }
            }
            catch (FileNotFoundException)
            {
                if (exceptionOnNotExists)
                    throw;
            }
        }

        /// <summary>Returns an enumerable collection of folders that matches a specified filter and recursion option.</summary>
        /// <param name="filter">An optional predicate used to filter the returned <see cref="TaskFolder"/> instances.</param>
        /// <returns>An enumerable collection of folders that matches <paramref name="filter"/>.</returns>
        [NotNull, ItemNotNull]
        public IEnumerable<TaskFolder> EnumerateFolders(Predicate<TaskFolder> filter = null)
        {
            foreach (var fld in SubFolders)
            {
                if (filter == null || filter(fld))
                    yield return fld;
            }
        }

        /// <summary>Returns an enumerable collection of tasks that matches a specified filter and recursion option.</summary>
        /// <param name="filter">An optional predicate used to filter the returned <see cref="Task"/> instances.</param>
        /// <param name="recurse">Specifies whether the enumeration should include tasks in any subfolders.</param>
        /// <returns>An enumerable collection of directories that matches <paramref name="filter"/> and <paramref name="recurse"/>.</returns>
        [NotNull, ItemNotNull]
        public IEnumerable<Task> EnumerateTasks(Predicate<Task> filter = null, bool recurse = false) => EnumerateFolderTasks(this, filter, recurse);

        /// <summary>Determines whether the specified <see cref="System.Object"/>, is equal to this instance.</summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            var folder = obj as TaskFolder;
            if (folder != null)
                return Path == folder.Path && TaskService.TargetServer == folder.TaskService.TargetServer && GetSecurityDescriptorSddlForm() == folder.GetSecurityDescriptorSddlForm();
            return false;
        }

        /// <summary>
        /// Gets a <see cref="TaskSecurity"/> object that encapsulates the specified type of access control list (ACL) entries for the task described by the
        /// current <see cref="TaskFolder"/> object.
        /// </summary>
        /// <returns>A <see cref="TaskSecurity"/> object that encapsulates the access control rules for the current folder.</returns>
        [NotNull]
        public TaskSecurity GetAccessControl() => GetAccessControl(Task.defaultAccessControlSections);

        /// <summary>
        /// Gets a <see cref="TaskSecurity"/> object that encapsulates the specified type of access control list (ACL) entries for the task folder described by
        /// the current <see cref="TaskFolder"/> object.
        /// </summary>
        /// <param name="includeSections">
        /// One of the <see cref="System.Security.AccessControl.AccessControlSections"/> values that specifies which group of access control entries to retrieve.
        /// </param>
        /// <returns>A <see cref="TaskSecurity"/> object that encapsulates the access control rules for the current folder.</returns>
        [NotNull]
        public TaskSecurity GetAccessControl(AccessControlSections includeSections) => new TaskSecurity(this, includeSections);

        /// <summary>Returns a hash code for this instance.</summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode() => new { A = Path, B = TaskService.TargetServer, C = GetSecurityDescriptorSddlForm() }.GetHashCode();

        /// <summary>Gets the security descriptor for the folder. Not available to Task Scheduler 1.0.</summary>
        /// <param name="includeSections">Section(s) of the security descriptor to return.</param>
        /// <returns>The security descriptor for the folder.</returns>
        [Obsolete("This method will be removed in deference to the GetAccessControl and GetSecurityDescriptorSddlForm methods.")]
        public GenericSecurityDescriptor GetSecurityDescriptor(SecurityInfos includeSections = Task.defaultSecurityInfosSections) => new RawSecurityDescriptor(GetSecurityDescriptorSddlForm(includeSections));

        /// <summary>
        /// Gets the security descriptor for the folder. Not available to Task Scheduler 1.0.
        /// </summary>
        /// <param name="includeSections">Section(s) of the security descriptor to return.</param>
        /// <returns>The security descriptor for the folder.</returns>
        /// <exception cref="NotV1SupportedException">Not supported under Task Scheduler 1.0.</exception>
        public string GetSecurityDescriptorSddlForm(SecurityInfos includeSections = Task.defaultSecurityInfosSections)
        {
            if (v2Folder != null)
                return v2Folder.GetSecurityDescriptor((int)includeSections);
            throw new NotV1SupportedException();
        }

        /// <summary>
        /// Gets a collection of all the tasks in the folder whose name matches the optional <paramref name="filter"/>.
        /// </summary>
        /// <param name="filter">The optional name filter expression.</param>
        /// <returns>Collection of all matching tasks.</returns>
        [NotNull, ItemNotNull]
        public TaskCollection GetTasks(Regex filter = null)
        {
            if (v2Folder != null)
                return new TaskCollection(this, v2Folder.GetTasks(1), filter);
            return new TaskCollection(TaskService, filter);
        }

        /// <summary>Imports a <see cref="Task" /> from an XML file.</summary>
        /// <param name="path">The task name. If this value is NULL, the task will be registered in the root task folder and the task name will be a GUID value that is created by the Task Scheduler service. A task name cannot begin or end with a space character. The '.' character cannot be used to specify the current task folder and the '..' characters cannot be used to specify the parent task folder in the path.</param>
        /// <param name="xmlFile">The file containing the XML-formatted definition of the task.</param>
        /// <param name="overwriteExisting">If set to <see langword="true" />, overwrites any existing task with the same name.</param>
        /// <returns>A <see cref="Task" /> instance that represents the new task.</returns>
        /// <exception cref="NotV1SupportedException">Importing from an XML file is only supported under Task Scheduler 2.0.</exception>
        public Task ImportTask(string path, [NotNull] string xmlFile, bool overwriteExisting = true) => RegisterTask(path, File.ReadAllText(xmlFile), overwriteExisting ? TaskCreation.CreateOrUpdate : TaskCreation.Create);

        /// <summary>
        /// Registers (creates) a new task in the folder using XML to define the task.
        /// </summary>
        /// <param name="path">The task name. If this value is NULL, the task will be registered in the root task folder and the task name will be a GUID value that is created by the Task Scheduler service. A task name cannot begin or end with a space character. The '.' character cannot be used to specify the current task folder and the '..' characters cannot be used to specify the parent task folder in the path.</param>
        /// <param name="xmlText">An XML-formatted definition of the task.</param>
        /// <param name="createType">A union of <see cref="TaskCreation"/> flags.</param>
        /// <param name="userId">The user credentials used to register the task.</param>
        /// <param name="password">The password for the userId used to register the task.</param>
        /// <param name="logonType">A <see cref="TaskLogonType"/> value that defines what logon technique is used to run the registered task.</param>
        /// <param name="sddl">The security descriptor associated with the registered task. You can specify the access control list (ACL) in the security descriptor for a task in order to allow or deny certain users and groups access to a task.</param>
        /// <returns>A <see cref="Task"/> instance that represents the new task.</returns>
        /// <example><code lang="cs"><![CDATA[
        /// // Define a basic task in XML
        /// var xml = "<?xml version=\"1.0\" encoding=\"UTF-16\"?>" +
        ///    "<Task version=\"1.2\" xmlns=\"http://schemas.microsoft.com/windows/2004/02/mit/task\">" +
        ///    "  <Principals>" +
        ///    "    <Principal id=\"Author\">" +
        ///    "      <UserId>S-1-5-18</UserId>" +
        ///    "    </Principal>" +
        ///    "  </Principals>" +
        ///    "  <Triggers>" +
        ///    "    <CalendarTrigger>" +
        ///    "      <StartBoundary>2017-09-04T14:04:03</StartBoundary>" +
        ///    "      <ScheduleByDay />" +
        ///    "    </CalendarTrigger>" +
        ///    "  </Triggers>" +
        ///    "  <Actions Context=\"Author\">" +
        ///    "    <Exec>" +
        ///    "      <Command>cmd</Command>" +
        ///    "    </Exec>" +
        ///    "  </Actions>" +
        ///    "</Task>";
        /// // Register the task in the root folder of the local machine using the SYSTEM account defined in XML
        /// TaskService.Instance.RootFolder.RegisterTaskDefinition("Test", xml);
        /// ]]></code></example>
        public Task RegisterTask(string path, [NotNull] string xmlText, TaskCreation createType = TaskCreation.CreateOrUpdate, string userId = null, string password = null, TaskLogonType logonType = TaskLogonType.S4U, string sddl = null)
        {
            if (v2Folder != null)
                return Task.CreateTask(TaskService, v2Folder.RegisterTask(path, xmlText, (int)createType, userId, password, logonType, sddl));

            TaskDefinition td = TaskService.NewTask();
            XmlSerializationHelper.ReadObjectFromXmlText(xmlText, td);
            return RegisterTaskDefinition(path, td, createType, userId ?? td.Principal.ToString(),
                password, logonType == TaskLogonType.S4U ? td.Principal.LogonType : logonType, sddl);
        }

        /// <summary>
        /// Registers (creates) a task in a specified location using a <see cref="TaskDefinition"/> instance to define a task.
        /// </summary>
        /// <param name="path">The task name. If this value is NULL, the task will be registered in the root task folder and the task name will be a GUID value that is created by the Task Scheduler service. A task name cannot begin or end with a space character. The '.' character cannot be used to specify the current task folder and the '..' characters cannot be used to specify the parent task folder in the path.</param>
        /// <param name="definition">The <see cref="TaskDefinition"/> of the registered task.</param>
        /// <returns>A <see cref="Task"/> instance that represents the new task.</returns>
        /// <example>
        /// <code lang="cs"><![CDATA[
        /// // Create a new task definition for the local machine and assign properties
        /// TaskDefinition td = TaskService.Instance.NewTask();
        /// td.RegistrationInfo.Description = "Does something";
        /// 
        /// // Add a trigger that, starting tomorrow, will fire every other week on Monday and Saturday
        /// td.Triggers.Add(new WeeklyTrigger(DaysOfTheWeek.Monday | DaysOfTheWeek.Saturday, 2));
        /// 
        /// // Create an action that will launch Notepad whenever the trigger fires
        /// td.Actions.Add("notepad.exe", "c:\\test.log");
        /// 
        /// // Register the task in the root folder of the local machine using the current user and the S4U logon type
        /// TaskService.Instance.RootFolder.RegisterTaskDefinition("Test", td);
        /// ]]></code></example>
        public Task RegisterTaskDefinition(string path, [NotNull] TaskDefinition definition) => RegisterTaskDefinition(path, definition, TaskCreation.CreateOrUpdate,
                definition.Principal.ToString(), null, definition.Principal.LogonType);

        /// <summary>
        /// Registers (creates) a task in a specified location using a <see cref="TaskDefinition" /> instance to define a task.
        /// </summary>
        /// <param name="path">The task name. If this value is NULL, the task will be registered in the root task folder and the task name will be a GUID value that is created by the Task Scheduler service. A task name cannot begin or end with a space character. The '.' character cannot be used to specify the current task folder and the '..' characters cannot be used to specify the parent task folder in the path.</param>
        /// <param name="definition">The <see cref="TaskDefinition" /> of the registered task.</param>
        /// <param name="createType">A union of <see cref="TaskCreation" /> flags.</param>
        /// <param name="userId">The user credentials used to register the task.</param>
        /// <param name="password">The password for the userId used to register the task.</param>
        /// <param name="logonType">A <see cref="TaskLogonType" /> value that defines what logon technique is used to run the registered task.</param>
        /// <param name="sddl">The security descriptor associated with the registered task. You can specify the access control list (ACL) in the security descriptor for a task in order to allow or deny certain users and groups access to a task.</param>
        /// <returns>
        /// A <see cref="Task" /> instance that represents the new task. This will return <c>null</c> if <paramref name="createType"/> is set to <c>ValidateOnly</c> and there are no validation errors.
        /// </returns>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Task names may not include any characters which are invalid for file names.
        /// or
        /// Task names ending with a period followed by three or fewer characters cannot be retrieved due to a bug in the native library.
        /// </exception>
        /// <exception cref="NotV1SupportedException">This LogonType is not supported on Task Scheduler 1.0.
        /// or
        /// Security settings are not available on Task Scheduler 1.0.
        /// or
        /// Registration triggers are not available on Task Scheduler 1.0.
        /// or
        /// XML validation not available on Task Scheduler 1.0.</exception>
        /// <remarks>This method is effectively the "Save" method for tasks. It takes a modified <c>TaskDefinition</c> instance and registers it in the folder defined by this <c>TaskFolder</c> instance. Optionally, you can use this method to override the user, password and logon type defined in the definition and supply security against the task.</remarks>
        /// <example>
        /// <para>This first example registers a simple task with a single trigger and action using the default security.</para>
        /// <code lang="cs"><![CDATA[
        /// // Create a new task definition for the local machine and assign properties
        /// TaskDefinition td = TaskService.Instance.NewTask();
        /// td.RegistrationInfo.Description = "Does something";
        /// 
        /// // Add a trigger that, starting tomorrow, will fire every other week on Monday and Saturday
        /// td.Triggers.Add(new WeeklyTrigger(DaysOfTheWeek.Monday | DaysOfTheWeek.Saturday, 2));
        /// 
        /// // Create an action that will launch Notepad whenever the trigger fires
        /// td.Actions.Add("notepad.exe", "c:\\test.log");
        /// 
        /// // Register the task in the root folder of the local machine using the current user and the S4U logon type
        /// TaskService.Instance.RootFolder.RegisterTaskDefinition("Test", td);
        /// ]]></code>
        /// <para>This example registers that same task using the SYSTEM account.</para>
        /// <code lang="cs"><![CDATA[
        /// TaskService.Instance.RootFolder.RegisterTaskDefinition("TaskName", taskDefinition, TaskCreation.CreateOrUpdate, "SYSTEM", null, TaskLogonType.ServiceAccount);
        /// ]]></code>
        /// <para>This example registers that same task using a specific username and password along with a security definition.</para>
        /// <code lang="cs"><![CDATA[
        /// TaskService.Instance.RootFolder.RegisterTaskDefinition("TaskName", taskDefinition, TaskCreation.CreateOrUpdate, "userDomain\\userName", "userPassword", TaskLogonType.Password, @"O:BAG:DUD:(A;ID;0x1f019f;;;BA)(A;ID;0x1f019f;;;SY)(A;ID;FA;;;BA)(A;;FR;;;BU)");
        /// ]]></code></example>
        public Task RegisterTaskDefinition([NotNull] string path, [NotNull] TaskDefinition definition, TaskCreation createType, string userId, string password = null, TaskLogonType logonType = TaskLogonType.S4U, string sddl = null)
        {
            if (definition.Actions.Count < 1 || definition.Actions.Count > 32)
                throw new ArgumentOutOfRangeException(nameof(definition.Actions), @"A task must be registered with at least one action and no more than 32 actions.");

            userId ??= definition.Principal.Account;
            if (userId == string.Empty) userId = null;
            User user = new User(userId);
            if (v2Folder != null)
            {
                definition.Actions.ConvertUnsupportedActions();
                if (logonType == TaskLogonType.ServiceAccount)
                {
                    if (string.IsNullOrEmpty(userId) || !user.IsServiceAccount)
                        throw new ArgumentException(@"A valid system account name must be supplied for TaskLogonType.ServiceAccount. Valid entries are ""NT AUTHORITY\SYSTEM"", ""SYSTEM"", ""NT AUTHORITY\LOCALSERVICE"", or ""NT AUTHORITY\NETWORKSERVICE"".", nameof(userId));
                    if (password != null)
                        throw new ArgumentException(@"A password cannot be supplied when specifying TaskLogonType.ServiceAccount.", nameof(password));
                }
                /*else if ((LogonType == TaskLogonType.Password || LogonType == TaskLogonType.InteractiveTokenOrPassword ||
					(LogonType == TaskLogonType.S4U && UserId != null && !user.IsCurrent)) && password == null)
				{
					throw new ArgumentException("A password must be supplied when specifying TaskLogonType.Password or TaskLogonType.InteractiveTokenOrPassword or TaskLogonType.S4U from another account.", nameof(password));
				}*/
                else if (logonType == TaskLogonType.Group && password != null)
                {
                    throw new ArgumentException(@"A password cannot be supplied when specifying TaskLogonType.Group.", nameof(password));
                }
                // The following line compensates for an omission in the native library that never actually sets the registration date (thanks ixm7).
                if (definition.RegistrationInfo.Date == DateTime.MinValue) definition.RegistrationInfo.Date = DateTime.Now;
                var iRegTask = v2Folder.RegisterTaskDefinition(path, definition.v2Def, (int)createType, userId ?? user.Name, password, logonType, sddl);
                if (createType == TaskCreation.ValidateOnly && iRegTask == null)
                    return null;
                return Task.CreateTask(TaskService, iRegTask);
            }

            // Check for V1 invalid task names
            string invChars = Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
            if (Regex.IsMatch(path, @"[" + invChars + @"]"))
                throw new ArgumentOutOfRangeException(nameof(path), @"Task names may not include any characters which are invalid for file names.");
            if (Regex.IsMatch(path, @"\.[^" + invChars + @"]{0,3}\z"))
                throw new ArgumentOutOfRangeException(nameof(path), @"Task names ending with a period followed by three or fewer characters cannot be retrieved due to a bug in the native library.");

            // Adds ability to set a password for a V1 task. Provided by Arcao.
            TaskFlags flags = definition.v1Task.GetFlags();
            if (logonType == TaskLogonType.InteractiveTokenOrPassword && string.IsNullOrEmpty(password))
                logonType = TaskLogonType.InteractiveToken;
            switch (logonType)
            {
                case TaskLogonType.Group:
                case TaskLogonType.S4U:
                case TaskLogonType.None:
                    throw new NotV1SupportedException("This LogonType is not supported on Task Scheduler 1.0.");
                case TaskLogonType.InteractiveToken:
                    flags |= (TaskFlags.RunOnlyIfLoggedOn | TaskFlags.Interactive);
                    definition.v1Task.SetAccountInformation(user.Name, IntPtr.Zero);
                    break;
                case TaskLogonType.ServiceAccount:
                    flags &= ~(TaskFlags.Interactive | TaskFlags.RunOnlyIfLoggedOn);
                    definition.v1Task.SetAccountInformation((String.IsNullOrEmpty(userId) || user.IsSystem) ? String.Empty : user.Name, IntPtr.Zero);
                    break;
                case TaskLogonType.InteractiveTokenOrPassword:
                    flags |= TaskFlags.Interactive;
                    using (CoTaskMemString cpwd = new CoTaskMemString(password))
                        definition.v1Task.SetAccountInformation(user.Name, cpwd.DangerousGetHandle());
                    break;
                case TaskLogonType.Password:
                    using (CoTaskMemString cpwd = new CoTaskMemString(password))
                        definition.v1Task.SetAccountInformation(user.Name, cpwd.DangerousGetHandle());
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(logonType), logonType, null);
            }
            definition.v1Task.SetFlags(flags);

            switch (createType)
            {
                case TaskCreation.Create:
                case TaskCreation.CreateOrUpdate:
                case TaskCreation.Disable:
                case TaskCreation.Update:
                    if (createType == TaskCreation.Disable)
                        definition.Settings.Enabled = false;
                    definition.V1Save(path);
                    break;
                case TaskCreation.DontAddPrincipalAce:
                    throw new NotV1SupportedException("Security settings are not available on Task Scheduler 1.0.");
                case TaskCreation.IgnoreRegistrationTriggers:
                    throw new NotV1SupportedException("Registration triggers are not available on Task Scheduler 1.0.");
                case TaskCreation.ValidateOnly:
                    throw new NotV1SupportedException("XML validation not available on Task Scheduler 1.0.");
                default:
                    throw new ArgumentOutOfRangeException(nameof(createType), createType, null);
            }
            return new Task(TaskService, definition.v1Task);
        }

        /// <summary>
        /// Applies access control list (ACL) entries described by a <see cref="TaskSecurity"/> object to the file described by the current <see cref="TaskFolder"/> object.
        /// </summary>
        /// <param name="taskSecurity">A <see cref="TaskSecurity"/> object that describes an access control list (ACL) entry to apply to the current folder.</param>
        public void SetAccessControl([NotNull] TaskSecurity taskSecurity) { taskSecurity.Persist(this); }

        /// <summary>
        /// Sets the security descriptor for the folder. Not available to Task Scheduler 1.0.
        /// </summary>
        /// <param name="sd">The security descriptor for the folder.</param>
        /// <param name="includeSections">Section(s) of the security descriptor to set.</param>
        [Obsolete("This method will be removed in deference to the SetAccessControl and SetSecurityDescriptorSddlForm methods.")]
        public void SetSecurityDescriptor([NotNull] GenericSecurityDescriptor sd, SecurityInfos includeSections = Task.defaultSecurityInfosSections) { SetSecurityDescriptorSddlForm(sd.GetSddlForm((AccessControlSections)includeSections)); }

        /// <summary>
        /// Sets the security descriptor for the folder. Not available to Task Scheduler 1.0.
        /// </summary>
        /// <param name="sddlForm">The security descriptor for the folder.</param>
        /// <param name="options">Flags that specify how to set the security descriptor.</param>
        /// <exception cref="NotV1SupportedException">Not supported under Task Scheduler 1.0.</exception>
        public void SetSecurityDescriptorSddlForm([NotNull] string sddlForm, TaskSetSecurityOptions options = TaskSetSecurityOptions.None)
        {
            if (v2Folder != null)
                v2Folder.SetSecurityDescriptor(sddlForm, (int)options);
            else
                throw new NotV1SupportedException();
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString() => Path;

        /// <summary>
        /// Enumerates the tasks in the specified folder and its child folders.
        /// </summary>
        /// <param name="folder">The folder in which to start enumeration.</param>
        /// <param name="filter">An optional filter to apply to the task list.</param>
        /// <param name="recurse"><c>true</c> if subfolders are to be queried recursively.</param>
        /// <returns>A <see cref="System.Collections.Generic.IEnumerator{Task}"/> that can be used to iterate through the tasks.</returns>
        internal static IEnumerable<Task> EnumerateFolderTasks(TaskFolder folder, Predicate<Task> filter = null, bool recurse = true)
        {
            foreach (var task in folder.Tasks)
                if (filter == null || filter(task))
                    yield return task;

            if (!recurse) yield break;

            foreach (var sfld in folder.SubFolders)
                foreach (var task in EnumerateFolderTasks(sfld, filter))
                    yield return task;
        }
    }
}
