using System;
using System.Collections.Generic;
using System.Text;

namespace FUL.Conversion
{
	public enum ConversionOptions
	{
		// Default setting.
		Unknown = 0,
		// Distance or Altitude/Ceiling.
		Feet,
		StatuteMiles,
		Meters,
		Kilometers,
		NauticalMiles,
		FlightLevel,
		// Speed.
		Knots,
		MilesPerHour,
		KilometersPerHour,
		MetersPerSecond,
        Mach,
		// Temperature.
		Fahrenheit,
		Celsius,
		// Pressure
		Hectopascals,
		Millibars,
		InchesOfMercury,
        // Move these two distance units down here to fix bug #23906
        Inches,
        Millimeters,
        //Fuel
        // Fuel Units: Pounds, Tens of Pounds, Hundreds of Pounds, Kilograms,Tens of Kilograms, Hundreds of Kilograms, US Gallons, UK Gallons, Liters
        // UNKNOWN means a value was supplied which is not in the list. UNDEFINED is the default when no value is supplied.
        UNDEFINED,
        LBS, 
        LBS10, 
        LBS100, 
        KILOS, 
        KILOS10, 
        KILOS100, 
        US_GAL, 
        UK_GAL, 
        LITERS
	}

	public enum ForceUnit
	{
		Dynamic = 0,	// Allow units to be converted if they pass a certain threshold (ft->SM, m->km)
		ToRequested,	// Units will always be returned with the unit requesetd.
		ToLarger		// If an upconvert option exists (ft->SM, m->km) always do it.
	}

	public struct ConversionResult
	{
		public double Value;
		public ConversionOptions Unit;

		public ConversionResult(double value, ConversionOptions unit)
		{
			Value = value;
			Unit = unit;
		}

		/// <summary>
		/// Returns the result valueas a string.
		/// </summary>
		public override string ToString()
		{
			return StringConversion(null, null, false, false);
		}

		/// <summary>
		/// Returns the result value as a string. Optionally can append the longhand unit.
		/// </summary>
		public string ToString(bool appendUnit)
		{
			return StringConversion(null, null, appendUnit, false);
		}

		/// <summary>
		/// Returns the result value as a string. Optionally can append the shorthand or longhand unit.
		/// </summary>
		public string ToString(bool appendUnit, bool shorthand)
		{
			return StringConversion(null, null, appendUnit, shorthand);
		}

		/// <summary>
		/// Returns the result value as a formatted string.
		/// </summary>
		public string ToString(string format)
		{
			return StringConversion(format, null, false, false);
		}

		/// <summary>
		/// Returns the result value as a formatted string. Optionally can append the longhand unit.
		/// </summary>
		public string ToString(string format, bool appendUnit)
		{
			return StringConversion(format, null, appendUnit, false);
		}

		/// <summary>
		/// Returns the result value as a formatted string. Optionally can append the shorthand or longhand unit.
		/// </summary>
		public string ToString(string format, bool appendUnit, bool shorthand)
		{
			return StringConversion(format, null, appendUnit, shorthand);
		}

		/// <summary>
		/// Returns the result valueas a string using the specified culture-specific format information.
		/// </summary>
		public string ToString(IFormatProvider provider)
		{
			return StringConversion(null, provider, false, false);
		}

		/// <summary>
		/// Returns the result valueas a string using the specified culture-specific format information. Optionally can append the longhand unit.
		/// </summary>
		public string ToString(IFormatProvider provider, bool appendUnit)
		{
			return StringConversion(null, provider, appendUnit, false);
		}

		/// <summary>
		/// Returns the result valueas a string using the specified culture-specific format information. Optionally can append the shorthand or longhand unit.
		/// </summary>
		public string ToString(IFormatProvider provider, bool appendUnit, bool shorthand)
		{
			return StringConversion(null, provider, appendUnit, shorthand);
		}

		/// <summary>
		/// Returns the result value as a formatted string using the specified culture-specific format information.
		/// </summary>
		public string ToString(string format, IFormatProvider provider)
		{
			return StringConversion(format, provider, false, false);
		}

		/// <summary>
		/// Returns the result value as a formatted string using the specified culture-specific format information. Optionally can append the longhand unit.
		/// </summary>
		public string ToString(string format, IFormatProvider provider, bool appendUnit)
		{
			return StringConversion(format, provider, appendUnit, false);
		}

		/// <summary>
		/// Returns the result value as a formatted string using the specified culture-specific format information. Optionally can append the shorthand or longhand unit.
		/// </summary>
		public string ToString(string format, IFormatProvider provider, bool appendUnit, bool shorthand)
		{
			return StringConversion(format, provider, appendUnit, shorthand);
		}

		private string StringConversion(string format, IFormatProvider provider, bool appendUnit, bool shorthand)
		{
			StringBuilder retval = new StringBuilder();

			if (!string.IsNullOrEmpty(format) && provider != null)
				retval.Append(Value.ToString(format, provider));
			else if (!string.IsNullOrEmpty(format))
				retval.Append(Value.ToString(format));
			else if (provider != null)
				retval.Append(Value.ToString(provider));
			else
				retval.Append(Value.ToString());

			if (appendUnit)
			{
				if (Unit == ConversionOptions.FlightLevel)		// Have to prepend flight level labels.
					retval.Insert(0, " ").Insert(0, Conversions.ConversionUnitAsString(Unit, shorthand));
				else
					retval.Append(" ").Append(Conversions.ConversionUnitAsString(Unit, shorthand));
			}

			return retval.ToString();
		}
	}

	public static class Conversions
	{
		private const double StatuteMilesCutoff = 5280.0;	// As in, when we get to this many feet switch to miles.
		private const double KilometersCutoff = 1000.0;		// As in, when we get to this many meters switch to kilometers.

		/// <summary>
		/// Return a longhand string representation of a unit.
		/// </summary>
		/// <param name="unit"></param>
		/// <returns></returns>
		public static string ConversionUnitAsString(ConversionOptions unit)
		{
			return ConversionUnitAsString(unit, false);
		}

