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
    public static CanInstantiate CanInstantiateType([NotNull] IType type, [NotNull] ITreeNode expression)
    {
      var declaredType = type as IDeclaredType;
      if (declaredType != null)
      {
        var typeElement = declaredType.GetTypeElement();
        if (typeElement != null)
        {
          return CanInstantiateType(typeElement, expression);
        }
      }

      return CanInstantiate.No;
    }

    public static CanInstantiate CanInstantiateType([NotNull] ITypeElement typeElement, [NotNull] ITreeNode expression)
    {
      if (typeElement is IStruct || typeElement is IEnum || typeElement is IClass)
      {
        // filter out abstract classes
        var classType = typeElement as IClass;
        if (classType != null && classType.IsAbstract) return CanInstantiate.No;

        // check type has any constructor accessible
        var accessContext = new ElementAccessContext(expression);
        var canInstantiate = CanInstantiate.No;

        foreach (var constructor in typeElement.Constructors)
        {
          if (constructor.IsStatic) continue;
          if (AccessUtil.IsSymbolAccessible(constructor, accessContext))
          {
            var parametersCount = constructor.Parameters.Count;
            canInstantiate |= (parametersCount == 0)
              ? CanInstantiate.DefaultConstructor
              : CanInstantiate.ConstructorWithParameters;
          }
        }

        return canInstantiate;
      }

      return CanInstantiate.No;
    }

    public static bool IsUsefulToCreateWithNew([CanBeNull] ITypeElement typeElement)
    {
      if (typeElement == null) return false;
      if (typeElement is IEnum) return false;

      var type = TypeFactory.CreateType(typeElement);
      if (type.IsSimplePredefined()) return false;

      return true;
    }
  }

  [Flags]
  public enum CanInstantiate
  {
    No,
    DefaultConstructor,
    ConstructorWithParameters
  }
}