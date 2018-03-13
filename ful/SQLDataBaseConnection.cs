using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Diagnostics;
using FUL.Logging;

namespace FUL.DataBaseConnection
{
	/// <summary>
	/// This is a utility class for managing connections to the SQL Server data base.
	/// It is being used to encapsulate retrys, logging and connection pooling issues.
	/// This is to help idenify and fix data base issues.
	/// </summary>
	public class SQLDataBaseConnection
	{
		/// <summary>
		/// Private copy of the connection string.
		/// </summary>
		private string connectionstring = String.Empty;
		/// <summary>
		/// Get the the actual data base connection string.  This is set by the contructor.
		/// </summary>
		public string ConnectionString
		{
			get
			{
				return connectionstring;
			}

			set { connectionstring = value; }
		}

		/// <summary>
		/// Local value for maximum number of times to try the data base connection.
		/// </summary>
		private int maxtrycount = 1;
		/// <summary>
		/// Flag to indicate if all exceptions are to be logged.  These are local logging outside the callers needs.
		/// </summary>
		private bool isLogExceptions = false;

		/// <summary>
		/// Empty constructor.  only here to to be a null contrcutor.  not expected to be used.
		/// </summary>
		public SQLDataBaseConnection()
		{
			init(String.Empty);
		}

		/// <summary>
		/// constructor ... pass two charater data base connection type from App Conifiguration
		/// </summary>
		/// <param name="connectionType">two character data base connection string used to identify connections string from App config file.</param>
		public SQLDataBaseConnection(string connectionType)
		{
			init(connectionType);
		}

		/// <summary>
		/// Execute a Non Query sql command.  returns results value from command.
		/// </summary>
		/// <param name="sqlCommand">sql command to run on data base/param>
		/// <returns>return value from execution of command</returns>
		public bool ExecuteNonQuery(SqlCommand sqlCommand)
		{
			bool executeresult = false;
			Exception saveException = null;

			// loop from 0 to max try count.
			for (int trycount = 0; trycount < this.maxtrycount; trycount++)
			{
				try
				{
					// instantiate sql connection with string
					using (sqlCommand.Connection = new SqlConnection(this.ConnectionString))
					{
						// open connection checking the pooled connections
						openConnectionWithPoolCheck(sqlCommand.Connection);
						// execute sql command
						sqlCommand.ExecuteNonQuery();

						// get results from call
						if (sqlCommand.Parameters.Count > 0 && sqlCommand.Parameters.Contains("@ReturnValue"))
							executeresult = Convert.ToBoolean(sqlCommand.Parameters["@ReturnValue"].Value);
						else
							executeresult = true;
					}
					// success, clear prior exceptions
					saveException = null;
					// get out of retry loop.
					break;
				}
				catch (Exception ex)
				{
					// there was an exception, log the exception.
					StackFrame frame = new StackFrame(1);
					logSqlCommandException(sqlCommand, ex, trycount, frame.GetMethod().Name + " / ExecuteNonQuery");
					// save the exception to return to caller once max tries is completet.
					saveException = ex;
				}
			}
			// if there was a saved exeption, throw it to the caller.
			if (saveException != null)
				throw (saveException);
			// sql exectuted so return result.
			return (executeresult);
		}

		/// <summary>
		/// Execute a Non Query sql command.  returns results value from command.
		/// </summary>
		/// <param name="sqlCommand">sql command to run on data base/param>
		/// <returns>return value from execution of command</returns>
		public bool ExecuteNonQueryWithParameters(SqlCommand sqlCommand, out SqlParameterCollection outCmdParameters)
		{
			bool executeresult = false;
			Exception saveException = null;
			outCmdParameters = null;
			// loop from 0 to max try count.
			for (int trycount = 0; trycount < this.maxtrycount; trycount++)
			{
				try
				{
					// instantiate sql connection with string
					using (sqlCommand.Connection = new SqlConnection(this.ConnectionString))
					{
						// open connection checking the pooled connections
						openConnectionWithPoolCheck(sqlCommand.Connection);
						// execute sql command
						sqlCommand.ExecuteNonQuery();

						outCmdParameters = sqlCommand.Parameters;

						// get results from call
						if (sqlCommand.Parameters.Count > 0 && sqlCommand.Parameters.Contains("@ReturnValue"))
							executeresult = Convert.ToBoolean(sqlCommand.Parameters["@ReturnValue"].Value);
						else
							executeresult = true;
					}
					// success, clear prior exceptions
					saveException = null;
					// get out of retry loop.
					break;
				}
				catch (Exception ex)
				{
					// there was an exception, log the exception.
					StackFrame frame = new StackFrame(1);
					logSqlCommandException(sqlCommand, ex, trycount, frame.GetMethod().Name + " / ExecuteNonQueryWithParameters");
					// save the exception to return to caller once max tries is completet.
					saveException = ex;
				}
			}
			// if there was a saved exeption, throw it to the caller.
			if (saveException != null)
				throw (saveException);
			// sql exectuted so return result.
			return (executeresult);
		}

