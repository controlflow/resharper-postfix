using System;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.PostfixTemplates
{
  public static class TypeUtils
  {
    public static TypeInstantiability IsInstantiable(
      [NotNull] IType type, [NotNull] ITreeNode expression)
    {
      var declaredType = type as IDeclaredType;
      if (declaredType != null)
      {
        var typeElement = declaredType.GetTypeElement();
        if (typeElement != null)
        {
          return IsInstantiable(typeElement, expression);
        }
      }

      return TypeInstantiability.NotInstantiable;
    }

    public static TypeInstantiability IsInstantiable(
      [NotNull] ITypeElement typeElement, [NotNull] ITreeNode expression)
    {
      if (typeElement is IStruct || typeElement is IEnum || typeElement is IClass)
      {
        // filter out abstract classes
        var classType = typeElement as IClass;
        if (classType != null && classType.IsAbstract)
          return TypeInstantiability.NotInstantiable;

        // check type has any constructor accessable
        var accessContext = new ElementAccessContext(expression);

        var instantiability = TypeInstantiability.NotInstantiable;
        foreach (var constructor in typeElement.Constructors)
        {
          if (constructor.IsStatic) continue;
          if (AccessUtil.IsSymbolAccessible(constructor, accessContext))
          {
            var parametersCount = constructor.Parameters.Count;
            instantiability |= (parametersCount == 0)
              ? TypeInstantiability.DefaultCtor
              : TypeInstantiability.CtorWithParameters;
          }
        }

        return instantiability;
      }

      return TypeInstantiability.NotInstantiable;
    }
  }

  [Flags] public enum TypeInstantiability
  {
    NotInstantiable,
    DefaultCtor,
    CtorWithParameters
  }
}