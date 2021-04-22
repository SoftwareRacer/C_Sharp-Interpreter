﻿#if !NETSTANDARD1_6
using System;
using System.Runtime.Serialization;
using System.Security.Permissions;
#endif
using Interpreter.Resources;

namespace Interpreter.Exceptions
{
	#if !NETSTANDARD1_6
	[Serializable]
	#endif
	public class ParseException : InterpreterException
	{
		public ParseException(string message, int position)
			: base(string.Format(ErrorMessages.Format, message, position)) 
		{
			Position = position;
		}

		public ParseException(string message, int position, Exception innerException)
			: base(string.Format(ErrorMessages.Format, message, position), innerException)
		{
			Position = position;
		}

		public int Position { get; private set; }

		#if !NETSTANDARD1_6
		protected ParseException(
			SerializationInfo info,
			StreamingContext context)
			: base(info, context) 
		{
			Position = info.GetInt32("Position");
		}

		[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("Position", Position);

			base.GetObjectData(info, context);
		}
		#endif
	}
}
