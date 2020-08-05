namespace System.Reflection
{
	/// <summary>Extensions related to <c>System.Reflection</c></summary>
	internal static class ReflectionHelper
	{
		/// <summary>Loads a type from a named assembly.</summary>
		/// <param name="typeName">Name of the type.</param>
		/// <param name="asmRef">The name or path of the file that contains the manifest of the assembly.</param>
		/// <returns>The <see cref="Type"/> reference, or <c>null</c> if type or assembly not found.</returns>
		public static Type LoadType(string typeName, string asmRef)
		{
			Type ret = null;
			if (!TryGetType(Assembly.GetCallingAssembly(), typeName, ref ret))
				if (!TryGetType(asmRef, typeName, ref ret))
					if (!TryGetType(Assembly.GetExecutingAssembly(), typeName, ref ret))
						TryGetType(Assembly.GetEntryAssembly(), typeName, ref ret);
			return ret;
		}

		/// <summary>Tries the retrieve a <see cref="Type"/> reference from an assembly.</summary>
		/// <param name="typeName">Name of the type.</param>
		/// <param name="asmRef">The assembly reference name from which to load the type.</param>
		/// <param name="type">The <see cref="Type"/> reference, if found.</param>
		/// <returns><c>true</c> if the type was found in the assembly; otherwise, <c>false</c>.</returns>
		private static bool TryGetType(string asmRef, string typeName, ref Type type)
		{
			try
			{
				if (System.IO.File.Exists(asmRef))
					return TryGetType(Assembly.LoadFrom(asmRef), typeName, ref type);
			}
			catch { }
			return false;
		}

		/// <summary>Tries the retrieve a <see cref="Type"/> reference from an assembly.</summary>
		/// <param name="typeName">Name of the type.</param>
		/// <param name="asm">The assembly from which to load the type.</param>
		/// <param name="type">The <see cref="Type"/> reference, if found.</param>
		/// <returns><c>true</c> if the type was found in the assembly; otherwise, <c>false</c>.</returns>
		private static bool TryGetType(Assembly asm, string typeName, ref Type type)
		{
			if (asm != null)
			{
				try
				{
					type = asm.GetType(typeName, false, false);
					return (type != null);
				}
				catch { }
			}
			return false;
		}

		/// <summary>Invokes a named method on a created instance of a type with parameters.</summary>
		/// <typeparam name="T">The expected type of the method's return value.</typeparam>
		/// <param name="type">The type to be instantiated and then used to invoke the method.</param>
		/// <param name="instArgs">The arguments to supply to the constructor.</param>
		/// <param name="methodName">Name of the method.</param>
		/// <param name="args">The arguments to provide to the method invocation.</param>
		/// <returns>The value returned from the method.</returns>
		public static T InvokeMethod<T>(Type type, object[] instArgs, string methodName, params object[] args)
		{
			object o = Activator.CreateInstance(type, instArgs);
			return InvokeMethod<T>(o, methodName, args);
		}

		/// <summary>Invokes a named method on an object with parameters and no return value.</summary>
		/// <param name="obj">The object on which to invoke the method.</param>
		/// <param name="methodName">Name of the method.</param>
		/// <param name="args">The arguments to provide to the method invocation.</param>
		public static T InvokeMethod<T>(object obj, string methodName, params object[] args)
		{
			Type[] argTypes = (args == null || args.Length == 0) ? Type.EmptyTypes : Array.ConvertAll(args, delegate(object o) { return o == null ? typeof(object) : o.GetType(); });
			return InvokeMethod<T>(obj, methodName, argTypes, args);
		}

		/// <summary>Invokes a named method on an object with parameters and no return value.</summary>
		/// <typeparam name="T">The expected type of the method's return value.</typeparam>
		/// <param name="obj">The object on which to invoke the method.</param>
		/// <param name="methodName">Name of the method.</param>
		/// <param name="argTypes">The types of the <paramref name="args"/>.</param>
		/// <param name="args">The arguments to provide to the method invocation.</param>
		/// <returns>The value returned from the method.</returns>
		public static T InvokeMethod<T>(object obj, string methodName, Type[] argTypes, object[] args)
		{
			MethodInfo mi = obj?.GetType().GetMethod(methodName, argTypes);
			if (mi != null)
				return (T)Convert.ChangeType(mi.Invoke(obj, args), typeof(T));
			return default(T);
		}

		/// <summary>Gets a named property value from an object.</summary>
		/// <typeparam name="T">The expected type of the property to be returned.</typeparam>
		/// <param name="obj">The object from which to retrieve the property.</param>
		/// <param name="propName">Name of the property.</param>
		/// <param name="defaultValue">The default value to return in the instance that the property is not found.</param>
		/// <returns>The property value, if found, or the <paramref name="defaultValue"/> if not.</returns>
		public static T GetProperty<T>(object obj, string propName, T defaultValue = default(T))
		{
			if (obj != null)
			{
				try { return (T)Convert.ChangeType(obj.GetType().InvokeMember(propName, BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, obj, null, null), typeof(T)); }
				catch { }
			}
			return defaultValue;
		}

		/// <summary>Sets a named property on an object.</summary>
		/// <typeparam name="T">The type of the property to be set.</typeparam>
		/// <param name="obj">The object on which to set the property.</param>
		/// <param name="propName">Name of the property.</param>
		/// <param name="value">The property value to set on the object.</param>
		public static void SetProperty<T>(object obj, string propName, T value)
		{
			try { obj?.GetType().InvokeMember(propName, BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, obj, new object[] { value }, null); }
			catch { }
		}
	}
}