using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;

namespace winPEAS.TaskScheduler
{
    /// <summary>
    /// Changes to tasks and the engine that cause events.
    /// </summary>
    public enum StandardTaskEventId
    {
        /// <summary>Task Scheduler started an instance of a task for a user.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd348558(v=ws.10).aspx">Event ID 100</a> on TechNet.</remarks>
        JobStart = 100,
        /// <summary>Task Scheduler failed to start a task for a user.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd315710(v=ws.10).aspx">Event ID 101</a> on TechNet.</remarks>
        JobStartFailed = 101,
        /// <summary>Task Scheduler successfully finished an instance of a task for a user.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd348530(v=ws.10).aspx">Event ID 102</a> on TechNet.</remarks>
        JobSuccess = 102,
        /// <summary>Task Scheduler failed to start an instance of a task for a user.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd315643(v=ws.10).aspx">Event ID 103</a> on TechNet.</remarks>
        JobFailure = 103,
        /// <summary>Task Scheduler failed to log on the user.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/cc727217(v=ws.10).aspx">Event ID 104</a> on TechNet.</remarks>
        LogonFailure = 104,
        /// <summary>Task Scheduler failed to impersonate a user.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd348541(v=ws.10).aspx">Event ID 105</a> on TechNet.</remarks>
        ImpersonationFailure = 105,
        /// <summary>The a user registered the Task Scheduler a task.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd363640(v=ws.10).aspx">Event ID 106</a> on TechNet.</remarks>
        JobRegistered = 106,
        /// <summary>Task Scheduler launched an instance of a task due to a time trigger.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd348590(v=ws.10).aspx">Event ID 107</a> on TechNet.</remarks>
        TimeTrigger = 107,
        /// <summary>Task Scheduler launched an instance of a task due to an event trigger.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/cc727031(v=ws.10).aspx">Event ID 108</a> on TechNet.</remarks>
        EventTrigger = 108,
        /// <summary>Task Scheduler launched an instance of a task due to a registration trigger.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd363702(v=ws.10).aspx">Event ID 109</a> on TechNet.</remarks>
        ImmediateTrigger = 109,
        /// <summary>Task Scheduler launched an instance of a task for a user.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd363721(v=ws.10).aspx">Event ID 110</a> on TechNet.</remarks>
        Run = 110,
        /// <summary>Task Scheduler terminated an instance of a task due to exceeding the time allocated for execution, as configured in the task definition.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd315549(v=ws.10).aspx">Event ID 111</a> on TechNet.</remarks>
        JobTermination = 111,
        /// <summary>Task Scheduler could not start a task because the network was unavailable. Ensure the computer is connected to the required network as specified in the task.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd363779(v=ws.10).aspx">Event ID 112</a> on TechNet.</remarks>
        JobNoStartWithoutNetwork = 112,
        /// <summary>The Task Scheduler registered the a task, but not all the specified triggers will start the task. Ensure all the task triggers are valid.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd315656(v=ws.10).aspx">Event ID 113</a> on TechNet.</remarks>
        TaskRegisteredWithoutSomeTriggers = 113,
        /// <summary>Task Scheduler could not launch a task as scheduled. Instance is started now as required by the configuration option to start the task when available, if the scheduled time is missed.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/cc775066(v=ws.10).aspx">Event ID 114</a> on TechNet.</remarks>
        MissedTaskLaunched = 114,
        /// <summary>Task Scheduler failed to roll back a transaction when updating or deleting a task.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd315711(v=ws.10).aspx">Event ID 115</a> on TechNet.</remarks>
        TransactionRollbackFailure = 115,
        /// <summary>Task Scheduler saved the configuration for a task, but the credentials used to run the task could not be stored.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd363682(v=ws.10).aspx">Event ID 116</a> on TechNet.</remarks>
        TaskRegisteredWithoutCredentials = 116,
        /// <summary>Task Scheduler launched an instance of a task due to an idle condition.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd315755(v=ws.10).aspx">Event ID 117</a> on TechNet.</remarks>
        IdleTrigger = 117,
        /// <summary>Task Scheduler launched an instance of a task due to system startup.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/cc775107(v=ws.10).aspx">Event ID 118</a> on TechNet.</remarks>
        BootTrigger = 118,
        /// <summary>Task Scheduler launched an instance of a task due to a user logon.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd315498(v=ws.10).aspx">Event ID 119</a> on TechNet.</remarks>
        LogonTrigger = 119,
        /// <summary>Task Scheduler launched an instance of a task due to a user connecting to the console.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd315609(v=ws.10).aspx">Event ID 120</a> on TechNet.</remarks>
        ConsoleConnectTrigger = 120,
        /// <summary>Task Scheduler launched an instance of a task due to a user disconnecting from the console.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd363748(v=ws.10).aspx">Event ID 121</a> on TechNet.</remarks>
        ConsoleDisconnectTrigger = 121,
        /// <summary>Task Scheduler launched an instance of a task due to a user remotely connecting.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/cc774994(v=ws.10).aspx">Event ID 122</a> on TechNet.</remarks>
        RemoteConnectTrigger = 122,
        /// <summary>Task Scheduler launched an instance of a task due to a user remotely disconnecting.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/cc775034(v=ws.10).aspx">Event ID 123</a> on TechNet.</remarks>
        RemoteDisconnectTrigger = 123,
        /// <summary>Task Scheduler launched an instance of a task due to a user locking the computer.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd315499(v=ws.10).aspx">Event ID 124</a> on TechNet.</remarks>
        SessionLockTrigger = 124,
        /// <summary>Task Scheduler launched an instance of a task due to a user unlocking the computer.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/cc727048(v=ws.10).aspx">Event ID 125</a> on TechNet.</remarks>
        SessionUnlockTrigger = 125,
        /// <summary>Task Scheduler failed to execute a task. Task Scheduler is attempting to restart the task.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd363647(v=ws.10).aspx">Event ID 126</a> on TechNet.</remarks>
        FailedTaskRestart = 126,
        /// <summary>Task Scheduler failed to execute a task due to a shutdown race condition. Task Scheduler is attempting to restart the task.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/cc726976(v=ws.10).aspx">Event ID 127</a> on TechNet.</remarks>
        RejectedTaskRestart = 127,
        /// <summary>Task Scheduler did not launch a task because the current time exceeds the configured task end time.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd315638(v=ws.10).aspx">Event ID 128</a> on TechNet.</remarks>
        IgnoredTaskStart = 128,
        /// <summary>Task Scheduler launched an instance of a task in a new process.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd315544(v=ws.10).aspx">Event ID 129</a> on TechNet.</remarks>
        CreatedTaskProcess = 129,
        /// <summary>The Task Scheduler service failed to start a task due to the service being busy.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd315521(v=ws.10).aspx">Event ID 130</a> on TechNet.</remarks>
        TaskNotRunServiceBusy = 130,
        /// <summary>Task Scheduler failed to start a task because the number of tasks in the task queue exceeds the quota currently configured.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd363733(v=ws.10).aspx">Event ID 131</a> on TechNet.</remarks>
        TaskNotStartedTaskQueueQuotaExceeded = 131,
        /// <summary>The Task Scheduler task launching queue quota is approaching its preset limit of tasks currently configured.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd363705(v=ws.10).aspx">Event ID 132</a> on TechNet.</remarks>
        TaskQueueQuotaApproaching = 132,
        /// <summary>Task Scheduler failed to start a task in the task engine for a user.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd315645(v=ws.10).aspx">Event ID 133</a> on TechNet.</remarks>
        TaskNotStartedEngineQuotaExceeded = 133,
        /// <summary>Task Engine for a user is approaching its preset limit of tasks.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/cc774865(v=ws.10).aspx">Event ID 134</a> on TechNet.</remarks>
        EngineQuotaApproaching = 134,
        /// <summary>Task Scheduler did not launch a task because launch condition not met, machine not idle.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd363654(v=ws.10).aspx">Event ID 135</a> on TechNet.</remarks>
        NotStartedWithoutIdle = 135,
        /// <summary>A user updated Task Scheduler a task</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd363717(v=ws.10).aspx">Event ID 140</a> on TechNet.</remarks>
        TaskUpdated = 140,
        /// <summary>A user deleted Task Scheduler a task</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd348535(v=ws.10).aspx">Event ID 141</a> on TechNet.</remarks>
        TaskDeleted = 141,
        /// <summary>A user disabled Task Scheduler a task</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd363727(v=ws.10).aspx">Event ID 142</a> on TechNet.</remarks>
        TaskDisabled = 142,
        /// <summary>Task Scheduler woke up the computer to run a task.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/cc775065(v=ws.10).aspx">Event ID 145</a> on TechNet.</remarks>
        TaskStartedOnComputerWakeup = 145,
        /// <summary>Task Scheduler failed to subscribe the event trigger for a task.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd315495(v=ws.10).aspx">Event ID 150</a> on TechNet.</remarks>
        TaskEventSubscriptionFailed = 150,
        /// <summary>Task Scheduler launched an action in an instance of a task.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/cc775088(v=ws.10).aspx">Event ID 200</a> on TechNet.</remarks>
        ActionStart = 200,
        /// <summary>Task Scheduler successfully completed a task instance and action.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd348568(v=ws.10).aspx">Event ID 201</a> on TechNet.</remarks>
        ActionSuccess = 201,
        /// <summary>Task Scheduler failed to complete an instance of a task with an action.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd315592(v=ws.10).aspx">Event ID 202</a> on TechNet.</remarks>
        ActionFailure = 202,
        /// <summary>Task Scheduler failed to launch an action in a task instance.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd363729(v=ws.10).aspx">Event ID 203</a> on TechNet.</remarks>
        ActionLaunchFailure = 203,
        /// <summary>Task Scheduler failed to retrieve the event triggering values for a task . The event will be ignored.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd348579(v=ws.10).aspx">Event ID 204</a> on TechNet.</remarks>
        EventRenderFailed = 204,
        /// <summary>Task Scheduler failed to match the pattern of events for a task. The events will be ignored.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd363661(v=ws.10).aspx">Event ID 205</a> on TechNet.</remarks>
        EventAggregateFailed = 205,
        /// <summary>Task Scheduler is shutting down the a task engine.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd348583(v=ws.10).aspx">Event ID 301</a> on TechNet.</remarks>
        SessionExit = 301,
        /// <summary>Task Scheduler is shutting down the a task engine due to an error. </summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/cc727080(v=ws.10).aspx">Event ID 303</a> on TechNet.</remarks>
        SessionError = 303,
        /// <summary>Task Scheduler sent a task to a task engine.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd315534(v=ws.10).aspx">Event ID 304</a> on TechNet.</remarks>
        SessionSentJob = 304,
        /// <summary>Task Scheduler did not send a task to a task engine.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd363776(v=ws.10).aspx">Event ID 305</a> on TechNet.</remarks>
        SessionSentJobFailed = 305,
        /// <summary>For a Task Scheduler task engine, the thread pool failed to process the message.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd315525(v=ws.10).aspx">Event ID 306</a> on TechNet.</remarks>
        SessionFailedToProcessMessage = 306,
        /// <summary>The Task Scheduler service failed to connect to a task engine process.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd315605(v=ws.10).aspx">Event ID 307</a> on TechNet.</remarks>
        SessionManagerConnectFailed = 307,
        /// <summary>Task Scheduler connected to a task engine process.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd315746(v=ws.10).aspx">Event ID 308</a> on TechNet.</remarks>
        SessionConnected = 308,
        /// <summary>There are Task Scheduler tasks orphaned during a task engine shutdown.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd348580(v=ws.10).aspx">Event ID 309</a> on TechNet.</remarks>
        SessionJobsOrphaned = 309,
        /// <summary>Task Scheduler started a task engine process.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd315668(v=ws.10).aspx">Event ID 310</a> on TechNet.</remarks>
        SessionProcessStarted = 310,
        /// <summary>Task Scheduler failed to start a task engine process due to an error.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd363691(v=ws.10).aspx">Event ID 311</a> on TechNet.</remarks>
        SessionProcessLaunchFailed = 311,
        /// <summary>Task Scheduler created the Win32 job object for a task engine.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/cc775071(v=ws.10).aspx">Event ID 312</a> on TechNet.</remarks>
        SessionWin32ObjectCreated = 312,
        /// <summary>The Task Scheduler channel is ready to send and receive messages.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd363685(v=ws.10).aspx">Event ID 313</a> on TechNet.</remarks>
        SessionChannelReady = 313,
        /// <summary>Task Scheduler has no tasks running for a task engine, and the idle timer has started.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd315517(v=ws.10).aspx">Event ID 314</a> on TechNet.</remarks>
        SessionIdle = 314,
        /// <summary>A task engine process failed to connect to the Task Scheduler service.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd363798(v=ws.10).aspx">Event ID 315</a> on TechNet.</remarks>
        SessionProcessConnectFailed = 315,
        /// <summary>A task engine failed to send a message to the Task Scheduler service.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd315748(v=ws.10).aspx">Event ID 316</a> on TechNet.</remarks>
        SessionMessageSendFailed = 316,
        /// <summary>Task Scheduler started a task engine process.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd315663(v=ws.10).aspx">Event ID 317</a> on TechNet.</remarks>
        SessionProcessMainStarted = 317,
        /// <summary>Task Scheduler shut down a task engine process.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/cc727204(v=ws.10).aspx">Event ID 318</a> on TechNet.</remarks>
        SessionProcessMainShutdown = 318,
        /// <summary>A task engine received a message from the Task Scheduler service requesting to launch a task.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd363712(v=ws.10).aspx">Event ID 319</a> on TechNet.</remarks>
        SessionProcessReceivedStartJob = 319,
        /// <summary>A task engine received a message from the Task Scheduler service requesting to stop a task instance.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/cc774993(v=ws.10).aspx">Event ID 320</a> on TechNet.</remarks>
        SessionProcessReceivedStopJob = 320,
        /// <summary>Task Scheduler did not launch a task because an instance of the same task is already running.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd315618(v=ws.10).aspx">Event ID 322</a> on TechNet.</remarks>
        NewInstanceIgnored = 322,
        /// <summary>Task Scheduler stopped an instance of a task in order to launch a new instance.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd315542(v=ws.10).aspx">Event ID 323</a> on TechNet.</remarks>
        RunningInstanceStopped = 323,
        /// <summary>Task Scheduler queued an instance of a task and will launch it as soon as another instance completes.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd363759(v=ws.10).aspx">Event ID 324</a> on TechNet.</remarks>
        NewInstanceQueued = 324,
        /// <summary>Task Scheduler queued an instance of a task that will launch immediately.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd363654(v=ws.10).aspx">Event ID 325</a> on TechNet.</remarks>
        InstanceQueued = 325,
        /// <summary>Task Scheduler did not launch a task because the computer is running on batteries. If launching the task on batteries is required, change the respective flag in the task configuration.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd315644(v=ws.10).aspx">Event ID 326</a> on TechNet.</remarks>
        NoStartOnBatteries = 326,
        /// <summary>Task Scheduler stopped an instance of a task because the computer is switching to battery power.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd348611(v=ws.10).aspx">Event ID 327</a> on TechNet.</remarks>
        StoppingOnBatteries = 327,
        /// <summary>Task Scheduler stopped an instance of a task because the computer is no longer idle.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd363732(v=ws.10).aspx">Event ID 328</a> on TechNet.</remarks>
        StoppingOffIdle = 328,
        /// <summary>Task Scheduler stopped an instance of a task because the task timed out.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd348536(v=ws.10).aspx">Event ID 329</a> on TechNet.</remarks>
        StoppingOnTimeout = 329,
        /// <summary>Task Scheduler stopped an instance of a task as request by a user .</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd348610(v=ws.10).aspx">Event ID 330</a> on TechNet.</remarks>
        StoppingOnRequest = 330,
        /// <summary>Task Scheduler will continue to execute an instance of a task even after the designated timeout, due to a failure to create the timeout mechanism.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd315673(v=ws.10).aspx">Event ID 331</a> on TechNet.</remarks>
        TimeoutWontWork = 331,
        /// <summary>Task Scheduler did not launch a task because a user was not logged on when the launching conditions were met. Ensure the user is logged on or change the task definition to allow the task to launch when the user is logged off.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd315612(v=ws.10).aspx">Event ID 332</a> on TechNet.</remarks>
        NoStartUserNotLoggedOn = 332,
        /// <summary>The Task Scheduler service has started.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd315626(v=ws.10).aspx">Event ID 400</a> on TechNet.</remarks>
        ScheduleServiceStart = 400,
        /// <summary>The Task Scheduler service failed to start due to an error.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd363598(v=ws.10).aspx">Event ID 401</a> on TechNet.</remarks>
        ScheduleServiceStartFailed = 401,
        /// <summary>Task Scheduler service is shutting down.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd363724(v=ws.10).aspx">Event ID 402</a> on TechNet.</remarks>
        ScheduleServiceStop = 402,
        /// <summary>The Task Scheduler service has encountered an error.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd315744(v=ws.10).aspx">Event ID 403</a> on TechNet.</remarks>
        ScheduleServiceError = 403,
        /// <summary>The Task Scheduler service has encountered an RPC initialization error.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd363713(v=ws.10).aspx">Event ID 404</a> on TechNet.</remarks>
        ScheduleServiceRpcInitError = 404,
        /// <summary>The Task Scheduler service has failed to initialize COM.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd363728(v=ws.10).aspx">Event ID 405</a> on TechNet.</remarks>
        ScheduleServiceComInitError = 405,
        /// <summary>The Task Scheduler service failed to initialize the credentials store.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd315727(v=ws.10).aspx">Event ID 406</a> on TechNet.</remarks>
        ScheduleServiceCredStoreInitError = 406,
        /// <summary>Task Scheduler service failed to initialize LSA.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd315580(v=ws.10).aspx">Event ID 407</a> on TechNet.</remarks>
        ScheduleServiceLsaInitError = 407,
        /// <summary>Task Scheduler service failed to initialize idle state detection module. Idle tasks may not be started as required.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd315523(v=ws.10).aspx">Event ID 408</a> on TechNet.</remarks>
        ScheduleServiceIdleServiceInitError = 408,
        /// <summary>The Task Scheduler service failed to initialize a time change notification. System time updates may not be picked by the service and task schedules may not be updated.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/cc774954(v=ws.10).aspx">Event ID 409</a> on TechNet.</remarks>
        ScheduleServiceTimeChangeInitError = 409,
        /// <summary>Task Scheduler service received a time system change notification.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd315633(v=ws.10).aspx">Event ID 411</a> on TechNet.</remarks>
        ScheduleServiceTimeChangeSignaled = 411,
        /// <summary>Task Scheduler service failed to launch tasks triggered by computer startup. Restart the Task Scheduler service.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd315637(v=ws.10).aspx">Event ID 412</a> on TechNet.</remarks>
        ScheduleServiceRunBootJobsFailed = 412,
        /// <summary>Task Scheduler service started Task Compatibility module.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/cc727087(v=ws.10).aspx">Event ID 700</a> on TechNet.</remarks>
        CompatStart = 700,
        /// <summary>Task Scheduler service failed to start Task Compatibility module. Tasks may not be able to register on previous Window versions.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd315530(v=ws.10).aspx">Event ID 701</a> on TechNet.</remarks>
        CompatStartFailed = 701,
        /// <summary>Task Scheduler failed to initialize the RPC server for starting the Task Compatibility module. Tasks may not be able to register on previous Window versions.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd315721(v=ws.10).aspx">Event ID 702</a> on TechNet.</remarks>
        CompatStartRpcFailed = 702,
        /// <summary>Task Scheduler failed to initialize Net Schedule API for starting the Task Compatibility module. Tasks may not be able to register on previous Window versions.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd348604(v=ws.10).aspx">Event ID 703</a> on TechNet.</remarks>
        CompatStartNetscheduleFailed = 703,
        /// <summary>Task Scheduler failed to initialize LSA for starting the Task Compatibility module. Tasks may not be able to register on previous Window versions.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd315572(v=ws.10).aspx">Event ID 704</a> on TechNet.</remarks>
        CompatStartLsaFailed = 704,
        /// <summary>Task Scheduler failed to start directory monitoring for the Task Compatibility module.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/cc727147(v=ws.10).aspx">Event ID 705</a> on TechNet.</remarks>
        CompatDirectoryMonitorFailed = 705,
        /// <summary>Task Compatibility module failed to update a task to the required status.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd315682(v=ws.10).aspx">Event ID 706</a> on TechNet.</remarks>
        CompatTaskStatusUpdateFailed = 706,
        /// <summary>Task Compatibility module failed to delete a task.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd348545(v=ws.10).aspx">Event ID 707</a> on TechNet.</remarks>
        CompatTaskDeleteFailed = 707,
        /// <summary>Task Compatibility module failed to set a security descriptor for a task.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd315719(v=ws.10).aspx">Event ID 708</a> on TechNet.</remarks>
        CompatTaskSetSdFailed = 708,
        /// <summary>Task Compatibility module failed to update a task.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd363614(v=ws.10).aspx">Event ID 709</a> on TechNet.</remarks>
        CompatTaskUpdateFailed = 709,
        /// <summary>Task Compatibility module failed to upgrade existing tasks. Upgrade will be attempted again next time 'Task Scheduler' service starts.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd363608(v=ws.10).aspx">Event ID 710</a> on TechNet.</remarks>
        CompatUpgradeStartFailed = 710,
        /// <summary>Task Compatibility module failed to upgrade NetSchedule account.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd348554(v=ws.10).aspx">Event ID 711</a> on TechNet.</remarks>
        CompatUpgradeNsAccountFailed = 711,
        /// <summary>Task Compatibility module failed to read existing store to upgrade tasks.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd315519(v=ws.10).aspx">Event ID 712</a> on TechNet.</remarks>
        CompatUpgradeStoreEnumFailed = 712,
        /// <summary>Task Compatibility module failed to load a task for upgrade.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd315728(v=ws.10).aspx">Event ID 713</a> on TechNet.</remarks>
        CompatUpgradeTaskLoadFailed = 713,
        /// <summary>Task Compatibility module failed to register a task for upgrade.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd363701(v=ws.10).aspx">Event ID 714</a> on TechNet.</remarks>
        CompatUpgradeTaskRegistrationFailed = 714,
        /// <summary>Task Compatibility module failed to delete LSA store for upgrade.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd348581(v=ws.10).aspx">Event ID 715</a> on TechNet.</remarks>
        CompatUpgradeLsaCleanupFailed = 715,
        /// <summary>Task Compatibility module failed to upgrade existing scheduled tasks.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd315624(v=ws.10).aspx">Event ID 716</a> on TechNet.</remarks>
        CompatUpgradeFailed = 716,
        /// <summary>Task Compatibility module failed to determine if upgrade is needed.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd315731(v=ws.10).aspx">Event ID 717</a> on TechNet.</remarks>
        CompatUpgradeNeedNotDetermined = 717,
        /// <summary>Task scheduler was unable to upgrade the credential store from the Beta 2 version. You may need to re-register any tasks that require passwords.</summary>
        /// <remarks>For detailed information, see the documentation for <a href="https://technet.microsoft.com/en-us/library/dd348576(v=ws.10).aspx">Event ID 718</a> on TechNet.</remarks>
        VistaBeta2CredstoreUpgradeFailed = 718,
        /// <summary>A unknown value.</summary>
        Unknown = -2
    }