		/// <summary>
		/// Return a string representation of a unit (for example, shorthand and longhand for MilesPerHour would be "mi/hr" and "Miles Per Hour."
		/// </summary>
		/// <param name="unit"></param>
		/// <param name="shorthand"></param>
		/// <returns></returns>
		public static string ConversionUnitAsString(ConversionOptions unit, bool shorthand)
		{
			switch (unit)
			{
				case ConversionOptions.Inches:
					return (shorthand) ? "in" : "Inches";
				case ConversionOptions.Feet:
					return (shorthand) ? "ft" : "Feet";
				case ConversionOptions.StatuteMiles:
					return (shorthand) ? "SM" : "Statute Miles";
				case ConversionOptions.Millimeters:
					return (shorthand) ? "mm" : "Millimeters";
				case ConversionOptions.Meters:
					return (shorthand) ? "m" : "Meters";
				case ConversionOptions.Kilometers:
					return (shorthand) ? "km" : "Kilometers";
				case ConversionOptions.NauticalMiles:
					return (shorthand) ? "NM" : "Nautical Miles";
				case ConversionOptions.FlightLevel:
					return (shorthand) ? "FL" : "Flight Level";
				case ConversionOptions.Knots:
					return (shorthand) ? "kts" : "Knots";
				case ConversionOptions.MilesPerHour:
					return (shorthand) ? "mph" : "Miles Per Hour";
				case ConversionOptions.KilometersPerHour:
					return (shorthand) ? "km/h" : "Kilometers Per Hour";
				case ConversionOptions.MetersPerSecond:
					return (shorthand) ? "m/s" : "Meters Per Second";
				case ConversionOptions.Fahrenheit:
					return (shorthand) ? "°F" : "Fahrenheit";
				case ConversionOptions.Celsius:
					return (shorthand) ? "°C" : "Celsius";
				case ConversionOptions.Hectopascals:
					return (shorthand) ? "hPa" : "Hectopascals";
				case ConversionOptions.Millibars:
					return (shorthand) ? "mb" : "Millibars";
				case ConversionOptions.InchesOfMercury:
					return (shorthand) ? "inHg" : "Inches of Mercury";
                case ConversionOptions.LBS:
                    return (shorthand) ? "lb" : "LBS";
                case ConversionOptions.KILOS:
                    return (shorthand) ? "kg" : "KILOS";
			}

			return string.Empty;
		}

		/// <summary>
		/// Return a string representation of a unit (for example, shorthand and longhand for MilesPerHour would be "mi/hr" and "Miles Per Hour."
		/// </summary>
		/// <param name="unit"></param>
		/// <param name="shorthand"></param>
		/// <returns></returns>
		public static string ConversionUnitAsString(ConversionOptions unit, bool shorthand, ForceUnit forceUnit)
		{
			switch (unit)
			{
				case ConversionOptions.Feet:
					return ConversionUnitAsString(ConversionOptions.StatuteMiles, shorthand);
				case ConversionOptions.Meters:
					return ConversionUnitAsString(ConversionOptions.Kilometers, shorthand);
			}

			return ConversionUnitAsString(unit, shorthand);
		}

		/// <summary>
		/// Convert value from fromUnit into toUnit.
		/// </summary>
		public static ConversionResult ConvertUnits(int value, ConversionOptions fromUnit, ConversionOptions toUnit)
		{
			return ConvertUnits((double)value, fromUnit, toUnit, false, -1);
		}

		/// <summary>
		/// Convert value from fromUnit into toUnit.
		/// </summary>
		public static ConversionResult ConvertUnits(float value, ConversionOptions fromUnit, ConversionOptions toUnit)
		{
			return ConvertUnits((double)((decimal)value), fromUnit, toUnit, false, -1);
		}

		/// <summary>
		/// Convert value from fromUnit into toUnit.
		/// </summary>
		public static ConversionResult ConvertUnits(double value, ConversionOptions fromUnit, ConversionOptions toUnit)
		{
			return ConvertUnits(value, fromUnit, toUnit, false, -1);
		}

		/// <summary>
		/// Convert value from fromUnit into toUnit.
		/// </summary>
		public static ConversionResult ConvertUnits(int value, ConversionOptions fromUnit, ConversionOptions toUnit, bool forceUnit)
		{
			return ConvertUnits((double)value, fromUnit, toUnit, forceUnit, -1);
		}

		/// <summary>
		/// Convert value from fromUnit into toUnit.
		/// </summary>
		public static ConversionResult ConvertUnits(float value, ConversionOptions fromUnit, ConversionOptions toUnit, bool forceUnit)
		{
			return ConvertUnits((double)((decimal)value), fromUnit, toUnit, forceUnit, -1);
		}

		/// <summary>
		/// Convert value from fromUnit into toUnit.
		/// </summary>
		public static ConversionResult ConvertUnits(double value, ConversionOptions fromUnit, ConversionOptions toUnit, bool forceUnit)
		{
			return ConvertUnits(value, fromUnit, toUnit, forceUnit, -1);
		}

		/// <summary>
		/// Convert value from fromUnit into toUnit.
		/// </summary>
		public static ConversionResult ConvertUnits(int value, ConversionOptions fromUnit, ConversionOptions toUnit, int roundingDigits)
		{
			return ConvertUnits((double)value, fromUnit, toUnit, false, roundingDigits);
		}

		/// <summary>
		/// Convert value from fromUnit into toUnit.
		/// </summary>
		public static ConversionResult ConvertUnits(float value, ConversionOptions fromUnit, ConversionOptions toUnit, int roundingDigits)
		{
			return ConvertUnits((double)((decimal)value), fromUnit, toUnit, false, roundingDigits);
		}

		/// <summary>
		/// Convert value from fromUnit into toUnit.
		/// </summary>
		public static ConversionResult ConvertUnits(double value, ConversionOptions fromUnit, ConversionOptions toUnit, int roundingDigits)
		{
			return ConvertUnits(value, fromUnit, toUnit, false, roundingDigits);
		}

		/// <summary>
		/// Convert value from fromUnit into toUnit.
		/// </summary>
		public static ConversionResult ConvertUnits(int value, ConversionOptions fromUnit, ConversionOptions toUnit, bool forceUnit, int roundingDigits)
		{
			return ConvertUnits((double)value, fromUnit, toUnit, forceUnit, roundingDigits);
		}

		/// <summary>
		/// Convert value from fromUnit into toUnit.
		/// </summary>
		public static ConversionResult ConvertUnits(float value, ConversionOptions fromUnit, ConversionOptions toUnit, bool forceUnit, int roundingDigits)
		{
			return ConvertUnits((double)((decimal)value), fromUnit, toUnit, forceUnit, roundingDigits);
		}

