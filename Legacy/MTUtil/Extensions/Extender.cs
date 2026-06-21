using System;
using System.Collections.Generic;
using System.Text;

namespace MTUtil.Extensions
{
#if _TARGET_DOTNET_3_5_
	public static class Extender
	{
		//public delegate T AssignAllDelegate<T>(int i);

		public static void AssignAll<T>(this IList<T> list, Func<int,T> assignFn)
		{
			for(int i = 0; i < list.Count; i++)
				list[i] = assignFn(i);
		}
	}
#endif
}
