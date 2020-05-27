using JobProcessingInterface;
using MSXML3;
using System;
using System.Globalization;
using System.IO;
using Tyler.Odyssey.JobProcessing;
using Tyler.Odyssey.Utils;
using ILCookOfficerKeyConfigImportJob.Helpers;
using System.Linq;
using ILCookOfficerKeyConfigImportJob.Exceptions;
using System.Xml;
using System.Xml.Linq;
using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using ILCookOfficerKeyConfigImportJob.Entities;
using System.Data.SqlClient;
using System.Data;


namespace ILCookOfficerKeyConfigImportJob
{
    internal class DataProcessor : TaskProcessor
    {
        // Constructor
        public DataProcessor(string SiteID, string JobTaskXML) : base(SiteID, JobTaskXML)
        {
            Logger.WriteToLog("JobTaskXML:\r\n" + JobTaskXML, LogLevel.Basic);

            // New up the context object
            Context = new Context(Logger);

            Logger.WriteToLog("Completed instantiation of context object", LogLevel.Verbose);

            // Retrieve the parameters for the job (which flags to add/remove)
            Context.DeriveParametersFromJobTaskXML(SiteID, JobTaskXML);
            Context.ValidateParameters();

            Logger.WriteToLog("Finished deriving parameters", LogLevel.Verbose);

            // TODO:  Add the code tables that need to be updated to the following function (Context.AddCacheItems())
            Context.AddCacheItems();
            //Context.UpdateCache();

            Logger.WriteToLog("Completed cache update.", LogLevel.Verbose);
        }

        // Static constructor
        static DataProcessor()
        {
            Logger = new UtilsLogger(LogManager);
            Logger.WriteToLog("Logger Instantiated", LogLevel.Basic);
        }

        // Destructor
        ~DataProcessor()
        {
            Logger.WriteToLog("Disposing!", LogLevel.Basic);

            if (Context != null)
                Context.Dispose();
        }

        public static IUtilsLogManager LogManager = new UtilsLogManagerBase(Constants.LOG_REGISTRY_KEY);
        public static readonly UtilsLogger Logger;

        public IXMLDOMDocument TaskDocument { get; set; }

        internal Context Context { get; set; }

        public ITYLJobTaskUtility TaskUtility { get; set; }

        private object taskParms;
        public object TaskParms { get { return taskParms; } set { taskParms = value; } }

        public override void Run()
        {
            Logger.WriteToLog("Beginning Run Method", LogLevel.Basic);

            // TODO: Update File Transformation Logic

            if (Directory.GetFiles(Context.Parameters.InputFilePath, "*.csv").Count() > 0)
            {
                foreach (string fileName in Directory.GetFiles(Context.Parameters.InputFilePath, "*.csv"))
                {
                    try
                    {
                        // 1. Determine which file is being retrieved.
                        string tableName = "";
                        switch(Path.GetFileName(fileName))
                        {
                            case "OFKYCTCL_UPDATE.csv":
                                tableName = "CookAutoHearingCallConfiguration";
                                break;
                            case "OFKYOFCR_UPDATE.csv":
                                tableName = "CookAutoHearingOfficerConfiguration";
                                break;
                            case "OFKYOFKD_UPDATE.csv":
                                tableName = "CookAutoHearingOfficerKeyConfiguration";
                                break;
                            case "OFKYOFKN_UPDATE.csv":
                                tableName = "xCookAutoHearingOfficerKeyConfigurationDate";
                                break;
                            default:
                                string errorMesage = "The file: " + fileName + " is not recognized by the program.";
                                Logger.WriteToLog(errorMesage, LogLevel.Basic);
                                break;
                        }
                            
                        // 2. Extract data and update tables with respective data.
                        OfficerKeyTablesEntity data = ExtractDataFromFile(fileName, tableName);
                        UpdateTable(data, tableName);

                        // 3. Move File to Procesed Folder with a timestamp.
                        string targetFileName = fileName.Replace(Context.Parameters.InputFilePath, Context.Parameters.ProcessedFilePath);

                        if (File.Exists(targetFileName))
                            File.Delete(targetFileName);
                        File.Move(fileName, targetFileName);
                                               
                    }
                    catch (Exception e)
                    {
                        Context.Errors.Add(new BaseCustomException(e.Message));
                    }
                }
            }

            // TODO: Handle errors we've collected during the job run.
            if (Context.Errors.Count > 0)
            {
                // Add a message to the job indicating that something went wrong.
                AddInformationToJob();

                // Collect errors, write them to a file, and attach the file to the job.
                LogErrors();
            }

            ContinueWithProcessing("Job Completed Successfully");
        }

