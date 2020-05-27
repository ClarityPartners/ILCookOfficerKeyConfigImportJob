using System.Xml;
using Tyler.Odyssey.Utils;

namespace ILCookOfficerKeyConfigImportJob.Helpers
{
    public class Parameters
    {
        public string InputFilePath { get; private set; }
        public string ProcessedFilePath { get; private set; }
        public string ReportFilePath { get; private set; }

        public Parameters(XmlElement taskNode, UtilsLogger logger)
        {
            logger.WriteToLog("Beginning Parameters() constructor", LogLevel.Verbose);
            logger.WriteToLog("taskNode: " + taskNode.OuterXml, LogLevel.Verbose);

            InputFilePath = taskNode.GetAttribute("InputFilePath");
            ProcessedFilePath = taskNode.GetAttribute("ProcessedFilePath");
            ReportFilePath = taskNode.GetAttribute("OutputFilePath");

            logger.WriteToLog("Instantiated Parameters", LogLevel.Verbose);
        }
    }
}