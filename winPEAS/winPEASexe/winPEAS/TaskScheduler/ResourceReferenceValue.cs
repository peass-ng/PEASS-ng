using System;
using System.Runtime.InteropServices;
using System.Text;
using JetBrains.Annotations;

namespace Microsoft.Win32.TaskScheduler
{
	/// <summary>
	/// Some string values of <see cref="TaskDefinition"/> properties can be set to retrieve their value from existing DLLs as a resource. This class facilitates creating those reference strings.
	/// </summary>
	[PublicAPI]
	public class ResourceReferenceValue
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ResourceReferenceValue"/> class.
		/// </summary>
		/// <param name="dllPath">The DLL path.</param>
		/// <param name="resourceId">The resource identifier.</param>
		public ResourceReferenceValue([NotNull] string dllPath, int resourceId)
		{
			ResourceFilePath = dllPath;
			ResourceIdentifier = resourceId;
		}

		/// <summary>
		/// Gets or sets the resource file path. This can be a relative path, full path or lookup path (e.g. %SystemRoot%\System32\ResourceName.dll).
		/// </summary>
		/// <value>
		/// The resource file path.
		/// </value>
		public string ResourceFilePath { get; set; }

		/// <summary>
		/// Gets or sets the resource identifier.
		/// </summary>
		/// <value>The resource identifier.</value>
		public int ResourceIdentifier { get; set; }

		/// <summary>
		/// Performs an implicit conversion from <see cref="Microsoft.Win32.TaskScheduler.ResourceReferenceValue" /> to <see cref="System.String" />.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The result of the conversion.</returns>
		public static implicit operator string(ResourceReferenceValue value) => value.ToString();

		/// <summary>
		/// Returns a <see cref="System.String" /> in the format required by the Task Scheduler to reference a string in a DLL.
		/// </summary>
		/// <returns>A formatted <see cref="System.String" /> in the format $(@ [Dll], [ResourceID]).</returns>
		public override string ToString() => $"$(@ {ResourceFilePath}, {ResourceIdentifier})";
	}
}