        private OfficerKeyTablesEntity ExtractDataFromFile(string fileName, string tableName)
        {
            OfficerKeyTablesEntity inputFileList = new OfficerKeyTablesEntity();
            inputFileList.CookAutoHearingCallConfiguration = new List<CookAutoHearingCallConfigurationEntity>();
            inputFileList.CookAutoHearingOfficerConfiguration = new List<CookAutoHearingOfficerConfigurationEntity>();
            inputFileList.CookAutoHearingOfficerKeyConfiguration = new List<CookAutoHearingOfficerKeyConfigurationEntity>();
            inputFileList.XCookAutoHearingOfficerKeyConfigurationDate = new List<xCookAutoHearingOfficerKeyConfigurationDateEntity>();


            using (StreamReader reader = new StreamReader(fileName))
            {
                Logger.WriteToLog("Processing File: " + fileName, LogLevel.Basic);

                int counter = 0;
                int dataRowPosition = 1;
                // File mapping and extraction
                
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    string[] lineValues = Regex.Split(line, ",(?=(?:[^']*'[^']*')*[^']*$)");

                    if (counter > 0)
                    {
                        Logger.WriteToLog("Validating record: " + counter, LogLevel.Verbose);
                        bool valid = ValidateRecord(lineValues);

                        if (valid)
                        {
                            if (tableName == "CookAutoHearingCallConfiguration")
                            {
                                CookAutoHearingCallConfigurationEntity inputFileRecord = new CookAutoHearingCallConfigurationEntity(
                                      lineValues[0].Trim()   // action
                                    , lineValues[1].Trim()   // hearingLocationCode
                                    , lineValues[2].Trim()   // callNumber
                                    , DateTime.Parse(lineValues[3].Trim(), CultureInfo.InvariantCulture).ToString("HH:mm")   // time
                                );

                                // Exclude adding header to data list
                                if (counter >= dataRowPosition)
                                    inputFileList.CookAutoHearingCallConfiguration.Add(inputFileRecord);
                            }
                            else if (tableName == "CookAutoHearingOfficerConfiguration")
                            {
                                CookAutoHearingOfficerConfigurationEntity inputFileRecord = new CookAutoHearingOfficerConfigurationEntity(
                                      lineValues[0].Trim() // action
                                    , lineValues[1].Trim() // nodeID
                                    , lineValues[2].Trim() // agencyCode
                                    , lineValues[3].Trim() // officerID
                                    , lineValues[4].Trim() // officerKey
                                    , lineValues[5].Trim() // hearingLocationCode
                                    , lineValues[6].Trim() // callNumber
                                    , lineValues[7].Trim() // isMajorOffenseAssignment
                                    , lineValues[8].Trim() // badgeNumber
                                );

                                // Exclude adding header to data list
                                if (counter >= dataRowPosition)
                                    inputFileList.CookAutoHearingOfficerConfiguration.Add(inputFileRecord);
                            }
                            else if (tableName == "CookAutoHearingOfficerKeyConfiguration")
                            {
                                CookAutoHearingOfficerKeyConfigurationEntity inputFileRecord = new CookAutoHearingOfficerKeyConfigurationEntity(
                                      lineValues[0].Trim() // action
                                    , lineValues[1].Trim() // nodeID
                                    , lineValues[2].Trim() // officerKey
                                );

                                // Exclude adding header to data list
                                if (counter >= dataRowPosition)
                                    inputFileList.CookAutoHearingOfficerKeyConfiguration.Add(inputFileRecord);
                            }
                            else if (tableName == "xCookAutoHearingOfficerKeyConfigurationDate")
                            {
                                xCookAutoHearingOfficerKeyConfigurationDateEntity inputFileRecord = new xCookAutoHearingOfficerKeyConfigurationDateEntity(
                                      lineValues[0].Trim() // action
                                    , lineValues[1].Trim() // nodeID
                                    , lineValues[2].Trim() // officerKey
                                    , lineValues[3].Trim() // date
                                );

                                // Exclude adding header to data list
                                if (counter >= dataRowPosition)
                                    inputFileList.XCookAutoHearingOfficerKeyConfigurationDate.Add(inputFileRecord);
                            }                            
                        }
                        counter++;
                    }
                    else
                    {
                        Logger.WriteToLog("Skipping Header Record.", LogLevel.Verbose);
                        counter++;
                    }
                }
            }

