namespace WalrusBot2.Data
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Walrus.WalrusProcessMonitor")]
    public partial class WalrusProcessMonitor
    {
        [Key]
        [StringLength(16)]
        public string User { get; set; }

        [Required]
        [StringLength(50)]
        public string Process { get; set; }

        [Required]
        [StringLength(50)]
        public string WindowName { get; set; }

        [Required]
        [StringLength(18)]
        public string MessageId { get; set; }

        [Required]
        [StringLength(18)]
        public string MessageChannel { get; set; }
    }
}