    /// <summary>
    /// Historical event information for a task. This class wraps and extends the <see cref="EventRecord"/> class.
    /// </summary>
    /// <remarks>
    /// For events on systems prior to Windows Vista, this class will only have information for the TaskPath, TimeCreated and EventId properties.
    /// </remarks>
    [PublicAPI]
    public sealed class TaskEvent : IComparable<TaskEvent>
    {
        internal TaskEvent([NotNull] EventRecord rec)
        {
            EventId = rec.Id;
            EventRecord = rec;
            Version = rec.Version;
            TaskCategory = rec.TaskDisplayName;
            OpCode = rec.OpcodeDisplayName;
            TimeCreated = rec.TimeCreated;
            RecordId = rec.RecordId;
            ActivityId = rec.ActivityId;
            Level = rec.LevelDisplayName;
            UserId = rec.UserId;
            ProcessId = rec.ProcessId;
            TaskPath = rec.Properties.Count > 0 ? rec.Properties[0]?.Value?.ToString() : null;
            DataValues = new EventDataValues(rec as EventLogRecord);
        }

        internal TaskEvent([NotNull] string taskPath, StandardTaskEventId id, DateTime time)
        {
            EventId = (int)id;
            TaskPath = taskPath;
            TimeCreated = time;
        }

        /// <summary>
        /// Gets the activity id. This value is <c>null</c> for V1 events.
        /// </summary>
        public Guid? ActivityId { get; internal set; }

