using System;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;
using JetBrains.Annotations;

namespace Microsoft.Win32.TaskScheduler
{
	/// <summary>
	/// Abstract class for throwing a method specific exception.
	/// </summary>
	[DebuggerStepThrough, Serializable]
	[PublicAPI]
	public abstract class TSNotSupportedException : Exception
	{
		/// <summary>Defines the minimum supported version for the action not allowed by this exception.</summary>
		protected readonly TaskCompatibility min;
		private readonly string myMessage;

		/// <summary>
		/// Initializes a new instance of the <see cref="TSNotSupportedException"/> class.
		/// </summary>
		/// <param name="serializationInfo">The serialization information.</param>
		/// <param name="streamingContext">The streaming context.</param>
		protected TSNotSupportedException(SerializationInfo serializationInfo, StreamingContext streamingContext)
			: base(serializationInfo, streamingContext)
		{
			try { min = (TaskCompatibility)serializationInfo.GetValue("min", typeof(TaskCompatibility)); }
			catch { min = TaskCompatibility.V1; }
		}

		internal TSNotSupportedException(TaskCompatibility minComp)
		{
			min = minComp;
			var stackTrace = new StackTrace();
			var stackFrame = stackTrace.GetFrame(2);
			var methodBase = stackFrame.GetMethod();
			myMessage = $"{methodBase.DeclaringType?.Name}.{methodBase.Name} is not supported on {LibName}";
		}

		internal TSNotSupportedException(string message, TaskCompatibility minComp)
		{
			myMessage = message;
			min = minComp;
		}

		internal abstract string LibName { get; }

		/// <summary>
		/// Gets the object data.
		/// </summary>
		/// <param name="info">The information.</param>
		/// <param name="context">The context.</param>
		[SecurityCritical, SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
				throw new ArgumentNullException(nameof(info));
			info.AddValue("min", min);
			base.GetObjectData(info, context);
		}
	}

	/// <summary>
	/// Thrown when the calling method is not supported by Task Scheduler 1.0.
	/// </summary>
	[DebuggerStepThrough, Serializable]
	public class NotV1SupportedException : TSNotSupportedException
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="NotV1SupportedException" /> class.
		/// </summary>
		/// <param name="serializationInfo">The serialization information.</param>
		/// <param name="streamingContext">The streaming context.</param>
		protected NotV1SupportedException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext) { }
		internal NotV1SupportedException() : base(TaskCompatibility.V2) { }
		/// <summary>
		/// Initializes a new instance of the <see cref="NotV1SupportedException" /> class.
		/// </summary>
		/// <param name="message">The message.</param>
		public NotV1SupportedException(string message) : base(message, TaskCompatibility.V2) { }
		internal override string LibName => "Task Scheduler 1.0";
	}

	/// <summary>
	/// Thrown when the calling method is not supported by Task Scheduler 2.0.
	/// </summary>
	[DebuggerStepThrough, Serializable]
	public class NotV2SupportedException : TSNotSupportedException
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="NotV1SupportedException" /> class.
		/// </summary>
		/// <param name="serializationInfo">The serialization information.</param>
		/// <param name="streamingContext">The streaming context.</param>
		internal NotV2SupportedException() : base(TaskCompatibility.V1) { }
		internal NotV2SupportedException(string message) : base(message, TaskCompatibility.V1) { }
		internal override string LibName => "Task Scheduler 2.0 (1.2)";
	}

	/// <summary>
	/// Thrown when the calling method is not supported by Task Scheduler versions prior to the one specified.
	/// </summary>
	[DebuggerStepThrough, Serializable]
	public class NotSupportedPriorToException : TSNotSupportedException
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="NotV1SupportedException" /> class.
		/// </summary>
		/// <param name="serializationInfo">The serialization information.</param>
		/// <param name="streamingContext">The streaming context.</param>
		internal NotSupportedPriorToException(TaskCompatibility supportedVersion) : base(supportedVersion) { }
		internal override string LibName => $"Task Scheduler versions prior to 2.{((int)min) - 2} (1.{(int)min})";
	}
}
