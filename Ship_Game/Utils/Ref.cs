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
            object obj = (body.Expression as ConstantExpression)?.Value;

            if (body.Member is PropertyInfo) // expression is a property
            {
                var prop = (PropertyInfo)body.Member;
                Getter = () => (T)prop.GetValue(obj, BindingFlags.Default, null, null, null);
                Setter =  x => prop.SetValue(obj, x, BindingFlags.Default, null, null, null);
            }
            else if (body.Member is FieldInfo) // expression is a regular variable
            {
                var field = (FieldInfo)body.Member;
                Getter = () => (T)field.GetValue(obj);
                Setter =  x => field.SetValue(obj, x);
            }
            else
            {
                throw new MemberAccessException("Unbindable reference. Perhaps the bind expression is too complex?");
            }
        }
    }
}