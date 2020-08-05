using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Microsoft.Win32.TaskScheduler
{
	/// <summary>
	/// Provides information and control for a collection of folders that contain tasks.
	/// </summary>
	public sealed class TaskFolderCollection : ICollection<TaskFolder>, IDisposable
	{
		private readonly TaskFolder parent;
		private readonly TaskFolder[] v1FolderList;
		private readonly V2Interop.ITaskFolderCollection v2FolderList;

		internal TaskFolderCollection()
		{
			v1FolderList = new TaskFolder[0];
		}

		internal TaskFolderCollection([NotNull] TaskFolder folder, [NotNull] V2Interop.ITaskFolderCollection iCollection)
		{
			parent = folder;
			v2FolderList = iCollection;
		}

		/// <summary>
		/// Gets the number of items in the collection.
		/// </summary>
		public int Count => v2FolderList?.Count ?? v1FolderList.Length;

		/// <summary>
		/// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only.
		/// </summary>
		bool ICollection<TaskFolder>.IsReadOnly => false;

		/// <summary>
		/// Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1" />.
		/// </summary>
		/// <param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
		/// <exception cref="System.NotImplementedException">This action is technically unfeasible due to limitations of the underlying library. Use the <see cref="TaskFolder.CreateFolder(string, string, bool)"/> instead.</exception>
		public void Add([NotNull] TaskFolder item) { throw new NotImplementedException(); }

		/// <summary>
		/// Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1" />.
		/// </summary>
		public void Clear()
		{
			if (v2FolderList != null)
			{
				for (int i = v2FolderList.Count; i > 0; i--)
					parent.DeleteFolder(v2FolderList[i].Name, false);
			}
		}

		/// <summary>
		/// Determines whether the <see cref="T:System.Collections.Generic.ICollection`1" /> contains a specific value.
		/// </summary>
		/// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
		/// <returns>
		/// true if <paramref name="item" /> is found in the <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, false.
		/// </returns>
		public bool Contains([NotNull] TaskFolder item)
		{
			if (v2FolderList != null)
			{
				for (int i = v2FolderList.Count; i > 0; i--)
					if (string.Equals(item.Path, v2FolderList[i].Path, StringComparison.CurrentCultureIgnoreCase))
						return true;
			}
			else
				return item.Path == "\\";
			return false;
		}

		/// <summary>
		/// Copies the elements of the ICollection to an Array, starting at a particular Array index.
		/// </summary>
		/// <param name="array">The one-dimensional Array that is the destination of the elements copied from <see cref="ICollection{T}"/>. The Array must have zero-based indexing.</param>
		/// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
		public void CopyTo(TaskFolder[] array, int arrayIndex)
		{
			if (arrayIndex < 0) throw new ArgumentOutOfRangeException(nameof(arrayIndex));
			if (array == null) throw new ArgumentNullException(nameof(array));
			if (v2FolderList != null)
			{
				if (arrayIndex + Count > array.Length)
					throw new ArgumentException();
				foreach (var f in this)
					array[arrayIndex++] = f;
			}
			else
			{
				if (arrayIndex + v1FolderList.Length > array.Length)
					throw new ArgumentException();
				v1FolderList.CopyTo(array, arrayIndex);
			}
		}

		/// <summary>
		/// Releases all resources used by this class.
		/// </summary>
		public void Dispose()
		{
			if (v1FolderList != null && v1FolderList.Length > 0)
			{
				v1FolderList[0].Dispose();
				v1FolderList[0] = null;
			}
			if (v2FolderList != null)
				System.Runtime.InteropServices.Marshal.ReleaseComObject(v2FolderList);
		}

		/// <summary>
		/// Gets a list of items in a collection.
		/// </summary>
		/// <returns>Enumerated list of items in the collection.</returns>
		public IEnumerator<TaskFolder> GetEnumerator()
		{
			if (v2FolderList != null)
				return new System.Runtime.InteropServices.ComEnumerator<TaskFolder, V2Interop.ITaskFolder>(() => v2FolderList.Count, (object o) => v2FolderList[o], o => new TaskFolder(parent.TaskService, o));
			return Array.AsReadOnly(v1FolderList).GetEnumerator();
		}

		/// <summary>
		/// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1" />.
		/// </summary>
		/// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
		/// <returns>
		/// true if <paramref name="item" /> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, false. This method also returns false if <paramref name="item" /> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1" />.
		/// </returns>
		public bool Remove([NotNull] TaskFolder item)
		{
			if (v2FolderList != null)
			{
				for (int i = v2FolderList.Count; i > 0; i--)
				{
					if (string.Equals(item.Path, v2FolderList[i].Path, StringComparison.CurrentCultureIgnoreCase))
					{
						try
						{
							parent.DeleteFolder(v2FolderList[i].Name);
						}
						catch
						{
							return false;
						}
						return true;
					}
				}
			}
			return false;
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
