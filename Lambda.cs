using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Interpreter.Exceptions;

namespace Interpreter
{
	// Represents a lambda expression that can be invoked. This class is thread safe.
	public class Lambda
	{
		private readonly Expression _expression;
		private readonly ParserArguments _parserArguments;

		private readonly Delegate _delegate;

		internal Lambda(Expression expression, ParserArguments parserArguments)
		{
			if (expression == null)
				throw new ArgumentNullException("expression");
			if (parserArguments == null)
				throw new ArgumentNullException("parserArguments");

			_expression = expression;
			_parserArguments = parserArguments;

			// Note: I always compile the generic lambda. Maybe in the future this can be a setting because if I generate a typed delegate this compilation is not required.
			var lambdaExpression = Expression.Lambda(_expression, _parserArguments.UsedParameters.Select(p => p.Expression).ToArray());
			_delegate = lambdaExpression.Compile();
		}

		public Expression Expression { get { return _expression; } }
		public bool CaseInsensitive { get { return _parserArguments.Settings.CaseInsensitive; } }
		public string ExpressionText { get { return _parserArguments.ExpressionText; } }
		public Type ReturnType { get { return _delegate.Method.ReturnType; } }

		// Gets the parameters actually used in the expression parsed.
		[Obsolete("Use UsedParameters or DeclaredParameters")]
		public IEnumerable<Parameter> Parameters { get { return _parserArguments.UsedParameters; } }

		// Gets the parameters actually used in the expression parsed.
		public IEnumerable<Parameter> UsedParameters { get { return _parserArguments.UsedParameters; } }
		// Gets the parameters declared when parsing the expression.
		public IEnumerable<Parameter> DeclaredParameters { get { return _parserArguments.DeclaredParameters; } }

		public IEnumerable<ReferenceType> Types { get { return _parserArguments.UsedTypes; } }
		public IEnumerable<Identifier> Identifiers { get { return _parserArguments.UsedIdentifiers; } }

		public object Invoke()
		{
			return InvokeWithUsedParameters(new object[0]);
		}

		public object Invoke(params Parameter[] parameters)
		{
			return Invoke((IEnumerable<Parameter>)parameters);
		}

		public object Invoke(IEnumerable<Parameter> parameters)
		{
			var args = (from usedParameter in UsedParameters
				from actualParameter in parameters
				where usedParameter.Name.Equals(actualParameter.Name, _parserArguments.Settings.KeyComparison)
				select actualParameter.Value)
				.ToArray();

			return InvokeWithUsedParameters(args);
		}

		// Invoke the expression with the given parameters values.
		public object Invoke(params object[] args)
		{
			var parameters = new List<Parameter>();
			var declaredParameters = DeclaredParameters.ToArray();

			if (args != null)
			{
				if (declaredParameters.Length != args.Length)
					throw new InvalidOperationException("Arguments count mismatch.");

				for (var i = 0; i < args.Length; i++)
				{
					var parameter = new Parameter(
						declaredParameters[i].Name,
						declaredParameters[i].Type,
						args[i]);

					parameters.Add(parameter);
				}
			}

			return Invoke(parameters);
		}

		private object InvokeWithUsedParameters(object[] orderedArgs)
		{
			try
			{
				return _delegate.DynamicInvoke(orderedArgs);
			}
			catch (TargetInvocationException exc)
			{
				if (exc.InnerException != null)
					ExceptionDispatchInfo.Capture(exc.InnerException).Throw();

				throw;
			}
		}

		public override string ToString()
		{
			return ExpressionText;
		}

		// Generate the given delegate by compiling the lambda expression.
		public TDelegate Compile<TDelegate>()
		{
			var lambdaExpression = LambdaExpression<TDelegate>();
			return lambdaExpression.Compile();
		}

		[Obsolete("Use Compile<TDelegate>()")]
		public TDelegate Compile<TDelegate>(IEnumerable<Parameter> parameters)
		{
			var lambdaExpression = Expression.Lambda<TDelegate>(_expression, parameters.Select(p => p.Expression).ToArray());
			return lambdaExpression.Compile();
		}

		// Generate a lambda expression.
		public Expression<TDelegate> LambdaExpression<TDelegate>()
		{
			return Expression.Lambda<TDelegate>(_expression, DeclaredParameters.Select(p => p.Expression).ToArray());
		}
	}
}
