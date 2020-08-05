/* MIT License

Copyright (c) 2016 JetBrains http://www.jetbrains.com

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE. */

using System;

#pragma warning disable 1591
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable IntroduceOptionalParameters.Global
// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable InconsistentNaming

namespace JetBrains.Annotations
{
	/// <summary>
	/// Indicates that the value of the marked element could be <c>null</c> sometimes,
	/// so the check for <c>null</c> is necessary before its usage.
	/// </summary>
	/// <example><code>
	/// [CanBeNull] object Test() => null;
	/// 
	/// void UseTest() {
	///   var p = Test();
	///   var s = p.ToString(); // Warning: Possible 'System.NullReferenceException'
	/// }
	/// </code></example>
	[AttributeUsage(
	  AttributeTargets.Method | AttributeTargets.Parameter | AttributeTargets.Property |
	  AttributeTargets.Delegate | AttributeTargets.Field | AttributeTargets.Event |
	  AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.GenericParameter)]
	internal sealed class CanBeNullAttribute : Attribute { }

	/// <summary>
	/// Indicates that the value of the marked element could never be <c>null</c>.
	/// </summary>
	/// <example><code>
	/// [NotNull] object Foo() {
	///   return null; // Warning: Possible 'null' assignment
	/// }
	/// </code></example>
	[AttributeUsage(
	  AttributeTargets.Method | AttributeTargets.Parameter | AttributeTargets.Property |
	  AttributeTargets.Delegate | AttributeTargets.Field | AttributeTargets.Event |
	  AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.GenericParameter)]
	internal sealed class NotNullAttribute : Attribute { }

	/// <summary>
	/// Can be appplied to symbols of types derived from IEnumerable as well as to symbols of Task
	/// and Lazy classes to indicate that the value of a collection item, of the Task.Result property
	/// or of the Lazy.Value property can never be null.
	/// </summary>
	[AttributeUsage(
	  AttributeTargets.Method | AttributeTargets.Parameter | AttributeTargets.Property |
	  AttributeTargets.Delegate | AttributeTargets.Field)]
	internal sealed class ItemNotNullAttribute : Attribute { }


	/// <summary>
	/// When applied to a target attribute, specifies a requirement for any type marked
	/// with the target attribute to implement or inherit specific type or types.
	/// </summary>
	/// <example><code>
	/// [BaseTypeRequired(typeof(IComponent)] // Specify requirement
	/// class ComponentAttribute : Attribute { }
	/// 
	/// [Component] // ComponentAttribute requires implementing IComponent interface
	/// class MyComponent : IComponent { }
	/// </code></example>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	[BaseTypeRequired(typeof(Attribute))]
	internal sealed class BaseTypeRequiredAttribute : Attribute
	{
		public BaseTypeRequiredAttribute([NotNull] Type baseType)
		{
			BaseType = baseType;
		}

		[NotNull] public Type BaseType { get; private set; }
	}

	/// <summary>
	/// Indicates that the marked symbol is used implicitly (e.g. via reflection, in external library),
	/// so this symbol will not be marked as unused (as well as by other usage inspections).
	/// </summary>
	[AttributeUsage(AttributeTargets.All)]
	internal sealed class UsedImplicitlyAttribute : Attribute
	{
		public UsedImplicitlyAttribute()
		  : this(ImplicitUseKindFlags.Default, ImplicitUseTargetFlags.Default) { }

		public UsedImplicitlyAttribute(ImplicitUseKindFlags useKindFlags, ImplicitUseTargetFlags targetFlags)
		{
			UseKindFlags = useKindFlags;
			TargetFlags = targetFlags;
		}

		public ImplicitUseKindFlags UseKindFlags { get; private set; }

		public ImplicitUseTargetFlags TargetFlags { get; private set; }
	}