		/// <summary>
		/// Convert value from fromUnit into toUnit.
		/// </summary>
		public static ConversionResult ConvertUnits(double value, ConversionOptions fromUnit, ConversionOptions toUnit, bool forceUnit, int roundingDigits)
		{
			return ConvertUnits(value, fromUnit, toUnit, (forceUnit ? ForceUnit.ToRequested : ForceUnit.Dynamic), roundingDigits);
		}

		/// <summary>
		/// Convert value from fromUnit into toUnit.
		/// </summary>
		public static ConversionResult ConvertUnits(double value, ConversionOptions fromUnit, ConversionOptions toUnit, ForceUnit forceUnit, int roundingDigits)
		{
			ConversionResult result = new ConversionResult(value, fromUnit);

			if (fromUnit == ConversionOptions.Unknown || toUnit == ConversionOptions.Unknown || fromUnit == toUnit)
			{
				if (forceUnit == ForceUnit.ToLarger)
				{
					// If we have a unit that allows an upconvert then do so.
					switch (toUnit)
					{
						case ConversionOptions.Feet:
							if (result.Unit == ConversionOptions.Feet)
							{
								// Convert to miles.
								result.Value = result.Value / 5280.0;
								result.Unit = ConversionOptions.StatuteMiles;
							}
							break;
						case ConversionOptions.Meters:
							if (result.Unit == ConversionOptions.Meters)
							{
								// Convert to kilometers.
								result.Value = result.Value / 1000.0;
								result.Unit = ConversionOptions.Kilometers;
							}
							break;
					}
				}

				if (roundingDigits > -1 && roundingDigits < 16)		// Math.Round throws an exception for values outside the 0-15 range.
					result.Value = Math.Round(result.Value, roundingDigits);

				return result;
			}

			switch (fromUnit)
			{
				case ConversionOptions.Inches:
					switch (toUnit)
					{
						case ConversionOptions.Feet:
							result.Value = value / 12.0;
							result.Unit = ConversionOptions.Feet;
							break;
						case ConversionOptions.StatuteMiles:
							result.Value = value / 63360.0;
							result.Unit = ConversionOptions.StatuteMiles;
							break;
						case ConversionOptions.Millimeters:
							result.Value = value * 25.4;
							result.Unit = ConversionOptions.Meters;
							break;
						case ConversionOptions.Meters:
							result.Value = FUL.Conversions.FeetToMeters(value);
							result.Unit = ConversionOptions.Meters;
							break;
						case ConversionOptions.Kilometers:
							result.Value = FUL.Conversions.FeetToMeters(value) / 1000.0;
							result.Unit = ConversionOptions.Kilometers;
							break;
						case ConversionOptions.NauticalMiles:
							result.Value = value * 0.000164578833693305;
							result.Unit = ConversionOptions.NauticalMiles;
							break;
						//case ConversionOptions.FlightLevel:
						//    result.Value = Math.Floor(value / 100.0);
						//    result.Unit = ConversionOptions.FlightLevel;
						//    break;
					}
					break;
				case ConversionOptions.Feet:
					switch (toUnit)
					{
						case ConversionOptions.Inches:
							result.Value = value * 12.0;
							result.Unit = ConversionOptions.Meters;
							break;
						case ConversionOptions.StatuteMiles:
							result.Value = value / 5280.0;
							result.Unit = ConversionOptions.StatuteMiles;
							break;
						case ConversionOptions.Millimeters:
							result.Value = value * 304.8;
							result.Unit = ConversionOptions.Meters;
							break;
						case ConversionOptions.Meters:
							result.Value = FUL.Conversions.FeetToMeters(value);
							result.Unit = ConversionOptions.Meters;
							break;
						case ConversionOptions.Kilometers:
							result.Value = FUL.Conversions.FeetToMeters(value) / 1000.0;
							result.Unit = ConversionOptions.Kilometers;
							break;
						case ConversionOptions.NauticalMiles:
							result.Value = value * 0.000164578833693305;
							result.Unit = ConversionOptions.NauticalMiles;
							break;
						//case ConversionOptions.FlightLevel:
						//    result.Value = Math.Floor(value / 100.0);
						//    result.Unit = ConversionOptions.FlightLevel;
						//    break;
					}
					break;
				case ConversionOptions.StatuteMiles:
					switch (toUnit)
					{
						case ConversionOptions.Inches:
							result.Value = value * 63360.0;
							result.Unit = ConversionOptions.Meters;
							break;
						case ConversionOptions.Feet:
							result.Value = value * 5280.0;
							result.Unit = ConversionOptions.Feet;
							break;
						case ConversionOptions.Millimeters:
							result.Value = value * 1609344.0;
							result.Unit = ConversionOptions.Meters;
							break;
						case ConversionOptions.Meters:
							result.Value = value * 1609.344;
							result.Unit = ConversionOptions.Meters;
							break;
						case ConversionOptions.Kilometers:
							result.Value = FUL.Conversions.MileToKM(value);
							result.Unit = ConversionOptions.Kilometers;
							break;
						case ConversionOptions.NauticalMiles:
							result.Value = FUL.Conversions.MiletoNM(value);
							result.Unit = ConversionOptions.NauticalMiles;
							break;
						//case ConversionOptions.FlightLevel:
						//    result.Value = value * 5.28;
						//    result.Unit = ConversionOptions.FlightLevel;
						//    break;
					}
					break;
				case ConversionOptions.Millimeters:
					switch (toUnit)
					{
						case ConversionOptions.Inches:
							result.Value = value / 25.4;
							result.Unit = ConversionOptions.Meters;
							break;
						case ConversionOptions.Feet:
							result.Value = value / 304.8;
							result.Unit = ConversionOptions.Feet;
							break;
						case ConversionOptions.StatuteMiles:
							result.Value = value / 1609344.0;
							result.Unit = ConversionOptions.StatuteMiles;
							break;
						case ConversionOptions.Meters:
							result.Value = value / 1000.0;
							result.Unit = ConversionOptions.Meters;
							break;
						case ConversionOptions.Kilometers:
							result.Value = value / 1000000.0;
							result.Unit = ConversionOptions.Kilometers;
							break;
						case ConversionOptions.NauticalMiles:
							result.Value = value / 1852000.0;
							result.Unit = ConversionOptions.NauticalMiles;
							break;
						//case ConversionOptions.FlightLevel:
						//    result.Value = Math.Floor(FUL.Conversions.MeterToFeet(value) / 100.0);
						//    result.Unit = ConversionOptions.FlightLevel;
						//    break;
					}
					break;
				case ConversionOptions.Meters:
					switch (toUnit)
					{
						case ConversionOptions.Inches:
							result.Value = FUL.Conversions.MeterToFeet(value) * 12.0;
							result.Unit = ConversionOptions.Meters;
							break;
						case ConversionOptions.Feet:
							result.Value = FUL.Conversions.MeterToFeet(value);
							result.Unit = ConversionOptions.Feet;
							break;
						case ConversionOptions.StatuteMiles:
							result.Value = value / 1609.344;
							result.Unit = ConversionOptions.StatuteMiles;
							break;
						case ConversionOptions.Millimeters:
							result.Value = value * 1000.0;
							result.Unit = ConversionOptions.Meters;
							break;
						case ConversionOptions.Kilometers:
							result.Value = value / 1000.0;
							result.Unit = ConversionOptions.Kilometers;
							break;
						case ConversionOptions.NauticalMiles:
							result.Value = value / 1852.0;
							result.Unit = ConversionOptions.NauticalMiles;
							break;
						//case ConversionOptions.FlightLevel:
						//    result.Value = Math.Floor(FUL.Conversions.MeterToFeet(value) / 100.0);
						//    result.Unit = ConversionOptions.FlightLevel;
						//    break;
					}
					break;
				case ConversionOptions.Kilometers:
					switch (toUnit)
					{
						case ConversionOptions.Inches:
							result.Value = FUL.Conversions.MeterToFeet(value * 1000.0) * 12.0;
							result.Unit = ConversionOptions.Meters;
							break;
						case ConversionOptions.Feet:
							result.Value = value * 3280.839895;
							result.Unit = ConversionOptions.Feet;
							break;
						case ConversionOptions.StatuteMiles:
							result.Value = FUL.Conversions.KMtoMile(value);
							result.Unit = ConversionOptions.StatuteMiles;
							break;
						case ConversionOptions.Millimeters:
							result.Value = value * 1000000.0;
							result.Unit = ConversionOptions.Meters;
							break;
						case ConversionOptions.Meters:
							result.Value = value * 1000.0;
							result.Unit = ConversionOptions.Meters;
							break;
						case ConversionOptions.NauticalMiles:
							result.Value = FUL.Conversions.KMtoNM(value);
							result.Unit = ConversionOptions.NauticalMiles;
							break;
						case ConversionOptions.FlightLevel:
							result.Value = Math.Floor(value * 3280.839895 / 100.0);
							result.Unit = ConversionOptions.FlightLevel;
							break;
					}
					break;
				case ConversionOptions.NauticalMiles:
					switch (toUnit)
					{
						case ConversionOptions.Inches:
							result.Value = FUL.Conversions.NMtoMile(value) * 63360.0;
							result.Unit = ConversionOptions.Meters;
							break;
						case ConversionOptions.Feet:
							result.Value = FUL.Conversions.NMtoMile(value) * 5280.0;
							result.Unit = ConversionOptions.Feet;
							break;
						case ConversionOptions.StatuteMiles:
							result.Value = FUL.Conversions.NMtoMile(value);
							result.Unit = ConversionOptions.StatuteMiles;
							break;
						case ConversionOptions.Millimeters:
							result.Value = value * 1852000.0;
							result.Unit = ConversionOptions.Meters;
							break;
						case ConversionOptions.Meters:
							result.Value = value * 1852.0;
							result.Unit = ConversionOptions.Meters;
							break;
						case ConversionOptions.Kilometers:
							result.Value = FUL.Conversions.NMToKM(value);
							result.Unit = ConversionOptions.Kilometers;
							break;
						//case ConversionOptions.FlightLevel:
						//    result.Value = Math.Floor(value * 6076.1154855643 / 100.0);
						//    result.Unit = ConversionOptions.FlightLevel;
						//    break;
					}
					break;
				//case ConversionOptions.FlightLevel:
				//    switch (toUnit)
				//    {
				//        case ConversionOptions.Feet:
				//            result.Value = value * 100.0;
				//            result.Unit = ConversionOptions.Feet;
				//            break;
				//        case ConversionOptions.StatuteMiles:
				//            result.Value = value * 100.0 / 5280.0;
				//            result.Unit = ConversionOptions.StatuteMiles;
				//            break;
				//        case ConversionOptions.Meters:
				//            result.Value = FUL.Conversions.FeetToMeters(value * 100.0);
				//            result.Unit = ConversionOptions.Meters;
				//            break;
				//        case ConversionOptions.Kilometers:
				//            result.Value = FUL.Conversions.FeetToMeters(value * 100.0) / 1000.0;
				//            result.Unit = ConversionOptions.Kilometers;
				//            break;
				//        case ConversionOptions.NauticalMiles:
				//            result.Value = value * 100.0 * 0.000164578833693305;
				//            result.Unit = ConversionOptions.NauticalMiles;
				//            break;
				//    }
				//    break;
				case ConversionOptions.Knots:
					switch (toUnit)
					{
						case ConversionOptions.MilesPerHour:
							result.Value = FUL.Conversions.KnotsToMPH(value);
							result.Unit = ConversionOptions.MilesPerHour;
							break;
						case ConversionOptions.KilometersPerHour:
							result.Value = value * 1.852;
							result.Unit = ConversionOptions.KilometersPerHour;
							break;
						case ConversionOptions.MetersPerSecond:
							result.Value = FUL.Conversions.KnotsToMetersPerSecond(value);
							result.Unit = ConversionOptions.MetersPerSecond;
							break;
					}
					break;
				case ConversionOptions.MilesPerHour:
					switch (toUnit)
					{
						case ConversionOptions.Knots:
							result.Value = FUL.Conversions.MPHtoKnots(value);
							result.Unit = ConversionOptions.Knots;
							break;
						case ConversionOptions.KilometersPerHour:
							result.Value = value * 1.609344;
							result.Unit = ConversionOptions.KilometersPerHour;
							break;
						case ConversionOptions.MetersPerSecond:
							result.Value = value * 0.44704;
							result.Unit = ConversionOptions.MetersPerSecond;
							break;
					}
					break;
				case ConversionOptions.KilometersPerHour:
					switch (toUnit)
					{
						case ConversionOptions.Knots:
							result.Value = value * 0.539956803455724;
							result.Unit = ConversionOptions.Knots;
							break;
						case ConversionOptions.MilesPerHour:
							result.Value = value * 0.621371192237334;
							result.Unit = ConversionOptions.MilesPerHour;
							break;
						case ConversionOptions.MetersPerSecond:
							result.Value = value * 0.277777777777778;
							result.Unit = ConversionOptions.MetersPerSecond;
							break;
					}
					break;
				case ConversionOptions.MetersPerSecond:
					switch (toUnit)
					{
						case ConversionOptions.Knots:
							result.Value = FUL.Conversions.MetersPerSecondToKnots(value);
							result.Unit = ConversionOptions.Knots;
							break;
						case ConversionOptions.MilesPerHour:
							result.Value = value * 2.2369362920544;
							result.Unit = ConversionOptions.MilesPerHour;
							break;
						case ConversionOptions.KilometersPerHour:
							result.Value = value * 3.6;
							result.Unit = ConversionOptions.KilometersPerHour;
							break;
					}
					break;
                case ConversionOptions.Mach:
                    switch (toUnit)
                    {
                        case ConversionOptions.Knots:
                            result.Value = FUL.Conversions.MachToKnots(value);
                            result.Unit = ConversionOptions.Knots;
                            break;
                    }
                    break;
				case ConversionOptions.Fahrenheit:
					if (toUnit == ConversionOptions.Celsius)
					{
						result.Value = (5.0 / 9.0) * (value - 32.0);
						result.Unit = ConversionOptions.Celsius;
					}
					break;
				case ConversionOptions.Celsius:
					if (toUnit == ConversionOptions.Fahrenheit)
					{
						result.Value = (9.0 / 5.0) * value + 32.0;
						result.Unit = ConversionOptions.Fahrenheit;
					}
					break;
				case ConversionOptions.Hectopascals:
					switch (toUnit)
					{
						case ConversionOptions.Millibars:
							result.Value = value;
							result.Unit = ConversionOptions.Hectopascals;
							break;
						case ConversionOptions.InchesOfMercury:
							result.Value = value * 0.0295333727;
							result.Unit = ConversionOptions.InchesOfMercury;
							break;
					}
					break;
				case ConversionOptions.Millibars:
					switch (toUnit)
					{
						case ConversionOptions.Hectopascals:
							result.Value = value;
							result.Unit = ConversionOptions.Hectopascals;
							break;
						case ConversionOptions.InchesOfMercury:
							result.Value = value * 0.0295333727;
							result.Unit = ConversionOptions.InchesOfMercury;
							break;
					}
					break;
				case ConversionOptions.InchesOfMercury:
					switch (toUnit)
					{
						case ConversionOptions.Hectopascals:
							result.Value = value * 33.86;
							result.Unit = ConversionOptions.Millibars;
							break;
						case ConversionOptions.Millibars:
							result.Value = value * 33.86;
							result.Unit = ConversionOptions.Millibars;
							break;
					}
					break;
                case ConversionOptions.LBS:
                    switch (toUnit)
                    {
                        case ConversionOptions.LBS10:
                            result.Value = value / 10.0;
                            result.Unit = ConversionOptions.LBS10;
                            break;
                        case ConversionOptions.LBS100:
                            result.Value = value / 100.0;
                            result.Unit = ConversionOptions.LBS100;
                            break;
                        case ConversionOptions.KILOS:
                            result.Value = value / 2.2046;
                            result.Unit = ConversionOptions.KILOS;
                            break;
                        case ConversionOptions.US_GAL:
                            result.Value = value / 6.7;  // at temperature 60 F
                            result.Unit = ConversionOptions.US_GAL;
                            break;
                    }
                    break;
                case ConversionOptions.LBS10:
                    switch (toUnit)
                    {
                        case ConversionOptions.LBS:
                            result.Value = value * 10.0;
                            result.Unit = ConversionOptions.LBS;
                            break;
                        case ConversionOptions.LBS100:
                            result.Value = value / 10.0;
                            result.Unit = ConversionOptions.LBS100;
                            break;
                        case ConversionOptions.KILOS:
                            result.Value = value * 10.0 / 2.2046;
                            result.Unit = ConversionOptions.KILOS;
                            break;
                    }
                    break;
                case ConversionOptions.LBS100:
                    switch (toUnit)
                    {
                        case ConversionOptions.LBS:
                            result.Value = value * 100.0;
                            result.Unit = ConversionOptions.LBS;
                            break;
                        case ConversionOptions.LBS10:
                            result.Value = value * 10.0;
                            result.Unit = ConversionOptions.LBS10;
                            break;
                        case ConversionOptions.KILOS:
                            result.Value = value * 100.0 / 2.2046;
                            result.Unit = ConversionOptions.KILOS;
                            break;
                    }
                    break;
                case ConversionOptions.KILOS:
                    switch (toUnit)
                    {
                        case ConversionOptions.KILOS10:
                            result.Value = value / 10.0;
                            result.Unit = ConversionOptions.KILOS10;
                            break;
                        case ConversionOptions.KILOS100:
                            result.Value = value / 100.0;
                            result.Unit = ConversionOptions.KILOS100;
                            break;
                        case ConversionOptions.LBS:
                            result.Value = value * 2.2046;
                            result.Unit = ConversionOptions.KILOS;
                            break;
                        case ConversionOptions.LITERS:
                            result.Value = value / 0.8075;
                            result.Unit = ConversionOptions.LITERS;
                            break;
                    }
                    break;
                case ConversionOptions.KILOS10:
                    switch (toUnit)
                    {
                        case ConversionOptions.KILOS:
                            result.Value = value * 10.0;
                            result.Unit = ConversionOptions.KILOS;
                            break;
                        case ConversionOptions.KILOS100:
                            result.Value = value / 10.0;
                            result.Unit = ConversionOptions.KILOS100;
                            break;
                        case ConversionOptions.LBS:
                            result.Value = value * 10.0 * 2.2046;
                            result.Unit = ConversionOptions.LBS;
                            break;
                    }
                    break;
                case ConversionOptions.KILOS100:
                    switch (toUnit)
                    {
                        case ConversionOptions.KILOS:
                            result.Value = value * 100.0;
                            result.Unit = ConversionOptions.KILOS;
                            break;
                        case ConversionOptions.KILOS10:
                            result.Value = value * 10.0;
                            result.Unit = ConversionOptions.KILOS10;
                            break;
                        case ConversionOptions.LBS:
                            result.Value = value * 100.0 * 2.2046;
                            result.Unit = ConversionOptions.LBS;
                            break;
                    }
                    break;
                case ConversionOptions.US_GAL:
                    switch (toUnit)
                    {
                        case ConversionOptions.LBS:
                            result.Value = value * 6.7;  // at temperature 60 F
                            result.Unit = ConversionOptions.LBS;
                            break;
                        case ConversionOptions.UK_GAL:
                            result.Value = value * 0.83267;
                            result.Unit = ConversionOptions.UK_GAL;
                            break;
                        case ConversionOptions.KILOS:
                            result.Value = value * 3.79;
                            result.Unit = ConversionOptions.KILOS;
                            break;
                    }
                    break;
                case ConversionOptions.UK_GAL:
                    switch (toUnit)
                    {
                        case ConversionOptions.US_GAL:
                            result.Value = value * 1.20095;
                            result.Unit = ConversionOptions.US_GAL;
                            break;
                        case ConversionOptions.LBS:
                            result.Value = value * 8.046365;  // at temperature 60 F
                            result.Unit = ConversionOptions.LBS;
                            break;
                        case ConversionOptions.KILOS:
                            result.Value = value * 3.6498;
                            result.Unit = ConversionOptions.KILOS;
                            break;
                    }
                    break;
                case ConversionOptions.LITERS:
                    switch (toUnit)
                    {
                        case ConversionOptions.KILOS:
                            result.Value = value * 0.8075;
                            result.Unit = ConversionOptions.KILOS;
                            break;
                        case ConversionOptions.LBS:
                            result.Value = value * 1.7544;
                            result.Unit = ConversionOptions.LBS;
                            break;
                    }
                    break;
            }

			if (forceUnit == ForceUnit.Dynamic)
			{
				// Check our cutoff values and if we've gone over then we'll do a different conversion.
				switch (toUnit)
				{
					case ConversionOptions.Feet:
						if (result.Unit == ConversionOptions.Feet && result.Value > StatuteMilesCutoff)
						{
							// Convert to miles.
							result.Value = result.Value / 5280.0;
							result.Unit = ConversionOptions.StatuteMiles;
						}
						break;
					case ConversionOptions.Meters:
						if (result.Unit == ConversionOptions.Meters && result.Value > KilometersCutoff)
						{
							// Convert to kilometers.
							result.Value = result.Value / 1000.0;
							result.Unit = ConversionOptions.Kilometers;
						}
						break;
				}
			}
			else if (forceUnit == ForceUnit.ToLarger)
			{
				// If we have a unit that allows an upconvert then do so.
				switch (toUnit)
				{
					case ConversionOptions.Feet:
						if (result.Unit == ConversionOptions.Feet)
						{
							// Convert to miles.
							result.Value = result.Value / 5280.0;
							result.Unit = ConversionOptions.StatuteMiles;
						}
						break;
					case ConversionOptions.Meters:
						if (result.Unit == ConversionOptions.Meters)
						{
							// Convert to kilometers.
							result.Value = result.Value / 1000.0;
							result.Unit = ConversionOptions.Kilometers;
						}
						break;
				}
			}

			if (roundingDigits > -1 && roundingDigits < 16)		// Math.Round throws an exception for values outside the 0-15 range.
				result.Value = Math.Round(result.Value, roundingDigits);

			return result;
		}