            return inputFileList;
        }


        private bool UpdateTable(OfficerKeyTablesEntity data, string tableName)
        {
            List<UCodeEntity> agencyCodeData = GetOdysseyCodeList("61");
            List<UCodeEntity> locationCodeData = GetOdysseyCodeList("163");

            UpdateConfigurationInTable(data, tableName, agencyCodeData, locationCodeData);            

            return true;
        }

        private bool ValidateRecord(string[] dataLine)
        {
            bool valid = true;

            // Validate NULLs
            if (string.IsNullOrEmpty(dataLine[0]) || (dataLine[0].ToUpper() != "ADD" && dataLine[0].ToUpper() != "DELETE"))
            {
                string error = "There is no valid action associated to this record.";
                Logger.WriteToLog(error, LogLevel.Verbose);
                WriteToCaseManifest(error);
                valid = false;
            }

            for (int i = 0; i < dataLine.Length; i++)
            {                
                // Validate NULLs
                if (string.IsNullOrEmpty(dataLine[i]))
                {
                    string error = "Value in record line is null.";
                    Logger.WriteToLog(error, LogLevel.Verbose);
                    WriteToCaseManifest(error);
                    valid = false;
                }
                // Validate SQL Regex
                string matchString = "[\\/'*?<>|]+";
                Match myMatch = Regex.Match(dataLine[i], matchString);
                if (myMatch.Success)
                {
                    string error = "There are invalid characters in record line.";
                    Logger.WriteToLog(error, LogLevel.Verbose);
                    WriteToCaseManifest(error);
                    valid = false;
                }

                // Time Fields are in the correct format
                if (dataLine[i].Contains(":"))
                {
                    // Example 1900-01-01 15:00:00.000
                    DateTime dateValue;
                    bool tryParse = DateTime.TryParse(dataLine[i], out dateValue);
                    if (!tryParse)
                    {
                        string error = "Date field is not in the correct format.";
                        Logger.WriteToLog(error, LogLevel.Verbose);
                        WriteToCaseManifest(error);
                    }                    
                }
            }

            return valid;

        }

        private List<UCodeEntity> GetOdysseyCodeList(string uCode)
        {
            List<UCodeEntity> agencyCodeEntityList = new List<UCodeEntity>();
            string queryString = "SELECT CodeID, Code FROM justice.dbo.uCode with(nolock) WHERE CacheTableID = " + uCode;
            DataSet agencyCodeDataSet = GetDatasetFromTable(queryString);

            foreach(DataRow row in agencyCodeDataSet.Tables[0].Rows)
            {
                UCodeEntity agencyCodeEntity = new UCodeEntity(row[0].ToString(), row[1].ToString());
                agencyCodeEntityList.Add(agencyCodeEntity);
            }

            return agencyCodeEntityList;
        }

        private string GetCodeIDFromCode(string inputCode, List<UCodeEntity> codeList)
        {
            foreach(UCodeEntity record in codeList)
            {
                if (record.Code == inputCode)
                    return record.CodeID;
            }
            return "0";
        }


        // SQL

