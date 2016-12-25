using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Ship_Game
{
	public sealed class Ref<T>
	{
		private readonly Func<T> Getter;
		private readonly Action<T> Setter;

		public T Value
		{
			get
			{
				return Getter();
			}
			set
			{
				Setter(value);
			}
		}

		public Ref(Func<T> getter, Action<T> setter)
		{
			Getter = getter;
			Setter = setter;
		}

        // Create a property reference to a GLOBAL STATIC FIELD or PROPERTY
        public Ref(Expression<Func<T>> fieldPropExpression) : this(null, fieldPropExpression)
        {
        }

        // Create an instance property or field reference
        public Ref(object obj, Expression<Func<T>> fieldPropExpression)
        {
            var expr = (MemberExpression)fieldPropExpression.Body;
            if (expr.Member is PropertyInfo)
            {
                var prop = (PropertyInfo)expr.Member;
                Getter = () => (T)prop.GetValue(obj);
                Setter = x  => prop.SetValue(obj, x);
            }
            else if (expr.Member is FieldInfo)
            {
                var field = (FieldInfo)expr.Member;
                Getter = () => (T) field.GetValue(obj);
                Setter = x  => field.SetValue(obj, x);
            }
            else
            {
                throw new MemberAccessException("Unbindable reference. Perhaps the bind expression is too complex?");
            }
        }
    }
}