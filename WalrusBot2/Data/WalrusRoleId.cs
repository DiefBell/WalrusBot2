namespace WalrusBot2.Data
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Walrus.WalrusRoleIds")]
    public partial class WalrusRoleId
    {
        [Key]
        [StringLength(32)]
        public string Role { get; set; }

        [Required]
        [StringLength(32)]
        public string Id { get; set; }
    }
}
