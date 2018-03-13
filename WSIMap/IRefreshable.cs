using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WSIMap
{
	// This interface is implemented by WSIMap classes that use OpenGL display
	// lists (retained mode drawing).  This does not include classes that use
	// static display lists such as the Symbol class.
	public interface IRefreshable : IDisposable
	{
		void Refresh(MapProjections mapProjection, short centralLongitude);
	}
}