		public static ConversionResult ConvertTempDiff(double value, ConversionOptions fromUnit, ConversionOptions toUnit, int roundingDigits)
		{
			ConversionResult result = new ConversionResult(value, fromUnit);

			if (fromUnit == ConversionOptions.Unknown || toUnit == ConversionOptions.Unknown || fromUnit == toUnit)
			{
				if (roundingDigits > -1 && roundingDigits < 16)		// Math.Round throws an exception for values outside the 0-15 range.
					result.Value = Math.Round(result.Value, roundingDigits);

				return result;
			}

			switch (fromUnit)
			{
				case FUL.Conversion.ConversionOptions.Fahrenheit:
					if (toUnit == FUL.Conversion.ConversionOptions.Celsius)
					{
						result.Value *= (5.0 / 9.0);
						result.Unit = ConversionOptions.Celsius;
					}
					break;
				case FUL.Conversion.ConversionOptions.Celsius:
                    if (toUnit == FUL.Conversion.ConversionOptions.Fahrenheit)
                    {
                        result.Value *= (9.0 / 5.0);
                        result.Unit = ConversionOptions.Fahrenheit;
                    }
					break;
				default:
					break;
			}

			if (roundingDigits > -1 && roundingDigits < 16)		// Math.Round throws an exception for values outside the 0-15 range.
				result.Value = Math.Round(result.Value, roundingDigits);

			return result;
		}
	}
}

