using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FUL
{
	public class FieldCondition
	{
		public FieldCondition()
		{
			icao = string.Empty;
			runways = new List<Runway>();
			latestReportTime = DateTime.MinValue;
		}

		private string icao;
		public string AirportICAO
		{
			get { return icao; }
			set { icao = value; }
		}

		private List<Runway> runways;
		public List<Runway> Runways
		{
			get { return runways; }
		}

		public void AddRunway(Runway runway)
		{
			runways.Add(runway);
		}

		private DateTime latestReportTime;
		public DateTime LatestReportTime
		{
			get { return latestReportTime; }
			set { latestReportTime = value; }
		}

		//private string remark;
		//public string AirportRemark
		//{
		//    get { return remark; }
		//    set { remark = value; }
		//}

		public List<Runway> GetRunwaysWithHighHeadWind(int speed)
		{
			List<Runway> runwayList = new List<Runway>();
			foreach (Runway runway in runways)
				if (runway.Active && (runway.HeadWindSpeed > speed))
					runwayList.Add(runway);
			return runwayList;
		}

		public List<Runway> GetRunwaysWithHighCrossWind(int speed)
		{
			List<Runway> runwayList = new List<Runway>();
			foreach (Runway runway in runways)
				if (runway.Active && (runway.CrossWindSpeed > speed))
					runwayList.Add(runway);
			return runwayList;
		}

		public List<Runway> GetRunwaysWithBreakingCondition(int condition)
		{
			List<Runway> runwayList = new List<Runway>();
			foreach (Runway runway in runways)
                if (runway.Active && ((runway.Brake != Runway.BrakeAction.Dry && Convert.ToInt32(runway.Brake) >= condition) || (runway.Brake != Runway.BrakeAction.Unknown && condition == -1))) // new for 6.2.  1) "Dry" added to end of enum to support backward compatibility and 2) "Dry" listed at top of Alert selection list
					runwayList.Add(runway);
			return runwayList;
		}

		public List<Runway> GetRunwaysWithSurfaceCondition(int condition)
		{
			List<Runway> runwayList = new List<Runway>();
			foreach (Runway runway in runways)
				if (runway.Active && (Convert.ToInt32(runway.Surface) >= condition))
					runwayList.Add(runway);
			return runwayList;
		}

		public List<Runway> GetRunwaysWithSurfaceCondition(int condition, double depth)
		{
			List<Runway> runwayList = new List<Runway>();
			foreach (Runway runway in runways)
				if (runway.Active && (Convert.ToInt32(runway.Surface) >= condition) &&  (runway.Depth > depth))
					runwayList.Add(runway);
			return runwayList;
		}

        public List<Runway> GetRunwaysWithSurfaceConditionContaminationAlert(int condition, double depth)
        {
            List<Runway> runwayList = new List<Runway>();
            foreach (Runway runway in runways)
            {
                if (Enum.IsDefined(typeof(Runway.SurfaceConditionContaminationAlert), runway.Surface.ToString()))
                {
                    Runway.SurfaceConditionContaminationAlert ContaminationAlertvalue = (Runway.SurfaceConditionContaminationAlert)Enum.Parse(typeof(Runway.SurfaceConditionContaminationAlert), runway.Surface.ToString());
                    if (runway.Active && (Convert.ToInt32(ContaminationAlertvalue) == condition) && (runway.Depth > depth))
                        runwayList.Add(runway);
                }
            }
            return runwayList;
        }

		public List<string> GetActiveRunwayNames()
		{
			List<string> runwayList = new List<string>();
			foreach (Runway runway in runways)
				if (runway.Active)
					runwayList.Add(runway.Name);
			return runwayList;
		}
	}

	public class Runway
	{
        public enum BrakeAction { Unknown = -1, Good, Good_Medium, Medium, Medium_Poor, Poor, Nil, Dry };
        public enum SurfaceCondition { Unknown = -1, Dry, Wet, StandingWater, Slush, CompactedSnow, DrySnow, WetIce, AdvisoryWet, DrySnowOverCmpctSnow, DrySnowOverIce, Frost, Ice, SlipperyWhenWet, SlushOverIce, Water, WaterOverCmpctSnow, WaterOverIce, WetSnow, WetSnowOverCmpctSnow, WetSnowOverIce, Ash, Mud, Oil, Rubber, Sand, None };
        public enum SurfaceConditionContaminationAlert { Unknown = -1, DrySnow, DrySnowOverCmpctSnow, DrySnowOverIce, Slush, SlushOverIce, Water, WaterOverCmpctSnow, WaterOverIce, WetSnow, WetSnowOverCmpctSnow, WetSnowOverIce, Mud, None };
        public enum RunwayState { Unknown = -1, T, L, B, C };

		public Runway()
		{
			name = string.Empty;
			active = true;
			headWindSpeed = -1;
			crossWindSpeed = -1;
			brake = BrakeAction.Unknown;
			surface = SurfaceCondition.Unknown;
			depth = -1;
		}

		private string name;
		public string Name
		{
			set { name = value; }
			get { return name; }
		}

		private bool active;
		public bool Active
		{
			set { active = value; }
			get { return active; }
		}

		private int headWindSpeed;
		public int HeadWindSpeed
		{
			get { return headWindSpeed; }
			set { headWindSpeed = value; }
		}

		private int crossWindSpeed;
		public int CrossWindSpeed
		{
			get { return crossWindSpeed; }
			set { crossWindSpeed = value; }
		}

		private BrakeAction brake;
		public BrakeAction Brake
		{
			get { return brake; }
			set { brake = value; }
		}

		private SurfaceCondition surface;
		public SurfaceCondition Surface
		{
			get { return surface; }
			set { surface = value; }
		}

		private double depth;
		public double Depth
		{
			get { return depth; }
			set { depth = value; }
		}

		private RunwayState state;
		public RunwayState State
		{
			get { return state; }
			set { state = value; }
		}

		public string GetSurfaceConditionAlertLabel()
		{
			return GetSurfaceCondition(surface.ToString()) + " runway " + this.name;
		}

		public string GetBrakeConditionAlertLabel()
		{
            //return brake.ToString().Replace('_', '-') + " runway " + this.name;
            return GetBreakAction(brake.ToString()) + " runway " + this.name;

		}

        public static string GetBreakAction(string ba)
        {
            FUL.Runway.BrakeAction BreakActionEnumValue = BrakeAction.Unknown;
            if (Enum.TryParse<FUL.Runway.BrakeAction>(ba, out BreakActionEnumValue))
            {
                switch (BreakActionEnumValue)
                {
                    case BrakeAction.Dry: return "6-Dry";
                    case BrakeAction.Good: return "5-Good";
                    case BrakeAction.Good_Medium: return "4-Good to Medium";
                    case BrakeAction.Medium: return "3-Medium";
                    case BrakeAction.Medium_Poor: return "2-Medium to Poor";
                    case BrakeAction.Poor: return "1-Poor";
                    case BrakeAction.Nil: return "0-Nil";
                    default: return ba.ToString();
                }
            }
            else return BrakeAction.Unknown.ToString();
        }

        // for 16.1 and earlier
        public static string GetSurfaceCondition(SurfaceCondition s)
        {
            switch (s)
            {
                case SurfaceCondition.StandingWater:
                    return "Standing water";
                case SurfaceCondition.CompactedSnow:
                    return "Compacted snow";
                case SurfaceCondition.DrySnow:
                    return "Dry snow";
                case SurfaceCondition.WetIce:
                    return "Wet ice";
                case SurfaceCondition.AdvisoryWet:
                    return "Advisory wet";
                default:
                    return s.ToString();
            }
        }

        // for greater than 16.1
        public static string GetSurfaceCondition(string s)
        {
            FUL.Runway.SurfaceCondition SurfaceConditionEnumValue = SurfaceCondition.Unknown;
            if (Enum.TryParse<FUL.Runway.SurfaceCondition>(s, out SurfaceConditionEnumValue))
            {
                switch (SurfaceConditionEnumValue)
                {
                    case SurfaceCondition.StandingWater: return "Standing water";
                    case SurfaceCondition.CompactedSnow: return "Compacted snow";
                    case SurfaceCondition.DrySnow: return "Dry snow";
                    case SurfaceCondition.WetIce: return "Wet ice";
                    case SurfaceCondition.AdvisoryWet: return "Advisory wet";
                    case SurfaceCondition.DrySnowOverCmpctSnow: return "Dry Snow Over Compacted Snow";
                    case SurfaceCondition.DrySnowOverIce: return "Dry Snow Over Ice";
                    case SurfaceCondition.SlipperyWhenWet: return "Slippery When Wet";
                    case SurfaceCondition.SlushOverIce: return "Slush Over Ice";
                    case SurfaceCondition.WaterOverCmpctSnow: return "Water Over Compacted Snow";
                    case SurfaceCondition.WaterOverIce: return "Water Over Ice";
                    case SurfaceCondition.WetSnow: return "Wet Snow";
                    case SurfaceCondition.WetSnowOverCmpctSnow: return "Wet Snow Over Compacted Snow";
                    case SurfaceCondition.WetSnowOverIce: return "Wet Snow Over Ice";
                    default: return s.ToString();
                }
            }
            return SurfaceCondition.Unknown.ToString();

        }

        public static string GetSurfaceConditionContaminationAlert(SurfaceConditionContaminationAlert s)
        {
            switch (s)
            {
                case SurfaceConditionContaminationAlert.DrySnow: return "Dry snow";
                case SurfaceConditionContaminationAlert.DrySnowOverCmpctSnow: return "Dry Snow Over Compacted Snow";
                case SurfaceConditionContaminationAlert.DrySnowOverIce: return "Dry Snow Over Ice";
                case SurfaceConditionContaminationAlert.SlushOverIce: return "Slush Over Ice";
                case SurfaceConditionContaminationAlert.WaterOverCmpctSnow: return "Water Over Compacted Snow";
                case SurfaceConditionContaminationAlert.WaterOverIce: return "Water Over Ice";
                case SurfaceConditionContaminationAlert.WetSnow: return "Wet Snow";
                case SurfaceConditionContaminationAlert.WetSnowOverCmpctSnow: return "Wet Snow Over Compacted Snow";
                case SurfaceConditionContaminationAlert.WetSnowOverIce: return "Wet Snow Over Ice";
                default: return s.ToString();

            }
        }

		//public static List<RunwayBrakeAction> GetBrakeActionList()
		//{
		//	List<RunwayBrakeAction> bal = new List<RunwayBrakeAction>();

		//	// Leave Unknown out on purpose.
		//	//bal.Add(new RunwayBrakeAction(BrakeAction.Dry));
		//	//bal.Add(new RunwayBrakeAction(BrakeAction.Wet_Good));
		//	//bal.Add(new RunwayBrakeAction(BrakeAction.Wet_Fair));
		//	//bal.Add(new RunwayBrakeAction(BrakeAction.Wet_Poor));
		//	bal.Add(new RunwayBrakeAction(BrakeAction.Nil));

		//	return bal;
		//}

		public static List<RunwaySurfaceCondition> GetSurfaceConditionList()
		{
			List<RunwaySurfaceCondition> scl = new List<RunwaySurfaceCondition>();

			// Leave Unknown out on purpose.

            // 16.1
            scl.Add(new RunwaySurfaceCondition(SurfaceCondition.Dry));
            scl.Add(new RunwaySurfaceCondition(SurfaceCondition.Wet));
            scl.Add(new RunwaySurfaceCondition(SurfaceCondition.StandingWater));
            scl.Add(new RunwaySurfaceCondition(SurfaceCondition.Slush));
            scl.Add(new RunwaySurfaceCondition(SurfaceCondition.CompactedSnow));
            scl.Add(new RunwaySurfaceCondition(SurfaceCondition.DrySnow));
            scl.Add(new RunwaySurfaceCondition(SurfaceCondition.WetIce));
            scl.Add(new RunwaySurfaceCondition(SurfaceCondition.AdvisoryWet));

            // additional for 16.2+
            //scl.Add(new RunwaySurfaceCondition(SurfaceCondition.DrySnowOverCmpctSnow));
            //scl.Add(new RunwaySurfaceCondition(SurfaceCondition.DrySnowOverIce));
            //scl.Add(new RunwaySurfaceCondition(SurfaceCondition.Frost));
            //scl.Add(new RunwaySurfaceCondition(SurfaceCondition.Ice));
            //scl.Add(new RunwaySurfaceCondition(SurfaceCondition.SlipperyWhenWet));
            //scl.Add(new RunwaySurfaceCondition(SurfaceCondition.SlushOverIce));
            //scl.Add(new RunwaySurfaceCondition(SurfaceCondition.Water));
            //scl.Add(new RunwaySurfaceCondition(SurfaceCondition.WaterOverCmpctSnow));
            //scl.Add(new RunwaySurfaceCondition(SurfaceCondition.WaterOverIce));
            //scl.Add(new RunwaySurfaceCondition(SurfaceCondition.WetSnow));
            //scl.Add(new RunwaySurfaceCondition(SurfaceCondition.WetSnowOverCmpctSnow));
            //scl.Add(new RunwaySurfaceCondition(SurfaceCondition.WetSnowOverIce));
            //scl.Add(new RunwaySurfaceCondition(SurfaceCondition.Ash));
            //scl.Add(new RunwaySurfaceCondition(SurfaceCondition.Mud));
            //scl.Add(new RunwaySurfaceCondition(SurfaceCondition.Oil));
            //scl.Add(new RunwaySurfaceCondition(SurfaceCondition.Rubber));
            //scl.Add(new RunwaySurfaceCondition(SurfaceCondition.Sand));
            //scl.Add(new RunwaySurfaceCondition(SurfaceCondition.None));

			return scl;
		}
	}

	public class RunwayBrakeAction
	{
		public Runway.BrakeAction BrakeAction;
		public string BrakeActionValue;

		public RunwayBrakeAction(Runway.BrakeAction ba)
		{
			BrakeAction = ba;
            BrakeActionValue = Runway.GetBreakAction(ba.ToString());
		}
	}

	public class RunwaySurfaceCondition
	{
		public Runway.SurfaceCondition SurfaceCondition;
		public string SurfaceConditionValue;

		public RunwaySurfaceCondition(Runway.SurfaceCondition sc)
		{
			SurfaceCondition = sc;
			SurfaceConditionValue = Runway.GetSurfaceCondition(sc.ToString());
		}
	}
}
