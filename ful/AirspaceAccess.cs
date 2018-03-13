using System;
using System.Collections;
using System.Data;
using System.Data.SqlClient;

namespace FUL
{
	public enum BoundaryType {ARTCC, OCA};
	public enum BoundaryAltType {NONE, LOW, HIGH, ALL};
    public enum NAVaidType { VOR, VORTAC, TACAN, VOR_DME, NDB, DME, Unknown }; // for symbol creation.
    public enum NavDataType { Unknown, Station, Point, Jetway_A, Jetway_N, Jetway_E, Star, Sid, Delimiter};

    [Serializable]
	public struct BoundaryStruct
	{
		public string FullName;
		public string ICAO;
		public ArrayList Points;
        public DateTime ValidTime;
	}

	[Serializable]
	public struct BoundaryCompleteStruct
	{
		public string Ident;
		public string FullName;
		public string ICAO;
		public string Shape;
		public string AltLevel;
		public string Type;
		public System.Collections.Generic.List<BoundaryCompletePointsStruct> Points;
		public DateTime ValidTime;
	}

	[Serializable]
	public struct BoundaryCompletePointsStruct
	{
		public int SequenceNumber;
		public FUL.Coordinate Location;
	}

    [Serializable]
    public struct NAVaidsStruct
	{
		public string ID;
        public string AreaCode;
        public string Class1;   // V=VOR, H=NDB
        public string Class2;   // D=DME, T=TACAN, M=Military TACAN
        public string Class3;   // T,L,H = Terminal, Low, High
        public string Name;
		public string Cycle;
        public string Type;     // VHF or NDB
        public FUL.Coordinate Location;
        public DateTime ValidTime;
	}

    [Serializable]
    public struct WaypointStruct
	{
		public string ID;
        public string AreaCode;
        public string Type1;    // C=Combined Name intersection and RNAV, I=Unnamed Charted, R=Named Intersection, W=RNAV, N=NDB, V=VFR
        public string Type2;    // F=Off Route Intersection, etc.
        public string Usage2;   // H=High, L=Low, B=Both, Blank=Terminal
        public string Name;
		public FUL.Coordinate Location;
        public string Cycle;
        public DateTime ValidTime;
        // Superset of NRS is Type1=R, Type2=F, Usage2=H
    }

    [Serializable]
    public struct AirwayStruct  // used for displaying Routes on the Map.
	{
        public int ID;
        public string IDENT;
        public string AreaCode;
        public ArrayList Points;    // of type AirwayPointsStruct
        public string Level;
        public string Cycle;
        public DateTime ValidTime;
    }

    [Serializable]
    public struct AirwayPointsStruct   // used for displaying Routes on the Map.
    {                                  // used for ArrayList in AirwayStruct.
        public int SequenceNumber;
        public string Fix;
        public string ICAOAreaCode;
        public string Direction;
        public string PointLevel;
        public FUL.Coordinate Location;
        public string Type;
        public string Cycle;
        public DateTime ValidTime;
    }

    [Serializable]
    public struct AirspaceStruct    // used for Controlled & Restrictive Airpace and UIR/FIR.
    {                               // ID is for UIR/FIR only. Designator is for Airspace only.
        public string AreaCode;
        public string Name;
        public string Type;
        public string Designator;
        public string ID;
        public ArrayList Points;    // of type AirspacePointsStruct
        public DateTime ValidTime;
		public string LowerAltitude;
		public string UpperAltitude;
		public string IcaoArea;
    }

    [Serializable]
    public struct AirspacePointsStruct  // used for ArrayList in AirspaceStruct.
    {
        public int Sequence;
        public string Via;
        public FUL.Coordinate Fix;
        public FUL.Coordinate Arc;
        public double ArcDistance;
        public double ArcBearing;
        public string Cycle;
    }

    [Serializable]
    public struct VolcanoStruct
    {
		public string Number;
        public string Name;
        public string Elevation;
        public string Location;
        public string Type;
        public double Latitude;
        public double Longitude;
		public string TimeFrame;
		public DateTime ValidTime;
    }

	[Serializable]
	public struct SidStarsStruct  // used for displaying Routes on the Map.
	{
		public int ID;
		public string IDENT;
		public string Type;
        public string SegmentType;
		public string Country;
		public string Origin;
		public string Destination;
		public string AirportICAO;
		public string AirportAreaCode;
		public System.Collections.Generic.List<SidStarsPointsStruct> Points;
		public DateTime ValidTime;
	}

	[Serializable]
	public struct SidStarsPointsStruct   // used for displaying Routes on the Map.
	{                                  // used for ArrayList in SidStarsStruct.
		public int SequenceNumber;
		public string Fix;
		public string FixAreaCode;
		public FUL.Coordinate Location;
	}
} // End namespace