namespace FUL
{
	public class Conversions
	{
		// The length and speed conversions below are using factors from the 
		// National Oceanograhic Data Center http://www.nodc.noaa.gov/dsdt/ucg/index.html

		/// <summary>
		/// Convert Nautical Miles to Statue Miles
		/// </summary>
		/// <param name="distance"></param>
		/// <returns>Miles</returns>
		public static double NMtoMile(double distance)
		{
			return distance * 1.1515144;
		}

		/// <summary>
		/// Convert Statue Miles to Nautical Miles
		/// </summary>
		/// <param name="distance"></param>
		/// <returns>Nautical Miles</returns>
		public static double MiletoNM(double distance)
		{
			return distance * 0.8684216;
		}

		/// <summary>
		/// Convert Kilometers to Statue Miles
		/// </summary>
		/// <param name="distance"></param>
		/// <returns>Miles</returns>
		public static double KMtoMile(double distance)
		{
			return distance * 0.621371;
		}

		/// <summary>
		/// Convert Statue Miles to Kilometers
		/// </summary>
		/// <param name="distance"></param>
		/// <returns>Kilometers</returns>
		public static double MileToKM(double distance)
		{
			return distance / 0.621371;
		}

		/// <summary>
		/// Convert Kilometers to Nautical Miles
		/// </summary>
		/// <param name="distance"></param>
		/// <returns>Nautical Miles</returns>
		public static double KMtoNM(double distance)
		{
			return distance * 0.539612;
		}

