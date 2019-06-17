namespace WalrusBot2.Data
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Walrus.WalrusStrings")]
    public partial class WalrusString
    {
        [Key]
        [StringLength(16)]
        public string StringKey { get; set; }

        [StringLength(16777215)]
        public string StringValue { get; set; }
    }
}