	/// <summary>
	/// Should be used on attributes and causes ReSharper to not mark symbols marked with such attributes
	/// as unused (as well as by other usage inspections)
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.GenericParameter)]
	internal sealed class MeansImplicitUseAttribute : Attribute
	{
		public MeansImplicitUseAttribute(ImplicitUseTargetFlags targetFlags)
		  : this(ImplicitUseKindFlags.Default, targetFlags) { }

		public MeansImplicitUseAttribute(ImplicitUseKindFlags useKindFlags, ImplicitUseTargetFlags targetFlags)
		{
			UseKindFlags = useKindFlags;
			TargetFlags = targetFlags;
		}

		[UsedImplicitly] public ImplicitUseKindFlags UseKindFlags { get; private set; }

		[UsedImplicitly] public ImplicitUseTargetFlags TargetFlags { get; private set; }
	}

	[Flags]
	internal enum ImplicitUseKindFlags
	{
		Default = Access | Assign | InstantiatedWithFixedConstructorSignature,
		/// <summary>Only entity marked with attribute considered used.</summary>
		Access = 1,
		/// <summary>Indicates implicit assignment to a member.</summary>
		Assign = 2,
		/// <summary>
		/// Indicates implicit instantiation of a type with fixed constructor signature.
		/// That means any unused constructor parameters won't be reported as such.
		/// </summary>
		InstantiatedWithFixedConstructorSignature = 4,
		/// <summary>Indicates implicit instantiation of a type.</summary>
		InstantiatedNoFixedConstructorSignature = 8,
	}

	/// <summary>
	/// Specify what is considered used implicitly when marked
	/// with <see cref="MeansImplicitUseAttribute"/> or <see cref="UsedImplicitlyAttribute"/>.
	/// </summary>
	[Flags]
	internal enum ImplicitUseTargetFlags
	{
		Default = Itself,
		Itself = 1,
		/// <summary>Members of entity marked with attribute are considered used.</summary>
		Members = 2,
		/// <summary>Entity marked with attribute and all its members considered used.</summary>
		WithMembers = Itself | Members
	}

	/// <summary>
	/// This attribute is intended to mark publicly available API
	/// which should not be removed and so is treated as used.
	/// </summary>
	[MeansImplicitUse(ImplicitUseTargetFlags.WithMembers)]
	internal sealed class PublicAPIAttribute : Attribute
	{
		public PublicAPIAttribute() { }
    }
    
	/// <summary>
	/// An extension method marked with this attribute is processed by ReSharper code completion
	/// as a 'Source Template'. When extension method is completed over some expression, it's source code
	/// is automatically expanded like a template at call site.
	/// </summary>
	/// <remarks>
	/// Template method body can contain valid source code and/or special comments starting with '$'.
	/// Text inside these comments is added as source code when the template is applied. Template parameters
	/// can be used either as additional method parameters or as identifiers wrapped in two '$' signs.
	/// Use the <see cref="MacroAttribute"/> attribute to specify macros for parameters.
	/// </remarks>
	/// <example>
	/// In this example, the 'forEach' method is a source template available over all values
	/// of enumerable types, producing ordinary C# 'foreach' statement and placing caret inside block:
	/// <code>
	/// [SourceTemplate]
	/// public static void forEach&lt;T&gt;(this IEnumerable&lt;T&gt; xs) {
	///   foreach (var x in xs) {
	///      //$ $END$
	///   }
	/// }
	/// </code>
	/// </example>
	[AttributeUsage(AttributeTargets.Method)]
	internal sealed class SourceTemplateAttribute : Attribute { }

	/// <summary>
	/// Allows specifying a macro for a parameter of a <see cref="SourceTemplateAttribute">source template</see>.
	/// </summary>
	/// <remarks>
	/// You can apply the attribute on the whole method or on any of its additional parameters. The macro expression
	/// is defined in the <see cref="MacroAttribute.Expression"/> property. When applied on a method, the target
	/// template parameter is defined in the <see cref="MacroAttribute.Target"/> property. To apply the macro silently
	/// for the parameter, set the <see cref="MacroAttribute.Editable"/> property value = -1.
	/// </remarks>
	/// <example>
	/// Applying the attribute on a source template method:
	/// <code>
	/// [SourceTemplate, Macro(Target = "item", Expression = "suggestVariableName()")]
	/// public static void forEach&lt;T&gt;(this IEnumerable&lt;T&gt; collection) {
	///   foreach (var item in collection) {
	///     //$ $END$
	///   }
	/// }
	/// </code>
	/// Applying the attribute on a template method parameter:
	/// <code>
	/// [SourceTemplate]
	/// public static void something(this Entity x, [Macro(Expression = "guid()", Editable = -1)] string newguid) {
	///   /*$ var $x$Id = "$newguid$" + x.ToString();
	///   x.DoSomething($x$Id); */
	/// }
	/// </code>
	/// </example>
	[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Method, AllowMultiple = true)]
	internal sealed class MacroAttribute : Attribute
	{
		/// <summary>
		/// Allows specifying a macro that will be executed for a <see cref="SourceTemplateAttribute">source template</see>
		/// parameter when the template is expanded.
		/// </summary>
		[CanBeNull] public string Expression { get; set; }

		/// <summary>
		/// Allows specifying which occurrence of the target parameter becomes editable when the template is deployed.
		/// </summary>
		/// <remarks>
		/// If the target parameter is used several times in the template, only one occurrence becomes editable;
		/// other occurrences are changed synchronously. To specify the zero-based index of the editable occurrence,
		/// use values >= 0. To make the parameter non-editable when the template is expanded, use -1.
		/// </remarks>>
		public int Editable { get; set; }

		/// <summary>
		/// Identifies the target parameter of a <see cref="SourceTemplateAttribute">source template</see> if the
		/// <see cref="MacroAttribute"/> is applied on a template method.
		/// </summary>
		[CanBeNull] public string Target { get; set; }
	}

	/// <summary>
	/// Indicates that the marked method is assertion method, i.e. it halts control flow if
	/// one of the conditions is satisfied. To set the condition, mark one of the parameters with 
	/// <see cref="AssertionConditionAttribute"/> attribute.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method)]
	internal sealed class AssertionMethodAttribute : Attribute { }

	/// <summary>
	/// Indicates the condition parameter of the assertion method. The method itself should be
	/// marked by <see cref="AssertionMethodAttribute"/> attribute. The mandatory argument of
	/// the attribute is the assertion type.
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter)]
	internal sealed class AssertionConditionAttribute : Attribute { }
}