        /// <summary>
        /// An indexer that gets the value of each of the data item values. This value is <c>null</c> for V1 events.
        /// </summary>
        /// <value>
        /// The data values.
        /// </value>
        public EventDataValues DataValues { get; }

        /// <summary>
        /// Gets the event id.
        /// </summary>
        public int EventId { get; internal set; }

        /// <summary>
        /// Gets the underlying <see cref="EventRecord"/>. This value is <c>null</c> for V1 events.
        /// </summary>
        public EventRecord EventRecord { get; internal set; }

        /// <summary>
        /// Gets the <see cref="StandardTaskEventId"/> from the <see cref="EventId"/>.
        /// </summary>
        /// <value>
        /// The <see cref="StandardTaskEventId"/>. If not found, returns <see cref="StandardTaskEventId.Unknown"/>.
        /// </value>
        public StandardTaskEventId StandardEventId
        {
            get
            {
                if (Enum.IsDefined(typeof(StandardTaskEventId), EventId))
                    return (StandardTaskEventId)EventId;
                return StandardTaskEventId.Unknown;
            }
        }

        /// <summary>
        /// Gets the level. This value is <c>null</c> for V1 events.
        /// </summary>
        public string Level { get; internal set; }

        /// <summary>
        /// Gets the op code. This value is <c>null</c> for V1 events.
        /// </summary>
        public string OpCode { get; internal set; }

