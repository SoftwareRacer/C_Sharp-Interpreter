﻿using Interpreter.Parsing;
using Interpreter.Reflection;
using Interpreter.Visitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Interpreter.Exceptions;

namespace Interpreter
{
	// Class used to parse and compile a text expression into an Expression or a Delegate that can be invoked. Expression are written using a subset of C# syntax.
	// Only get properties, Parse and Eval methods are thread safe.
	public class Interpreter
	{
		private readonly ParserSettings _settings;
		private readonly ISet<ExpressionVisitor> _visitors = new HashSet<ExpressionVisitor>();

		#region Constructors
		// Creates a new Interpreter using InterpreterOptions.Default.
		public Interpreter()
			: this(InterpreterOptions.Default)
		{
		}

		// Creates a new Interpreter using the specified options.
		public Interpreter(InterpreterOptions options)
		{
			var caseInsensitive = options.HasFlag(InterpreterOptions.CaseInsensitive);

			_settings = new ParserSettings(caseInsensitive);

			if ((options & InterpreterOptions.SystemKeywords) == InterpreterOptions.SystemKeywords)
			{
				SetIdentifiers(LanguageConstants.Literals);
			}

			if ((options & InterpreterOptions.PrimitiveTypes) == InterpreterOptions.PrimitiveTypes)
			{
				Reference(LanguageConstants.PrimitiveTypes);
				Reference(LanguageConstants.CSharpPrimitiveTypes);
			}

			if ((options & InterpreterOptions.CommonTypes) == InterpreterOptions.CommonTypes)
			{
				Reference(LanguageConstants.CommonTypes);
			}

			_visitors.Add(new DisableReflectionVisitor());
		}
		#endregion

		#region Properties
		public bool CaseInsensitive
		{
			get
			{
				return _settings.CaseInsensitive;
			}
		}

		// Gets a list of registeres types. Add types by using the Reference method.
		public IEnumerable<ReferenceType> ReferencedTypes
		{
			get
			{
				return _settings.KnownTypes
					.Select(p => p.Value)
					.ToList();
			}
		}

		// Gets a list of known identifiers. Add identifiers using SetVariable, SetFunction or SetExpression methods.
		public IEnumerable<Identifier> Identifiers
		{
			get
			{
				return _settings.Identifiers
					.Select(p => p.Value)
					.ToList();
			}
		}

		// Gets the available assignment operators.
		public AssignmentOperators AssignmentOperators
		{
			get { return _settings.AssignmentOperators; }
		}
		#endregion

		#region Options
		/* Allows to enable/disable assignment operators. 
		For security when expression are generated by the users is more safe to disable assignment operators.
		<param name="assignmentOperators"></param>
		<returns></returns>
		*/
		public Interpreter EnableAssignment(AssignmentOperators assignmentOperators)
		{
			_settings.AssignmentOperators = assignmentOperators;

			return this;
		}
		#endregion

		#region Visitors
		public ISet<ExpressionVisitor> Visitors
		{
			get { return _visitors; }
		}

		// Enable reflection expression (like x.GetType().GetMethod() or typeof(double).Assembly) by removing the DisableReflectionVisitor.
		public Interpreter EnableReflection()
		{
			var visitor = Visitors.FirstOrDefault(p => p is DisableReflectionVisitor);
			if (visitor != null)
				Visitors.Remove(visitor);

			return this;
		}
		#endregion

		#region Register identifiers
		// Allow the specified function delegate to be called from a parsed expression.
		public Interpreter SetFunction(string name, Delegate value)
		{
			return SetVariable(name, value);
		}

		// Allow the specified variable to be used in a parsed expression.
		public Interpreter SetVariable(string name, object value)
		{
			if (string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException(nameof(name));

			return SetExpression(name, Expression.Constant(value));
		}

		// Allow the specified variable to be used in a parsed expression.
		public Interpreter SetVariable(string name, object value, Type type)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));
			if (string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException(nameof(name));

			return SetExpression(name, Expression.Constant(value, type));
		}

		// Allow the specified Expression to be used in a parsed expression.
		// Basically add the specified expression as a known identifier.
		public Interpreter SetExpression(string name, Expression expression)
		{
			return SetIdentifier(new Identifier(name, expression));
		}

		// Allow the specified list of identifiers to be used in a parsed expression.
		// Basically add the specified expressions as a known identifier.
		public Interpreter SetIdentifiers(IEnumerable<Identifier> identifiers)
		{
			foreach (var i in identifiers)
				SetIdentifier(i);

			return this;
		}

		// Allow the specified identifier to be used in a parsed expression.
		// Basically add the specified expression as a known identifier.
		public Interpreter SetIdentifier(Identifier identifier)
		{
			if (identifier == null)
				throw new ArgumentNullException(nameof(identifier));

			if (LanguageConstants.ReservedKeywords.Contains(identifier.Name))
				throw new InvalidOperationException($"{identifier.Name} is a reserved word");

			_settings.Identifiers[identifier.Name] = identifier;

			return this;
		}
		#endregion

