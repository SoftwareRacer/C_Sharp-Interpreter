using System;

namespace DynamicExpresso
{
	[Flags]
	public enum AssignmentOperators
	{
		// Disable all the assignment operators
		None = 0,
		// Enable the assignment equal operator
		AssignmentEqual = 1,
		// Enable all assignment operators
		All = AssignmentEqual
	}
}
