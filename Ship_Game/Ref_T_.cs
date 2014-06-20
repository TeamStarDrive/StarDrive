using System;

namespace Ship_Game
{
	public class Ref<T>
	{
		private Func<T> getter;

		private Action<T> setter;

		public T Value
		{
			get
			{
				return this.getter();
			}
			set
			{
				this.setter(value);
			}
		}

		public Ref(Func<T> getter, Action<T> setter)
		{
			this.getter = getter;
			this.setter = setter;
		}
	}
}