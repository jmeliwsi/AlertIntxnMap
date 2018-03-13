using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using FUL.DataBaseConnection;

namespace FUL.WriteServiceErrors
{
	public enum ErrorSource { WebService, DataWebService, AjaxService, WebApi, WebServiceObsolete, DataRepeatingService }
	public enum ErrorLevel { Debug, Info, Warning, Error }

	public class WriteServiceErrors
	{
		private static bool LogErrors()
		{
			try
			{
				string logging = ConfigurationManager.AppSettings["logging"];
				return string.Compare(logging, "on", true) == 0;
			}
			catch
			{
				return false;
			}
		}

		public static void WriteServiceError(string method, string message, string notes, string sql, ErrorSource source, ErrorLevel level = ErrorLevel.Error, int userId = -1)
		{
			if (!LogErrors())
				return;

			try
			{
				SqlCommand cmd = new SqlCommand();
				cmd.CommandType = System.Data.CommandType.StoredProcedure;
				cmd.CommandText = "WriteServiceError";
				cmd.Parameters.Add(new SqlParameter("@time", DateTime.UtcNow));
				cmd.Parameters.Add(new SqlParameter("@method", method));
				cmd.Parameters.Add(new SqlParameter("@message", message));
				if (userId > -1)
					cmd.Parameters.Add(new SqlParameter("@UserId", userId));
				if (!string.IsNullOrEmpty(notes))
					cmd.Parameters.Add(new SqlParameter("@notes", notes));
				if (!string.IsNullOrEmpty(sql))
					cmd.Parameters.Add(new SqlParameter("@SQL", sql));
				cmd.Parameters.Add(new SqlParameter("@Source", source.ToString()));
				cmd.Parameters.Add(new SqlParameter("@ErrorLevel", (int)level));

				SQLDataBaseConnection dbConnection = new SQLDataBaseConnection("at");
				dbConnection.ExecuteNonQuery(cmd);
			}
			catch (Exception e)
			{
				WriteServiceErrorToFile(method, message, notes, sql, source, level);
				WriteServiceErrorToFile((new StackFrame()).GetMethod().Name, e.Message, string.Empty, string.Empty, source, ErrorLevel.Warning);
			}
		}

		private static void WriteServiceErrorToFile(string method, string message, string notes, string sql, ErrorSource source, ErrorLevel level)
		{
			string logtext = string.Format("{0}: {1}, {2}, Notes: {3}, SQL: {4}, Source: {5}, ErrorLevel: {6}{7}", DateTime.UtcNow.ToString(), method, message, GetString(notes), GetString(sql), source.ToString(), (int)level, Environment.NewLine);
			File.AppendAllText(ConfigurationManager.AppSettings["logfile"], logtext);
		}

		private static string GetString(string s)
		{
			return string.IsNullOrWhiteSpace(s) ? string.Empty : s;
		}

		public static void WriteNavDataError(int customerID, int userID, string method, string elementType, string elementName, string notes, ErrorSource source)
		{
			try
			{
				SqlCommand cmd = new SqlCommand();
				cmd.CommandType = System.Data.CommandType.StoredProcedure;
				cmd.CommandText = "WriteNavDataError";
				if (customerID > 0)
					cmd.Parameters.Add(new SqlParameter("@CustomerID", customerID));
				if (userID > 0)
					cmd.Parameters.Add(new SqlParameter("@UserID", userID));
				cmd.Parameters.Add(new SqlParameter("@Method", method));
				if (!string.IsNullOrEmpty(elementType))
					cmd.Parameters.Add(new SqlParameter("@ElementType", elementType));
				cmd.Parameters.Add(new SqlParameter("@ElementName", elementName));
				if (!string.IsNullOrEmpty(notes))
					cmd.Parameters.Add(new SqlParameter("@Comment", notes));
				cmd.Parameters.Add(new SqlParameter("@Source", source.ToString()));

				SQLDataBaseConnection dbConnection = new SQLDataBaseConnection("at");
				dbConnection.ExecuteNonQuery(cmd);
			}
			catch (Exception e)
			{
				WriteServiceErrors.WriteServiceError((new StackFrame()).GetMethod().Name, e.Message, string.Empty, string.Empty, source);
			}
		}
	}
}