		#region Register referenced types
		// Allow the specified type to be used inside an expression. The type will be available using its name.
		// If the type contains method extensions methods they will be available inside expressions.
		public Interpreter Reference(Type type)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));

			return Reference(type, type.Name);
		}

		// Allow the specified type to be used inside an expression.
		// See Reference(Type, string) method.
		public Interpreter Reference(IEnumerable<ReferenceType> types)
		{
			if (types == null)
				throw new ArgumentNullException(nameof(types));

			foreach (var t in types)
				Reference(t);

			return this;
		}

		// Allow the specified type to be used inside an expression by using a custom alias.
		// If the type contains extensions methods they will be available inside expressions.
		public Interpreter Reference(Type type, string typeName)
		{
			return Reference(new ReferenceType(typeName, type));
		}

		// Allow the specified type to be used inside an expression by using a custom alias.
		// If the type contains extensions methods they will be available inside expressions.
		public Interpreter Reference(ReferenceType type)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));

			_settings.KnownTypes[type.Name] = type;

			foreach (var extensionMethod in type.ExtensionMethods)
			{
				_settings.ExtensionMethods.Add(extensionMethod);
			}

			return this;
		}
		#endregion

		#region Parse
		// Parse a text expression and returns a Lambda class that can be used to invoke it.
		public Lambda Parse(string expressionText, params Parameter[] parameters)
		{
			return Parse(expressionText, typeof(void), parameters);
		}

		// Parse a text expression and returns a Lambda class that can be used to invoke it.
		// If the expression cannot be converted to the type specified in the expressionType parameter
		// an exception is throw.
		public Lambda Parse(string expressionText, Type expressionType, params Parameter[] parameters)
		{
			return ParseAsLambda(expressionText, expressionType, parameters);
		}

		[Obsolete("Use ParseAsDelegate<TDelegate>(string, params string[])")]
		public TDelegate Parse<TDelegate>(string expressionText, params string[] parametersNames)
		{
			return ParseAsDelegate<TDelegate>(expressionText, parametersNames);
		}

		// Parse a text expression and convert it into a delegate.
		public TDelegate ParseAsDelegate<TDelegate>(string expressionText, params string[] parametersNames)
		{
			var lambda = ParseAs<TDelegate>(expressionText, parametersNames);
			return lambda.Compile<TDelegate>();
		}

		// Parse a text expression and convert it into a lambda expression.
		public Expression<TDelegate> ParseAsExpression<TDelegate>(string expressionText, params string[] parametersNames)
		{
			var lambda = ParseAs<TDelegate>(expressionText, parametersNames);
			return lambda.LambdaExpression<TDelegate>();
		}

		public Lambda ParseAs<TDelegate>(string expressionText, params string[] parametersNames)
		{
			var delegateInfo = ReflectionExtensions.GetDelegateInfo(typeof(TDelegate), parametersNames);

			return ParseAsLambda(expressionText, delegateInfo.ReturnType, delegateInfo.Parameters);
		}
		#endregion

		#region Eval
		// Parse and invoke the specified expression.
		public object Eval(string expressionText, params Parameter[] parameters)
		{
			return Eval(expressionText, typeof(void), parameters);
		}

		// Parse and invoke the specified expression.
		public T Eval<T>(string expressionText, params Parameter[] parameters)
		{
			return (T)Eval(expressionText, typeof(T), parameters);
		}

		// Parse and invoke the specified expression.
		public object Eval(string expressionText, Type expressionType, params Parameter[] parameters)
		{
			return Parse(expressionText, expressionType, parameters).Invoke(parameters);
		}
		#endregion

		#region Detection
		public IdentifiersInfo DetectIdentifiers(string expression)
		{
			var detector = new Detector(_settings);

			return detector.DetectIdentifiers(expression);
		}
		#endregion

		#region Private methods

		private Lambda ParseAsLambda(string expressionText, Type expressionType, Parameter[] parameters)
		{
			var arguments = new ParserArguments(
												expressionText,
												_settings,
												expressionType,
												parameters);

			var expression = Parser.Parse(arguments);

			foreach (var visitor in Visitors)
				expression = visitor.Visit(expression);

			var lambda = new Lambda(expression, arguments);

			#if TEST_DetectIdentifiers
				AssertDetectIdentifiers(lambda);
			#endif

			return lambda;
		}

		#if TEST_DetectIdentifiers
		private void AssertDetectIdentifiers(Lambda lambda)
		{
			var info = DetectIdentifiers(lambda.ExpressionText);

			if (info.Identifiers.Count() != lambda.Identifiers.Count())
				throw new Exception("Detected identifiers doesn't match actual identifiers");
			if (info.Types.Count() != lambda.Types.Count())
				throw new Exception("Detected types doesn't match actual types");
			if (info.UnknownIdentifiers.Count() != lambda.UsedParameters.Count())
				throw new Exception("Detected unknown identifiers doesn't match actual parameters");
		}
		#endif
		#endregion
	}
}
