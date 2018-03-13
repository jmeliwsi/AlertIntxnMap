using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WSIMap
{
	public interface IProjectable
	{
		// This interface is implemented by WSIMap classes that support map
		// projections.  The interface is largely cosmetic since its primary
		// purpose is to show which classes support map projections when
		// viewing a class diagram of WSIMap.
		MapProjections MapProjection
		{
			get;
		}
	}
}
