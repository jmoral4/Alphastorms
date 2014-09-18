using System;

namespace PatNet.Lib
{
    public class Patent
    {
        public string PatentNumber { get; set; }
        
        public string AmendmentFile { get; set; }
        public string Bodyfile { get; set; }
        public int BodyFileCount { get; set; }
        public string Abstract { get; set; }
        public int PageCount { get; set; }
        public int ClaimCount { get; set; }
        public bool IsComplex { get; set; }

        public bool HasAmendment
        {
            get { return !String.IsNullOrEmpty(AmendmentFile); }
        }

        public Patent ()
        {
                
        }
        

    }
}