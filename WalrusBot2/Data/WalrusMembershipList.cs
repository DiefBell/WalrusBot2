namespace WalrusBot2.Data
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Walrus.WalrusMembershipList")]
    public partial class WalrusMembershipList
    {
        [Key]
        [StringLength(254)]
        public string Email { get; set; }

        [Column(TypeName = "text")]
        [Required]
        [StringLength(65535)]
        public string Membership { get; set; }
    }
}
