using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace FUL
{
    public static class FileWriter
    {
        public enum EventType { ProductUpdate, NavDataUpdate, Error, Info, ASD_DB_Error, ASD_Parse_Error, ASD_RouteParseFail, CustomFlightData, Reroute, AA_Parse_Error, FlightCorrelation };

        public static StreamWriter ErrorFile;
        public static StreamWriter ASDParseErrorFile;
        public static StreamWriter ASDdbErrorFile;

        public static StreamWriter LogFile;
        public static StreamWriter ProductUpdateFile;
        public static StreamWriter NavDataUpdateFile;
        public static StreamWriter ASDRouteParseFailFile;
        public static StreamWriter CustomFlightDataFile;
        public static StreamWriter RerouteDataFile;
        public static StreamWriter AAParseErrorFile;
        public static StreamWriter FlightCorrelationFile;

        private static DateTime TimeNow;
        private static string CurrentDirectory;
        private static string IngestorServerPath;
		private static bool SubdirectoriesExist;


        static FileWriter()
        {
            IngestorServerPath = FUL.Utils.Get_IngestorServerExecutablePath();
            if (IngestorServerPath.Length > 0)
                CurrentDirectory = IngestorServerPath; 
            else
                CurrentDirectory = Directory.GetCurrentDirectory();

			// Only want to write to Errors/Logs directories for DataIngestor and AA_DataControl (and Developer's sandbox)
			// Not WSI Data Services and Not the Fusion Client.
			SubdirectoriesExist = false;
			if ((Directory.Exists(CurrentDirectory + "\\Errors")) && (Directory.Exists(CurrentDirectory + "\\Logs")))
				SubdirectoriesExist = true;
			else if ((CurrentDirectory.Contains("DataIngest")) || (CurrentDirectory.Contains("FusionAALData")) || (CurrentDirectory.Contains("ServiceMisc")) || (IngestorServerPath.Length > 0))
			{ // Create Directories for programs that need them.
				if (!Directory.Exists(CurrentDirectory + "\\Errors"))
					Directory.CreateDirectory(CurrentDirectory + "\\Errors");
				if (!Directory.Exists(CurrentDirectory + "\\Logs"))
					Directory.CreateDirectory(CurrentDirectory + "\\Logs");
				SubdirectoriesExist = true;
			}

			if (SubdirectoriesExist)
			{
				FUL.FileWriter.CheckCreateNewDayFiles();

				if (ErrorFile == null)
					InitializeOutputFile(EventType.Error);

				if (LogFile == null)
					InitializeOutputFile(EventType.Info);

				if (!CurrentDirectory.Contains("ServiceMisc"))
				{
                    if ((FlightCorrelationFile == null) && (!CurrentDirectory.Contains("Fusion Data Service")) && (!CurrentDirectory.Contains("FusionDataService")))
						InitializeOutputFile(EventType.FlightCorrelation);

					if (CurrentDirectory.Contains("FusionAALData"))
					{
						if (RerouteDataFile == null)
							InitializeOutputFile(EventType.Reroute);

						if (AAParseErrorFile == null)
							InitializeOutputFile(EventType.AA_Parse_Error);
					}
					else if ((CurrentDirectory.Contains("DataIngest")) || (IngestorServerPath.Length > 0))
					{
						if (ASDParseErrorFile == null)
							InitializeOutputFile(EventType.ASD_Parse_Error);

						if (ASDdbErrorFile == null)
							InitializeOutputFile(EventType.ASD_DB_Error);

                        if ((ProductUpdateFile == null) && (!CurrentDirectory.Contains("Fusion Data Service")) && (!CurrentDirectory.Contains("FusionDataService")))
							InitializeOutputFile(EventType.ProductUpdate);

                        if ((NavDataUpdateFile == null) && (!CurrentDirectory.Contains("Fusion Data Service")) && (!CurrentDirectory.Contains("FusionDataService")))
							InitializeOutputFile(EventType.NavDataUpdate);

                        if ((ASDRouteParseFailFile == null) && (!CurrentDirectory.Contains("Fusion Data Service")) && (!CurrentDirectory.Contains("FusionDataService")))
							InitializeOutputFile(EventType.ASD_RouteParseFail);

                        if ((CustomFlightDataFile == null) && (!CurrentDirectory.Contains("Fusion Data Service")) && (!CurrentDirectory.Contains("FusionDataService")))
							InitializeOutputFile(EventType.CustomFlightData);
					}
				}
			} // end-if Subdirectories Exist
        }

        // -----------------------------------------------------------------
        public static void WriteLine(bool DisplayTime, EventType Event, string Remarks)
        {
            DateTime DefaultTime = new DateTime(1800, 1, 1);
            string Product = string.Empty;
            WriteLine(DisplayTime, Event, Product, DefaultTime, Remarks);
        }

        // -----------------------------------------------------------------
        public static void WriteLine(bool DisplayTime, EventType Event, string Product, string Remarks)
        {
            DateTime DefaultTime = new DateTime(1800, 1, 1);
            WriteLine(DisplayTime, Event, Product, DefaultTime, Remarks);
        }

        // -----------------------------------------------------------------
		public static void Write(bool DisplayTime, EventType Event, string Product, string Remarks)
		{
			if (SubdirectoriesExist)
			{
				switch (Event)
				{
					case EventType.Info:
						LogFile.Write(Remarks);
						break;
					case EventType.Error:
						ErrorFile.Write(Remarks);
						break;
					case EventType.ProductUpdate:
						ProductUpdateFile.Write(Remarks);
						break;
					case EventType.NavDataUpdate:
						NavDataUpdateFile.Write(Remarks);
						break;
					case EventType.ASD_Parse_Error:
						ASDParseErrorFile.Write(Remarks);
						break;
					case EventType.ASD_DB_Error:
						ASDdbErrorFile.Write(Remarks);
						break;
					case EventType.Reroute:
						RerouteDataFile.Write(Remarks);
						break;
					case EventType.AA_Parse_Error:
						AAParseErrorFile.Write(Remarks);
						break;
					case EventType.FlightCorrelation:
						FlightCorrelationFile.Write(Remarks);
						break;
					default:
						break;
				} // end-switch
			} // end-if Subdirectories Exist

		} // End Write

        // -----------------------------------------------------------------
		public static void WriteLine(bool DisplayTime, EventType Event, string Product, DateTime ValidTime, string Remarks)
		{
            try
            {
                if (SubdirectoriesExist)
                {
                    FUL.FileWriter.CheckCreateNewDayFiles(); // Check for the need to create new Day files.

                    string Time = string.Empty;
                    if (DisplayTime)
                        Time = DateTime.UtcNow.ToLongTimeString();

                    if (Product.Length > 0)
                    {
                        if (Product.Length < 16)
                            Product = Product + "\t";
                        if (Product.Length <= 8)
                            Product = Product + "\t";
                    }

                    switch (Event)
                    {
                        case EventType.Error:
                            if (Product.Length == 0)
                                ErrorFile.WriteLine(Time + " | " + Remarks);
                            else
                                ErrorFile.WriteLine(Time + " | " + Product + "\t| " + Remarks);
                            break;
                        case EventType.ASD_Parse_Error:
                            ASDParseErrorFile.WriteLine(Time + " | " + Remarks);
                            break;
                        case EventType.ASD_DB_Error:
                            ASDdbErrorFile.WriteLine(Time + " | " + Remarks);
                            break;
                        case EventType.Info:
                            if (Product.Length == 0)
                                LogFile.WriteLine(Time + " | " + Remarks);
                            else
                                LogFile.WriteLine(Time + " | " + Product + "\t| " + Remarks);
                            break;
                        case EventType.ProductUpdate:
                            ProductUpdateFile.WriteLine(Product + "\t|" + Remarks + " " + ValidTime);
                            break;
                        case EventType.NavDataUpdate:
                            NavDataUpdateFile.WriteLine(Remarks);
                            break;
                        case EventType.ASD_RouteParseFail:
                            ASDRouteParseFailFile.WriteLine(Time + " | " + Remarks);
                            break;
                        case EventType.CustomFlightData:
                            CustomFlightDataFile.WriteLine(Time + " | " + Remarks);
                            break;
                        case EventType.Reroute:
                            RerouteDataFile.WriteLine(Time + " | " + Remarks);
                            break;
                        case EventType.AA_Parse_Error:
                            AAParseErrorFile.WriteLine(Time + " | " + Remarks);
                            break;
                        case EventType.FlightCorrelation:
                            FlightCorrelationFile.WriteLine(Time + " | " + Remarks);
                            break;
                        default:
                            if (ValidTime.Year < 2000)
                                LogFile.WriteLine(Time + " | " + Product + "\t|" + Event + "\t|" + "\t\t\t| " + Remarks);
                            else
                                LogFile.WriteLine(Time + " | " + Product + "\t|" + Event + "\t| " + ValidTime + "\t| " + Remarks);
                            break;
                    } // end-switch
                } // end-if Subdirectories Exist
            }
            catch { }

		} // End WriteLine

        // -----------------------------------------------------------------
        public static bool CheckCreateNewDayFiles()
        {
            try
            {
                if (SubdirectoriesExist)
                {
                    // Create new file at the start of every day.
                    TimeNow = DateTime.UtcNow;
                    bool RemoveOldLogFiles = false;
                    bool RemoveOldErrorFiles = false;
                    FileInfo LogFileName;
                    FileInfo ErrorfileName;

                    // Errors\Error File
                    ErrorfileName = new FileInfo(CurrentDirectory + "\\Errors\\Error" + "_" + TimeNow.Year + "_" + TimeNow.Month + "_" + TimeNow.Day + ".txt");
                    if (!ErrorfileName.Exists)
                    {
                        if (ErrorFile != null)
                        {
                            ErrorFile.Close();
                            ErrorFile.Dispose();
                        }
                        InitializeOutputFile(EventType.Error);

                        RemoveOldErrorFiles = true;
                    }

                    // Logs\Info Day File
                    LogFileName = new FileInfo(CurrentDirectory + "\\Logs\\Info" + "_" + TimeNow.Year + "_" + TimeNow.Month + "_" + TimeNow.Day + ".txt");
                    if (!LogFileName.Exists)
                    {
                        if (LogFile != null)
                        {
                            LogFile.Close();
                            LogFile.Dispose();
                        }
                        InitializeOutputFile(EventType.Info);

                        RemoveOldLogFiles = true;
                    }

                    if (!CurrentDirectory.Contains("ServiceMisc"))
					{
                        if ((!CurrentDirectory.Contains("Fusion Data Service"))  && (!CurrentDirectory.Contains("FusionDataService")))
                        {
                            // Logs\FlightCorrelation Day File
                            LogFileName = new FileInfo(CurrentDirectory + "\\Logs\\FlightCorrelation" + "_" + TimeNow.Year + "_" + TimeNow.Month + "_" + TimeNow.Day + ".txt");
                            if (!LogFileName.Exists)
                            {
                                if (FlightCorrelationFile != null)
                                {
                                    FlightCorrelationFile.Close();
                                    FlightCorrelationFile.Dispose();
                                }
                                InitializeOutputFile(EventType.FlightCorrelation);

                                RemoveOldLogFiles = true;
                            }
                        }

						if (CurrentDirectory.Contains("FusionAALData"))
						{
							// Logs\RerouteData Day File
							LogFileName = new FileInfo("Logs//Reroute" + "_" + TimeNow.Year + "_" + TimeNow.Month + "_" + TimeNow.Day + ".txt");
							if (!LogFileName.Exists)
							{
								if (RerouteDataFile != null)
								{
									RerouteDataFile.Close();
									RerouteDataFile.Dispose();
								}
								InitializeOutputFile(EventType.Reroute);

								RemoveOldLogFiles = true;
							}

							// Logs\AAParseError Day File
							LogFileName = new FileInfo("Errors//AA_Parse_Error" + "_" + TimeNow.Year + "_" + TimeNow.Month + "_" + TimeNow.Day + ".txt");
							if (!LogFileName.Exists)
							{
								if (AAParseErrorFile != null)
								{
									AAParseErrorFile.Close();
									AAParseErrorFile.Dispose();
								}
								InitializeOutputFile(EventType.AA_Parse_Error);

								RemoveOldLogFiles = true;
							}

						}
						else if ((CurrentDirectory.Contains("DataIngest")) || (CurrentDirectory.Contains("Projects")) || (IngestorServerPath.Length > 0))
						{// these files only needed for Data Ingestor.
							// Errors\ASD_Parse_Error File
							ErrorfileName = new FileInfo(CurrentDirectory + "\\Errors\\ASD_Parse_Error" + "_" + TimeNow.Year + "_" + TimeNow.Month + "_" + TimeNow.Day + ".txt");
							if (!ErrorfileName.Exists)
							{
								if (ASDParseErrorFile != null)
								{
									ASDParseErrorFile.Close();
									ASDParseErrorFile.Dispose();
								}
								InitializeOutputFile(EventType.ASD_Parse_Error);

								RemoveOldErrorFiles = true;
							}

							// Errors\ASD_DB_Error File
							ErrorfileName = new FileInfo(CurrentDirectory + "\\Errors\\ASD_DB_Error" + "_" + TimeNow.Year + "_" + TimeNow.Month + "_" + TimeNow.Day + ".txt");
							if (!ErrorfileName.Exists)
							{
								if (ASDdbErrorFile != null)
								{
									ASDdbErrorFile.Close();
									ASDdbErrorFile.Dispose();
								}
								InitializeOutputFile(EventType.ASD_DB_Error);

								RemoveOldErrorFiles = true;
							}

                            if ((!CurrentDirectory.Contains("Fusion Data Service"))  && (!CurrentDirectory.Contains("FusionDataService")))
                            {
                                // Logs\ProductUpdate Day File
                                LogFileName = new FileInfo(CurrentDirectory + "\\Logs\\ProductUpdate" + "_" + TimeNow.Year + "_" + TimeNow.Month + "_" + TimeNow.Day + ".txt");
                                if (!LogFileName.Exists)
                                {
                                    if (ProductUpdateFile != null)
                                    {
                                        ProductUpdateFile.Close();
                                        ProductUpdateFile.Dispose();
                                    }
                                    InitializeOutputFile(EventType.ProductUpdate);

                                    RemoveOldLogFiles = true;
                                }


                                // Logs\ASD_RouteParseFail Day File
                                LogFileName = new FileInfo(CurrentDirectory + "\\Logs\\ASD_RouteParseFail" + "_" + TimeNow.Year + "_" + TimeNow.Month + "_" + TimeNow.Day + ".txt");
                                if (!LogFileName.Exists)
                                {
                                    if (ASDRouteParseFailFile != null)
                                    {
                                        ASDRouteParseFailFile.Close();
                                        ASDRouteParseFailFile.Dispose();
                                    }
                                    InitializeOutputFile(EventType.ASD_RouteParseFail);

                                    RemoveOldLogFiles = true;
                                }

                                // Logs\CustomFlightData Day File
                                LogFileName = new FileInfo(CurrentDirectory + "\\Logs\\CustomFlightData" + "_" + TimeNow.Year + "_" + TimeNow.Month + "_" + TimeNow.Day + ".txt");
                                if (!LogFileName.Exists)
                                {
                                    if (CustomFlightDataFile != null)
                                    {
                                        CustomFlightDataFile.Close();
                                        CustomFlightDataFile.Dispose();
                                    }
                                    InitializeOutputFile(EventType.CustomFlightData);

                                    RemoveOldLogFiles = true;
                                }

                                // Logs\NavDataUpdate Day File
                                LogFileName = new FileInfo(CurrentDirectory + "\\Logs\\NavDataUpdate" + "_" + TimeNow.Year + "_" + TimeNow.Month + "_" + TimeNow.Day + ".txt");
                                if (!LogFileName.Exists)
                                {
                                    if (NavDataUpdateFile != null)
                                    {
                                        NavDataUpdateFile.Close();
                                        NavDataUpdateFile.Dispose();
                                    }
                                    InitializeOutputFile(EventType.NavDataUpdate);

                                    RemoveOldLogFiles = true;
                                }
                            }
						} // end-if Fusion Data Ingestor process/path.
					}


                    // Remove old files.
                    if (RemoveOldLogFiles == true)
                    {
                        CheckRemoveOldFiles(CurrentDirectory + "\\Logs", "*.txt");
                        CheckRemoveOldFiles(CurrentDirectory + "\\Logs", "*.xml");
                    }
                    if (RemoveOldErrorFiles == true)
                        CheckRemoveOldFiles(CurrentDirectory + "\\Errors", "*.txt");

                } // end-if Subdirectories Exist
            }
            catch (Exception)
            {
                return false;
            }

            return true;

        } // End CreateNewDayFiles

        // -----------------------------------------------------------------
        public static void CheckRemoveOldFiles(string DirName, string FileType)
        {
            TimeSpan cleanupAge = new TimeSpan(21, 0, 0, 0); // 21 days since last write.

            DirectoryInfo dirInfo = new DirectoryInfo(DirName);
            FileInfo[] files = dirInfo.GetFiles(FileType);   // return all log files in the above directory
            
            foreach (FileInfo file in files)
            {
                try
                {
                    if (DateTime.Now.Subtract(cleanupAge) > file.LastWriteTime)
                    {
                        file.Delete();
                    }
                }
                catch (IOException ex)
                {
                    FUL.FileWriter.WriteLine(true, FUL.FileWriter.EventType.Error, "FileWriter", " IO Exception caught removing old files. \r\n" + ex);
                }
            }

        } // End CheckRemoveOldFiles


        // -----------------------------------------------------------------
        public static void InitializeOutputFile(EventType et)
        {
            TimeNow = DateTime.UtcNow;
            string filename;
            FileStream fs;

            switch (et)
            {
                case EventType.Info:
                    filename = CurrentDirectory + "\\Logs\\" + et + "_" + TimeNow.Year + "_" + TimeNow.Month + "_" + TimeNow.Day + ".txt";
                    fs = new FileStream(filename, FileMode.Append, FileAccess.Write, FileShare.Read);
                    LogFile = new StreamWriter(fs);
                    LogFile.AutoFlush = true;
                    //LogFile.WriteLine("\r\n    INFORMATION LOG FILE      Creation date: " + TimeNow + "\r\n");
                    break;
                case EventType.ProductUpdate:
                    filename = CurrentDirectory + "\\Logs\\" + et + "_" + TimeNow.Year + "_" + TimeNow.Month + "_" + TimeNow.Day + ".txt";
                    fs = new FileStream(filename, FileMode.Append, FileAccess.Write, FileShare.Read);
                    ProductUpdateFile = new StreamWriter(fs);
                    ProductUpdateFile.AutoFlush = true;
                    ProductUpdateFile.WriteLine("\r\n    SWINDS PRODUCTS UPDATE LOG FILE      Creation date: " + TimeNow + "\r\n");
                    break;
                case EventType.NavDataUpdate:
                    filename = CurrentDirectory + "\\Logs\\" + et + "_" + TimeNow.Year + "_" + TimeNow.Month + "_" + TimeNow.Day + ".txt";
                    fs = new FileStream(filename, FileMode.Append, FileAccess.Write, FileShare.Read);
                    NavDataUpdateFile = new StreamWriter(fs);
                    NavDataUpdateFile.AutoFlush = true;
                    NavDataUpdateFile.WriteLine("\r\n    NAVIGATION DATA UPDATE LOG FILE      Creation date: " + TimeNow + "\r\n");
                    break;
                case EventType.ASD_RouteParseFail:
                    filename = CurrentDirectory + "\\Logs\\" + et + "_" + TimeNow.Year + "_" + TimeNow.Month + "_" + TimeNow.Day + ".txt";
                    fs = new FileStream(filename, FileMode.Append, FileAccess.Write, FileShare.Read);
                    ASDRouteParseFailFile = new StreamWriter(fs);
                    ASDRouteParseFailFile.AutoFlush = true;
                    ASDRouteParseFailFile.WriteLine("\r\n    ASD ROUTE PARSE FAIL LOG FILE      Creation date: " + TimeNow + "\r\n");
                    break;
                case EventType.CustomFlightData:
                    filename = CurrentDirectory + "\\Logs\\" + et + "_" + TimeNow.Year + "_" + TimeNow.Month + "_" + TimeNow.Day + ".txt";
                    fs = new FileStream(filename, FileMode.Append, FileAccess.Write, FileShare.Read);
                    CustomFlightDataFile = new StreamWriter(fs);
                    CustomFlightDataFile.AutoFlush = true;
                    //CustomFlightData.WriteLine("\r\n    CUSTOM FLIGHT DATA LOG FILE      Creation date: " + TimeNow + "\r\n");
                    break;
                case EventType.Error:
                    filename = CurrentDirectory + "\\Errors\\" + et + "_" + TimeNow.Year + "_" + TimeNow.Month + "_" + TimeNow.Day + ".txt";
                    fs = new FileStream(filename, FileMode.Append, FileAccess.Write, FileShare.Read);
                    ErrorFile = new StreamWriter(fs);
                    ErrorFile.AutoFlush = true;
                    //ErrorFile.WriteLine("\r\n    ERROR FILE      Creation date: " + TimeNow + "\r\n");
                    break;
                case EventType.ASD_Parse_Error:
                    filename = CurrentDirectory + "\\Errors\\" + et + "_" + TimeNow.Year + "_" + TimeNow.Month + "_" + TimeNow.Day + ".txt";
                    fs = new FileStream(filename, FileMode.Append, FileAccess.Write, FileShare.Read);
                    ASDParseErrorFile = new StreamWriter(fs);
                    ASDParseErrorFile.AutoFlush = true;
                    //ASDParseErrorFile.WriteLine("\r\n    ASD PARSE ERROR FILE      Creation date: " + TimeNow + "\r\n");
                    break;
                case EventType.ASD_DB_Error:
                    filename = CurrentDirectory + "\\Errors\\" + et + "_" + TimeNow.Year + "_" + TimeNow.Month + "_" + TimeNow.Day + ".txt";
                    fs = new FileStream(filename, FileMode.Append, FileAccess.Write, FileShare.Read);
                    ASDdbErrorFile = new StreamWriter(fs);
                    ASDdbErrorFile.AutoFlush = true;
                    //ASDdbErrorFile.WriteLine("\r\n    ASD DATABASE ERROR FILE      Creation date: " + TimeNow + "\r\n");
                    break;
                case EventType.Reroute:
                    filename = CurrentDirectory + "\\Logs\\" + et + "_" + TimeNow.Year + "_" + TimeNow.Month + "_" + TimeNow.Day + ".txt";
                    fs = new FileStream(filename, FileMode.Append, FileAccess.Write, FileShare.Read);
                    RerouteDataFile = new StreamWriter(fs);
                    RerouteDataFile.AutoFlush = true;
                    break;
                case EventType.AA_Parse_Error:
                    filename = CurrentDirectory + "\\Errors\\" + et + "_" + TimeNow.Year + "_" + TimeNow.Month + "_" + TimeNow.Day + ".txt";
                    fs = new FileStream(filename, FileMode.Append, FileAccess.Write, FileShare.Read);
                    AAParseErrorFile = new StreamWriter(fs);
                    AAParseErrorFile.AutoFlush = true;
                    break;
                case EventType.FlightCorrelation:
                    filename = CurrentDirectory + "\\Logs\\" + et + "_" + TimeNow.Year + "_" + TimeNow.Month + "_" + TimeNow.Day + ".txt";
                    fs = new FileStream(filename, FileMode.Append, FileAccess.Write, FileShare.Read);
                    FlightCorrelationFile = new StreamWriter(fs);
                    FlightCorrelationFile.AutoFlush = true;
                    break;
                default:
                    FUL.FileWriter.WriteLine(true, EventType.Error, "FileWriter", "Unknown FileWriter Event Type: " + et);
                    break;
            } // end-switch on Event Type

        } // End InitializeOutputFile

    } // End class
} // End namespace

// FileInfo DSfile = new FileInfo("DataServer.exe");
//FileInfo ERfile = new FileInfo("ErrorTest.txt");
//if ((!ERfile.Exists) && (DSfile.Exists))
