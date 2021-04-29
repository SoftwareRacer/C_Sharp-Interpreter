using System.Diagnostics;
using NUnit.Framework;

namespace Interpreter.UnitTest
{
	[TestFixture]
	public class PerformanceTest
	{
		[Test]
		public void InterpreterCreation()
		{
			// TODO Study if there is a better way to test performance

			var stopwatch = Stopwatch.StartNew();

			for (var i = 0; i < 1000; i++)
			{
				new Interpreter(InterpreterOptions.Default);
			}

			Assert.Less(stopwatch.ElapsedMilliseconds, 200);
		}
	}
}