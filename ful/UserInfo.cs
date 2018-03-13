using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace FUL
{
	public class UserInfo
	{
		public enum DeskPermission { None = 1, Administrator, ChangeDefaultDesk, ChangeCurrentDesk };
		public enum UserPermission
		{
			AlertOptOut = 1,
			SaveAdminAlert,
			WriteAdminDesk,
			ChangeDeskFromAssigned,
			WriteAdminWorkspace,
			WriteSharedWorkspace,
			WriteSharedView,
			WriteSharedFilter,
			AddRemoveUser,
			WriteAdminView,
			CreateRole,
			WriteSharedAirportList,
			WriteAirportThreshold,
			ChangeCurrentDesk,
			PublishAirportCondition,
			PublishWarningArea,
			ChangeLightningTimeSpan,
			ChangeWeatherColor,
			ChangeAirportMinimum,
			ChangeAirspaceColor,
			WriteSharedAdHocRoute = 22,
			AdminTool,
			RerouteMonitor,
			FreeTextMessage,
			RouteSelection,
			NOTAM,
			EmailAlert,
			AudibleAlert,
			MultipleRadar,
			ChangeSelectiveDownload,
			DispatchBrief,
			ArchiveViewer,
			WxPacket,
			Replay,
			CustomerDocuments,
			SurfaceMovement,
			OverrideFlightInfo = 40,
            HideDiversions,
			UserFeedback,
            ChangeFilterDataSource,  //43
            WritePirep,
            ViewPirep,
			DispatchbriefCharts,
			ShowAirportList,
            ATFMTool,  //48
            USER_PERMISSION
		}

		private int customerid;
		public int CustomerID
		{
			get { return (customerid); }
			set { customerid = value; }
		}

		private int userid;
		public int UserID
		{
			get { return (userid); }
			set { userid = value; }
		}

		private Guid token;
		public Guid Token
		{
			get { return (token); }
			set { token = value; }
		}

		private Guid replayToken;
		public Guid ReplayToken
		{
			get { return (replayToken); }
			set { replayToken = value; }
		}

		private DeskPermission role;
		public DeskPermission Role
		{
			get { return (role); }
			set { role = value; }
		}

		private string username;
		public string UserName
		{
			get { return (username); }
			set { username = value; }
		}

		private bool administrator;
		public bool IsAdministrator
		{
			get { return (administrator); }
			set { administrator = value; }
		}

		private int serviceplan;
		public int ServicePlan
		{
			get { return (serviceplan); }
			set { serviceplan = value; }
		}

		private string firstname;
		public string FirstName
		{
			get { return (firstname); }
			set { firstname = value; }
		}

		private string lastname;
		public string LastName
		{
			get { return (lastname); }
			set { lastname = value; }
		}

		private bool[] userpermissions;
		public bool[] UserPermissions
		{
			get { return (userpermissions); }
			set { userpermissions = value; }
		}

		public int LightningSpanMin;
		public int LightningSpanMax;

		public UserInfoDictionary InfoTable;

		private bool newVersionCheck;
		public bool NewVersionCheck
		{
			get { return newVersionCheck; }
			set { newVersionCheck = value; }
		}

		private bool savePrompt_View;
		public bool SavePrompt_View
		{
			get { return savePrompt_View; }
			set { savePrompt_View = value; }
		}

		private bool savePrompt_Workspace;
		public bool SavePrompt_Workspace
		{
			get { return savePrompt_Workspace; }
			set { savePrompt_Workspace = value; }
		}

		private bool showTipsAndTricks;
		public bool ShowTipsAndTricks
		{
			get { return showTipsAndTricks; }
			set { showTipsAndTricks = value; }
		}

		private int flightUpdateInterval;
		public int FlightUpdateInterval
		{
			get { return flightUpdateInterval; }
			set { flightUpdateInterval = value; }
		}

        private int messageFrequentUpdateInterval;
        public int MessageFrequentUpdateInterval
        {
            get { return messageFrequentUpdateInterval; }
            set { messageFrequentUpdateInterval = value; }
        }

        private int messageGeneralUpdateInterval;
        public int MessageGeneralUpdateInterval
        {
            get { return messageGeneralUpdateInterval; }
            set { messageGeneralUpdateInterval = value; }
        }

		private int replayHistoryLength;
		public int ReplayHistoryLength
		{
			get { return replayHistoryLength; }
			set { replayHistoryLength = value; }
		}

		private Guid webAPIAuthenticationCode;
		public Guid WebAPIAuthenticationCode
		{
			get { return webAPIAuthenticationCode; }
			set { webAPIAuthenticationCode = value; }
		}

		private string designator;
		public string Designator
		{
			get { return designator; }
			set { designator = value; }
		}

		private string userFeedbackUrl;
		public string UserFeedbackUrl
		{
			get { return userFeedbackUrl;}
			set { userFeedbackUrl = value;}
		}

		private string companyFlightsDesignators;
		public string CompanyFlightsDesignators
		{
			get { return companyFlightsDesignators; }
			set { companyFlightsDesignators = value; }
		}

		private string tafSourceID;
		public string TAFSourceID
		{
			get { return tafSourceID; }
			set { tafSourceID = value; }
		}

		public ArrayList FlightFilterDataSources;

		public UserInfo()
		{
			customerid = -1;
			userid = -1;
			token = Guid.Empty;
			replayToken = Guid.Empty;
			role = DeskPermission.None;
			username = string.Empty;
			administrator = false;
			serviceplan = 0;
			firstname = string.Empty;
			lastname = string.Empty;
			LightningSpanMin = 6;
			LightningSpanMax = 60;
			userpermissions = new bool[(int)UserPermission.USER_PERMISSION];
			InfoTable = new UserInfoDictionary();
			newVersionCheck = true;
			savePrompt_View = true;
			savePrompt_Workspace = true;
			showTipsAndTricks = true;
			flightUpdateInterval = 10;
			replayHistoryLength = 92;
			webAPIAuthenticationCode = Guid.NewGuid();
			designator = string.Empty;
			userFeedbackUrl = string.Empty;
			companyFlightsDesignators = string.Empty;
			FlightFilterDataSources = new ArrayList();
            messageFrequentUpdateInterval = 0;
            messageGeneralUpdateInterval = 30000; //in ms
		}
	}
}