        /// <summary>
        /// Gets the process id. This value is <c>null</c> for V1 events.
        /// </summary>
        public int? ProcessId { get; internal set; }

        /// <summary>
        /// Gets the record id. This value is <c>null</c> for V1 events.
        /// </summary>
        public long? RecordId { get; internal set; }

        /// <summary>
        /// Gets the task category. This value is <c>null</c> for V1 events.
        /// </summary>
        public string TaskCategory { get; internal set; }

        /// <summary>
        /// Gets the task path.
        /// </summary>
        public string TaskPath { get; internal set; }

        /// <summary>
        /// Gets the time created.
        /// </summary>
        public DateTime? TimeCreated { get; internal set; }

        /// <summary>
        /// Gets the user id. This value is <c>null</c> for V1 events.
        /// </summary>
        public System.Security.Principal.SecurityIdentifier UserId { get; internal set; }

        /// <summary>
        /// Gets the version. This value is <c>null</c> for V1 events.
        /// </summary>
        public byte? Version { get; internal set; }

        /// <summary>
        /// Gets the data value from the task specific event data item list.
        /// </summary>
        /// <param name="name">The name of the data element.</param>
        /// <returns>Contents of the requested data element if found. <c>null</c> if no value found.</returns>
        [Obsolete("Use the DataVales property instead.")]
        public string GetDataValue(string name) => DataValues?[name];

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString() => EventRecord?.FormatDescription() ?? TaskPath;

