using System;
using System.IO;
using Tao.OpenGl;

namespace WSIMap
{
	/**
	 * \class Feature
	 * \brief The abstract base class for map features
	 */
	[Serializable] public abstract class Feature
	{
		#region Data Members
		internal string id;
#if WSIMAP_OBJECT_TRACKING
		internal Guid uniqueId;
#endif
		internal bool draw;
        internal bool visible;
        protected object tag;
        protected int filterID;
		protected string featureName;
		protected string featureInfo;
		protected int numVertices;
		protected int openglDisplayList;
		protected bool useToolTip;
		protected const double deg2rad = Math.PI / 180;
        protected const double deg2mi = FUL.Utils.EarthRadius_sm * deg2rad;
        protected bool Updated = false;
#if TRACK_OPENGL_DISPLAY_LISTS
		// Don't need concurrent dictionary since all successful interactions should be on main (UI) thread
		private static int mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
		private static System.Collections.Generic.Dictionary<string, int> displayListCountMap = new System.Collections.Generic.Dictionary<string, int>();
		private static long openGLCreations = 0;
		private const long OPENGL_ALLOC_LOGGING_PERIOD = 1000;
#endif
		#endregion

		public Feature()
		{
            this.tag = null;
			this.id = null;
#if WSIMAP_OBJECT_TRACKING
			this.uniqueId = Guid.NewGuid();
#endif
			this.draw = true;
			this.featureInfo = string.Empty;
			this.featureName = string.Empty;
			this.openglDisplayList = -1;
			this.useToolTip = false;
            this.visible = true;
            this.filterID = -1;
		}

        public object Tag
        {
            get { return tag; }
            set { tag = value; }
        }

        public int FilterID
        {
            get { return filterID; }
            set { filterID = value; }
        }

		public string FeatureName
		{
			get	{ return featureName;	}
			set	{ featureName = value; }
		}

		public string FeatureInfo
		{
			get	{ return featureInfo;	}
			set { featureInfo = FUL.Utils.WrapString(value, 83); }
		}

		public int NumVertices
		{
			get { return numVertices; }
		}

		public bool ToolTip
		{
			get { return useToolTip; }
			set { useToolTip = value; }
		}

		public string Id
		{
			get { return id; }
		}

		public bool Visible
		{
			get { return visible; }
			set { visible = value; }
		}

		internal virtual RectangleD GetBoundingRect(MapGL parentMap)
		{
			return null;
		}

		internal float glc(byte color)	// convert byte color to OpenGL float color
		{
			return ((float)color) / 255f;
		}

#if WSIMAP_OBJECT_TRACKING
		internal void Log(string logEntryPrefix)
		{
			// Hard coded log file name that will be written to the same directory as WSI Fusion Client.exe.
			// WARNING: This might not work on all systems due to file system permissions.
			const string logFileName = "DisplayListLog.txt";

			// Look for a stack entry from the Fusion client.
			const string startsWith = "at DispatchManager.";

			// Get the stack trace and split it into individual lines
			string stack = Environment.StackTrace;
			string[] stackFrames = stack.Split(new char[] { '\n' });

			// Starting from the top of the stack, find the first entry that is from the Fusion client.
			string stackFrame = string.Empty;
			for (int i = 2; i < stackFrames.Length; i++)
			{
				if (stackFrames[i].TrimStart().StartsWith(startsWith, StringComparison.CurrentCultureIgnoreCase))
				{
					stackFrame = stackFrames[i].TrimStart();
					break;
				}
			}

			// Create the log entry and append it to the log file. The log entry contains
			// a prefix, the class name, an object ID and a stack frame from above.
			string logEntry = logEntryPrefix + ";" + this.GetType().ToString() + ";" + uniqueId.ToString() + ";" + stackFrame;
			System.IO.File.AppendAllText(logFileName, logEntry);
		}
#endif

		protected void DeleteOpenGLDisplayList(String contextString)
		{
			if (openglDisplayList != -1)
			{
				Gl.glDeleteLists(openglDisplayList, 1);
#if TRACK_OPENGL_DISPLAY_LISTS
				ConfirmMainThread(contextString + " DeleteOpenGLDisplayList()");
				int glError = Gl.glGetError();
				if (glError == 0)
					displayListCountMap[contextString] = displayListCountMap.ContainsKey(contextString) ? displayListCountMap[contextString] - 1 : 0;
				else
					LogOpenGLMessage("OpenGL Error Deleting " + contextString + ": " + glError);
#endif
			}

			openglDisplayList = -1; // reset DL even if fail since Delete operation is logically expected to free the list
		}

		protected void CreateOpenGLDisplayList(String contextString)
		{
			if (openglDisplayList != -1)
			{
				Gl.glDeleteLists(openglDisplayList, 1);
#if TRACK_OPENGL_DISPLAY_LISTS
				ConfirmMainThread(contextString + " CreateOpenGLDisplayList()");
				int glError = Gl.glGetError();
				if (glError == 0)
					displayListCountMap[contextString] = displayListCountMap.ContainsKey(contextString) ? displayListCountMap[contextString] - 1 : 0;
				else
					LogOpenGLMessage("OpenGL Error Deleting (prior to Creating) " + contextString + ": " + glError);

				if (++openGLCreations % OPENGL_ALLOC_LOGGING_PERIOD == 0)
				{
					foreach(string key in displayListCountMap.Keys)
					{
						LogOpenGLMessage(key + "==>" + displayListCountMap[key]);
					}
				}
#endif
			}

			openglDisplayList = Gl.glGenLists(1);
#if TRACK_OPENGL_DISPLAY_LISTS
			if (-1 != openglDisplayList)
				displayListCountMap[contextString] = displayListCountMap.ContainsKey(contextString) ? displayListCountMap[contextString] + 1 : 1;
			else
				LogOpenGLMessage("OpenGL Error Creating " + contextString + ": " + Gl.glGetError());
#endif
		}

#if TRACK_OPENGL_DISPLAY_LISTS
		public static bool ConfirmMainThread(String contextString)
		{
			System.Threading.Thread currentThread = System.Threading.Thread.CurrentThread;
			if (currentThread.GetApartmentState() != System.Threading.ApartmentState.STA ||
				currentThread.ManagedThreadId != mainThreadId ||
				currentThread.IsBackground || 
				currentThread.IsThreadPoolThread)
			{
				LogOpenGLMessage("Non-main thread:" + currentThread.ManagedThreadId + " Context:" + contextString);
				return false;
			}

			// assume this is the main thread
			return true;
		}

		private static void LogOpenGLMessage(string message)
		{
			try
			{
				using (StreamWriter outputFile = new StreamWriter("OpenGLDisplayListTracking.log", true))
				{
					if (outputFile != null)
						outputFile.WriteLine(DateTime.UtcNow.ToString() + ": " + message);
				}
			}
			catch { }
		}
#endif

		abstract internal void Draw(MapGL parentMap, Layer parentLayer);
	}
}