        private bool UpdateConfigurationInTable(OfficerKeyTablesEntity data, string tableName, List<UCodeEntity> agencyCodeData, List<UCodeEntity> locationCodeData)
        {
            
            string queryStringDelete = "DELETE FROM [justice].[StateIllinois].[" + tableName + "]" + " " + "WHERE" + " ";
            int deleteCounter = 0;

            string queryStringAdd = "INSERT INTO [justice].[StateIllinois].[" + tableName + "]" + " ";
            int addCounter = 0;

            switch (tableName)
            {
                case "CookAutoHearingCallConfiguration":
                    queryStringAdd = queryStringAdd + "(HearingLocationID, CallNumber, Time) VALUES ";
                    // Build DELETE Query string based off of table                     
                    foreach (CookAutoHearingCallConfigurationEntity configurationEntity in data.CookAutoHearingCallConfiguration)
                    {
                        if (configurationEntity.Action == "DELETE")
                        {
                            if (deleteCounter > 0)
                                queryStringDelete = queryStringDelete + "OR" + " ";

                            queryStringDelete = queryStringDelete + "(HearingLocationID = " + GetCodeIDFromCode(configurationEntity.HearingLocationCode, locationCodeData) + " " +
                                "AND CallNumber = " + configurationEntity.CallNumber + " " +
                                "AND Time = '1900-01-01 " + configurationEntity.Time + ":00.000')" + " ";
                            deleteCounter++;
                        }
                        else if (configurationEntity.Action == "ADD")
                        {
                            if (addCounter > 0)
                                queryStringAdd = queryStringAdd + "," + " ";

                            queryStringAdd = queryStringAdd +
                                "(" +
                                GetCodeIDFromCode(configurationEntity.HearingLocationCode, locationCodeData) + "," +
                                configurationEntity.CallNumber + "," +
                                "'1900-01-01 " + configurationEntity.Time + ":00.000'" +
                                ")" + " ";
                            addCounter++;
                        }
                        else
                        {
                            Logger.WriteToLog("Could not determine record action.", LogLevel.Verbose);
                        }


                    }
                    break;
                case "CookAutoHearingOfficerConfiguration":
                    queryStringAdd = queryStringAdd + "(NodeID, AgencyID, OfficerID, OfficerKey, HearingLocationID, CallNumber, IsMajorOffenseAssignment) VALUES ";
                    foreach (CookAutoHearingOfficerConfigurationEntity configurationEntity in data.CookAutoHearingOfficerConfiguration)
                    {
                        if (configurationEntity.Action == "DELETE")
                        {
                            if (deleteCounter > 0)
                                queryStringDelete = queryStringDelete + "OR" + " ";

                            queryStringDelete = queryStringDelete + "(NodeID = " + configurationEntity.NodeID + " " +
                                "AND AgencyID = " + GetCodeIDFromCode(configurationEntity.AgencyCode, agencyCodeData) + " " +
                                "AND OfficerID = " + configurationEntity.OfficerID + " " +
                                "AND OfficerKey = '" + configurationEntity.OfficerKey + "' " +
                                "AND HearingLocationID = " + GetCodeIDFromCode(configurationEntity.HearingLocationCode, locationCodeData) + " " +
                                "AND CallNumber = " + configurationEntity.CallNumber + " " +
                                "AND IsMajorOffenseAssignment = '" + configurationEntity.IsMajorOffenseAssignment + "') ";
                            deleteCounter++;
                        }
                        else if (configurationEntity.Action == "ADD")
                        {
                            if (addCounter > 0)
                                queryStringAdd = queryStringAdd + "," + " ";

                            queryStringAdd = queryStringAdd +
                                "(" +
                                configurationEntity.NodeID + "," +
                                GetCodeIDFromCode(configurationEntity.AgencyCode, agencyCodeData) + "," +
                                configurationEntity.OfficerID + "," +
                                "'" + configurationEntity.OfficerKey + "'," +
                                GetCodeIDFromCode(configurationEntity.HearingLocationCode, locationCodeData) + "," +
                                configurationEntity.CallNumber + "," +
                                "'" + configurationEntity.IsMajorOffenseAssignment + "'" +
                                ")" + " ";
                            addCounter++;
                        }
                        else
                        {
                            Logger.WriteToLog("Could not determine record action.", LogLevel.Verbose);
                        }
                    }
                    break;
                case "CookAutoHearingOfficerKeyConfiguration":
                    queryStringAdd = queryStringAdd + "(NodeID, OfficerKey) VALUES ";
                    foreach (CookAutoHearingOfficerKeyConfigurationEntity configurationEntity in data.CookAutoHearingOfficerKeyConfiguration)
                    {
                        if (configurationEntity.Action == "DELETE")
                        {
                            if (deleteCounter > 0)
                                queryStringDelete = queryStringDelete + "OR" + " ";

                            queryStringDelete = queryStringDelete + "(NodeID = " + configurationEntity.NodeID + " " +
                                "AND OfficerKey = '" + configurationEntity.OfficerKey + "' ";
                            deleteCounter++;
                        }
                        else if (configurationEntity.Action == "ADD")
                        {
                            if (addCounter > 0)
                                queryStringAdd = queryStringAdd + "," + " ";

                            queryStringAdd = queryStringAdd +
                                "(" +
                                configurationEntity.NodeID + "," +
                                configurationEntity.OfficerKey +
                                ")" + " ";
                            addCounter++;
                        }
                        else
                        {
                            Logger.WriteToLog("Could not determine record action.", LogLevel.Verbose);
                        }
                    }
                    break;
                case "xCookAutoHearingOfficerKeyConfigurationDate":
                    queryStringAdd = queryStringAdd + "(NodeID, OfficerKey, Date) VALUES ";
                    foreach (xCookAutoHearingOfficerKeyConfigurationDateEntity configurationEntity in data.XCookAutoHearingOfficerKeyConfigurationDate)
                    {
                        if (configurationEntity.Action == "DELETE")
                        {
                            // Date Cleanup
                            DateTime cleanDateTime = DateTime.ParseExact(configurationEntity.Date, "yyyy-mm-dd", System.Globalization.CultureInfo.CurrentCulture);
                            if (deleteCounter > 0)
                                queryStringDelete = queryStringDelete + "OR" + " ";

                            queryStringDelete = queryStringDelete + "(NodeID = " + configurationEntity.NodeID + " " +
                                "AND OfficerKey = '" + configurationEntity.OfficerKey + "' " +
                                "AND Date = '" + configurationEntity.Date + "' " + cleanDateTime.ToString() + "' ";
                            deleteCounter++;
                        }
                        else if (configurationEntity.Action == "ADD")
                        {
                            if (addCounter > 0)
                                queryStringAdd = queryStringAdd + "," + " ";

                            queryStringAdd = queryStringAdd +
                                "(" +
                                configurationEntity.NodeID + "," +
                                configurationEntity.OfficerKey + "," +
                                configurationEntity.Date +
                                ")" + " ";
                            addCounter++;
                        }
                        else
                        {
                            Logger.WriteToLog("Could not determine record action.", LogLevel.Verbose);
                        }
                    }
                    break;

                default:
                    break;
            }

            bool validCheckDelete = false;
            bool validCheckAdd = false;
                        
            // Try to run it if there are values in the data set.
            if (deleteCounter > 0)
                validCheckDelete = UpdateTable(queryStringDelete);

            if (addCounter > 0)
                validCheckAdd = UpdateTable(queryStringAdd);

            return (validCheckDelete && validCheckAdd);

        }        