        /// <summary>
        /// Compares the current object with another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// A 32-bit signed integer that indicates the relative order of the objects being compared. The return value has the following meanings: Value Meaning Less than zero This object is less than the other parameter.Zero This object is equal to other. Greater than zero This object is greater than other.
        /// </returns>
        public int CompareTo(TaskEvent other)
        {
            int i = string.Compare(TaskPath, other.TaskPath, StringComparison.Ordinal);
            if (i == 0 && EventRecord != null)
            {
                i = string.Compare(ActivityId.ToString(), other.ActivityId.ToString(), StringComparison.Ordinal);
                if (i == 0)
                    i = Convert.ToInt32(RecordId - other.RecordId);
            }
            return i;
        }

        /// <summary>
        /// Get indexer class for <see cref="EventLogRecord"/> data values.
        /// </summary>
        public class EventDataValues
        {
            private readonly EventLogRecord rec;

            internal EventDataValues(EventLogRecord eventRec)
            {
                rec = eventRec;
            }

            /// <summary>
            /// Gets the <see cref="System.String"/> value of the specified property name.
            /// </summary>
            /// <value>
            /// The value.
            /// </value>
            /// <param name="propertyName">Name of the property.</param>
            /// <returns>Value of the specified property name. <c>null</c> if property does not exist.</returns>
            public string this[string propertyName]
            {
                get
                {
                    var propsel = new EventLogPropertySelector(new[] { $"Event/EventData/Data[@Name='{propertyName}']" });
                    try
                    {
                        var logEventProps = rec.GetPropertyValues(propsel);
                        return logEventProps[0].ToString();
                    }
                    catch { }
                    return null;
                }
            }
        }
    }

