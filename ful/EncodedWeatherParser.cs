using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace FUL
{
	public class EncodedWeatherParser
	{
		public static Regex FreezingDrizzle = new Regex(@"[+-]?([A-Z]{2})*FZDZ([A-Z]{2})*", RegexOptions.ExplicitCapture | RegexOptions.Compiled);
		public static Regex FreezingFog = new Regex(@"[+-]?([A-Z]{2})*FZFG([A-Z]{2})*", RegexOptions.ExplicitCapture | RegexOptions.Compiled);
		public static Regex FreezingRain = new Regex(@"[+-]?([A-Z]{2})*FZRA([A-Z]{2})*", RegexOptions.ExplicitCapture | RegexOptions.Compiled);
		public static Regex Hail = new Regex(@"[+-]?([A-Z]{2})*GR([A-Z]{2})*", RegexOptions.ExplicitCapture | RegexOptions.Compiled);
		public static Regex IcePellets = new Regex(@"[+-]?([A-Z]{2})*(PL|PE)([A-Z]{2})*", RegexOptions.ExplicitCapture | RegexOptions.Compiled);
		public static Regex Rain = new Regex(@"[+-]?([A-Z]{2})*RA([A-Z]{2})*", RegexOptions.ExplicitCapture | RegexOptions.Compiled);
		public static Regex RainSnowMix = new Regex(@"[+-]?([A-Z]{2})*(RA|SN)([A-Z]{2})*(RA|SN)([A-Z]{2})*", RegexOptions.ExplicitCapture | RegexOptions.Compiled);
		public static Regex RainAndSnow = new Regex(@"[+-]?([A-Z]{2})*(RA|SN)([A-Z]{2})*\s([A-Z]{2})*(RA|SN)([A-Z]{2})*", RegexOptions.ExplicitCapture | RegexOptions.Compiled);
		public static Regex SeriousConditions = new Regex(@"[+-]?([A-Z]{2})*(VA|DU|SS|SQ|FC)([A-Z]{2})*", RegexOptions.ExplicitCapture | RegexOptions.Compiled);
		public static Regex SmallHail = new Regex(@"[+-]?([A-Z]{2})*GS([A-Z]{2})*", RegexOptions.ExplicitCapture | RegexOptions.Compiled);
		public static Regex Snow = new Regex(@"[+-]?([A-Z]{2})*SN([A-Z]{2})*", RegexOptions.ExplicitCapture | RegexOptions.Compiled);
		public static Regex SnowGrains = new Regex(@"[+-]?([A-Z]{2})*SG([A-Z]{2})*", RegexOptions.ExplicitCapture | RegexOptions.Compiled);
		public static Regex Thunderstorms = new Regex(@"[+-]?([A-Z]{2})*TS([A-Z]{2})*", RegexOptions.ExplicitCapture | RegexOptions.Compiled);

		public static Regex Parser = new Regex(@"[+-]|[A-Z]{2}|\s", RegexOptions.ExplicitCapture | RegexOptions.Compiled);

		public static string ParseWeatherComponent(string weather)
		{
			return ParseWeatherComponent(weather, false);
		}

		public static string ParseWeatherComponent(string weather, bool mix)
		{
			StringBuilder description = new StringBuilder();
			MatchCollection components = Parser.Matches(weather);
			bool addWith = false;

			foreach (Match component in components)
			{
				if (addWith && !mix)
					description.Append("with ");

				switch (component.Value)
				{
					case "+":
						description.Append("Heavy ");
						break;
					case "-":
						description.Append("Light ");
						break;
					case "VC":
						description.Append("In the vicinity, ");
						break;
					case "MI":
						description.Append("Shallow ");
						break;
					case "BC":
						description.Append("Patches of ");
						break;
					case "SH":
						description.Append("Showers of ");
						break;
					case "TS":
						description.Append("Thunderstorms ");
						break;
					case "FZ":
						description.Append("Freezing ");
						break;
					case "PR":
						description.Append("Partial ");
						break;
					case "DZ":
						description.Append("Drizzle ");
						addWith = true;
						break;
					case "RA":
						description.Append("Rain ");
						addWith = true;
						break;
					case "SN":
						description.Append("Snow ");
						addWith = true;
						break;
					case "SG":
						description.Append("Snow grains ");
						addWith = true;
						break;
					case "IC":
						description.Append("Ice Crystals ");
						addWith = true;
						break;
					case "PL":
					case "PE":
						description.Append("Ice Pellets ");
						addWith = true;
						break;
					case "GR":
						description.Append("Hail ");
						addWith = true;
						break;
					case "GS":
						description.Append("Small Hail ");
						addWith = true;
						break;
					case "BR":
						description.Append("Mist ");
						addWith = true;
						break;
					case "FG":
						description.Append("Fog ");
						addWith = true;
						break;
					case "FU":
						description.Append("Smoke ");
						addWith = true;
						break;
					case "VA":
						description.Append("Volcanic Ash ");
						addWith = true;
						break;
					case "DU":
						description.Append("Widespread dust ");
						addWith = true;
						break;
					case "SA":
						description.Append("Sand ");
						addWith = true;
						break;
					case "HZ":
						description.Append("Haze ");
						addWith = true;
						break;
					case "PO":
						description.Append("Dust/Sand whirls ");
						addWith = true;
						break;
					case "SQ":
						description.Append("Squall ");
						addWith = true;
						break;
					case "FC":
						description.Append("Funnel clouds ");
						addWith = true;
						break;
					case "SS":
						description.Append("Sandstorm ");
						addWith = true;
						break;
					case "DS":
						description.Append("Duststorm ");
						addWith = true;
						break;
					case " ":
						description.Append("and ");
						break;
				}
			}

			if (mix)
				description.Append("Mix");

			return description.ToString().Trim();
		}
	}
}
