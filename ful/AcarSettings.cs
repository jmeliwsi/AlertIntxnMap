using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FUL
{
	public class AcarSettings
	{
		private string airlineIATA;
		public string AirlineIATA
		{
			set { airlineIATA = value; }
			get { return airlineIATA; }
		}

		private bool hasActiveFlightRule;
		public bool HasActiveRules
		{
			set { hasActiveFlightRule = value; }
			get { return hasActiveFlightRule; }
		}

		private int preflightMin;
		public int PreflightMinutes
		{
			set { preflightMin = value; }
			get { return preflightMin; }
		}

		private int landedFlightMin;
		public int LandedFlightMinutes
		{
			set { landedFlightMin = value; }
			get { return landedFlightMin; }
		}

		private int earliestPreflightMin;
		public int EarliestPreMinutes
		{
			set { earliestPreflightMin = value; }
			get { return earliestPreflightMin; }
		}

        private int lastFlightMin;
        public int LastFlightMinutes
        {
            set { lastFlightMin = value; }
            get { return lastFlightMin; }
        }

		private string invalidCharacters;
		public string InvalidCharacters
		{
			set { invalidCharacters = value; }
			get { return invalidCharacters; }
		}

		private string validCharacters;
		public string ValidCharacters
		{
			set { validCharacters = value; }
			get { return validCharacters; }
		}

		private int maxTextSize;
		public int MaximumTextSize
		{
			set { maxTextSize = value; }
			get { return maxTextSize; }
		}

		private int maxLines;
		public int MaximumLines
		{
			set { maxLines = value; }
			get { return maxLines; }
		}

		private int maxSizePerLine;
		public int MaximumSizePerLine
		{
			set { maxSizePerLine = value; }
			get { return maxSizePerLine; }
		}

		private bool hasControl;
		public bool HasControl
		{
			set { hasControl = value; }
			get { return hasControl; }
		}

		private bool hasThread;
		public bool HasThread
		{
			set { hasThread = value; }
			get { return hasThread; }
		}

		private bool hasInbox;
		public bool HasInbox
		{
			set { hasInbox = value; }
			get { return hasInbox; }
		}

		private bool hasSent;
		public bool HasSent
		{
			set { hasSent = value; }
			get { return hasSent; }
		}

		private bool hasSearch;
		public bool HasSearch
		{
			set { hasSearch = value; }
			get { return hasSearch; }
		}

		private bool messagePopup;
		public bool MessagePopup
		{
			set { messagePopup = value; }
			get { return messagePopup; }
		}

		private int messagePopupMinutes;
		public int MessagePopupMinutes
		{
			set { messagePopupMinutes = value; }
			get { return messagePopupMinutes; }
		}

		public bool MessageChime
		{
			get { return !string.IsNullOrEmpty(messageChimeFileName); }
		}

		public bool MessageRechime
		{
			get { return MessageChimeMinutes > 0; }
		}

		private int messageChimeMinutes;
		public int MessageChimeMinutes
		{
			set { messageChimeMinutes = value; }
			get { return messageChimeMinutes; }
		}

		private string messageChimeFileName;
		public string MessageChimeFileName
		{
			set { messageChimeFileName = value; }
			get { return messageChimeFileName; }
		}

		private string messageRechimeFileName;
		public string MessageRechimeFileName
		{
			set { messageRechimeFileName = value; }
			get { return messageRechimeFileName; }
		}

		private int timeThreshold;
		public int TimeThreshold
		{
			set { timeThreshold = value; }
			get { return timeThreshold; }
		}
	}
}