		/// <summary>
		/// Execute a Non Query sql command.  returns results value from command.
		/// </summary>
		/// <param name="sqlCommand">sql command to run on data base/param>
		/// <returns>return value from execution of command</returns>
		public bool ExecuteNonQueryWithTransaction(List<SqlCommand> commandList)
		{
			bool executeresult = false;
			Exception saveException = null;

			// loop from 0 to max try count.
			for (int trycount = 0; trycount < this.maxtrycount; trycount++)
			{
				try
				{
					// instantiate sql connection with string
					using (SqlConnection connection = new SqlConnection(this.ConnectionString))
					{
						// open connection checking the pooled connections
						openConnectionWithPoolCheck(connection);
						using (SqlTransaction transaction = connection.BeginTransaction())
						{
							foreach (SqlCommand sqlCommand in commandList)
							{
								sqlCommand.Connection = connection;
								sqlCommand.Transaction = transaction;
								// execute sql command
								try
								{
									sqlCommand.ExecuteNonQuery();
								}
								catch (Exception ex1)
								{
									StackFrame frame = new StackFrame(1);
									logSqlCommandException(sqlCommand, ex1, trycount, frame.GetMethod().Name + " / ExecuteNonQueryWithTransaction");
									transaction.Rollback();
									executeresult = false;
									throw (ex1);
								}
								executeresult = true;
							}
							transaction.Commit();
						}
					}
					// success, clear prior exceptions
					saveException = null;
					// get out of retry loop.
					break;
				}
				catch (Exception ex)
				{
					// there was an exception, log the exception.
					StackFrame frame = new StackFrame(1);
					logSqlCommandException(null, ex, trycount, frame.GetMethod().Name + " / ExecuteNonQueryWithTransaction");
					// save the exception to return to caller once max tries is completet.
					saveException = ex;
				}
			}
			// if there was a saved exeption, throw it to the caller.
			if (saveException != null)
				throw (saveException);
			// sql exectuted so return result.
			return (executeresult);
		}

		/// <summary>
		/// Fill a sql data set from a sql command
		/// </summary>
		/// <param name="sqlCommand">sql command to execute</param>
		/// <returns>data set from call</returns>
		public DataSet FillDataSet(SqlCommand sqlCommand)
		{
			DataSet dsresult = new DataSet();
			Exception saveException = null;
			// loop from 0 to max try count.
			for (int trycount = 0; trycount < this.maxtrycount; trycount++)
			{
				try
				{
					// instantiate sql connection
					using (sqlCommand.Connection = new SqlConnection(this.ConnectionString))
					{
						// open connection checking that we have a good pool connection.
						openConnectionWithPoolCheck(sqlCommand.Connection);
						// create adapter to use for the command.
						SqlDataAdapter sqlAdapter = new SqlDataAdapter(sqlCommand);
						// fill the data set from the command.
						sqlAdapter.Fill(dsresult);
					}
					// success, get rid of any previous exceptions
					saveException = null;
					// break the loop
					break;
				}
				catch (Exception ex)
				{
					// log any exceptions
					StackFrame frame = new StackFrame(1);
					logSqlCommandException(sqlCommand, ex, trycount, frame.GetMethod().Name + " / FillDataSet");
					// save the exception to return to the caller.
					saveException = ex;
				}
			}
			// if there was an exeption, throw it to the caller.
			if (saveException != null)
				throw (saveException);
			// success, return the data set.
			return (dsresult);
		}

