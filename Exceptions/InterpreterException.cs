using System;

namespace Interpreter.Exceptions
{
#if !NETSTANDARD1_6
	[Serializable]
#endif
	public class InterpreterException : Exception //inherits exception
	{
		public InterpreterException() { }
		public InterpreterException(string message) : base(message) { }
		public InterpreterException(string message, Exception inner) : base(message, inner) { }

#if !NETSTANDARD1_6
		protected InterpreterException(
		System.Runtime.Serialization.SerializationInfo info,
		System.Runtime.Serialization.StreamingContext context)
			: base(info, context) { }
#endif
	}
}
