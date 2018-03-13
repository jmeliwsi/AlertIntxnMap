using System;
using System.Linq;
using System.Configuration;
using System.Diagnostics;
using log4net;
using System.IO;

// watch for updates to .config which which may change behavior of log4net. 
// also initializes a log4net object so static methods can be used.
[assembly: log4net.Config.XmlConfigurator(Watch = true)]

/// <summary>
/// This file contains Logging classes which use third party Logging tools.
/// The intention is to keep the method interfaces similar so that code can be changed to use different third party tools.
/// </summary>
namespace FUL.Logging
{
    /// <summary>
    /// The Logger class is for logging which is independent of the the actual logging mechanism.  
    /// Internally it uses log4net.  Since this implementation is internal the actual logging mechanism could be swapped out with 
    /// another mechanism.  Or another mechanism could be added.
    /// 
    /// By using log4net configurations are used to determine the level of logging (DEBUG, INFO, WARN, ERROR) and the 
    /// location (data base, event view, file).  The App.config or Web.config is used to define this per application.
    /// 
    /// If the configuration is change, the change will be picked up here without having to restart services.
    /// </summary>
    public class Logger
    {
        /// <summary>
        /// Static method to log information.  It will log exceptions including stack traces by just passing the exception object.
        /// </summary>
        /// <param name="method">name of method which is logging the message</param>
        /// <param name="message">message to log</param>
        /// <param name="sql">sql or long data informaton to log</param>
        /// <param name="severity">severity of the message.  the logging can be filtered by this value</param>
        /// <param name="source">the fusion component where the logging is coming from</param>
        /// <param name="CustomerID">the customer or user id which applies to this message.  default or unknown is -1</param>
        /// <param name="exception">exception object if one was thrown to log the message and stack trace</param>
        public static void WriteMessage(string method, string message, string sql, FUL.WriteServiceErrors.ErrorLevel severity, FUL.WriteServiceErrors.ErrorSource source, int CustomerID, Exception exception = null)
        {
            try
            {
                // call lo4net to do the actually logging.
                Log4Net.WriteMessage(method, message, sql, severity.ToString(), source.ToString(), CustomerID, exception);
            }
            catch (Exception ex)
            {
                // if log4net failes, to the event viewer what happened.
                EventLog.WriteEntry("Logging", "FUL.Logging.Logger.WriteMessage Exception: " + ex.Message, EventLogEntryType.Error);
            }
        }
    }
    /// <summary>
    /// This class logs using the log4net third party library.
    /// </summary>
    public class Log4Net
    {
        /// <summary>
        /// Setup Log4Net interface.
        /// </summary>
        private static readonly ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public Log4Net()
        {

        }

        /// <summary>
        /// Static method to log using log4net
        /// </summary>
        /// <param name="method">name of method which is logging the message</param>
        /// <param name="message">message to log</param>
        /// <param name="sql">sql or long data informaton to log</param>
        /// <param name="severity">severity of the message.  the logging can be filtered by this value</param>
        /// <param name="source">the fusion component where the logging is coming from</param>
        /// <param name="CustomerID">the customer or user id which applies to this message.  default or unknown is -1</param>
        /// <param name="exception">exception object if one was thrown to log the message and stack trace</param>
        public static void WriteMessage(string method, string message, string sql, string strseverity, string strsource, int CustomerID, Exception exception = null)
        {
            // check to see if there is an adonet adapter, if so set it from the fusion connection strings
            SetLoggingConnectionString();

            // save variables which are not native to log4net.
            GlobalContext.Properties["methodName"] = method;
            GlobalContext.Properties["sqlCommand"] = sql;
            GlobalContext.Properties["userId"] = CustomerID;
            GlobalContext.Properties["source"] = strsource;

            // create date/time for log file strings
            string logfiledate = DateTime.UtcNow.ToString("_yyyy_MM_dd");

            // convert severity to basic string.
            strseverity = strseverity.ToLower().Trim();
            // convert the FUL severity types to log4net types.
            if (strseverity.Contains("error") || strseverity.Contains("fatal"))
            {
                // let the log file name
                SetLogFileName("Logger_" + strseverity + logfiledate + ".txt");
                GlobalContext.Properties["severity"] = 3;
                if (exception != null)
                    log.Error(message, exception);
                else
                    log.Error(message);
            }
            else if (strseverity.Contains("warn"))
            {
                GlobalContext.Properties["severity"] = 2;
                SetLogFileName("Logger_" + strseverity + logfiledate + ".txt");
                if (exception != null)
                    log.Warn(message, exception);
                else
                    log.Warn(message);
            }
            else if (strseverity.Contains("debug"))
            {
                GlobalContext.Properties["severity"] = 1;
                SetLogFileName("Logger_" + strseverity + logfiledate + ".txt");
                if (exception != null)
                    log.Debug(message, exception);
                else
                    log.Debug(message);
            }
            else
            {
                GlobalContext.Properties["severity"] = 1;
                SetLogFileName("Logger_" + strseverity + logfiledate + ".txt");
                // severity info and all others
                if (exception != null)
                    log.Info(message, exception);
                else
                    log.Info(message);
            }
        }