		/// <summary>
		/// Fill a sql data set from a sql command
		/// </summary>
		/// <param name="sqlCommand">sql command to execute</param>
		/// <returns>data set from call</returns>
		public DataSet FillDataSetWithParameters(SqlCommand sqlCommand, out SqlParameterCollection outparameters)
		{
			DataSet dsresult = new DataSet();
			Exception saveException = null;
			// loop from 0 to max try count.
			for (int trycount = 0; trycount < this.maxtrycount; trycount++)
			{
				try
				{
					// instantiate sql connection
					using (sqlCommand.Connection = new SqlConnection(this.ConnectionString))
					{
						// open connection checking that we have a good pool connection.
						openConnectionWithPoolCheck(sqlCommand.Connection);
						// create adapter to use for the command.
						SqlDataAdapter sqlAdapter = new SqlDataAdapter(sqlCommand);
						// fill the data set from the command.
						sqlAdapter.Fill(dsresult);
					}
					// success, get rid of any previous exceptions
					saveException = null;
					// break the loop
					break;
				}
				catch (Exception ex)
				{
					// log any exceptions
					StackFrame frame = new StackFrame(1);
					logSqlCommandException(sqlCommand, ex, trycount, frame.GetMethod().Name + " / FillDataSetWithParameters");
					// save the exception to return to the caller.
					saveException = ex;
				}
			}
			// if there was an exeption, throw it to the caller.
			if (saveException != null)
				throw (saveException);
			// update parameters for return to caller.
			outparameters = sqlCommand.Parameters;
			// success, return the data set.
			return (dsresult);
		}

