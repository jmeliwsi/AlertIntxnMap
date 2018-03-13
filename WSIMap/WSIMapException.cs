using System;

namespace WSIMap
{
	/**
	 * \class WSIMapException
	 * \brief Used for exception thrown by the WSIMap Library
	 */
	public class WSIMapException : ApplicationException
	{
		public WSIMapException() : base()
		{
		}

		public WSIMapException(string message) : base(message)
		{
		}
	}
}
