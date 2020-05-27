using Tyler.Odyssey.JobProcessing;
using ILCookOfficerKeyConfigImportJob;
using ILCookOfficerKeyConfigImportJob.Helpers;
using System.Xml;
using JobProcessingInterface;
using System;

namespace _TestHarness
{
    public class JobTester
    {
        private string JobParameterXml;

        public JobTester()
        {
            JobParameterXml = BuildXmlString();
        }

        public void Test()
        {
            object parameters = new object();
            string siteName = Constants.SiteName;
            int jobTaskId = 100116;
            int eventHandleId = 0;

            Task task = new JobTask();
            int returnValue = task.RunTask(siteName, jobTaskId, eventHandleId, ref JobParameterXml, ref parameters);
        }

        private string BuildXmlString()
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load("..\\..\\JobInputXML.xml");

            return xmlDoc.OuterXml;
        }
    }
}