    /// <summary>
    /// An enumerator over a task's history of events.
    /// </summary>
    public sealed class TaskEventEnumerator : IEnumerator<TaskEvent>
    {
        private EventRecord curRec;
        private EventLogReader log;

        internal TaskEventEnumerator([NotNull] EventLogReader log)
        {
            this.log = log;
        }

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        /// <returns>
        /// The element in the collection at the current position of the enumerator.
        ///   </returns>
        public TaskEvent Current => new TaskEvent(curRec);

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        /// <returns>
        /// The element in the collection at the current position of the enumerator.
        ///   </returns>
        object System.Collections.IEnumerator.Current => Current;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            log.CancelReading();
            log.Dispose();
            log = null;
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        /// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
        /// </returns>
        /// <exception cref="T:System.InvalidOperationException">
        /// The collection was modified after the enumerator was created.
        ///   </exception>
        public bool MoveNext() => (curRec = log.ReadEvent()) != null;

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        /// <exception cref="T:System.InvalidOperationException">
        /// The collection was modified after the enumerator was created.
        ///   </exception>
        public void Reset()
        {
            log.Seek(System.IO.SeekOrigin.Begin, 0L);
        }

        /// <summary>
        /// Seeks the specified bookmark.
        /// </summary>
        /// <param name="bookmark">The bookmark.</param>
        /// <param name="offset">The offset.</param>
        public void Seek(EventBookmark bookmark, long offset = 0L)
        {
            log.Seek(bookmark, offset);
        }

        /// <summary>
        /// Seeks the specified origin.
        /// </summary>
        /// <param name="origin">The origin.</param>
        /// <param name="offset">The offset.</param>
        public void Seek(System.IO.SeekOrigin origin, long offset)
        {
            log.Seek(origin, offset);
        }
    }

