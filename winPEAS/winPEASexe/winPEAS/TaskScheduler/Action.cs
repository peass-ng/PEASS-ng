using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using winPEAS.TaskScheduler.V1;
using winPEAS.TaskScheduler.V2;

namespace winPEAS.TaskScheduler
{
    /// <summary>Defines the type of actions a task can perform.</summary>
    /// <remarks>The action type is defined when the action is created and cannot be changed later. See <see cref="ActionCollection.AddNew"/>.</remarks>
    public enum TaskActionType
    {
        /// <summary>
        /// This action performs a command-line operation. For example, the action can run a script, launch an executable, or, if the name
        /// of a document is provided, find its associated application and launch the application with the document.
        /// </summary>
        Execute = 0,

        /// <summary>This action fires a handler.</summary>
        ComHandler = 5,

        /// <summary>This action sends and e-mail.</summary>
        SendEmail = 6,

        /// <summary>This action shows a message box.</summary>
        ShowMessage = 7
    }

    /// <summary>An interface that exposes the ability to convert an actions functionality to a PowerShell script.</summary>
    internal interface IBindAsExecAction
    {
    }

    /// <summary>
    /// Abstract base class that provides the common properties that are inherited by all action objects. An action object is created by the
    /// <see cref="ActionCollection.AddNew"/> method.
    /// </summary>
    [PublicAPI]
    public abstract class Action : IDisposable, ICloneable, IEquatable<Action>, INotifyPropertyChanged, IComparable, IComparable<Action>
    {
        internal IAction iAction;
        internal ITask v1Task;

        /// <summary>List of unbound values when working with Actions not associated with a registered task.</summary>
        protected readonly Dictionary<string, object> unboundValues = new Dictionary<string, object>();

        internal Action()
        {
        }

        internal Action([NotNull] IAction action) => iAction = action;

        internal Action([NotNull] ITask iTask) => v1Task = iTask;

        /// <summary>Occurs when a property value changes.</summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>Gets the type of the action.</summary>
        /// <value>The type of the action.</value>
        [XmlIgnore]
        public TaskActionType ActionType => iAction?.Type ?? InternalActionType;

        /// <summary>Gets or sets the identifier of the action.</summary>
        [DefaultValue(null)]
        [XmlAttribute(AttributeName = "id")]
        public virtual string Id
        {
            get => GetProperty<string, IAction>(nameof(Id));
            set => SetProperty<string, IAction>(nameof(Id), value);
        }

        internal abstract TaskActionType InternalActionType { get; }

        /// <summary>Creates the specified action.</summary>
        /// <param name="actionType">Type of the action to instantiate.</param>
        /// <returns><see cref="Action"/> of specified type.</returns>
        public static Action CreateAction(TaskActionType actionType) => Activator.CreateInstance(GetObjectType(actionType)) as Action;

        /// <summary>Creates a new object that is a copy of the current instance.</summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        public object Clone()
        {
            var ret = CreateAction(ActionType);
            ret.CopyProperties(this);
            return ret;
        }

        /// <summary>
        /// Compares the current instance with another object of the same type and returns an integer that indicates whether the current
        /// instance precedes, follows, or occurs in the same position in the sort order as the other object.
        /// </summary>
        /// <param name="obj">An object to compare with this instance.</param>
        /// <returns>A value that indicates the relative order of the objects being compared.</returns>
        public int CompareTo(Action obj) => string.Compare(Id, obj?.Id, StringComparison.InvariantCulture);

        /// <summary>Releases all resources used by this class.</summary>
        public virtual void Dispose()
        {
            if (iAction != null)
                Marshal.ReleaseComObject(iAction);
        }

        /// <summary>Determines whether the specified <see cref="object"/>, is equal to this instance.</summary>
        /// <param name="obj">The <see cref="object"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified <see cref="object"/> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals([CanBeNull] object obj)
        {
            if (obj is Action)
                return Equals((Action)obj);
            return base.Equals(obj);
        }

        /// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns><c>true</c> if the current object is equal to the <paramref name="other"/> parameter; otherwise, <c>false</c>.</returns>
        public virtual bool Equals([NotNull] Action other) => ActionType == other.ActionType && Id == other.Id;

        /// <summary>Returns a hash code for this instance.</summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode() => new { A = ActionType, B = Id }.GetHashCode();

        /// <summary>Returns the action Id.</summary>
        /// <returns>String representation of action.</returns>
        public override string ToString() => Id;

        /// <summary>Returns a <see cref="string"/> that represents this action.</summary>
        /// <param name="culture">The culture.</param>
        /// <returns>String representation of action.</returns>
        public virtual string ToString([NotNull] System.Globalization.CultureInfo culture)
        {
            using (new CultureSwitcher(culture))
                return ToString();
        }

        int IComparable.CompareTo(object obj) => CompareTo(obj as Action);

        internal static Action ActionFromScript(string actionType, string script)
        {
            var tat = TryParse(actionType, TaskActionType.Execute);
            var t = GetObjectType(tat);
            return (Action)t.InvokeMember("FromPowerShellCommand", BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.InvokeMethod, null, null, new object[] { script });
        }

        internal static Action ConvertFromPowerShellAction(ExecAction execAction)
        {
            var psi = execAction.ParsePowerShellItems();
            if (psi != null && psi.Length == 2)
            {
                var a = ActionFromScript(psi[0], psi[1]);
                if (a != null)
                {
                    a.v1Task = execAction.v1Task;
                    a.iAction = execAction.iAction;
                    return a;
                }
            }
            return null;
        }