		/// <summary>
		/// Convert Nautical Miles to Kilometers
		/// </summary>
		/// <param name="distance"></param>
		/// <returns>Kilometers</returns>
		public static double NMToKM(double distance)
		{
			return distance / 0.539612;
		}

		/// <summary>
		/// Convert Knots to Meters per Second
		/// </summary>
		/// <param name="speed"></param>
		/// <returns>meters/second</returns>
		public static double KnotsToMetersPerSecond(double speed)
		{
			return speed * 0.514444;
		}

		/// <summary>
		/// Convert Meters per Second to Knots
		/// </summary>
		/// <param name="speed"></param>
		/// <returns>Knots</returns>
		public static double MetersPerSecondToKnots(double speed)
		{
			return speed / 0.514444;
		}

        public static double MachToKnots(double speed)
        {
            return speed * 666.738661;
        }
		/// <summary>
		/// Convert Knots to Miles per Hour
		/// </summary>
		/// <param name="speed"></param>
		/// <returns>mph</returns>
		public static double KnotsToMPH(double speed)
		{
			return speed * 1.15083;
		}

		/// <summary>
		/// Convert Miles per Hour to Knots
		/// </summary>
		/// <param name="speed"></param>
		/// <returns>Knots</returns>
		public static double MPHtoKnots(double speed)
		{
			return speed / 1.15083;
		}