    /// <summary>
    /// Historical event log for a task. Only available for Windows Vista and Windows Server 2008 and later systems.
    /// </summary>
    /// <remarks>Many applications have the need to audit the execution of the tasks they supply. To enable this, the library provides the TaskEventLog class that allows for TaskEvent instances to be enumerated. This can be done for single tasks or the entire system. It can also be filtered by specific events or criticality.</remarks>
    /// <example><code lang="cs"><![CDATA[
    /// // Create a log instance for the Maint task in the root directory
    /// TaskEventLog log = new TaskEventLog(@"\Maint",
    ///    // Specify the event id(s) you want to enumerate
    ///    new int[] { 141 /* TaskDeleted */, 201 /* ActionSuccess */ },
    ///    // Specify the start date of the events to enumerate. Here, we look at the last week.
    ///    DateTime.Now.AddDays(-7));
    /// 
    /// // Tell the enumerator to expose events 'newest first'
    /// log.EnumerateInReverse = false;
    /// 
    /// // Enumerate the events
    /// foreach (TaskEvent ev in log)
    /// {
    ///    // TaskEvents can interpret event ids into a well known, readable, enumerated type
    ///    if (ev.StandardEventId == StandardTaskEventId.TaskDeleted)
    ///       output.WriteLine($"  Task '{ev.TaskPath}' was deleted");
    /// 
    ///    // TaskEvent exposes a number of properties regarding the event
    ///    else if (ev.EventId == 201)
    ///       output.WriteLine($"  Completed action '{ev.DataValues["ActionName"]}',
    ///          ({ev.DataValues["ResultCode"]}) at {ev.TimeCreated.Value}.");
    /// }
    /// ]]></code></example>
    public sealed class TaskEventLog : IEnumerable<TaskEvent>
    {
        private const string TSEventLogPath = "Microsoft-Windows-TaskScheduler/Operational";
        private static readonly bool IsVistaOrLater = Environment.OSVersion.Version.Major >= 6;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskEventLog"/> class.
        /// </summary>
        /// <param name="taskPath">The task path. This can be retrieved using the <see cref="Task.Path"/> property.</param>
        /// <exception cref="NotSupportedException">Thrown when instantiated on an OS prior to Windows Vista.</exception>
        public TaskEventLog([CanBeNull] string taskPath) : this(".", taskPath)
        {
            Initialize(".", BuildQuery(taskPath), true);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskEventLog" /> class.
        /// </summary>
        /// <param name="machineName">Name of the machine.</param>
        /// <param name="taskPath">The task path. This can be retrieved using the <see cref="Task.Path" /> property.</param>
        /// <param name="domain">The domain.</param>
        /// <param name="user">The user.</param>
        /// <param name="password">The password.</param>
        /// <exception cref="NotSupportedException">Thrown when instantiated on an OS prior to Windows Vista.</exception>
        public TaskEventLog([NotNull] string machineName, [CanBeNull] string taskPath, string domain = null, string user = null, string password = null)
        {
            Initialize(machineName, BuildQuery(taskPath), true, domain, user, password);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskEventLog" /> class that looks at all task events from a specified time.
        /// </summary>
        /// <param name="startTime">The start time.</param>
        /// <param name="taskName">Name of the task.</param>
        /// <param name="machineName">Name of the machine (optional).</param>
        /// <param name="domain">The domain.</param>
        /// <param name="user">The user.</param>
        /// <param name="password">The password.</param>
        public TaskEventLog(DateTime startTime, string taskName = null, string machineName = null, string domain = null, string user = null, string password = null)
        {
            int[] numArray = new int[] { 100, 102, 103, 107, 108, 109, 111, 117, 118, 119, 120, 121, 122, 123, 124, 125 };
            Initialize(machineName, BuildQuery(taskName, numArray, startTime), false, domain, user, password);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskEventLog"/> class.
        /// </summary>
        /// <param name="taskName">Name of the task.</param>
        /// <param name="eventIDs">The event ids.</param>
        /// <param name="startTime">The start time.</param>
        /// <param name="machineName">Name of the machine (optional).</param>
        /// <param name="domain">The domain.</param>
        /// <param name="user">The user.</param>
        /// <param name="password">The password.</param>
        public TaskEventLog(string taskName = null, int[] eventIDs = null, DateTime? startTime = null, string machineName = null, string domain = null, string user = null, string password = null)
        {
            Initialize(machineName, BuildQuery(taskName, eventIDs, startTime), true, domain, user, password);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskEventLog" /> class.
        /// </summary>
        /// <param name="taskName">Name of the task.</param>
        /// <param name="eventIDs">The event ids.</param>
        /// <param name="levels">The levels.</param>
        /// <param name="startTime">The start time.</param>
        /// <param name="machineName">Name of the machine (optional).</param>
        /// <param name="domain">The domain.</param>
        /// <param name="user">The user.</param>
        /// <param name="password">The password.</param>
        public TaskEventLog(string taskName = null, int[] eventIDs = null, int[] levels = null, DateTime? startTime = null, string machineName = null, string domain = null, string user = null, string password = null)
        {
            Initialize(machineName, BuildQuery(taskName, eventIDs, startTime, levels), true, domain, user, password);
        }

        internal static string BuildQuery(string taskName = null, int[] eventIDs = null, DateTime? startTime = null, int[] levels = null)
        {
            const string queryString =
                "<QueryList>" +
                "  <Query Id=\"0\" Path=\"" + TSEventLogPath + "\">" +
                "    <Select Path=\"" + TSEventLogPath + "\">{0}</Select>" +
                "  </Query>" +
                "</QueryList>";
            const string OR = " or ";
            const string AND = " and ";

            System.Text.StringBuilder sb = new System.Text.StringBuilder("*");
            if (eventIDs != null && eventIDs.Length > 0)
            {
                if (sb.Length > 1) sb.Append(AND);
                sb.AppendFormat("({0})", string.Join(OR, Array.ConvertAll(eventIDs, i => $"EventID={i}")));
            }
            if (levels != null && levels.Length > 0)
            {
                if (sb.Length > 1) sb.Append(AND);
                sb.AppendFormat("({0})", string.Join(OR, Array.ConvertAll(levels, i => $"Level={i}")));
            }
            if (startTime.HasValue)
            {
                if (sb.Length > 1) sb.Append(AND);
                sb.AppendFormat("TimeCreated[@SystemTime>='{0}']", System.Xml.XmlConvert.ToString(startTime.Value, System.Xml.XmlDateTimeSerializationMode.RoundtripKind));
            }
            if (sb.Length > 1)
            {
                sb.Insert(1, "[System[Provider[@Name='Microsoft-Windows-TaskScheduler'] and ");
                sb.Append(']');
            }
            if (!string.IsNullOrEmpty(taskName))
            {
                if (sb.Length == 1)
                    sb.Append('[');
                else
                    sb.Append("]" + AND + "*[");
                sb.AppendFormat("EventData[Data[@Name='TaskName']='{0}']", taskName);
            }
            if (sb.Length > 1)
                sb.Append(']');
            return string.Format(queryString, sb);
        }

        private void Initialize(string machineName, string query, bool revDir, string domain = null, string user = null, string password = null)
        {
            if (!IsVistaOrLater)
                throw new NotSupportedException("Enumeration of task history not available on systems prior to Windows Vista and Windows Server 2008.");

            System.Security.SecureString spwd = null;
            if (password != null)
            {
                spwd = new System.Security.SecureString();
                foreach (char c in password)
                    spwd.AppendChar(c);
            }

            Query = new EventLogQuery(TSEventLogPath, PathType.LogName, query) { ReverseDirection = revDir };
            if (machineName != null && machineName != "." && !machineName.Equals(Environment.MachineName, StringComparison.InvariantCultureIgnoreCase))
                Query.Session = new EventLogSession(machineName, domain, user, spwd, SessionAuthentication.Default);
        }

        /// <summary>
        /// Gets the total number of events for this task.
        /// </summary>
        public long Count
        {
            get
            {
                using (EventLogReader log = new EventLogReader(Query))
                {
                    long seed = 64L, l = 0L, h = seed;
                    while (log.ReadEvent() != null)
                        log.Seek(System.IO.SeekOrigin.Begin, l += seed);
                    bool foundLast = false;
                    while (l > 0L && h >= 1L)
                    {
                        if (foundLast)
                            l += (h /= 2L);
                        else
                            l -= (h /= 2L);
                        log.Seek(System.IO.SeekOrigin.Begin, l);
                        foundLast = (log.ReadEvent() != null);
                    }
                    return foundLast ? l + 1L : l;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="TaskEventLog" /> is enabled.
        /// </summary>
        /// <value>
        /// <c>true</c> if enabled; otherwise, <c>false</c>.
        /// </value>
        public bool Enabled
        {
            get
            {
                if (!IsVistaOrLater)
                    return false;
                using (var cfg = new EventLogConfiguration(TSEventLogPath, Query.Session))
                    return cfg.IsEnabled;
            }
            set
            {
                if (!IsVistaOrLater)
                    throw new NotSupportedException("Task history not available on systems prior to Windows Vista and Windows Server 2008.");
                using (var cfg = new EventLogConfiguration(TSEventLogPath, Query.Session))
                {
                    if (cfg.IsEnabled != value)
                    {
                        cfg.IsEnabled = value;
                        cfg.SaveChanges();
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to enumerate in reverse when calling the default enumerator (typically with foreach statement).
        /// </summary>
        /// <value>
        ///   <c>true</c> if enumerates in reverse (newest to oldest) by default; otherwise, <c>false</c> to enumerate oldest to newest.
        /// </value>
        [System.ComponentModel.DefaultValue(false)]
        public bool EnumerateInReverse { get; set; }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        IEnumerator<TaskEvent> IEnumerable<TaskEvent>.GetEnumerator() => GetEnumerator(EnumerateInReverse);

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <param name="reverse">if set to <c>true</c> reverse.</param>
        /// <returns>
        /// A <see cref="TaskEventEnumerator" /> that can be used to iterate through the collection.
        /// </returns>
        [NotNull]
        public TaskEventEnumerator GetEnumerator(bool reverse)
        {
            Query.ReverseDirection = !reverse;
            return new TaskEventEnumerator(new EventLogReader(Query));
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator(false);

        internal EventLogQuery Query { get; private set; }
    }
}
