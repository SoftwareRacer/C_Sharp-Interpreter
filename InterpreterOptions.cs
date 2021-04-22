using System;

namespace Interpreter
{
	[Flags]
	public enum InterpreterOptions
	{
		None = 0,
		// Load primitive types like 'string', 'double', 'int', 'DateTime', 'Guid', ... See also LanguageConstants.CSharpPrimitiveTypes and LanguageConstants.PrimitiveTypes
		PrimitiveTypes = 1,
		// Load system keywords like 'true', 'false', 'null'. See also LanguageConstants.Literals.
		SystemKeywords = 2,
		// Load common types like 'System.Math', 'System.Convert', 'System.Linq.Enumerable'. See also LanguageConstants.CommonTypes.
		CommonTypes = 4,
		// Variables and parameters names are case insensitive.
		CaseInsensitive = 8,
		// Load all default configurations: PrimitiveTypes + SystemKeywords + CommonTypes
		Default = PrimitiveTypes | SystemKeywords | CommonTypes,
		// Load all default configurations: PrimitiveTypes + SystemKeywords + CommonTypes + CaseInsensitive
		DefaultCaseInsensitive = PrimitiveTypes | SystemKeywords | CommonTypes | CaseInsensitive,
	}
}
