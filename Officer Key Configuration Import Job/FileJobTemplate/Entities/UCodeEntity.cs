using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ILCookOfficerKeyConfigImportJob.Entities
{
    public class UCodeEntity
    {
        private string codeID;
        private string code;
        
        public string CodeID { get => codeID; set => codeID = value; }
        public string Code { get => code; set => code = value; }
        
        public UCodeEntity(string codeID, string code)
        {
            CodeID = codeID;
            Code = code;
        }

    }
}