		/// <summary>
		/// Execute UPDATE command using a SELECT statement and SELECT dataset to the caller.
		/// </summary>
		/// <param name="sqlCommand">sql SELECT command</param>
		/// <returns>data set from call</returns>
		public void Update(SqlCommand sqlCommand, ref DataTable dsTable)
		{
			DataSet dsresult = new DataSet();
			Exception saveException = null;
			// loop from 0 to max try count.
			for (int trycount = 0; trycount < this.maxtrycount; trycount++)
			{
				try
				{
					// instantiate sql connection
					using (sqlCommand.Connection = new SqlConnection(this.ConnectionString))
					{
						// open connection checking that we have a good pool connection.
						openConnectionWithPoolCheck(sqlCommand.Connection);
						// create adapter to use for the command.
						SqlDataAdapter sqlAdapter = new SqlDataAdapter();
						SqlCommandBuilder cb = new SqlCommandBuilder(sqlAdapter);
						sqlAdapter.SelectCommand = sqlCommand;
						int nRows = sqlAdapter.Update(dsTable);
						dsTable.Clear();
						// fill the data set from the command.
						sqlAdapter.Fill(dsTable);
					}
					// success, get rid of any previous exceptions
					saveException = null;
					// break the loop
					break;
				}
				catch (Exception ex)
				{
					// log any exceptions
					StackFrame frame = new StackFrame(1);
					logSqlCommandException(sqlCommand, ex, trycount, frame.GetMethod().Name + " / Update");
					// save the exception to return to the caller.
					saveException = ex;
				}
			}
			// if there was an exeption, throw it to the caller.
			if (saveException != null)
				throw (saveException);
			// success, return the data set.
			//return (dsresult);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sqlCommand"></param>
		/// <returns></returns>
		public object ExecuteScaler(SqlCommand sqlCommand)
		{
			object executeresult = null;
			Exception saveException = null;
			// loop from 0 to max try count.
			for (int trycount = 0; trycount < this.maxtrycount; trycount++)
			{
				try
				{
					// instantiate sql connection with string
					using (sqlCommand.Connection = new SqlConnection(this.ConnectionString))
					{
						// open connection checking the pooled connections
						openConnectionWithPoolCheck(sqlCommand.Connection);
						// execute sql command
						executeresult = sqlCommand.ExecuteScalar();
					}
					// success, clear prior exceptions
					saveException = null;
					// get out of retry loop.
					break;
				}
				catch (Exception ex)
				{
					// there was an exception, log the exception.
					StackFrame frame = new StackFrame(1);
					logSqlCommandException(sqlCommand, ex, trycount, frame.GetMethod().Name + " / ExecuteScaler");
					// save the exception to return to caller once max tries is completet.
					saveException = ex;
				}
			}
			// if there was a saved exeption, throw it to the caller.
			if (saveException != null)
				throw (saveException);
			// sql exectuted so return result.
			return (executeresult);
		}

		/// <summary>
		/// Execute a Non Query sql command.  returns results value from command.
		/// </summary>
		/// <param name="dt">DataTable to upload/param>
		/// <returns>return value from execution of command</returns>
		public bool BulkLoad(DataTable dt, string destinationTableName)
		{
			bool executeresult = false;
			Exception saveException = null;
			SqlCommand sqlCommand = new SqlCommand();

			// loop from 0 to max try count.
			for (int trycount = 0; trycount < this.maxtrycount; trycount++)
			{
				try
				{
					// instantiate sql connection with string
					using (sqlCommand.Connection = new SqlConnection(this.ConnectionString))
					{
						// open connection checking the pooled connections
						openConnectionWithPoolCheck(sqlCommand.Connection);

						using (SqlTransaction transaction = sqlCommand.Connection.BeginTransaction())
						{
							sqlCommand.Transaction = transaction;

							SqlBulkCopy bulkCopy = new SqlBulkCopy(sqlCommand.Connection, SqlBulkCopyOptions.Default, transaction);
							bulkCopy.DestinationTableName = destinationTableName;

							// execute sql command
							try
							{
								bulkCopy.WriteToServer(dt);
							}
							catch (Exception ex1)
							{
								StackFrame frame = new StackFrame(1);
								logSqlCommandException(sqlCommand, ex1, trycount, frame.GetMethod().Name + " / BulkLoad");
								transaction.Rollback();
								executeresult = false;
								throw (ex1);
							}
							executeresult = true;

							transaction.Commit();
						}
					}

					// success, clear prior exceptions
					saveException = null;
					// get out of retry loop.
					break;
				}
				catch (Exception ex)
				{
					// there was an exception, log the exception.
					StackFrame frame = new StackFrame(1);
					logSqlCommandException(sqlCommand, ex, trycount, frame.GetMethod().Name + " / BulkLoad");
					// save the exception to return to caller once max tries is completet.
					saveException = ex;
				}
			}
			// if there was a saved exeption, throw it to the caller.
			if (saveException != null)
				throw (saveException);
			// sql exectuted so return result.
			return (executeresult);
		}

		/// <summary>
		/// helper method to open connection.
		/// </summary>
		/// <param name="connectionType">two character data base connection string used to identify connections string from App config file.</param>
		private void init(string connectionType)
		{
			// if there is a connectiontype use it to create a connection string.
			if (!String.IsNullOrEmpty(connectionType))
			{
				this.connectionstring = setConnectionString(connectionType);
			}
			// get max try count from app settings.  default value is 1, or only try once.
			string appSetting;
			if (getAppSetting("DBConnectMaxTryCount", out appSetting))
			{
				this.maxtrycount = Convert.ToInt32(appSetting);
			}
			else
			{
				this.maxtrycount = 1;
			}
			// get flag if class is to log all db exceptins.
			if (getAppSetting("LogAllDBExceptions", out appSetting))
			{
				this.isLogExceptions = Convert.ToBoolean(appSetting);
			}
			else
			{
				this.isLogExceptions = false;
			}
		}

		/// <summary>
		/// Parses connection string form connection string list in app configs.
		/// </summary>
		/// <param name="connectionType">two character data base connection string used to identify connections string from App config file.</param>
		/// <returns>sql server connection string.</returns>
		private string setConnectionString(string connectionType)
		{
			return (FUL.Encryption.Decrypt(ConfigurationManager.ConnectionStrings[connectionType].ConnectionString));
		}

		/// <summary>
		/// Sets the connection string if being used by the ingestor.
		/// </summary>
		/// <param name="connectionString">The connection string as determined by the ingestor.</param>
		/// <returns>sql server connection string.</returns>
		public void SetConnectionStringForIngestor(string connectionString)
		{
			if (!string.IsNullOrWhiteSpace(connectionString))
				this.connectionstring = connectionString;
		}

		/// <summary>
		/// Log exceptions in local log (type tbd)
		/// </summary>
		/// <param name="sqlCommand">sql command which caused exception</param>
		/// <param name="ex">execption message</param>
		/// <param name="trycount">the number of try count this failed on.</param>
		private void logSqlCommandException(SqlCommand sqlCommand, Exception ex, int trycount, string methodname = "NoMethodName")
		{
			try
			{
				if (this.isLogExceptions)
				{
					StringBuilder sqlString = new StringBuilder();

					if (sqlCommand != null)
					{
						sqlString.Append(sqlCommand.CommandText);
						if (sqlCommand.Parameters.Count > 0)
						{
							// old fashsion way to get memebers of Parameter collection.  could not get newer methods to work.
							SqlParameter[] paramlist = new SqlParameter[sqlCommand.Parameters.Count];
							sqlCommand.Parameters.CopyTo(paramlist, 0);
							foreach (SqlParameter param in paramlist)
							{
								sqlString.Append(" " + param.ParameterName + "=" + Convert.ToString(param.Value));
							}
						}
					}					
					string message = "SQLDataBaseConnections: Failed to execute SQL command " + (trycount + 1) + "/" + this.maxtrycount;
                    Logger.WriteMessage(methodname, message, sqlString.ToString(), WriteServiceErrors.ErrorLevel.Debug, WriteServiceErrors.ErrorSource.WebService, -1, ex);
				}
			}
			catch( Exception e)
			{
				// failed to enable log4net adonet logging.  Log error directly to the event log.
				StackFrame frame = new StackFrame(1);
				EventLog.WriteEntry("Logging", "logSqlCommandException was not successful: " + frame.GetMethod().Name + ": " + e.Message, EventLogEntryType.Error);
			}
		}

		/// <summary>
		/// Log when a connection caused the connection pool to be cleared for this connection.
		/// </summary>
		/// <param name="myconnection">sql connection</param>
		private static void logConnectionClearPool(SqlConnection myconnection)
		{
			string message = "SQLDataBaseConnections: Failed to execute create SQL Connection, Pool Reset";
            Logger.WriteMessage("ConnectionClearPool", message, myconnection.ConnectionString, WriteServiceErrors.ErrorLevel.Debug, WriteServiceErrors.ErrorSource.WebService, -1, null);
		}

		/// <summary>
		/// open a connection ... if the connection fails to open, then try to clear teh pools first.
		/// why do we need to do this??  
		/// sources:
		/// http://stackoverflow.com/questions/2154024/sql-server-connection-pool-doesnt-detect-closed-connections
		/// http://msdn.microsoft.com/en-us/library/8xx3tyca%28v=vs.100%29.aspx
		/// </summary>
		/// <param name="myconnection">connection to open</param>
		/// <returns>opened connection</returns>
		private static SqlConnection openConnectionWithPoolCheck(SqlConnection myconnection)
		{
			try
			{
				// Try to open the sql connection.
				myconnection.Open();
				// check to see if we got a pooled connection in the open state
				if (myconnection.State != ConnectionState.Open)
				{
					// we did not get a pooled connection in a open state.  Log this.
					logConnectionClearPool(myconnection);
					// clear the pool for this connection.
					SqlConnection.ClearPool(myconnection);
					// try to open the connection again.
					myconnection.Open();
				}
			}
			catch
			{
				// An exception was thrown trying to open the connection, log that we are going to try an clear the pools.
				logConnectionClearPool(myconnection);
				// clear the pool for this connectin.
				SqlConnection.ClearPool(myconnection);
				// try to open the connection again. if this open fails, let exception be thrown to caller, this is not a clear pool connection issue.
				myconnection.Open();
			}
			// return successful open.
			return (myconnection);
		}

		/// <summary>
		/// helper to get app settings and deal with missing values.
		/// </summary>
		/// <param name="keyname">app setting key name</param>
		/// <param name="value">app setting value</param>
		/// <returns>true of setting was found, false if it was not or error</returns>
		private bool getAppSetting(string keyname, out string value)
		{
			value = String.Empty;
			try
			{
				if (ConfigurationManager.AppSettings.Count > 0)
				{
					value = ConfigurationManager.AppSettings[keyname];
					if (!String.IsNullOrEmpty(value))
						return true;
				}
			}
			catch { }
			return false;
		}
	}
}
