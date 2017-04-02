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
			get { return Getter(); }
			set { Setter(value);   }
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
            var body = (MemberExpression)fieldPropExpression.Body;

            // try to get the instance of the expression,
            // for instance fields/properties this should never be null
            object obj = body.Expression?.GetTargetInstance();

            var member = body.Member;
            if (member is PropertyInfo propInfo) // expression is a property
            {
                var prop = propInfo; // VS2017 RC requires a copy for lambda capture
                Getter = () => (T)prop.GetValue(obj, BindingFlags.Default, null, null, null);
                Setter =  x => prop.SetValue(obj, x, BindingFlags.Default, null, null, null);
            }
            else if (member is FieldInfo fieldInfo) // expression is a regular variable
            {
                var field = fieldInfo;
                Getter = () => (T)field.GetValue(obj);
                Setter = x => field.SetValue(obj, x);
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