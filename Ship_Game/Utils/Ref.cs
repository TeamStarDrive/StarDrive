using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Ship_Game
{
	public sealed class Ref<T>
	{
		private readonly Func<T>   Getter;
		private readonly Action<T> Setter;

		public T Value
		{
			get => Getter();
		    set => Setter(value);
		}

		public Ref(Func<T> getter, Action<T> setter)
		{
			Getter = getter;
			Setter = setter;
		}

        // Create a property reference to a FIELD or PROPERTY
        // Usage:
        //   new Ref<bool>( () => this.BoolValueToBind );
        public Ref(Expression<Func<T>> fieldPropExpression)
        {
            Getter = fieldPropExpression.Compile();

            // we need to extract the body and capture it inside
            // Setter lambdas. The lambdas need to evaluate the expressions
            // again dynamically to handle reference changes, eg:
            // `() => this.ActiveHull.CarrierOnly`  <-- ActiveHull may change
            var body = (MemberExpression)fieldPropExpression.Body;

            MemberInfo member = body.Member;
            if (member is PropertyInfo propInfo) // expression is a property
            {
                PropertyInfo prop = propInfo; // VS2017 RC requires a copy for lambda capture
                Setter = (x) => prop.SetValue(body.Expression.GetTargetInstance(), x, BindingFlags.Default, null, null, null);
            }
            else if (member is FieldInfo fieldInfo) // expression is a regular variable
            {
                FieldInfo field = fieldInfo;
                Setter = (x) => field.SetValue(body.Expression.GetTargetInstance(), x);
            }
            else
            {
                throw new MemberAccessException("Unbindable reference. Perhaps the bind expression is too complex?");
            }
        }
    }

    internal static class RefHelper
    {
        internal static object GetTargetInstance(this Expression expr)
        {
            if (expr is ConstantExpression constexpr)
                return constexpr.Value;

            if (expr is MemberExpression membexpr)
            {
                object obj = membexpr.Expression?.GetTargetInstance();
                var member = membexpr.Member;

                if (member is PropertyInfo propInfo)
                    return propInfo.GetValue(obj, BindingFlags.Default, null, null, null);

                if (member is FieldInfo fieldInfo)
                    return fieldInfo.GetValue(obj);
            }
            return null;
        }
    }
}