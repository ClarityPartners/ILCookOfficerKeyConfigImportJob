using System;
using System.Runtime.InteropServices;
using Tyler.Odyssey.JobProcessing;

namespace ILCookOfficerKeyConfigImportJob
{
    [ClassInterface(ClassInterfaceType.None)]
    [Guid("1fc58829-b20d-42ed-b916-0fa5caa5d0dd")]
    [ComVisible(true)]
    public class JobTask : Task
    {
        protected override void SetupProcessor(string SiteID, string JobTaskXML)
        {
            Processor = new DataProcessor(SiteID, JobTaskXML);

            ((DataProcessor)Processor).TaskParms = this.jobTaskParms;
            ((DataProcessor)Processor).TaskUtility = this.taskUtility;
            ((DataProcessor)Processor).TaskDocument = this.taskDocument;

            UserID = ((DataProcessor)Processor).Context.UserID;
        }

        private int UserID { get; set; }
    }
}