		/// <summary>
		/// Convert meters to feet
		/// </summary>
		/// <param name="speed"></param>
		/// <returns>feet</returns>
		public static double MeterToFeet(double distance)
		{
			return distance * 3.280839895;
		}

		/// <summary>
		/// Convert feet to meters
		/// </summary>
		/// <param name="speed"></param>
		/// <returns>meters</returns>
		public static double FeetToMeters(double distance)
		{
			return distance * 0.3048;
		}

		/// <summary>
		/// Convert Kilograms to pounds
		/// </summary>
		/// <param name="Kilos"></param>
		/// <returns>Pounds</returns>
		public static double KilosToPounds(double Kilos)
		{
			return Kilos * 2.20462262;
		}

		public static double MilesToInches(double Miles)
		{
			return Miles * 63360;
		}

		public static double InchesToMiles(double Inches)
		{
			return Inches / 63360;
		}

        public static double FeetToMiles(double Feet)
        {
            return Feet / 5280;
        }


		// ------------------------------------------------------------------------
		/// <summary>
		/// This function converts a string lat/long to a float.
		/// North & East are positive. South & West are negitive values.
		/// </summary>
		/// <param name="StringDataValue">5 to 8 character string value (i.e., 423109N)</param>
		/// <returns>value (i.e., -125.231)</returns>
		public static float ConvertStringToDegrees(string StringDataValue)
		{
			float DataValue = -181;

			string hemisphere = StringDataValue.Substring(StringDataValue.Length - 1, 1); // access the hemisphere indicator
			StringDataValue = StringDataValue.Substring(0, StringDataValue.Length - 1);  // strip off the hemisphere indicator
			switch (StringDataValue.Length)
			{
				case 4:
					DataValue = Convert.ToSingle(Convert.ToInt16(StringDataValue.Substring(0, 2)) + (Convert.ToInt16(StringDataValue.Substring(2, 2))) / 60.0);
					break;
				case 5:
					DataValue = Convert.ToSingle(Convert.ToInt16(StringDataValue.Substring(0, 3)) + (Convert.ToInt16(StringDataValue.Substring(3, 2))) / 60.0);
					break;
				case 6:
					DataValue = Convert.ToSingle(Convert.ToInt16(StringDataValue.Substring(0, 2)) + (Convert.ToInt16(StringDataValue.Substring(2, 2))) / 60.0) + Convert.ToSingle((Convert.ToInt16(StringDataValue.Substring(4, 2))) / 3600.0);
					break;
				case 7:
					DataValue = Convert.ToSingle(Convert.ToInt16(StringDataValue.Substring(0, 3)) + (Convert.ToInt16(StringDataValue.Substring(3, 2))) / 60.0) + Convert.ToSingle((Convert.ToInt16(StringDataValue.Substring(5, 2))) / 3600.0);
					break;
				default:
					break;
			}
			if ((string.Equals(hemisphere.ToUpper(), "S")) || (string.Equals(hemisphere.ToUpper(), "W")))
				DataValue = 0 - DataValue;
			return DataValue;
		} // End ConvertStringToDegrees

		// ------------------------------------------------------------------------
		/// <summary>
		/// This function takes the float representation of a Lat or Long and makes
		/// it into a string
		/// </summary>
		/// <param name="DataValue">the value to convert</param>
		/// <param name="LatLong">if the value is a "Lat" or "Long"</param>
		/// <returns>String representation of data value</returns>
		public static string ConvertDegreesToString(float DataValue, string LatLong)
		{
			return ConvertDegreesToString2(DataValue, LatLong, true);

		} // End ConvertDegreesToString

		// ------------------------------------------------------------------------
		/// <summary>
		/// This function takes the float representation of a Lat or Long and makes
		/// it into a string. Caller has control over placing the Hemisphere character before/after digits.
		/// </summary>
		/// <param name="DataValue"></param>
		/// <param name="LatLong"></param>
		/// <param name="HemisphereSuffix"></param>
		/// <returns></returns>
		public static string ConvertDegreesToString2(float DataValue, string LatLong, bool HemisphereSuffix)
		{
			LatLong = LatLong.ToLower();
			try
			{
				// Get hemisphere character set
				string hemisphere;
				if (LatLong.Equals("long"))
				{
					hemisphere = "E";
					if (DataValue < 0)
						hemisphere = "W";
				}
				else
				{
					hemisphere = "N";
					if (DataValue < 0)
						hemisphere = "S";
				}

				// now that hemisphere is known, any negative numbers
				// will no longer be significant
				DataValue = Math.Abs(DataValue);
				string data = DataValue.ToString();

				// the whole part of the number is the degree value
				string degrees = data;
				if (data.IndexOf(".") > -1)
				{
					degrees = data.Substring(0, data.IndexOf("."));
				}

				// Make sure lats are 2 characters and longs are 3
				while (LatLong.Equals("long") && degrees.Length < 3)
					degrees = "0" + degrees;
				while (LatLong.Equals("lat") && degrees.Length < 2)
					degrees = "0" + degrees;

				int minutes = (int)Math.Round((decimal)(((DataValue - (int)(DataValue / 1.0)) * 3600.0) / 60.0), 0);
				if (minutes == 60)
					minutes = 0;
				string min = minutes.ToString();
				if (min.Length < 2) min = "0" + min;

				if (HemisphereSuffix == true)
					return degrees + min + hemisphere;
				else
					return hemisphere + degrees + min;

			}
			catch
			{
				return "unknown";
			}
		} // End ConvertDegreesToString2

