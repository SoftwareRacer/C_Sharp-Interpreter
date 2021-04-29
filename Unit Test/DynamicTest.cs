﻿using Microsoft.CSharp.RuntimeBinder;
using NUnit.Framework;
using System;
using System.Dynamic;
using System.Linq.Expressions;

namespace Interpreter.UnitTest
{
	[TestFixture]
	public class DynamicTest
	{
		[Test]
		public void Get_Property_of_an_ExpandoObject()
		{
			dynamic dyn = new ExpandoObject();
			dyn.Foo = "bar";

			var interpreter = new Interpreter()
				.SetVariable("dyn", (object)dyn);

			Assert.AreEqual(dyn.Foo, interpreter.Eval("dyn.Foo"));
		}

		[Test]
		public void Get_Property_of_a_nested_ExpandoObject()
		{
			dynamic dyn = new ExpandoObject();
			dyn.Sub = new ExpandoObject();
			dyn.Sub.Foo = "bar";

			var interpreter = new Interpreter()
					.SetVariable("dyn", (object)dyn);

			Assert.AreEqual(dyn.Sub.Foo, interpreter.Eval("dyn.Sub.Foo"));
		}

		//[Test]
		//public void Set_Property_of_an_ExpandoObject()
		//{
		//	dynamic dyn = new ExpandoObject();

		//	var interpreter = new Interpreter()
		//		.SetVariable("dyn", dyn);

		//	interpreter.Eval("dyn.Foo = 6");

		//	Assert.AreEqual(6, dyn.Foo);
		//}

		[Test]
		public void Standard_properties_have_precedence_over_dynamic_properties()
		{
			var dyn = new TestDynamicClass();
			dyn.RealProperty = "bar";

			var interpreter = new Interpreter()
				.SetVariable("dyn", dyn);

			Assert.AreEqual(dyn.RealProperty, interpreter.Eval("dyn.RealProperty"));
		}

		[Test]
		public void Invoke_Method_of_an_ExpandoObject()
		{
			dynamic dyn = new ExpandoObject();
			dyn.Foo = new Func<string>(() => "bar");

			var interpreter = new Interpreter()
				.SetVariable("dyn", dyn);

			Assert.AreEqual(dyn.Foo(), interpreter.Eval("dyn.Foo()"));
		}

		[Test]
		public void Invoke_Method_of_a_nested_ExpandoObject()
		{
			dynamic dyn = new ExpandoObject();
			dyn.Sub = new ExpandoObject();
			dyn.Sub.Foo = new Func<string>(() => "bar");

			var interpreter = new Interpreter()
					.SetVariable("dyn", dyn);

			Assert.AreEqual(dyn.Sub.Foo(), interpreter.Eval("dyn.Sub.Foo()"));
		}

		[Test]
		public void Standard_methods_have_precedence_over_dynamic_methods()
		{
			var dyn = new TestDynamicClass();

			var interpreter = new Interpreter()
				.SetVariable("dyn", dyn);

			Assert.AreEqual(dyn.ToString(), interpreter.Eval("dyn.ToString()"));
		}

		[Test]
		public void Case_Insensitive_Dynamic_Members()
		{
			dynamic dyn = new ExpandoObject();
			dyn.Bar = 10;

			var interpreter = new Interpreter();
			Assert.Throws<RuntimeBinderException>(() => interpreter.Eval("dyn.BAR", new Parameter("dyn", dyn)));
		}

		[Test]
		public void Test_With_Standard_Object()
		{
			var myInstance = DateTime.Now;

			var methodInfo = myInstance.GetType().GetMethod("ToUniversalTime");

			var methodCallExpression = Expression.Call(Expression.Constant(myInstance), methodInfo);
			var expression = Expression.Lambda(methodCallExpression);

			Assert.AreEqual(myInstance.ToUniversalTime(), expression.Compile().DynamicInvoke());
		}

		[Test]
		public void Test_With_Dynamic_Object()
		{
			dynamic myInstance = new ExpandoObject();
			myInstance.MyMethod = new Func<string>(() => "hello world");

			var binder = Binder.InvokeMember(
				CSharpBinderFlags.None,
				"MyMethod",
				null,
				this.GetType(),
				new[] { CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.Constant, null) });

			var methodCallExpression = Expression.Dynamic(binder, typeof(object), Expression.Constant(myInstance));
			var expression = Expression.Lambda(methodCallExpression);

			Assert.AreEqual(myInstance.MyMethod(), expression.Compile().DynamicInvoke());
		}

		public class TestDynamicClass : DynamicObject
		{
			public string RealProperty { get; set; }

			public override DynamicMetaObject GetMetaObject(Expression parameter)
			{
				throw new Exception("This should not be called");
			}
			public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
			{
				throw new Exception("This should not be called");
			}
			public override bool TryGetMember(GetMemberBinder binder, out object result)
			{
				throw new Exception("This should not be called");
			}
		}
	}
}
