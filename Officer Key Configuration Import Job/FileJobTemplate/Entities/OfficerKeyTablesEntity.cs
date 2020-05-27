using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ILCookOfficerKeyConfigImportJob.Entities
{
    class OfficerKeyTablesEntity
    {
        private List<CookAutoHearingCallConfigurationEntity> cookAutoHearingCallConfiguration;
        private List<CookAutoHearingOfficerConfigurationEntity> cookAutoHearingOfficerConfiguration;
        private List<CookAutoHearingOfficerKeyConfigurationEntity> cookAutoHearingOfficerKeyConfiguration;
        private List<xCookAutoHearingOfficerKeyConfigurationDateEntity> xCookAutoHearingOfficerKeyConfigurationDate;

        internal List<CookAutoHearingCallConfigurationEntity> CookAutoHearingCallConfiguration { get => cookAutoHearingCallConfiguration; set => cookAutoHearingCallConfiguration = value; }
        internal List<CookAutoHearingOfficerConfigurationEntity> CookAutoHearingOfficerConfiguration { get => cookAutoHearingOfficerConfiguration; set => cookAutoHearingOfficerConfiguration = value; }
        internal List<CookAutoHearingOfficerKeyConfigurationEntity> CookAutoHearingOfficerKeyConfiguration { get => cookAutoHearingOfficerKeyConfiguration; set => cookAutoHearingOfficerKeyConfiguration = value; }
        internal List<xCookAutoHearingOfficerKeyConfigurationDateEntity> XCookAutoHearingOfficerKeyConfigurationDate { get => xCookAutoHearingOfficerKeyConfigurationDate; set => xCookAutoHearingOfficerKeyConfigurationDate = value; }
    }

    class CookAutoHearingCallConfigurationEntity
    {
        /* Table Columns
         * HearingLocationID, CallNumber, Time
         */
        private string action;
        private string hearingLocationCode;
        private string callNumber;
        private string time;

        public string Action { get => action; set => action = value; }
        public string HearingLocationCode { get => hearingLocationCode; set => hearingLocationCode = value; }
        public string CallNumber { get => callNumber; set => callNumber = value; }
        public string Time { get => time; set => time = value; }
        

        public CookAutoHearingCallConfigurationEntity(string action, string hearingLocationCode, string callNumber, string time)
        {
            this.action = action;
            this.hearingLocationCode = hearingLocationCode;
            this.callNumber = callNumber;
            this.time = time;
        }
    }

     class CookAutoHearingOfficerConfigurationEntity
    {
        /* Table Columns
        nodeID, agencyID, officerID, officerKey, hearingLocationID, callNumber, isMajorOffenseAssignment
        */
        private string action;
        private string nodeID;
        private string agencyCode;
        private string officerID;
        private string officerKey;
        private string hearingLocationCode;
        private string callNumber;
        private string isMajorOffenseAssignment;
        private string badgeNumber;

        public string Action { get => action; set => action = value; }
        public string NodeID { get => nodeID; set => nodeID = value; }
        public string AgencyCode { get => agencyCode; set => agencyCode = value; }
        public string OfficerID { get => officerID; set => officerID = value; }
        public string OfficerKey { get => officerKey; set => officerKey = value; }
        public string HearingLocationCode { get => hearingLocationCode; set => hearingLocationCode = value; }
        public string CallNumber { get => callNumber; set => callNumber = value; }
        public string IsMajorOffenseAssignment { get => isMajorOffenseAssignment; set => isMajorOffenseAssignment = value; }
        public string BadgeNumber { get => badgeNumber; set => badgeNumber = value; }

        public CookAutoHearingOfficerConfigurationEntity(string action, string nodeID, string agencyCode, string officerID
            , string officerKey, string hearingLocationCode, string callNumber, string isMajorOffenseAssignment, string badgeNumber)
        {
            this.action = action;
            this.nodeID = nodeID;
            this.agencyCode = agencyCode;
            this.officerID = officerID;
            this.officerKey = officerKey;
            this.hearingLocationCode = hearingLocationCode;
            this.callNumber = callNumber;
            this.isMajorOffenseAssignment = isMajorOffenseAssignment;
            this.badgeNumber = badgeNumber;
        }
    }

    class CookAutoHearingOfficerKeyConfigurationEntity
    {
        /* Table Columns
         * NodeID, OfficerKey
         */
        private string action;
        private string nodeID;
        private string officerKey;

        public string Action { get => action; set => action = value; }
        public string NodeID { get => nodeID; set => nodeID = value; }
        public string OfficerKey { get => officerKey; set => officerKey = value; }

        public CookAutoHearingOfficerKeyConfigurationEntity(string action, string nodeID, string officerKey)
        {
            this.action = action;
            this.nodeID     = nodeID;
            this.officerKey = officerKey;
        }
    }

    class xCookAutoHearingOfficerKeyConfigurationDateEntity
    {
        /* Table Columns
         * NodeID, OfficerKey
         */
        private string action;
        private string nodeID;
        private string officerKey;
        private string date;

        public string Action { get => action; set => action = value; }
        public string NodeID { get => nodeID; set => nodeID = value; }
        public string OfficerKey { get => officerKey; set => officerKey = value; }
        public string Date { get => date; set => date = value; }

        public xCookAutoHearingOfficerKeyConfigurationDateEntity(string action, string nodeID, string officerKey, string date)
        {
            this.action = action;
            this.nodeID = nodeID;
            this.officerKey = officerKey;
            this.date = date;
        }
    }


}