		// ------------------------------------------------------------------------
		/// <summary>
		/// Convert string format to FUL.Coordinate
		/// 1 or 2 digits for "Seconds". N27000W078200 or N270001W0782001
		/// The boolean indicates if the last 1 or 2 digits is Seconds or Tenths of Minutes.
		/// </summary>
		/// <param name="PositionString"></param>
		/// <param name="TenthsOfMinutes"></param>
		/// <returns></returns>
		public static Coordinate ConvertToCoordinates(string PositionString, bool TenthsOfMinutes)
		{
			Coordinate Coordinate = new FUL.Coordinate(true);
			int StringLength = PositionString.Length;
			double Degrees = 0;
			double Minutes = 0;
			double Seconds = 0;
			double SecDivisor = 1;
			int LonIndex = 0;
			int SecondsLength = 0;

			if ((StringLength == 13) || (StringLength == 15))
			{
				if (StringLength == 13)
				{
					LonIndex = 7;
					SecondsLength = 1;
					SecDivisor = 600;
				}
				else
				{
					LonIndex = 8;
					SecondsLength = 2;
					SecDivisor = 6000;
				}

				Degrees = Convert.ToDouble(PositionString.Substring(1, 2));
				Minutes = Convert.ToDouble(PositionString.Substring(3, 2));
				Seconds = Convert.ToDouble(PositionString.Substring(5, SecondsLength));
				if (TenthsOfMinutes == true)
					Coordinate.Lat = Math.Round(Degrees + (Minutes / 60.0) + (Seconds / SecDivisor), 4, MidpointRounding.AwayFromZero);
				else
					Coordinate.Lat = Math.Round(Degrees + (Minutes / 60.0) + (Seconds / 3600.0), 4, MidpointRounding.AwayFromZero);
				if (string.Equals(PositionString.Substring(0, 1), "S"))
					Coordinate.Lat = 0 - Coordinate.Lat;

				Degrees = Convert.ToDouble(PositionString.Substring(LonIndex, 3));
				Minutes = Convert.ToDouble(PositionString.Substring(LonIndex + 3, 2));
				Seconds = Convert.ToDouble(PositionString.Substring(LonIndex + 5, SecondsLength));
				if (TenthsOfMinutes == true)
					Coordinate.Lon = Math.Round(Degrees + (Minutes / 60.0) + (Seconds / SecDivisor), 4, MidpointRounding.AwayFromZero);
				else
					Coordinate.Lon = Math.Round(Degrees + (Minutes / 60.0) + (Seconds / 3600.0), 4, MidpointRounding.AwayFromZero);
				if (string.Equals(PositionString.Substring(LonIndex - 1, 1), "W"))
					Coordinate.Lon = 0 - Coordinate.Lon;
			}
			else
			{
				FUL.FileWriter.WriteLine(true, FileWriter.EventType.Error, "FUL.ConvertToCoordinates   Unknown lat/lon format " + PositionString);
			}
			return Coordinate;
		}

		/// <summary>
		/// Change lat/lon string from DD.DDD or DDD.DDD to DDMM.mm without the dots.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static string ConvertDecDegToDDMMdotM(string value)
		{
			string Result = string.Empty;

			string FrontPart = value.Substring(0, value.Length - 3);
			string BackPart = value.Substring(value.Length - 3, 3);

			int Minutes = int.Parse(BackPart) * 6;
			string StrMinutes = Minutes.ToString().PadLeft(4, '0');

			Result = string.Concat(FrontPart, StrMinutes);
			return Result;
		}

        /// <summary>
        /// Change float lat/lon to string DDMM.mm without the dots.
        /// </summary>
        /// <param name="Lat"></param>
        /// <param name="Lon"></param>
        /// <returns></returns>
        public static string ConvertDoublePositionToDDMMdotM(double Lat, double Lon)
        {
            string Position = string.Empty;
            string Min = string.Empty;

            string Hemisphere = "N";
            string LatString = string.Empty;
            Lat = Math.Round(Lat, 3, MidpointRounding.AwayFromZero);
            if (Lat < 0)
                Hemisphere = "S";
            Lat = Math.Abs(Lat);
            string[] Degs = Lat.ToString().Split('.');
            string Deg = Degs[0].PadLeft(2, '0');
            if (Degs.Length > 1)
                Min = Degs[1].PadRight(3, '0');
            else
                Min = "000";
            LatString = Hemisphere + Deg + Min;

            Hemisphere = "E";
            string LonString = string.Empty;
            Lon = Math.Round(Lon, 3, MidpointRounding.AwayFromZero);
            if (Lon < 0)
                Hemisphere = "W";
            Lon = Math.Abs(Lon);
            Degs = Lon.ToString().Split('.');
            Deg = Degs[0].PadLeft(3, '0');
            if (Degs.Length > 1)
                Min = Degs[1].PadRight(3, '0');
            else
                Min = "000";
            LonString = Hemisphere + Deg + Min;

            Position = string.Concat(FUL.Conversions.ConvertDecDegToDDMMdotM(LatString), FUL.Conversions.ConvertDecDegToDDMMdotM(LonString));
            return Position;
        }

		public static string ConvertDegreesToCardinalDirection(int value)
		{
			return ConvertDegreesToCardinalDirection((float)value);
		}

		public static string ConvertDegreesToCardinalDirection(float value)
		{
			if (value >= 348.75 || value < 11.25)
				return "N";
			else if (value >= 11.25 && value < 33.75)
				return "NNE";
			else if (value >= 33.75 && value < 56.25)
				return "NE";
			else if (value >= 56.25 && value < 78.75)
				return "ENE";
			else if (value >= 78.75 && value < 101.25)
				return "E";
			else if (value >= 101.25 && value < 123.75)
				return "ESE";
			else if (value >= 123.75 && value < 146.25)
				return "SE";
			else if (value >= 146.25 && value < 168.75)
				return "SSE";
			else if (value >= 168.75 && value < 191.25)
				return "S";
			else if (value >= 191.25 && value < 213.75)
				return "SSW";
			else if (value >= 213.75 && value < 236.25)
				return "SW";
			else if (value >= 236.25 && value < 258.75)
				return "WSW";
			else if (value >= 258.75 && value < 281.25)
				return "W";
			else if (value >= 281.25 && value < 303.75)
				return "WNW";
			else if (value >= 303.75 && value < 326.25)
				return "NW";
			else if (value >= 326.25 && value < 348.75)
				return "NNW";

			return string.Empty;
		}
	} // End Class
} // End namespace
