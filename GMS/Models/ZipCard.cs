//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace GMS.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class ZipCard
    {
        public int zipID { get; set; }
        public string uaNetID { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public bool gender { get; set; }
    
        public virtual Resident Resident { get; set; }
    }
}
