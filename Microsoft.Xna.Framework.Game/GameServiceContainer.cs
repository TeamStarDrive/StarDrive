// Decompiled with JetBrains decompiler
// Type: Microsoft.Xna.Framework.GameServiceContainer
// Assembly: Microsoft.Xna.Framework.Game, Version=3.1.0.0, Culture=neutral, PublicKeyToken=6d5c3888ef60e27d
// MVID: E4BD910E-73ED-465E-A91E-14AAAB0CE109
// Assembly location: C:\WINDOWS\assembly\GAC_32\Microsoft.Xna.Framework.Game\3.1.0.0__6d5c3888ef60e27d\Microsoft.Xna.Framework.Game.dll

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.Xna.Framework
{
  public class GameServiceContainer : IServiceProvider
  {
    private Dictionary<Type, object> services = new Dictionary<Type, object>();

    public void AddService(Type type, object provider)
    {
      if (type == null)
        throw new ArgumentNullException("type", Resources.ServiceTypeCannotBeNull);
      if (provider == null)
        throw new ArgumentNullException("provider", Resources.ServiceProviderCannotBeNull);
      if (this.services.ContainsKey(type))
        throw new ArgumentException(Resources.ServiceAlreadyPresent, "type");
      if (!type.IsAssignableFrom(provider.GetType()))
        throw new ArgumentException(string.Format((IFormatProvider) CultureInfo.CurrentUICulture, Resources.ServiceMustBeAssignable, new object[2]
        {
          (object) provider.GetType().FullName,
          (object) type.GetType().FullName
        }));
      this.services.Add(type, provider);
    }

    public void RemoveService(Type type)
    {
      if (type == null)
        throw new ArgumentNullException("type", Resources.ServiceTypeCannotBeNull);
      this.services.Remove(type);
    }

    public object GetService(Type type)
    {
      if (type == null)
        throw new ArgumentNullException("type", Resources.ServiceTypeCannotBeNull);
      if (this.services.ContainsKey(type))
        return this.services[type];
      return (object) null;
    }
  }
}