        /// <summary>Creates a specialized class from a defined interface.</summary>
        /// <param name="iTask">Version 1.0 interface.</param>
        /// <returns>Specialized action class</returns>
        internal static Action CreateAction(ITask iTask)
        {
            var tempAction = new ExecAction(iTask);
            return ConvertFromPowerShellAction(tempAction) ?? tempAction;
        }

        /// <summary>Creates a specialized class from a defined interface.</summary>
        /// <param name="iAction">Version 2.0 Action interface.</param>
        /// <returns>Specialized action class</returns>
        internal static Action CreateAction(IAction iAction)
        {
            var t = GetObjectType(iAction.Type);
            return Activator.CreateInstance(t, BindingFlags.CreateInstance | BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { iAction }, null) as Action;
        }

        internal static T TryParse<T>(string val, T defaultVal)
        {
            var ret = defaultVal;
            if (val != null)
                try { ret = (T)Enum.Parse(typeof(T), val); } catch { }
            return ret;
        }

        internal virtual void Bind(ITask iTask)
        {
            if (Id != null)
                iTask.SetDataItem("ActionId", Id);
            var bindable = this as IBindAsExecAction;
            if (bindable != null)
                iTask.SetDataItem("ActionType", InternalActionType.ToString());
            unboundValues.TryGetValue("Path", out var o);
            iTask.SetApplicationName(bindable != null ? ExecAction.PowerShellPath : o?.ToString() ?? string.Empty);
            unboundValues.TryGetValue("Arguments", out o);
            iTask.SetParameters(bindable != null ? ExecAction.BuildPowerShellCmd(ActionType.ToString(), GetPowerShellCommand()) : o?.ToString() ?? string.Empty);
            unboundValues.TryGetValue("WorkingDirectory", out o);
            iTask.SetWorkingDirectory(o?.ToString() ?? string.Empty);
        }

        internal virtual void Bind(ITaskDefinition iTaskDef)
        {
            var iActions = iTaskDef.Actions;
            if (iActions.Count >= ActionCollection.MaxActions)
                throw new ArgumentOutOfRangeException(nameof(iTaskDef), @"A maximum of 32 actions is allowed within a single task.");
            CreateV2Action(iActions);
            Marshal.ReleaseComObject(iActions);
            foreach (var key in unboundValues.Keys)
            {
                try { ReflectionHelper.SetProperty(iAction, key, unboundValues[key]); }
                catch (TargetInvocationException tie) { throw tie.InnerException; }
                catch { }
            }
            unboundValues.Clear();
        }

        /// <summary>Copies the properties from another <see cref="Action"/> the current instance.</summary>
        /// <param name="sourceAction">The source <see cref="Action"/>.</param>
        internal virtual void CopyProperties([NotNull] Action sourceAction) => Id = sourceAction.Id;

        internal abstract void CreateV2Action(IActionCollection iActions);

        internal abstract string GetPowerShellCommand();

        internal T GetProperty<T, TB>(string propName, T defaultValue = default)
        {
            if (iAction == null)
                return unboundValues.TryGetValue(propName, out var value) ? (T)value : defaultValue;
            return ReflectionHelper.GetProperty((TB)iAction, propName, defaultValue);
        }

        internal void OnPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

        internal void SetProperty<T, TB>(string propName, T value)
        {
            if (iAction == null)
            {
                if (Equals(value, default(T)))
                    unboundValues.Remove(propName);
                else
                    unboundValues[propName] = value;
            }
            else
                ReflectionHelper.SetProperty((TB)iAction, propName, value);
            OnPropertyChanged(propName);
        }

        [NotNull]
        private static Type GetObjectType(TaskActionType actionType)
        {
            switch (actionType)
            {
                case TaskActionType.ComHandler:
                    return typeof(ComHandlerAction);
                case TaskActionType.SendEmail:
                    return typeof(EmailAction);
                case TaskActionType.ShowMessage:
                    return typeof(ShowMessageAction);

                default:
                    return typeof(ExecAction);
            }
        }

        /// <summary>
        /// Represents an action that fires a handler. Only available on Task Scheduler 2.0. <note>Only available for Task Scheduler 2.0 on
        /// Windows Vista or Windows Server 2003 and later.</note>
        /// </summary>
        /// <remarks>
        /// This action is the most complex. It allows the task to execute and In-Proc COM server object that implements the ITaskHandler
        /// interface. There is a sample project that shows how to do this in the Downloads section.
        /// </remarks>
        /// <example>
        /// <code lang="cs">
        ///<![CDATA[
        ///ComHandlerAction comAction = new ComHandlerAction(new Guid("{CE7D4428-8A77-4c5d-8A13-5CAB5D1EC734}"));
        ///comAction.Data = "Something specific the COM object needs to execute. This can be left unassigned as well.";
        ///]]>
        /// </code>
        /// </example>
        [XmlType(IncludeInSchema = true)]
        [XmlRoot("ComHandler", Namespace = TaskDefinition.tns, IsNullable = false)]
        public class ComHandlerAction : Action, IBindAsExecAction
        {
            /// <summary>Creates an unbound instance of <see cref="ComHandlerAction"/>.</summary>
            public ComHandlerAction() { }

            /// <summary>Creates an unbound instance of <see cref="ComHandlerAction"/>.</summary>
            /// <param name="classId">Identifier of the handler class.</param>
            /// <param name="data">Addition data associated with the handler.</param>
            public ComHandlerAction(Guid classId, [CanBeNull] string data)
            {
                ClassId = classId;
                Data = data;
            }

            internal ComHandlerAction([NotNull] ITask task) : base(task)
            {
            }

            internal ComHandlerAction([NotNull] IAction action) : base(action)
            {
            }

