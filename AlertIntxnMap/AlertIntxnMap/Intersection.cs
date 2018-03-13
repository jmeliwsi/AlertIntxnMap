using System.Collections.Generic;

namespace AlertIntxnMap
{
    public enum HazardType { UNKNOWN, TAPS, PIREP, SIGMET, TBCA, FPG };

    public class IntersectPosition
    {
        public double lat { get; set; }
        public double lon { get; set; }
        public int alt { get; set; }
        public string time { get; set; }
    }

    public class Pt
    {
        public double lat { get; set; }
        public double lon { get; set; }
        public int alt { get; set; }
    }

    public class FlightState
    {
        public int pid { get; set; }
        public string fn { get; set; }
        public int st { get; set; }
        public int aspd { get; set; }
        public int aalt { get; set; }
        public string dt { get; set; }
        public string odt { get; set; }
        public string eta { get; set; }
        public string fut { get; set; }
        public string pt { get; set; }
        public string pit { get; set; }
        public double plat { get; set; }
        public double plon { get; set; }
        public int pspd { get; set; }
        public int phd { get; set; }
        public int palt { get; set; }
        public int arid { get; set; }
        public string rs { get; set; }
        public string dep { get; set; }
        public double deplat { get; set; }
        public double deplon { get; set; }
        public string dst { get; set; }
        public double dstlat { get; set; }
        public double dstlon { get; set; }
        public List<Pt> pts { get; set; }
    }

    public class Key
    {
        public int cid { get; set; }
        public string icao { get; set; }
        public string iata { get; set; }
        public string sdt { get; set; }
        public string sdi { get; set; }
        public string fn { get; set; }
        public int fli { get; set; }
        public string pid { get; set; }
        public string sfn { get; set; }
    }

    public class FlightPlan
    {
        public int fpid { get; set; }
        public Key key { get; set; }
        public string dst { get; set; }
        public double dstlat { get; set; }
        public double dstlon { get; set; }
        public double sdilat { get; set; }
        public double sdilon { get; set; }
        public string off { get; set; }
        public string on { get; set; }
        public string pin { get; set; }
        public string pout { get; set; }
        public List<Pt> pts { get; set; }
        public int AverageSpeed { get; set; }
    }

    // Could be TAPS, PIREP, SIGMET or TBCA
    public class WeatherAdvisory
    {
        public string aircraftType { get; set; }
        public double aircraftWeight { get; set; }
        public string airline { get; set; }
        public string discussion { get; set; }
        public double edr { get; set; }
        public int flightLevel { get; set; }
        public string flightNumber { get; set; }
        public string hazardId { get; set; }
        public double hazardMetric { get; set; }
        public double hazardScale { get; set; }
        public int ias { get; set; }
        public string insertedAt { get; set; }
        public string intensity { get; set; }
        public int intensityCode { get; set; }
        public string issueTime { get; set; }
        public double lat { get; set; }
        public string locationText { get; set; }
        public double lon { get; set; }
        public int lowerFlightLevel { get; set; }
        public int maintFlag { get; set; }
        public double maxLatAcc { get; set; }
        public int maxRollAngle { get; set; }
        public double maxVertAcc { get; set; }
        public double minLatAcc { get; set; }
        public double minVertAcc { get; set; }
        public double movingDirection { get; set; }
        public double? movingSpeed { get; set; }
        public string navfixes { get; set; }
        public long objectId { get; set; }
        public string outlook { get; set; }
        public List<Pt> points { get; set; }
        public int procStatus { get; set; }
        public double radius { get; set; }
        public string rawType { get; set; }
        public double rmsLoad { get; set; }
        public long rowId { get; set; }
        public int severity { get; set; }
        public string tailNumber { get; set; }
        public int tas { get; set; }
        public int temperature { get; set; }
        public string turbulenceText { get; set; }
        public string type { get; set; }
        public int upperFlightLevel { get; set; }
        public string validEnd { get; set; }
        public string validStart { get; set; }
        public string validTime { get; set; }
        public double windDirection { get; set; }
        public int windSpeed { get; set; }
    }

    public class Intersection
    {
        public string id { get; set; }
        public IntersectPosition intersectPosition { get; set; }
        public string reportCreatedAt { get; set; }
        public int alertingEngineCriteriaId { get; set; }
        public FlightState flightState { get; set; }
        public FlightPlan flightPlan { get; set; }
        public WeatherAdvisory weatherAdvisory { get; set; }
    }
}
