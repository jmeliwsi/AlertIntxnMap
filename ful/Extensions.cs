using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FUL
{
	public static class Extensions
	{
		public static int ListCount(this String str)
		{
			return str.Split(new char[] { ',', ';', '|' }, StringSplitOptions.None).Length;
		}

		public static bool Contains(this string source, string value, StringComparison comparisonType)
		{
			return source.IndexOf(value, comparisonType) >= 0;
		}

		/// <summary>
		/// Extension method to convert a string to uppper case for the first letter of each word.
		/// A word is defined as white space between characters.
		/// 
		/// Example:  ALL CAPS HERE
		/// To:  All Caps Here
		/// 
		/// NOte must convert to lower case first as ToTileCase only works with lower case to start with.
		/// </summary>
		/// <param name="source">string to convert</param>
		/// <returns>converted string</returns>
		public static string ToTitleCase( this string source)
		{
			return(System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(source.ToLower()));
		}
    }
}