            /// <summary>Gets or sets the identifier of the handler class.</summary>
            public Guid ClassId
            {
                get => new Guid(GetProperty<string, IComHandlerAction>(nameof(ClassId), Guid.Empty.ToString()));
                set => SetProperty<string, IComHandlerAction>(nameof(ClassId), value.ToString());
            }

            /// <summary>Gets the name of the object referred to by <see cref="ClassId"/>.</summary>
            public string ClassName => GetNameForCLSID(ClassId);

            /// <summary>Gets or sets additional data that is associated with the handler.</summary>
            [DefaultValue(null)]
            [CanBeNull]
            public string Data
            {
                get => GetProperty<string, IComHandlerAction>(nameof(Data));
                set => SetProperty<string, IComHandlerAction>(nameof(Data), value);
            }

            internal override TaskActionType InternalActionType => TaskActionType.ComHandler;

            /// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
            /// <param name="other">An object to compare with this object.</param>
            /// <returns><c>true</c> if the current object is equal to the <paramref name="other"/> parameter; otherwise, <c>false</c>.</returns>
            public override bool Equals(Action other) => base.Equals(other) && ClassId == ((ComHandlerAction)other).ClassId && Data == ((ComHandlerAction)other).Data;

            /// <summary>Gets a string representation of the <see cref="ComHandlerAction"/>.</summary>
            /// <returns>String representation of this action.</returns>
            public override string ToString() => string.Format(Properties.Resources.ComHandlerAction, ClassId, Data, Id, ClassName);

            internal static Action FromPowerShellCommand(string p)
            {
                var match = System.Text.RegularExpressions.Regex.Match(p, @"^\[Reflection.Assembly\]::LoadFile\('(?:[^']*)'\); \[Microsoft.Win32.TaskScheduler.TaskService\]::RunComHandlerAction\(\[GUID\]\('(?<g>[^']*)'\), '(?<d>[^']*)'\);?\s*$");
                return match.Success ? new ComHandlerAction(new Guid(match.Groups["g"].Value), match.Groups["d"].Value.Replace("''", "'")) : null;
            }

            /// <summary>Copies the properties from another <see cref="System.Action"/> the current instance.</summary>
            /// <param name="sourceAction">The source <see cref="System.Action"/>.</param>
            internal override void CopyProperties(Action sourceAction)
            {
                if (sourceAction.GetType() == GetType())
                {
                    base.CopyProperties(sourceAction);
                    ClassId = ((ComHandlerAction)sourceAction).ClassId;
                    Data = ((ComHandlerAction)sourceAction).Data;
                }
            }

            internal override void CreateV2Action([NotNull] IActionCollection iActions) => iAction = iActions.Create(TaskActionType.ComHandler);

            internal override string GetPowerShellCommand()
            {
                var sb = new System.Text.StringBuilder();
                sb.Append($"[Reflection.Assembly]::LoadFile('{Assembly.GetExecutingAssembly().Location}'); ");
                sb.Append($"[Microsoft.Win32.TaskScheduler.TaskService]::RunComHandlerAction([GUID]('{ClassId:D}'), '{Data?.Replace("'", "''") ?? string.Empty}'); ");
                return sb.ToString();
            }