        /// <summary>
		/// Method to set logging db connection string for log4net/ADONetAppender to strings from .config files.
		/// </summary>
		private static void SetLoggingConnectionString()
        {
            try
            {
                // get log4net respository from config file
                log4net.Repository.Hierarchy.Hierarchy hier =  LogManager.GetRepository() as log4net.Repository.Hierarchy.Hierarchy;

                if (hier != null)
                {
                    // get list of appenders
                    log4net.Appender.IAppender[] adoAppenderArray = hier.GetAppenders();
                    // find the ado.net appender
                    var adoAppender = (log4net.Appender.AdoNetAppender)hier.GetAppenders()
                        .Where(appender => appender.Name.Equals("AdoNetAppender", StringComparison.InvariantCultureIgnoreCase))
                        .FirstOrDefault();
                    if (adoAppender != null)
                    {
                        // there is an ado.net appender defined, so get fusion connection string from logging.
                        string newconnectstring = FUL.Encryption.Decrypt(ConfigurationManager.ConnectionStrings["at"].ConnectionString) + "Connect Timeout = 1;";
                        if (String.IsNullOrEmpty(adoAppender.ConnectionString) || newconnectstring != adoAppender.ConnectionString)
                        {
                            // set the connection string
                            adoAppender.ConnectionString = newconnectstring;
                            // refresh settings so we can connect to the data base.
                            adoAppender.ActivateOptions();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // failed to enable log4net adonet logging.  Log error directly to the event log.
                EventLog.WriteEntry("Logging", "Logger.SetLoggingConnectionString was not successful: " + ex.Message, EventLogEntryType.Error);
            }

        }
        /// <summary>
        /// set log filename in the logfileappender
        /// </summary>
        /// <param name="logfilename">name of logfile.  config will include directory name</param>
        private static void SetLogFileName( string logfilename)
        {
            try
            {
                // get log4net respository from config file.
                log4net.Repository.Hierarchy.Hierarchy hier = LogManager.GetRepository() as log4net.Repository.Hierarchy.Hierarchy;

                if (hier != null)
                {
                    //get all log4net appenders
                    log4net.Appender.IAppender[] fileAppenderArray = hier.GetAppenders();
 
                    // search to see if a log file appender was defined.
                    var fileAppender = (log4net.Appender.FileAppender)hier.GetAppenders()
                        .Where(appender => appender.Name.Equals("LogFileAppender", StringComparison.InvariantCultureIgnoreCase))
                        .FirstOrDefault();
                    if (fileAppender != null)
                    {
                        // a log file appender was defined, set the path to the log file.
                        string mylogfilename = Path.Combine(Path.GetDirectoryName(fileAppender.File), logfilename);
                        if (String.IsNullOrEmpty(fileAppender.File) || mylogfilename != fileAppender.File)
                        {
                            // set log file name for the file appender
                            fileAppender.File = mylogfilename;
                            //refresh settings of appender
                            fileAppender.ActivateOptions();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // failed to enable log4net file appender logging.  Log error directly to the event log.
                EventLog.WriteEntry("Logging", "Logger.SetLogFileName was not successful: " + ex.Message, EventLogEntryType.Error);
            }
        }
    }
}
