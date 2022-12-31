using System;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using winPEAS.TaskScheduler.TaskEditor.Native;

namespace winPEAS.TaskScheduler.V2
{

    internal enum TaskEnumFlags
    {
        Hidden = 1
    }

#pragma warning disable CS0618 // Type or member is obsolete
    [ComImport, Guid("BAE54997-48B1-4CBE-9965-D6BE263EBEA4"), InterfaceType(ComInterfaceType.InterfaceIsDual), System.Security.SuppressUnmanagedCodeSecurity]
    internal interface IAction
    {
        string Id { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        TaskActionType Type { get; }
    }

    [ComImport, Guid("02820E19-7B98-4ED2-B2E8-FDCCCEFF619B"), InterfaceType(ComInterfaceType.InterfaceIsDual), System.Security.SuppressUnmanagedCodeSecurity]
    internal interface IActionCollection
    {
        int Count { get; }
        IAction this[int index] { [return: MarshalAs(UnmanagedType.Interface)] get; }
        [return: MarshalAs(UnmanagedType.Interface)]
        IEnumerator GetEnumerator();
        string XmlText { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        [return: MarshalAs(UnmanagedType.Interface)]
        IAction Create([In] TaskActionType Type);
        void Remove([In, MarshalAs(UnmanagedType.Struct)][NotNull] object index);
        void Clear();
        string Context { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
    }

    [ComImport, Guid("2A9C35DA-D357-41F4-BBC1-207AC1B1F3CB"), InterfaceType(ComInterfaceType.InterfaceIsDual), System.Security.SuppressUnmanagedCodeSecurity]
    internal interface IBootTrigger : ITrigger
    {
        new TaskTriggerType Type { get; }
        new string Id { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new IRepetitionPattern Repetition { [return: MarshalAs(UnmanagedType.Interface)] get; [param: In, MarshalAs(UnmanagedType.Interface)] set; }
        new string ExecutionTimeLimit { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new string StartBoundary { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new string EndBoundary { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new bool Enabled { get; [param: In] set; }

        string Delay { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
    }

    [ComImport, Guid("6D2FD252-75C5-4F66-90BA-2A7D8CC3039F"), InterfaceType(ComInterfaceType.InterfaceIsDual), System.Security.SuppressUnmanagedCodeSecurity]
    internal interface IComHandlerAction : IAction
    {
        new string Id { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new TaskActionType Type { get; }
        string ClassId { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        string Data { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
    }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsDual), Guid("126C5CD8-B288-41D5-8DBF-E491446ADC5C"), System.Security.SuppressUnmanagedCodeSecurity]
    internal interface IDailyTrigger : ITrigger
    {
        new TaskTriggerType Type { get; }
        new string Id { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new IRepetitionPattern Repetition { [return: MarshalAs(UnmanagedType.Interface)] get; [param: In, MarshalAs(UnmanagedType.Interface)] set; }
        new string ExecutionTimeLimit { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new string StartBoundary { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new string EndBoundary { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new bool Enabled { get; [param: In] set; }

        short DaysInterval { get; [param: In] set; }
        string RandomDelay { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
    }

    [ComImport, Guid("10F62C64-7E16-4314-A0C2-0C3683F99D40"), InterfaceType(ComInterfaceType.InterfaceIsDual), System.Security.SuppressUnmanagedCodeSecurity]
    internal interface IEmailAction : IAction
    {
        new string Id { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new TaskActionType Type { get; }

        string Server { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        string Subject { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        string To { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        string Cc { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        string Bcc { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        string ReplyTo { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        string From { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        ITaskNamedValueCollection HeaderFields { [return: MarshalAs(UnmanagedType.Interface)] get; [param: In, MarshalAs(UnmanagedType.Interface)] set; }
        string Body { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        object[] Attachments { [return: MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_VARIANT)] get; [param: In, MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_VARIANT)] set; }
    }

    [ComImport, Guid("D45B0167-9653-4EEF-B94F-0732CA7AF251"), InterfaceType(ComInterfaceType.InterfaceIsDual), System.Security.SuppressUnmanagedCodeSecurity]
    internal interface IEventTrigger : ITrigger
    {
        new TaskTriggerType Type { get; }
        new string Id { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new IRepetitionPattern Repetition { [return: MarshalAs(UnmanagedType.Interface)] get; [param: In, MarshalAs(UnmanagedType.Interface)] set; }
        new string ExecutionTimeLimit { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new string StartBoundary { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new string EndBoundary { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new bool Enabled { get; [param: In] set; }

        string Subscription { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        string Delay { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        ITaskNamedValueCollection ValueQueries { [return: MarshalAs(UnmanagedType.Interface)] get; [param: In, MarshalAs(UnmanagedType.Interface)] set; }
    }

    [ComImport, Guid("4C3D624D-FD6B-49A3-B9B7-09CB3CD3F047"), InterfaceType(ComInterfaceType.InterfaceIsDual), System.Security.SuppressUnmanagedCodeSecurity]
    internal interface IExecAction : IAction
    {
        new string Id { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new TaskActionType Type { get; }

        string Path { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        string Arguments { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        string WorkingDirectory { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
    }

    [ComImport, Guid("84594461-0053-4342-A8FD-088FABF11F32"), InterfaceType(ComInterfaceType.InterfaceIsDual), System.Security.SuppressUnmanagedCodeSecurity]
    internal interface IIdleSettings
    {
        string IdleDuration { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        string WaitTimeout { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        bool StopOnIdleEnd { get; [param: In] set; }
        bool RestartOnIdle { get; [param: In] set; }
    }

    [ComImport, Guid("D537D2B0-9FB3-4D34-9739-1FF5CE7B1EF3"), InterfaceType(ComInterfaceType.InterfaceIsDual), System.Security.SuppressUnmanagedCodeSecurity]
    internal interface IIdleTrigger : ITrigger
    {
        new TaskTriggerType Type { get; }
        new string Id { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new IRepetitionPattern Repetition { [return: MarshalAs(UnmanagedType.Interface)] get; [param: In, MarshalAs(UnmanagedType.Interface)] set; }
        new string ExecutionTimeLimit { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new string StartBoundary { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new string EndBoundary { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new bool Enabled { get; [param: In] set; }
    }

    [ComImport, Guid("72DADE38-FAE4-4B3E-BAF4-5D009AF02B1C"), InterfaceType(ComInterfaceType.InterfaceIsDual), System.Security.SuppressUnmanagedCodeSecurity]
    internal interface ILogonTrigger : ITrigger
    {
        new TaskTriggerType Type { get; }
        new string Id { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new IRepetitionPattern Repetition { [return: MarshalAs(UnmanagedType.Interface)] get; [param: In, MarshalAs(UnmanagedType.Interface)] set; }
        new string ExecutionTimeLimit { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new string StartBoundary { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new string EndBoundary { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new bool Enabled { get; [param: In] set; }

        string Delay { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        string UserId { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
    }

    [ComImport, Guid("77D025A3-90FA-43AA-B52E-CDA5499B946A"), InterfaceType(ComInterfaceType.InterfaceIsDual), System.Security.SuppressUnmanagedCodeSecurity]
    internal interface IMonthlyDOWTrigger : ITrigger
    {
        new TaskTriggerType Type { get; }
        new string Id { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new IRepetitionPattern Repetition { [return: MarshalAs(UnmanagedType.Interface)] get; [param: In, MarshalAs(UnmanagedType.Interface)] set; }
        new string ExecutionTimeLimit { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new string StartBoundary { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new string EndBoundary { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new bool Enabled { get; [param: In] set; }

        short DaysOfWeek { get; [param: In] set; }
        short WeeksOfMonth { get; [param: In] set; }
        short MonthsOfYear { get; [param: In] set; }
        bool RunOnLastWeekOfMonth { get; [param: In] set; }
        string RandomDelay { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
    }

    [ComImport, Guid("97C45EF1-6B02-4A1A-9C0E-1EBFBA1500AC"), InterfaceType(ComInterfaceType.InterfaceIsDual), System.Security.SuppressUnmanagedCodeSecurity]
    internal interface IMonthlyTrigger : ITrigger
    {
        new TaskTriggerType Type { get; }
        new string Id { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new IRepetitionPattern Repetition { [return: MarshalAs(UnmanagedType.Interface)] get; [param: In, MarshalAs(UnmanagedType.Interface)] set; }
        new string ExecutionTimeLimit { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new string StartBoundary { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new string EndBoundary { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new bool Enabled { get; [param: In] set; }

        int DaysOfMonth { get; [param: In] set; }
        short MonthsOfYear { get; [param: In] set; }
        bool RunOnLastDayOfMonth { get; [param: In] set; }
        string RandomDelay { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
    }

    [ComImport, Guid("9F7DEA84-C30B-4245-80B6-00E9F646F1B4"), InterfaceType(ComInterfaceType.InterfaceIsDual), System.Security.SuppressUnmanagedCodeSecurity]
    internal interface INetworkSettings
    {
        string Name { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        string Id { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
    }

    [ComImport, Guid("D98D51E5-C9B4-496A-A9C1-18980261CF0F"), InterfaceType(ComInterfaceType.InterfaceIsDual), System.Security.SuppressUnmanagedCodeSecurity]
    internal interface IPrincipal
    {
        string Id { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        string DisplayName { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        string UserId { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        TaskLogonType LogonType { get; set; }
        string GroupId { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        TaskRunLevel RunLevel { get; set; }
    }

    [ComImport, Guid("248919AE-E345-4A6D-8AEB-E0D3165C904E"), InterfaceType(ComInterfaceType.InterfaceIsDual), System.Security.SuppressUnmanagedCodeSecurity]
    internal interface IPrincipal2
    {
        TaskProcessTokenSidType ProcessTokenSidType { get; [param: In] set; }
        int RequiredPrivilegeCount { get; }
        string this[int index] { [return: MarshalAs(UnmanagedType.BStr)] get; }
        void AddRequiredPrivilege([In, MarshalAs(UnmanagedType.BStr)] string privilege);
    }

    [ComImport, Guid("9C86F320-DEE3-4DD1-B972-A303F26B061E"), InterfaceType(ComInterfaceType.InterfaceIsDual), System.Security.SuppressUnmanagedCodeSecurity, DefaultMember("Path")]
    internal interface IRegisteredTask
    {
        string Name { [return: MarshalAs(UnmanagedType.BStr)] get; }
        string Path { [return: MarshalAs(UnmanagedType.BStr)] get; }
        TaskState State { get; }
        bool Enabled { get; set; }
        [return: MarshalAs(UnmanagedType.Interface)]
        IRunningTask Run([In, MarshalAs(UnmanagedType.Struct)] object parameters);
        [return: MarshalAs(UnmanagedType.Interface)]
        IRunningTask RunEx([In, MarshalAs(UnmanagedType.Struct)] object parameters, [In] int flags, [In] int sessionID, [In, MarshalAs(UnmanagedType.BStr)] string user);
        [return: MarshalAs(UnmanagedType.Interface)]
        IRunningTaskCollection GetInstances(int flags);
        DateTime LastRunTime { get; }
        int LastTaskResult { get; }
        int NumberOfMissedRuns { get; }
        DateTime NextRunTime { get; }
        ITaskDefinition Definition { [return: MarshalAs(UnmanagedType.Interface)] get; }
        string Xml { [return: MarshalAs(UnmanagedType.BStr)] get; }
        [return: MarshalAs(UnmanagedType.BStr)]
        string GetSecurityDescriptor(int securityInformation);
        void SetSecurityDescriptor([In, MarshalAs(UnmanagedType.BStr)] string sddl, [In] int flags);
        void Stop(int flags);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(0x60020011)]
        void GetRunTimes([In] ref NativeMethods.SYSTEMTIME pstStart, [In] ref NativeMethods.SYSTEMTIME pstEnd, [In, Out] ref uint pCount, [In, Out] ref IntPtr pRunTimes);
    }

    [ComImport, Guid("86627EB4-42A7-41E4-A4D9-AC33A72F2D52"), InterfaceType(ComInterfaceType.InterfaceIsDual), System.Security.SuppressUnmanagedCodeSecurity]
    internal interface IRegisteredTaskCollection
    {
        int Count { get; }
        IRegisteredTask this[object index] { [return: MarshalAs(UnmanagedType.Interface)] get; }
        [return: MarshalAs(UnmanagedType.Interface)]
        IEnumerator GetEnumerator();
    }

    [ComImport, Guid("416D8B73-CB41-4EA1-805C-9BE9A5AC4A74"), InterfaceType(ComInterfaceType.InterfaceIsDual), System.Security.SuppressUnmanagedCodeSecurity]
    internal interface IRegistrationInfo
    {
        string Description { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        string Author { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        string Version { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        string Date { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        string Documentation { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        string XmlText { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        string URI { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        object SecurityDescriptor { [return: MarshalAs(UnmanagedType.Struct)] get; [param: In, MarshalAs(UnmanagedType.Struct)] set; }
        string Source { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
    }

    [ComImport, Guid("4C8FEC3A-C218-4E0C-B23D-629024DB91A2"), InterfaceType(ComInterfaceType.InterfaceIsDual), System.Security.SuppressUnmanagedCodeSecurity]
    internal interface IRegistrationTrigger : ITrigger
    {
        new TaskTriggerType Type { get; }
        new string Id { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new IRepetitionPattern Repetition { [return: MarshalAs(UnmanagedType.Interface)] get; [param: In, MarshalAs(UnmanagedType.Interface)] set; }
        new string ExecutionTimeLimit { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new string StartBoundary { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new string EndBoundary { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new bool Enabled { get; [param: In] set; }

        string Delay { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
    }

    [ComImport, Guid("7FB9ACF1-26BE-400E-85B5-294B9C75DFD6"), InterfaceType(ComInterfaceType.InterfaceIsDual), System.Security.SuppressUnmanagedCodeSecurity]
    internal interface IRepetitionPattern
    {
        string Interval { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        string Duration { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        bool StopAtDurationEnd { get; [param: In] set; }
    }

    [ComImport, Guid("653758FB-7B9A-4F1E-A471-BEEB8E9B834E"), InterfaceType(ComInterfaceType.InterfaceIsDual), System.Security.SuppressUnmanagedCodeSecurity, DefaultMember("InstanceGuid")]
    internal interface IRunningTask
    {
        string Name { [return: MarshalAs(UnmanagedType.BStr)] get; }
        string InstanceGuid { [return: MarshalAs(UnmanagedType.BStr)] get; }
        string Path { [return: MarshalAs(UnmanagedType.BStr)] get; }
        TaskState State { get; }
        string CurrentAction { [return: MarshalAs(UnmanagedType.BStr)] get; }
        void Stop();
        void Refresh();
        uint EnginePID { get; }
    }

    [ComImport, Guid("6A67614B-6828-4FEC-AA54-6D52E8F1F2DB"), InterfaceType(ComInterfaceType.InterfaceIsDual), System.Security.SuppressUnmanagedCodeSecurity]
    internal interface IRunningTaskCollection
    {
        int Count { get; }
        IRunningTask this[object index] { [return: MarshalAs(UnmanagedType.Interface)] get; }
        [return: MarshalAs(UnmanagedType.Interface)]
        IEnumerator GetEnumerator();
    }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsDual), Guid("754DA71B-4385-4475-9DD9-598294FA3641"), System.Security.SuppressUnmanagedCodeSecurity]
    internal interface ISessionStateChangeTrigger : ITrigger
    {
        new TaskTriggerType Type { get; }
        new string Id { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new IRepetitionPattern Repetition { [return: MarshalAs(UnmanagedType.Interface)] get; [param: In, MarshalAs(UnmanagedType.Interface)] set; }
        new string ExecutionTimeLimit { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new string StartBoundary { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new string EndBoundary { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new bool Enabled { get; [param: In] set; }

        string Delay { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        string UserId { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        TaskSessionStateChangeType StateChange { get; [param: In] set; }
    }

    [ComImport, Guid("505E9E68-AF89-46B8-A30F-56162A83D537"), InterfaceType(ComInterfaceType.InterfaceIsDual), System.Security.SuppressUnmanagedCodeSecurity]
    internal interface IShowMessageAction : IAction
    {
        new string Id { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new TaskActionType Type { get; }

        string Title { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        string MessageBody { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
    }

    [ComImport, Guid("F5BC8FC5-536D-4F77-B852-FBC1356FDEB6"), InterfaceType(ComInterfaceType.InterfaceIsDual), System.Security.SuppressUnmanagedCodeSecurity]
    internal interface ITaskDefinition
    {
        IRegistrationInfo RegistrationInfo { [return: MarshalAs(UnmanagedType.Interface)] get; [param: In, MarshalAs(UnmanagedType.Interface)] set; }
        ITriggerCollection Triggers { [return: MarshalAs(UnmanagedType.Interface)] get; [param: In, MarshalAs(UnmanagedType.Interface)] set; }
        ITaskSettings Settings { [return: MarshalAs(UnmanagedType.Interface)] get; [param: In, MarshalAs(UnmanagedType.Interface)] set; }
        string Data { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        IPrincipal Principal { [return: MarshalAs(UnmanagedType.Interface)] get; [param: In, MarshalAs(UnmanagedType.Interface)] set; }
        IActionCollection Actions { [return: MarshalAs(UnmanagedType.Interface)] get; [param: In, MarshalAs(UnmanagedType.Interface)] set; }
        string XmlText { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
    }

    [ComImport, Guid("8CFAC062-A080-4C15-9A88-AA7C2AF80DFC"), InterfaceType(ComInterfaceType.InterfaceIsDual), System.Security.SuppressUnmanagedCodeSecurity, DefaultMember("Path")]
    internal interface ITaskFolder
    {
        string Name { [return: MarshalAs(UnmanagedType.BStr)] get; }
        string Path { [return: MarshalAs(UnmanagedType.BStr)] get; }
        [return: MarshalAs(UnmanagedType.Interface)]
        ITaskFolder GetFolder([MarshalAs(UnmanagedType.BStr)] string Path);
        [return: MarshalAs(UnmanagedType.Interface)]
        ITaskFolderCollection GetFolders(int flags);
        [return: MarshalAs(UnmanagedType.Interface)]
        ITaskFolder CreateFolder([In, MarshalAs(UnmanagedType.BStr)] string subFolderName, [In, Optional, MarshalAs(UnmanagedType.Struct)] object sddl);
        void DeleteFolder([MarshalAs(UnmanagedType.BStr)] string subFolderName, [In] int flags);
        [return: MarshalAs(UnmanagedType.Interface)]
        IRegisteredTask GetTask([MarshalAs(UnmanagedType.BStr)][NotNull] string Path);
        [return: MarshalAs(UnmanagedType.Interface)]
        IRegisteredTaskCollection GetTasks(int flags);
        void DeleteTask([In, MarshalAs(UnmanagedType.BStr)][NotNull] string Name, [In] int flags);
        [return: MarshalAs(UnmanagedType.Interface)]
        IRegisteredTask RegisterTask([In, MarshalAs(UnmanagedType.BStr)][NotNull] string Path, [In, MarshalAs(UnmanagedType.BStr)][NotNull] string XmlText, [In] int flags, [In, MarshalAs(UnmanagedType.Struct)] object UserId, [In, MarshalAs(UnmanagedType.Struct)] object password, [In] TaskLogonType LogonType, [In, Optional, MarshalAs(UnmanagedType.Struct)] object sddl);
        [return: MarshalAs(UnmanagedType.Interface)]
        IRegisteredTask RegisterTaskDefinition([In, MarshalAs(UnmanagedType.BStr)][NotNull] string Path, [In, MarshalAs(UnmanagedType.Interface)][NotNull] ITaskDefinition pDefinition, [In] int flags, [In, MarshalAs(UnmanagedType.Struct)] object UserId, [In, MarshalAs(UnmanagedType.Struct)] object password, [In] TaskLogonType LogonType, [In, Optional, MarshalAs(UnmanagedType.Struct)] object sddl);
        [return: MarshalAs(UnmanagedType.BStr)]
        string GetSecurityDescriptor(int securityInformation);
        void SetSecurityDescriptor([In, MarshalAs(UnmanagedType.BStr)] string sddl, [In] int flags);
    }

    [ComImport, Guid("79184A66-8664-423F-97F1-637356A5D812"), InterfaceType(ComInterfaceType.InterfaceIsDual), System.Security.SuppressUnmanagedCodeSecurity]
    internal interface ITaskFolderCollection
    {
        int Count { get; }
        ITaskFolder this[object index] { [return: MarshalAs(UnmanagedType.Interface)] get; }
        [return: MarshalAs(UnmanagedType.Interface)]
        IEnumerator GetEnumerator();
    }

    [ComImport, Guid("B4EF826B-63C3-46E4-A504-EF69E4F7EA4D"), InterfaceType(ComInterfaceType.InterfaceIsDual), System.Security.SuppressUnmanagedCodeSecurity]
    internal interface ITaskNamedValueCollection
    {
        int Count { get; }
        ITaskNamedValuePair this[int index] { [return: MarshalAs(UnmanagedType.Interface)] get; }
        [return: MarshalAs(UnmanagedType.Interface)]
        IEnumerator GetEnumerator();
        [return: MarshalAs(UnmanagedType.Interface)]
        ITaskNamedValuePair Create([In, MarshalAs(UnmanagedType.BStr)][NotNull] string Name, [In, MarshalAs(UnmanagedType.BStr)] string Value);
        void Remove([In] int index);
        void Clear();
    }

    [ComImport, Guid("39038068-2B46-4AFD-8662-7BB6F868D221"), InterfaceType(ComInterfaceType.InterfaceIsDual), System.Security.SuppressUnmanagedCodeSecurity, DefaultMember("Name")]
    internal interface ITaskNamedValuePair
    {
        string Name { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        string Value { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
    }

    [ComImport, DefaultMember("TargetServer"), Guid("2FABA4C7-4DA9-4013-9697-20CC3FD40F85"), System.Security.SuppressUnmanagedCodeSecurity, CoClass(typeof(TaskSchedulerClass))]
    internal interface ITaskService
    {
        [return: MarshalAs(UnmanagedType.Interface)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(1)]
        ITaskFolder GetFolder([In, MarshalAs(UnmanagedType.BStr)][NotNull] string Path);
        [return: MarshalAs(UnmanagedType.Interface)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(2)]
        IRunningTaskCollection GetRunningTasks(int flags);
        [return: MarshalAs(UnmanagedType.Interface)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(3)]
        ITaskDefinition NewTask([In] uint flags);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(4)]
        void Connect([In, Optional, MarshalAs(UnmanagedType.Struct)] object serverName, [In, Optional, MarshalAs(UnmanagedType.Struct)] object user, [In, Optional, MarshalAs(UnmanagedType.Struct)] object domain, [In, Optional, MarshalAs(UnmanagedType.Struct)] object password);
        [DispId(5)]
        bool Connected { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(5)] get; }
        [DispId(0)]
        string TargetServer { [return: MarshalAs(UnmanagedType.BStr)][MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(0)] get; }
        [DispId(6)]
        string ConnectedUser { [return: MarshalAs(UnmanagedType.BStr)][MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(6)] get; }
        [DispId(7)]
        string ConnectedDomain { [return: MarshalAs(UnmanagedType.BStr)][MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(7)] get; }
        [DispId(8)]
        uint HighestVersion { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(8)] get; }
    }

    [ComImport, DefaultMember("TargetServer"), Guid("0F87369F-A4E5-4CFC-BD3E-73E6154572DD"), ClassInterface((short)0), System.Security.SuppressUnmanagedCodeSecurity]
    internal class TaskSchedulerClass
    {
    }

    [ComImport, Guid("8FD4711D-2D02-4C8C-87E3-EFF699DE127E"), InterfaceType(ComInterfaceType.InterfaceIsDual), System.Security.SuppressUnmanagedCodeSecurity]
    internal interface ITaskSettings
    {
        bool AllowDemandStart { get; [param: In] set; }
        string RestartInterval { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        int RestartCount { get; [param: In] set; }
        TaskInstancesPolicy MultipleInstances { get; [param: In] set; }
        bool StopIfGoingOnBatteries { get; [param: In] set; }
        bool DisallowStartIfOnBatteries { get; [param: In] set; }
        bool AllowHardTerminate { get; [param: In] set; }
        bool StartWhenAvailable { get; [param: In] set; }
        string XmlText { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        bool RunOnlyIfNetworkAvailable { get; [param: In] set; }
        string ExecutionTimeLimit { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        bool Enabled { get; [param: In] set; }
        string DeleteExpiredTaskAfter { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        int Priority { get; [param: In] set; }
        TaskCompatibility Compatibility { get; [param: In] set; }
        bool Hidden { get; [param: In] set; }
        IIdleSettings IdleSettings { [return: MarshalAs(UnmanagedType.Interface)] get; [param: In, MarshalAs(UnmanagedType.Interface)] set; }
        bool RunOnlyIfIdle { get; [param: In] set; }
        bool WakeToRun { get; [param: In] set; }
        INetworkSettings NetworkSettings { [return: MarshalAs(UnmanagedType.Interface)] get; [param: In, MarshalAs(UnmanagedType.Interface)] set; }
    }

    [ComImport, Guid("2C05C3F0-6EED-4c05-A15F-ED7D7A98A369"), InterfaceType(ComInterfaceType.InterfaceIsDual), System.Security.SuppressUnmanagedCodeSecurity]
    internal interface ITaskSettings2
    {
        bool DisallowStartOnRemoteAppSession { get; [param: In] set; }
        bool UseUnifiedSchedulingEngine { get; [param: In] set; }
    }

    [ComImport, Guid("0AD9D0D7-0C7F-4EBB-9A5F-D1C648DCA528"), InterfaceType(ComInterfaceType.InterfaceIsDual), System.Security.SuppressUnmanagedCodeSecurity]
    internal interface ITaskSettings3 : ITaskSettings
    {
        new bool AllowDemandStart { get; [param: In] set; }
        new string RestartInterval { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new int RestartCount { get; [param: In] set; }
        new TaskInstancesPolicy MultipleInstances { get; [param: In] set; }
        new bool StopIfGoingOnBatteries { get; [param: In] set; }
        new bool DisallowStartIfOnBatteries { get; [param: In] set; }
        new bool AllowHardTerminate { get; [param: In] set; }
        new bool StartWhenAvailable { get; [param: In] set; }
        new string XmlText { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new bool RunOnlyIfNetworkAvailable { get; [param: In] set; }
        new string ExecutionTimeLimit { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new bool Enabled { get; [param: In] set; }
        new string DeleteExpiredTaskAfter { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new int Priority { get; [param: In] set; }
        new TaskCompatibility Compatibility { get; [param: In] set; }
        new bool Hidden { get; [param: In] set; }
        new IIdleSettings IdleSettings { [return: MarshalAs(UnmanagedType.Interface)] get; [param: In, MarshalAs(UnmanagedType.Interface)] set; }
        new bool RunOnlyIfIdle { get; [param: In] set; }
        new bool WakeToRun { get; [param: In] set; }
        new INetworkSettings NetworkSettings { [return: MarshalAs(UnmanagedType.Interface)] get; [param: In, MarshalAs(UnmanagedType.Interface)] set; }

        bool DisallowStartOnRemoteAppSession { get; [param: In] set; }
        bool UseUnifiedSchedulingEngine { get; [param: In] set; }
        IMaintenanceSettings MaintenanceSettings { [return: MarshalAs(UnmanagedType.Interface)] get; [param: In, MarshalAs(UnmanagedType.Interface)] set; }
        [return: MarshalAs(UnmanagedType.Interface)]
        IMaintenanceSettings CreateMaintenanceSettings();
        bool Volatile { get; [param: In] set; }
    }

    [ComImport, Guid("A6024FA8-9652-4ADB-A6BF-5CFCD877A7BA"), InterfaceType(ComInterfaceType.InterfaceIsDual), System.Security.SuppressUnmanagedCodeSecurity]
    internal interface IMaintenanceSettings
    {
        string Period { [param: In, MarshalAs(UnmanagedType.BStr)] set; [return: MarshalAs(UnmanagedType.BStr)] get; }
        string Deadline { [param: In, MarshalAs(UnmanagedType.BStr)] set; [return: MarshalAs(UnmanagedType.BStr)] get; }
        bool Exclusive { [param: In] set; get; }
    }

    [ComImport, Guid("3E4C9351-D966-4B8B-BB87-CEBA68BB0107"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), System.Security.SuppressUnmanagedCodeSecurity]
    internal interface ITaskVariables
    {
        [return: MarshalAs(UnmanagedType.BStr)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        string GetInput();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetOutput([In, MarshalAs(UnmanagedType.BStr)] string input);
        [return: MarshalAs(UnmanagedType.BStr)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        string GetContext();
    }

    [ComImport, Guid("B45747E0-EBA7-4276-9F29-85C5BB300006"), InterfaceType(ComInterfaceType.InterfaceIsDual), System.Security.SuppressUnmanagedCodeSecurity]
    internal interface ITimeTrigger : ITrigger
    {
        new TaskTriggerType Type { get; }
        new string Id { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new IRepetitionPattern Repetition { [return: MarshalAs(UnmanagedType.Interface)] get; [param: In, MarshalAs(UnmanagedType.Interface)] set; }
        new string ExecutionTimeLimit { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new string StartBoundary { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new string EndBoundary { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new bool Enabled { get; [param: In] set; }

        string RandomDelay { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
    }

    [ComImport, Guid("09941815-EA89-4B5B-89E0-2A773801FAC3"), InterfaceType(ComInterfaceType.InterfaceIsDual), System.Security.SuppressUnmanagedCodeSecurity]
    internal interface ITrigger
    {
        TaskTriggerType Type { get; }
        string Id { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        IRepetitionPattern Repetition { [return: MarshalAs(UnmanagedType.Interface)] get; [param: In, MarshalAs(UnmanagedType.Interface)] set; }
        string ExecutionTimeLimit { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        string StartBoundary { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        string EndBoundary { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        bool Enabled { get; [param: In] set; }
    }

    [ComImport, Guid("85DF5081-1B24-4F32-878A-D9D14DF4CB77"), InterfaceType(ComInterfaceType.InterfaceIsDual), System.Security.SuppressUnmanagedCodeSecurity]
    internal interface ITriggerCollection
    {
        int Count { get; }
        ITrigger this[int index] { [return: MarshalAs(UnmanagedType.Interface)] get; }
        [return: MarshalAs(UnmanagedType.Interface)]
        IEnumerator GetEnumerator();
        [return: MarshalAs(UnmanagedType.Interface)]
        ITrigger Create([In] TaskTriggerType Type);
        void Remove([In, MarshalAs(UnmanagedType.Struct)] object index);
        void Clear();
    }

    [ComImport, Guid("5038FC98-82FF-436D-8728-A512A57C9DC1"), InterfaceType(ComInterfaceType.InterfaceIsDual), System.Security.SuppressUnmanagedCodeSecurity]
    internal interface IWeeklyTrigger : ITrigger
    {
        new TaskTriggerType Type { get; }
        new string Id { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new IRepetitionPattern Repetition { [return: MarshalAs(UnmanagedType.Interface)] get; [param: In, MarshalAs(UnmanagedType.Interface)] set; }
        new string ExecutionTimeLimit { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new string StartBoundary { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new string EndBoundary { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
        new bool Enabled { get; [param: In] set; }

        short DaysOfWeek { get; [param: In] set; }
        short WeeksInterval { get; [param: In] set; }
        string RandomDelay { [return: MarshalAs(UnmanagedType.BStr)] get; [param: In, MarshalAs(UnmanagedType.BStr)] set; }
    }
}