            /// <summary>Gets the name for CLSID.</summary>
            /// <param name="guid">The unique identifier.</param>
            /// <returns></returns>
            [CanBeNull]
            private static string GetNameForCLSID(Guid guid)
            {
                using (var k = Registry.ClassesRoot.OpenSubKey("CLSID", false))
                {
                    if (k != null)
                    {
                        using (var k2 = k.OpenSubKey(guid.ToString("B"), false))
                        {
                            return k2?.GetValue(null) as string;
                        }
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Represents an action that sends an e-mail. <note>Only available for Task Scheduler 2.0 on Windows Vista or Windows Server 2003 and
        /// later.</note><note type="warning">This action has been deprecated in Windows 8 and later. However, this library is able to mimic its
        /// functionality using PowerShell if the <see cref="ActionCollection.PowerShellConversion"/> property is set to <see
        /// cref="PowerShellActionPlatformOption.All"/>. To disable this conversion, set the value to <see cref="PowerShellActionPlatformOption.Never"/>.</note>
        /// </summary>
        /// <remarks>The EmailAction allows for an email to be sent when the task is triggered.</remarks>
        /// <example>
        /// <code lang="cs">
        ///<![CDATA[
        ///EmailAction ea = new EmailAction("Task fired", "sender@email.com", "recipient@email.com", "You just got a message", "smtp.company.com");
        ///ea.Bcc = "alternate@email.com";
        ///ea.HeaderFields.Add("reply-to", "dh@mail.com");
        ///ea.Priority = System.Net.Mail.MailPriority.High;
        /// // All attachement paths are checked to ensure there is an existing file
        ///ea.Attachments = new object[] { "localpath\\ondiskfile.txt" };
        ///]]>
        /// </code>
        /// </example>
        [XmlType(IncludeInSchema = true)]
        [XmlRoot("SendEmail", Namespace = TaskDefinition.tns, IsNullable = false)]
        public sealed class EmailAction : Action, IBindAsExecAction
        {
            private const string ImportanceHeader = "Importance";

            private NamedValueCollection nvc;
            private bool validateAttachments = true;

            /// <summary>Creates an unbound instance of <see cref="EmailAction"/>.</summary>
            public EmailAction() { }

            /// <summary>Creates an unbound instance of <see cref="EmailAction"/>.</summary>
            /// <param name="subject">Subject of the e-mail.</param>
            /// <param name="from">E-mail address that you want to send the e-mail from.</param>
            /// <param name="to">E-mail address or addresses that you want to send the e-mail to.</param>
            /// <param name="body">Body of the e-mail that contains the e-mail message.</param>
            /// <param name="mailServer">Name of the server that you use to send e-mail from.</param>
            public EmailAction([CanBeNull] string subject, [NotNull] string from, [NotNull] string to, [CanBeNull] string body, [NotNull] string mailServer)
            {
                Subject = subject;
                From = from;
                To = to;
                Body = body;
                Server = mailServer;
            }

            internal EmailAction([NotNull] ITask task) : base(task)
            {
            }

            internal EmailAction([NotNull] IAction action) : base(action)
            {
            }

            /// <summary>
            /// Gets or sets an array of file paths to be sent as attachments with the e-mail. Each item must be a <see cref="string"/> value
            /// containing a path to file.
            /// </summary>
            [XmlArray("Attachments", IsNullable = true)]
            [XmlArrayItem("File", typeof(string))]
            [DefaultValue(null)]
            public object[] Attachments
            {
                get => GetProperty<object[], IEmailAction>(nameof(Attachments));
                set
                {
                    if (value != null)
                    {
                        if (value.Length > 8)
                            throw new ArgumentOutOfRangeException(nameof(Attachments), @"Attachments array cannot contain more than 8 items.");
                        if (validateAttachments)
                        {
                            foreach (var o in value)
                                if (!(o is string) || !System.IO.File.Exists((string)o))
                                    throw new ArgumentException(@"Each value of the array must contain a valid file reference.", nameof(Attachments));
                        }
                    }
                    if (iAction == null && (value == null || value.Length == 0))
                    {
                        unboundValues.Remove(nameof(Attachments));
                        OnPropertyChanged(nameof(Attachments));
                    }
                    else
                        SetProperty<object[], IEmailAction>(nameof(Attachments), value);
                }
            }

            /// <summary>Gets or sets the e-mail address or addresses that you want to Bcc in the e-mail.</summary>
            [DefaultValue(null)]
            public string Bcc
            {
                get => GetProperty<string, IEmailAction>(nameof(Bcc));
                set => SetProperty<string, IEmailAction>(nameof(Bcc), value);
            }

            /// <summary>Gets or sets the body of the e-mail that contains the e-mail message.</summary>
            [DefaultValue(null)]
            public string Body
            {
                get => GetProperty<string, IEmailAction>(nameof(Body));
                set => SetProperty<string, IEmailAction>(nameof(Body), value);
            }

            /// <summary>Gets or sets the e-mail address or addresses that you want to Cc in the e-mail.</summary>
            [DefaultValue(null)]
            public string Cc
            {
                get => GetProperty<string, IEmailAction>(nameof(Cc));
                set => SetProperty<string, IEmailAction>(nameof(Cc), value);
            }

            /// <summary>Gets or sets the e-mail address that you want to send the e-mail from.</summary>
            [DefaultValue(null)]
            public string From
            {
                get => GetProperty<string, IEmailAction>(nameof(From));
                set => SetProperty<string, IEmailAction>(nameof(From), value);
            }

            /// <summary>Gets or sets the header information in the e-mail message to send.</summary>
            [XmlArray]
            [XmlArrayItem("HeaderField", typeof(NameValuePair))]
            [NotNull]
            public NamedValueCollection HeaderFields
            {
                get
                {
                    if (nvc == null)
                    {
                        nvc = iAction == null ? new NamedValueCollection() : new NamedValueCollection(((IEmailAction)iAction).HeaderFields);
                        nvc.AttributedXmlFormat = false;
                        nvc.CollectionChanged += (o, e) => OnPropertyChanged(nameof(HeaderFields));
                    }
                    return nvc;
                }
            }

            /// <summary>Gets or sets the priority of the e-mail message.</summary>
            /// <value>A <see cref="System.Net.Mail.MailPriority"/> that contains the priority of this message.</value>
            [XmlIgnore]
            [DefaultValue(typeof(System.Net.Mail.MailPriority), "Normal")]
            public System.Net.Mail.MailPriority Priority
            {
                get
                {
                    if (nvc != null && HeaderFields.TryGetValue(ImportanceHeader, out var s))
                        return TryParse(s, System.Net.Mail.MailPriority.Normal);
                    return System.Net.Mail.MailPriority.Normal;
                }
                set => HeaderFields[ImportanceHeader] = value.ToString();
            }

            /// <summary>Gets or sets the e-mail address that you want to reply to.</summary>
            [DefaultValue(null)]
            public string ReplyTo
            {
                get => GetProperty<string, IEmailAction>(nameof(ReplyTo));
                set => SetProperty<string, IEmailAction>(nameof(ReplyTo), value);
            }

            /// <summary>Gets or sets the name of the server that you use to send e-mail from.</summary>
            [DefaultValue(null)]
            public string Server
            {
                get => GetProperty<string, IEmailAction>(nameof(Server));
                set => SetProperty<string, IEmailAction>(nameof(Server), value);
            }

            /// <summary>Gets or sets the subject of the e-mail.</summary>
            [DefaultValue(null)]
            public string Subject
            {
                get => GetProperty<string, IEmailAction>(nameof(Subject));
                set => SetProperty<string, IEmailAction>(nameof(Subject), value);
            }

            /// <summary>Gets or sets the e-mail address or addresses that you want to send the e-mail to.</summary>
            [DefaultValue(null)]
            public string To
            {
                get => GetProperty<string, IEmailAction>(nameof(To));
                set => SetProperty<string, IEmailAction>(nameof(To), value);
            }

            internal override TaskActionType InternalActionType => TaskActionType.SendEmail;

            /// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
            /// <param name="other">An object to compare with this object.</param>
            /// <returns><c>true</c> if the current object is equal to the <paramref name="other"/> parameter; otherwise, <c>false</c>.</returns>
            public override bool Equals(Action other) => base.Equals(other) && GetPowerShellCommand() == other.GetPowerShellCommand();

            /// <summary>Gets a string representation of the <see cref="EmailAction"/>.</summary>
            /// <returns>String representation of this action.</returns>
            public override string ToString() => string.Format(Properties.Resources.EmailAction, Subject, To, Cc, Bcc, From, ReplyTo, Body, Server, Id);

            internal static Action FromPowerShellCommand(string p)
            {
                var match = System.Text.RegularExpressions.Regex.Match(p, @"^Send-MailMessage -From '(?<from>(?:[^']|'')*)' -Subject '(?<subject>(?:[^']|'')*)' -SmtpServer '(?<server>(?:[^']|'')*)'(?: -Encoding UTF8)?(?: -To (?<to>'(?:(?:[^']|'')*)'(?:, '(?:(?:[^']|'')*)')*))?(?: -Cc (?<cc>'(?:(?:[^']|'')*)'(?:, '(?:(?:[^']|'')*)')*))?(?: -Bcc (?<bcc>'(?:(?:[^']|'')*)'(?:, '(?:(?:[^']|'')*)')*))?(?:(?: -BodyAsHtml)? -Body '(?<body>(?:[^']|'')*)')?(?: -Attachments (?<att>'(?:(?:[^']|'')*)'(?:, '(?:(?:[^']|'')*)')*))?(?: -Priority (?<imp>High|Normal|Low))?;?\s*$");
                if (match.Success)
                {
                    var action = new EmailAction(UnPrep(FromUTF8(match.Groups["subject"].Value)), UnPrep(match.Groups["from"].Value), FromPS(match.Groups["to"]), UnPrep(FromUTF8(match.Groups["body"].Value)), UnPrep(match.Groups["server"].Value))
                    { Cc = FromPS(match.Groups["cc"]), Bcc = FromPS(match.Groups["bcc"]) };
                    action.validateAttachments = false;
                    if (match.Groups["att"].Success)
                        action.Attachments = Array.ConvertAll<string, object>(FromPS(match.Groups["att"].Value), s => s);
                    action.validateAttachments = true;
                    if (match.Groups["imp"].Success)
                        action.HeaderFields[ImportanceHeader] = match.Groups["imp"].Value;
                    return action;
                }
                return null;
            }

            internal override void Bind(ITaskDefinition iTaskDef)
            {
                base.Bind(iTaskDef);
                nvc?.Bind(((IEmailAction)iAction).HeaderFields);
            }

            /// <summary>Copies the properties from another <see cref="System.Action"/> the current instance.</summary>
            /// <param name="sourceAction">The source <see cref="System.Action"/>.</param>
            internal override void CopyProperties(Action sourceAction)
            {
                if (sourceAction.GetType() == GetType())
                {
                    base.CopyProperties(sourceAction);
                    if (((EmailAction)sourceAction).Attachments != null)
                        Attachments = (object[])((EmailAction)sourceAction).Attachments.Clone();
                    Bcc = ((EmailAction)sourceAction).Bcc;
                    Body = ((EmailAction)sourceAction).Body;
                    Cc = ((EmailAction)sourceAction).Cc;
                    From = ((EmailAction)sourceAction).From;
                    if (((EmailAction)sourceAction).nvc != null)
                        ((EmailAction)sourceAction).HeaderFields.CopyTo(HeaderFields);
                    ReplyTo = ((EmailAction)sourceAction).ReplyTo;
                    Server = ((EmailAction)sourceAction).Server;
                    Subject = ((EmailAction)sourceAction).Subject;
                    To = ((EmailAction)sourceAction).To;
                }
            }

            internal override void CreateV2Action(IActionCollection iActions) => iAction = iActions.Create(TaskActionType.SendEmail);

            internal override string GetPowerShellCommand()
            {
                // Send-MailMessage [-To] <String[]> [-Subject] <String> [[-Body] <String> ] [[-SmtpServer] <String> ] -From <String>
                // [-Attachments <String[]> ] [-Bcc <String[]> ] [-BodyAsHtml] [-Cc <String[]> ] [-Credential <PSCredential> ]
                // [-DeliveryNotificationOption <DeliveryNotificationOptions> ] [-Encoding <Encoding> ] [-Port <Int32> ] [-Priority
                // <MailPriority> ] [-UseSsl] [ <CommonParameters>]
                var bodyIsHtml = Body != null && Body.Trim().StartsWith("<") && Body.Trim().EndsWith(">");
                var sb = new System.Text.StringBuilder();
                sb.AppendFormat("Send-MailMessage -From '{0}' -Subject '{1}' -SmtpServer '{2}' -Encoding UTF8", Prep(From), ToUTF8(Prep(Subject)), Prep(Server));
                if (!string.IsNullOrEmpty(To))
                    sb.AppendFormat(" -To {0}", ToPS(To));
                if (!string.IsNullOrEmpty(Cc))
                    sb.AppendFormat(" -Cc {0}", ToPS(Cc));
                if (!string.IsNullOrEmpty(Bcc))
                    sb.AppendFormat(" -Bcc {0}", ToPS(Bcc));
                if (bodyIsHtml)
                    sb.Append(" -BodyAsHtml");
                if (!string.IsNullOrEmpty(Body))
                    sb.AppendFormat(" -Body '{0}'", ToUTF8(Prep(Body)));
                if (Attachments != null && Attachments.Length > 0)
                    sb.AppendFormat(" -Attachments {0}", ToPS(Array.ConvertAll(Attachments, o => Prep(o.ToString()))));
                var hdr = new List<string>(HeaderFields.Names);
                if (hdr.Contains(ImportanceHeader))
                {
                    var p = Priority;
                    if (p != System.Net.Mail.MailPriority.Normal)
                        sb.Append($" -Priority {p}");
                    hdr.Remove(ImportanceHeader);
                }
                if (hdr.Count > 0)
                    throw new InvalidOperationException("Under Windows 8 and later, EmailAction objects are converted to PowerShell. This action contains headers that are not supported.");
                sb.Append("; ");
                return sb.ToString();

                /*var msg = new System.Net.Mail.MailMessage(this.From, this.To, this.Subject, this.Body);
                if (!string.IsNullOrEmpty(this.Bcc))
                    msg.Bcc.Add(this.Bcc);
                if (!string.IsNullOrEmpty(this.Cc))
                    msg.CC.Add(this.Cc);
                if (!string.IsNullOrEmpty(this.ReplyTo))
                    msg.ReplyTo = new System.Net.Mail.MailAddress(this.ReplyTo);
                if (this.Attachments != null && this.Attachments.Length > 0)
                    foreach (string s in this.Attachments)
                        msg.Attachments.Add(new System.Net.Mail.Attachment(s));
                if (this.nvc != null)
                    foreach (var ha in this.HeaderFields)
                        msg.Headers.Add(ha.Name, ha.Value);
                var client = new System.Net.Mail.SmtpClient(this.Server);
                client.Send(msg);*/
            }

            private static string[] FromPS(string p)
            {
                var list = p.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
                return Array.ConvertAll(list, i => UnPrep(i).Trim('\''));
            }

            private static string FromPS(System.Text.RegularExpressions.Group g, string delimeter = ";") => g.Success ? string.Join(delimeter, FromPS(g.Value)) : null;

            private static string FromUTF8(string s)
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(s);
                return System.Text.Encoding.Default.GetString(bytes);
            }

            private static string Prep(string s) => s?.Replace("'", "''");

            private static string ToPS(string input, char[] delimeters = null)
            {
                if (delimeters == null)
                    delimeters = new[] { ';', ',' };
                return ToPS(Array.ConvertAll(input.Split(delimeters), i => Prep(i.Trim())));
            }

            private static string ToPS(string[] input) => string.Join(", ", Array.ConvertAll(input, i => string.Concat("'", i.Trim(), "'")));

            private static string ToUTF8(string s)
            {
                if (s == null) return null;
                var bytes = System.Text.Encoding.Default.GetBytes(s);
                return System.Text.Encoding.UTF8.GetString(bytes);
            }

            private static string UnPrep(string s) => s?.Replace("''", "'");
        }

        /// <summary>Represents an action that executes a command-line operation.</summary>
        /// <remarks>
        /// All versions of the base library support the ExecAction. It only has three properties that allow it to run an executable with parameters.
        /// </remarks>
        /// <example>
        /// <code lang="cs">
        ///<![CDATA[
        ///ExecAction ea1 = new ExecAction("notepad.exe", "file.txt", null);
        ///ExecAction ea2 = new ExecAction();
        ///ea2.Path = "notepad.exe";
        ///ea.Arguments = "file2.txt";
        ///]]>
        /// </code>
        /// </example>
        [XmlRoot("Exec", Namespace = TaskDefinition.tns, IsNullable = false)]
        public class ExecAction : Action
        {
#if DEBUG
        internal const string PowerShellArgFormat = "-NoExit -Command \"& {{<# {0}:{1} #> {2}}}\"";
#else
            internal const string PowerShellArgFormat = "-NoLogo -NonInteractive -WindowStyle Hidden -Command \"& {{<# {0}:{1} #> {2}}}\"";
#endif
            internal const string PowerShellPath = "powershell";
            internal const string ScriptIdentifer = "TSML_20140424";

            /// <summary>Creates a new instance of an <see cref="ExecAction"/> that can be added to <see cref="TaskDefinition.Actions"/>.</summary>
            public ExecAction() { }

            /// <summary>Creates a new instance of an <see cref="ExecAction"/> that can be added to <see cref="TaskDefinition.Actions"/>.</summary>
            /// <param name="path">Path to an executable file.</param>
            /// <param name="arguments">Arguments associated with the command-line operation. This value can be null.</param>
            /// <param name="workingDirectory">
            /// Directory that contains either the executable file or the files that are used by the executable file. This value can be null.
            /// </param>
            public ExecAction([NotNull] string path, string arguments = null, string workingDirectory = null)
            {
                Path = path;
                Arguments = arguments;
                WorkingDirectory = workingDirectory;
            }

            internal ExecAction([NotNull] ITask task) : base(task)
            {
            }

            internal ExecAction([NotNull] IAction action) : base(action)
            {
            }

            /// <summary>Gets or sets the arguments associated with the command-line operation.</summary>
            [DefaultValue("")]
            public string Arguments
            {
                get
                {
                    if (v1Task != null)
                        return v1Task.GetParameters();
                    return GetProperty<string, IExecAction>(nameof(Arguments), "");
                }
                set
                {
                    if (v1Task != null)
                        v1Task.SetParameters(value);
                    else
                        SetProperty<string, IExecAction>(nameof(Arguments), value);
                }
            }

            /// <summary>Gets or sets the path to an executable file.</summary>
            [XmlElement("Command")]
            [DefaultValue("")]
            public string Path
            {
                get
                {
                    if (v1Task != null)
                        return v1Task.GetApplicationName();
                    return GetProperty<string, IExecAction>(nameof(Path), "");
                }
                set
                {
                    if (v1Task != null)
                        v1Task.SetApplicationName(value);
                    else
                        SetProperty<string, IExecAction>(nameof(Path), value);
                }
            }

            /// <summary>
            /// Gets or sets the directory that contains either the executable file or the files that are used by the executable file.
            /// </summary>
            [DefaultValue("")]
            public string WorkingDirectory
            {
                get
                {
                    if (v1Task != null)
                        return v1Task.GetWorkingDirectory();
                    return GetProperty<string, IExecAction>(nameof(WorkingDirectory), "");
                }
                set
                {
                    if (v1Task != null)
                        v1Task.SetWorkingDirectory(value);
                    else
                        SetProperty<string, IExecAction>(nameof(WorkingDirectory), value);
                }
            }

            internal override TaskActionType InternalActionType => TaskActionType.Execute;

            /// <summary>Determines whether the specified path is a valid filename and, optionally, if it exists.</summary>
            /// <param name="path">The path.</param>
            /// <param name="checkIfExists">if set to <c>true</c> check if file exists.</param>
            /// <param name="throwOnException">if set to <c>true</c> throw exception on error.</param>
            /// <returns><c>true</c> if the specified path is a valid filename; otherwise, <c>false</c>.</returns>
            public static bool IsValidPath(string path, bool checkIfExists = true, bool throwOnException = false)
            {
                try
                {
                    if (path == null) throw new ArgumentNullException(nameof(path));
                    /*if (path.StartsWith("\"") && path.EndsWith("\"") && path.Length > 1)
                        path = path.Substring(1, path.Length - 2);*/
                    var fn = System.IO.Path.GetFileName(path);
                    System.Diagnostics.Debug.WriteLine($"IsValidPath fn={fn}");
                    if (fn == string.Empty)
                        return false;
                    var dn = System.IO.Path.GetDirectoryName(path);
                    System.Diagnostics.Debug.WriteLine($"IsValidPath dir={dn ?? "null"}");
                    System.IO.Path.GetFullPath(path);
                    return true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"IsValidPath exc={ex}");
                    if (throwOnException) throw;
                }
                return false;
            }

            /// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
            /// <param name="other">An object to compare with this object.</param>
            /// <returns><c>true</c> if the current object is equal to the <paramref name="other"/> parameter; otherwise, <c>false</c>.</returns>
            public override bool Equals(Action other) => base.Equals(other) && Path == ((ExecAction)other).Path && Arguments == ((ExecAction)other).Arguments && WorkingDirectory == ((ExecAction)other).WorkingDirectory;

            /// <summary>
            /// Validates the input as a valid filename and optionally checks for its existence. If valid, the <see cref="Path"/> property is
            /// set to the validated absolute file path.
            /// </summary>
            /// <param name="path">The file path to validate.</param>
            /// <param name="checkIfExists">if set to <c>true</c> check if the file exists.</param>
            public void SetValidatedPath([NotNull] string path, bool checkIfExists = true)
            {
                if (IsValidPath(path, checkIfExists, true))
                    Path = path;
            }

            /// <summary>Gets a string representation of the <see cref="ExecAction"/>.</summary>
            /// <returns>String representation of this action.</returns>
            public override string ToString() => string.Format(Properties.Resources.ExecAction, Path, Arguments, WorkingDirectory, Id);

            internal static string BuildPowerShellCmd(string actionType, string cmd) => string.Format(PowerShellArgFormat, ScriptIdentifer, actionType, cmd);

            internal static ExecAction ConvertToPowerShellAction(Action action) => CreatePowerShellAction(action.ActionType.ToString(), action.GetPowerShellCommand());

            internal static ExecAction CreatePowerShellAction(string actionType, string cmd) => new ExecAction(PowerShellPath, BuildPowerShellCmd(actionType, cmd));

            internal static Action FromPowerShellCommand(string p)
            {
                var match = System.Text.RegularExpressions.Regex.Match(p, "^Start-Process -FilePath '(?<p>[^']*)'(?: -ArgumentList '(?<a>[^']*)')?(?: -WorkingDirectory '(?<d>[^']*)')?;?\\s*$");
                return match.Success ? new ExecAction(match.Groups["p"].Value, match.Groups["a"].Success ? match.Groups["a"].Value.Replace("''", "'") : null, match.Groups["d"].Success ? match.Groups["d"].Value : null) : null;
            }

            /// <summary>Copies the properties from another <see cref="System.Action"/> the current instance.</summary>
            /// <param name="sourceAction">The source <see cref="System.Action"/>.</param>
            internal override void CopyProperties(Action sourceAction)
            {
                if (sourceAction.GetType() == GetType())
                {
                    base.CopyProperties(sourceAction);
                    Path = ((ExecAction)sourceAction).Path;
                    Arguments = ((ExecAction)sourceAction).Arguments;
                    WorkingDirectory = ((ExecAction)sourceAction).WorkingDirectory;
                }
            }

            internal override void CreateV2Action(IActionCollection iActions) => iAction = iActions.Create(TaskActionType.Execute);

            internal override string GetPowerShellCommand()
            {
                var sb = new System.Text.StringBuilder($"Start-Process -FilePath '{Path}'");
                if (!string.IsNullOrEmpty(Arguments))
                    sb.Append($" -ArgumentList '{Arguments.Replace("'", "''")}'");
                if (!string.IsNullOrEmpty(WorkingDirectory))
                    sb.Append($" -WorkingDirectory '{WorkingDirectory}'");
                return sb.Append("; ").ToString();
            }

            internal string[] ParsePowerShellItems()
            {
                if (((Path?.EndsWith(PowerShellPath, StringComparison.InvariantCultureIgnoreCase) ?? false) ||
                     (Path?.EndsWith(PowerShellPath + ".exe", StringComparison.InvariantCultureIgnoreCase) ?? false)) && (Arguments?.Contains(ScriptIdentifer) ?? false))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(Arguments, @"<# " + ScriptIdentifer + ":(?<type>\\w+) #> (?<cmd>.+)}\"$");
                    if (match.Success)
                        return new[] { match.Groups["type"].Value, match.Groups["cmd"].Value };
                }
                return null;
            }
        }

        /// <summary>
        /// Represents an action that shows a message box when a task is activated. <note>Only available for Task Scheduler 2.0 on Windows Vista
        /// or Windows Server 2003 and later.</note><note type="warning">This action has been deprecated in Windows 8 and later. However, this
        /// library is able to mimic its functionality using PowerShell if the <see cref="ActionCollection.PowerShellConversion"/> property is
        /// set to <see cref="PowerShellActionPlatformOption.All"/>. To disable this conversion, set the value to <see cref="PowerShellActionPlatformOption.Never"/>.</note>
        /// </summary>
        /// <remarks>Display a message when the trigger fires using the ShowMessageAction.</remarks>
        /// <example>
        /// <code lang="cs">
        ///<![CDATA[
        ///ShowMessageAction msg = new ShowMessageAction("You just got a message!", "SURPRISE");
        ///]]>
        /// </code>
        /// </example>
        [XmlType(IncludeInSchema = true)]
        [XmlRoot("ShowMessage", Namespace = TaskDefinition.tns, IsNullable = false)]
        public sealed class ShowMessageAction : Action, IBindAsExecAction
        {
            /// <summary>Creates a new unbound instance of <see cref="ShowMessageAction"/>.</summary>
            public ShowMessageAction()
            {
            }

            /// <summary>Creates a new unbound instance of <see cref="ShowMessageAction"/>.</summary>
            /// <param name="messageBody">Message text that is displayed in the body of the message box.</param>
            /// <param name="title">Title of the message box.</param>
            public ShowMessageAction([CanBeNull] string messageBody, [CanBeNull] string title)
            {
                MessageBody = messageBody;
                Title = title;
            }

            internal ShowMessageAction([NotNull] ITask task) : base(task)
            {
            }

            internal ShowMessageAction([NotNull] IAction action) : base(action)
            {
            }

            /// <summary>Gets or sets the message text that is displayed in the body of the message box.</summary>
            [XmlElement("Body")]
            [DefaultValue(null)]
            public string MessageBody
            {
                get => GetProperty<string, IShowMessageAction>(nameof(MessageBody));
                set => SetProperty<string, IShowMessageAction>(nameof(MessageBody), value);
            }

            /// <summary>Gets or sets the title of the message box.</summary>
            [DefaultValue(null)]
            public string Title
            {
                get => GetProperty<string, IShowMessageAction>(nameof(Title));
                set => SetProperty<string, IShowMessageAction>(nameof(Title), value);
            }

            internal override TaskActionType InternalActionType => TaskActionType.ShowMessage;

            /// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
            /// <param name="other">An object to compare with this object.</param>
            /// <returns><c>true</c> if the current object is equal to the <paramref name="other"/> parameter; otherwise, <c>false</c>.</returns>
            public override bool Equals(Action other) => base.Equals(other) &&
                                                         string.Equals(Title, (other as ShowMessageAction)?.Title) &&
                                                         string.Equals(MessageBody,
                                                             (other as ShowMessageAction)?.MessageBody);

            /// <summary>Gets a string representation of the <see cref="ShowMessageAction"/>.</summary>
            /// <returns>String representation of this action.</returns>
            public override string ToString() =>
                string.Format(Properties.Resources.ShowMessageAction, Title, MessageBody, Id);

            internal static Action FromPowerShellCommand(string p)
            {
                var match = System.Text.RegularExpressions.Regex.Match(p,
                    @"^\[System.Reflection.Assembly\]::LoadWithPartialName\('System.Windows.Forms'\); \[System.Windows.Forms.MessageBox\]::Show\('(?<msg>(?:[^']|'')*)'(?:,'(?<t>(?:[^']|'')*)')?\);?\s*$");
                return match.Success
                    ? new ShowMessageAction(match.Groups["msg"].Value.Replace("''", "'"),
                        match.Groups["t"].Success ? match.Groups["t"].Value.Replace("''", "'") : null)
                    : null;
            }

            /// <summary>Copies the properties from another <see cref="System.Action"/> the current instance.</summary>
            /// <param name="sourceAction">The source <see cref="System.Action"/>.</param>
            internal override void CopyProperties(Action sourceAction)
            {
                if (sourceAction.GetType() == GetType())
                {
                    base.CopyProperties(sourceAction);
                    Title = ((ShowMessageAction)sourceAction).Title;
                    MessageBody = ((ShowMessageAction)sourceAction).MessageBody;
                }
            }

            internal override void CreateV2Action(IActionCollection iActions) =>
                iAction = iActions.Create(TaskActionType.ShowMessage);

            internal override string GetPowerShellCommand()
            {
                // [System.Reflection.Assembly]::LoadWithPartialName('System.Windows.Forms'); [System.Windows.Forms.MessageBox]::Show('Your_Desired_Message','Your_Desired_Title');
                var sb = new System.Text.StringBuilder(
                    "[System.Reflection.Assembly]::LoadWithPartialName('System.Windows.Forms'); [System.Windows.Forms.MessageBox]::Show('");
                sb.Append(MessageBody.Replace("'", "''"));
                if (Title != null)
                {
                    sb.Append("','");
                    sb.Append(Title.Replace("'", "''"));
                }

                sb.Append("'); ");
                return sb.ToString();
            }
        }

    }
}