        public bool UpdateTable(string query)
        {
            
            string siteID = Context.SiteID;

            try
            {
                Logger.WriteToLog("SQL: " + query, LogLevel.Verbose);
                DataSet ds = ExecuteSQL(siteID, query);
                return true;

            }
            catch (Exception e)
            {
                return false;
            }
        }

        public DataSet GetDatasetFromTable(string query)
        {
            DataSet ds = null;
            string siteID = Context.SiteID;

            try
            {
                Logger.WriteToLog("SQL: " + query, LogLevel.Verbose);
                ds = ExecuteSQL(siteID, query);
                return ds;

            }
            catch (Exception e)
            {
                return null;
            }
        }
        

        private DataSet ExecuteSQL(string siteID, string query)
        {
            
            CDBBroker broker = new CDBBroker(siteID);
            var brokerConnection = broker.GetConnection("Justice");
            DataSet ret = null;
            SqlCommand cmd = new SqlCommand(string.Format(query), brokerConnection as SqlConnection);

            try
            {
                cmd.Connection.Open();
                ret = CDBBroker.LoadDataSet(siteID, cmd, true);
            }
            finally
            {
                if (cmd != null)
                {
                    cmd.Connection.Close();
                }
            }

            return ret;

        }


        private void AddInformationToJob()
        {
            int jobTaskID = 0;
            int jobProcessID = 0;

            if (Int32.TryParse(Context.taskID, out jobTaskID) && Int32.TryParse(Context.jobProcessID, out jobProcessID))
            {
                object Parms = new object[,] { { "SEVERITY" }, { "2" } };

                ITYLJobTaskUtility taskUtility = (JobProcessingInterface.ITYLJobTaskUtility)Activator.CreateInstance(Type.GetTypeFromProgID("Tyler.Odyssey.JobProcessing.TYLJobTaskUtility.cTask"));

                taskUtility.AddTextMessage(Context.SiteID, jobProcessID, jobTaskID, "The job completed successfully, but some cases were not processed. Please see the attached error file for a list of those cases and the errors associated with each. A list manager list containing the cases in error was also created.", ref Parms);
            }
        }


        private void LogErrors()
        {
            using (StreamWriter writer = GetTempFile())
            {
                Logger.WriteToLog("Beginning to write to temp file.", LogLevel.Intermediate);

                // Write the file header
                writer.WriteLine("CaseNumber,CaseID,CaseFlag,Error");

                // For each error, write some information.
                Context.Errors.ForEach((BaseCustomException f) => WriteErrorToLog(f, writer));

                Logger.WriteToLog("Finished writing to temp file.", LogLevel.Intermediate);

                AttachTempFileToJobOutput(writer, @"Add Remove Case Flags Action - Errors");
            }
        }


        private void WriteErrorToLog(BaseCustomException exception, StreamWriter writer)
        {
            writer.WriteLine(string.Format("\"{0}\"", exception.CustomMessage));
        }


