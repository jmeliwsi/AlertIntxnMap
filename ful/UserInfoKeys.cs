using System;
using System.Collections.Generic;
using System.Text;

namespace FUL
{
	public class UserInfoKeys
	{
		#region Permissions
		// All Permissions MUST map to an associated row in the Fusion_Admin.CustomerPermissions table.
		public const string IntegratedData = "IntegratedData";
		public const string RouteSelection = "RouteSelection";
		public const string Reroutes = "Reroutes";
		public const string TextMessaging = "TextMessaging";
		public const string AutoDeskOwner = "AutoDeskOwner";
		public const string OOOIAlerting = "OOOIAlerting";
		public const string CustomerRAIM = "CustomerRAIM";
		public const string FlightPlanning = "FlightPlanning";
		public const string CustomerFlightRoutes = "CustomerFlightRoutes";
		public const string Replay = "Replay";
		public const string NoFusion = "NoFusion";
		public const string WxPacket = "WxPacket";
		public const string ArchiveViewer = "ArchiveViewer";
		public const string CustomerFieldConditons = "CustomerFieldConditions";
		public const string CustomerDATIS = "CustomerDATIS";
		public const string CustomerRAF = "CustomerRAF";
		public const string CustomerDocuments = "CustomerDocuments";
		public const string CustomerWx = "CustomerWx";
		public const string PosRepWx = "PosRepWx";
		public const string OverrideFlightInfo = "OverrideFlightInfo";
		public const string UserGeneratedPireps = "UserGeneratedPireps";
		public const string FlightPlanningClientIntegration = "FlightPlanningClientIntegration";
		public const string FieldFacility = "FieldFacilities";
        public const string ATFMTool = "ATFMTool";
        public const string StationCurfew = "StationCurfew";
        public const string FocusFlight = "FocusFlight";
        #endregion Permissions

        #region Info
        // All Info MUST map to an associated row in the Fusion_Admin.CustomerPermissions table.
        public const string SecondDataControl = "SecondDataControl";
		public const string IntegratedDataDB = "IntegratedDataDB";
		public const string IntegratedDataSuffix = "IntegratedDataSuffix";
		public const string FusionFlightsViewSuffix = "FusionFlightsViewSuffix";
		public const string ExtraFlightFields = "ExtraFlightFields";
		public const string DeskFlightProgressClass = "DeskFlightProgressClass";
		public const string FlightDesignator = "FlightDesignator";
		public const string DeskFlightProgressRemoveUncorrelated = "DeskFlightProgressRemoveUncorrelated";
		public const string CustomNotamsCompanyOnly = "CustomNotamsCompanyOnly";
		public const string EONtoETAinDFP = "EONtoETAinDFP";
		public const string SurfaceMovementStations = "SurfaceMovementStations";
        public const string TextMessagingDirectory = "TextMessagingDirectory";

        #endregion Info

        #region Miscellaneous
        public const string UnblockTails = "HasUnblockTails";
		public const string ASDClassTwo = "ASDClassTwo";
		public const string UserFeedbackUrl = "UserFeedbackUrl";
		public const string CompanyFlightDesignators = "CompanyFlightDesignators";
		#endregion Miscellaneous
	}
}
