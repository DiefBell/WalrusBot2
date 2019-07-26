namespace WalrusBot2.Data
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Walrus.WalrusConf")]
    public partial class WalrusConf
    {
        [Key]
        [StringLength(32)]
        public string Key { get; set; }

        [Required]
        [StringLength(128)]
        public string Value { get; set; }
    }
}
