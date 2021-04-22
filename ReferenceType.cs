using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using interpreter.Reflection;

namespace Interpreter
{
	public class ReferenceType
	{
		public Type Type { get; private set; }

		// Public name that must be used in the expression.
		public string Name { get; private set; }

		public IList<MethodInfo> ExtensionMethods { get; private set; }

		public ReferenceType(string name, Type type)
		{
			if (string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException(nameof(name));

			if (type == null)
				throw new ArgumentNullException(nameof(type));

			Type = type;
			Name = name;
			ExtensionMethods = ReflectionExtensions.GetExtensionMethods(type).ToList();
		}

		public ReferenceType(Type type)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));

			Type = type;
			Name = type.Name;
			ExtensionMethods = ReflectionExtensions.GetExtensionMethods(type).ToList();
		}
	}
}
