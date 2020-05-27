using System.Collections.Generic;
using Tyler.Odyssey.Utils;
using System;
using System.Xml;
using ILCookOfficerKeyConfigImportJob.Exceptions;
using System.IO;

namespace ILCookOfficerKeyConfigImportJob.Helpers
{
    public class Context : ServerContext
    {
        public XmlElement taskNode;
        public string jobProcessID;
        public string taskID;

        // Constructors
        public Context(UtilsLogger logger) : base()
        {
            Errors = new List<BaseCustomException>();
            Logger = logger;
        }

        public Context(ServerContext serverContext, UtilsLogger logger) : base(serverContext)
        {
            Logger = logger;
        }

        public Parameters Parameters { get; set; }
        public List<BaseCustomException> Errors;
        public UtilsLogger Logger { get; set; }

        internal void DeriveParametersFromJobTaskXML(string siteID, string jobTaskXML)
        {
            Logger.WriteToLog("Beginning DeriveParametersFromJobTaskXML()", LogLevel.Verbose);

            XmlDocument jobTaskDoc = new XmlDocument();
            jobTaskDoc.LoadXml(jobTaskXML);

            Logger.WriteToLog("Loaded DOM document", LogLevel.Verbose);

            XmlElement jobNode = (XmlElement)jobTaskDoc.SelectSingleNode("/Job");
            taskNode = (XmlElement)jobTaskDoc.SelectSingleNode("/Job/Params/Input/Task/Params");

            Logger.WriteToLog("Created Job Node", LogLevel.Verbose);

            if (taskNode == null)
                throw new Exception("Task node does not exist in provided parameters");

            jobProcessID = jobNode.GetAttribute("JobProcessID");
            taskID = taskNode.GetAttribute("ID");
            int userID = 0;
            bool success = Int32.TryParse(jobNode.GetAttribute("UserID"), out userID);

            Logger.WriteToLog("Finished setting job process and task ids", LogLevel.Verbose);

            if (success)
                DeriveParameters(siteID, userID, taskNode);

            Logger.WriteToLog("Completed DeriveParameters()", LogLevel.Verbose);
        }

        private void DeriveParameters(string siteID, int userId, XmlElement taskNode)
        {
            // We need to set the user id and site id on the base class,
            // because it will allow the base context object to do
            // code queries and user rights lookups.
            Logger.WriteToLog("About to set userid and site id", LogLevel.Verbose);

            base.UserID = userId;
            base.SiteID = siteID;

            Logger.WriteToLog("Completed setting user id and site id", LogLevel.Verbose);

            Parameters = new Parameters(taskNode, Logger);

            Logger.WriteToLog("Completed instantiation of Parameters object", LogLevel.Verbose);
        }

        public void ValidateParameters()
        {
            ValidateRequiredJobParameter(Parameters.InputFilePath, "Input File Path");
            ValidateRequiredJobParameter(Parameters.ProcessedFilePath, "Processed File Path");
            ValidateRequiredJobParameter(Parameters.ReportFilePath, "Report File Path");
           
            ValidateFileDirectoryExists(Parameters.InputFilePath);
            ValidateFileDirectoryExists(Parameters.ProcessedFilePath);
            ValidateFileDirectoryExists(Parameters.ReportFilePath);
        }

        private void ValidateRequiredJobParameter(string parameterName, string parameterValue)
        {
            if (string.IsNullOrEmpty(parameterValue))
                throw new BaseCustomException(string.Format("The required Job Parameter is missing: {1}", parameterName));
        }

        public void ValidateFileDirectoryExists(string filePath)
        {
            bool error = !Directory.Exists(filePath);

            if (!error)
            {
                string fullPath = filePath + @"\test.test";

                try
                {
                    using (FileStream fs = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write))
                    {
                        fs.WriteByte(0xff);
                    }

                    if (File.Exists(fullPath))
                    {
                        File.Delete(fullPath);
                    }
                }
                catch
                {
                    error = true;
                }
            }

            if (error)
                throw new Exception("The File Path specified is invalid or cannot be accessed.");
        }


        internal void AddCacheItems()
        {
            this.AddCacheItem("Justice", "uCaseFlag");
        }
    }
}