using System;

namespace AniDBmini.Collections
{
	public class DebugLine
	{
		private string time, message;

		public DebugLine(string _t, string _m)
		{
			time = _t;
			message = _m;
		}

		public string Time { get { return time; } }
		public string Message { get { return message; } }
	}
}
