using System.CodeDom;
using System.Collections.Generic;

public class ParameterTypeComparer : IEqualityComparer<CodeParameterDeclarationExpression>
{
	public bool Equals (CodeParameterDeclarationExpression x, CodeParameterDeclarationExpression y)
	{
		return x.Type.BaseType == y.Type.BaseType;
	}

	public int GetHashCode (CodeParameterDeclarationExpression obj)
	{
		return obj.Type.GetHashCode ();
	}
}