        private StreamWriter GetTempFile()
        {
            if (TaskUtility == null)
                return null;

            string filePath = TaskUtility.GenerateFile(Context.SiteID, ref taskParms);
            StreamWriter fileWriter = new StreamWriter(filePath, true);

            Logger.WriteToLog("Created temp file at location: " + filePath, LogLevel.Basic);

            return fileWriter;
        }


        private void AttachTempFileToJobOutput(StreamWriter writer, string errorFileName)
        {
            Logger.WriteToLog("Begining AttachTempFileToJobOutput()", LogLevel.Intermediate);
            Logger.WriteToLog(writer == null ? "File is NULL" : "File is NOT NULL", LogLevel.Intermediate);

            if (writer != null && TaskUtility != null)
            {
                FileStream fileStream = writer.BaseStream as FileStream;
                string filePath = fileStream.Name;
                Logger.WriteToLog("File Path: " + filePath, LogLevel.Intermediate);

                writer.Close();

                if (filePath.Length > 0 && errorFileName.Length > 0)
                    AttachFile(filePath, errorFileName);

                Logger.WriteToLog("Completed AttachTempFileToJobOutput()", LogLevel.Intermediate);
            }
        }


        private void AttachFile(string filepath, string filename)
        {
            DataProcessor.Logger.WriteToLog("Begin AttachFile()", Tyler.Odyssey.Utils.LogLevel.Intermediate);
            int nodeID = 0;
            int taskIDInt = 0;
            int jobProcessIDInt = 0;

            if (TaskUtility != null)
            {
                if (Int32.TryParse(Context.taskID, out taskIDInt) && Int32.TryParse(Context.jobProcessID, out jobProcessIDInt))
                {
                    int documentID = TaskUtility.AddOutputDocument(this.siteKey, taskIDInt, jobProcessIDInt, -1, filepath, Context.UserID, nodeID, ref taskParms);

                    if (documentID > 0)
                    {
                        TaskUtility.AddOutputParams(this.siteKey, taskIDInt, "TEXT", documentID, filename, TaskDocument, ref taskParms);

                        TaskUtility.DeleteTempFile(filepath);

                        this.OutputJobTaskXML = TaskDocument.documentElement.xml;
                    }
                }
            }

            DataProcessor.Logger.WriteToLog("End Attach()", Tyler.Odyssey.Utils.LogLevel.Intermediate);
        }

        private void AttachFileDontDeleteTemp(string filepath, string filename)
        {
            DataProcessor.Logger.WriteToLog("Begin AttachFile()", Tyler.Odyssey.Utils.LogLevel.Intermediate);
            int nodeID = 0;
            int taskIDInt = 0;
            int jobProcessIDInt = 0;

            if (TaskUtility != null)
            {
                if (Int32.TryParse(Context.taskID, out taskIDInt) && Int32.TryParse(Context.jobProcessID, out jobProcessIDInt))
                {
                    int documentID = TaskUtility.AddOutputDocument(this.siteKey, taskIDInt, jobProcessIDInt, -1, filepath, Context.UserID, nodeID, ref taskParms);

                    if (documentID > 0)
                    {
                        TaskUtility.AddOutputParams(this.siteKey, taskIDInt, "TEXT", documentID, filename, TaskDocument, ref taskParms);
                        //TaskUtility.DeleteTempFile(filepath);
                        // File Created
                        DataProcessor.Logger.WriteToLog("File attached to job.", Tyler.Odyssey.Utils.LogLevel.Intermediate);

                        this.OutputJobTaskXML = TaskDocument.documentElement.xml;
                    }
                }
            }

            DataProcessor.Logger.WriteToLog("End Attach()", Tyler.Odyssey.Utils.LogLevel.Intermediate);
        }

        private void WriteToCaseManifest(string text)
        {            
            string path = Context.Parameters.ReportFilePath + @"\" + "CaseMainfest_" + DateTime.Now.ToString("yyyyMMdd") + ".csv";

            if (!File.Exists(path))
            {
                using (var tw = new StreamWriter(path, true))
                {
                    var newLine = string.Format("{0},{1}", DateTime.Now.ToString("HH:mm:ss"), text);
                    tw.WriteLine(newLine);
                }
            }
            else
            {
                var newLine = string.Format("{0},{1}", DateTime.Now.ToString("HH:mm:ss"), text);
                File.AppendAllText(path, newLine + Environment.NewLine);
            }            
        }
    }
}