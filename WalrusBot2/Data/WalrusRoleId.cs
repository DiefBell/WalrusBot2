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
        [StringLength(16)]
        public string Role { get; set; }

        [StringLength(16)]
        public string Id { get; set; }
    }
}
