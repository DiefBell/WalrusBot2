namespace WalrusBot2.Data
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Walrus.WalrusUserInfo")]
    public partial class WalrusUserInfo
    {
        [Key]
        [StringLength(18)]
        public string UserId { get; set; }

        [Column(TypeName = "bit")]
        public bool Verified { get; set; }

        [Required]
        [StringLength(37)]
        public string Username { get; set; }

        [Required]
        [StringLength(254)]
        public string Email { get; set; }

        [Column(TypeName = "char")]
        [Required]
        [StringLength(8)]
        public string Code { get; set; }

        [StringLength(1073741823)]
        public string IGNsJSON { get; set; }

        [StringLength(1073741823)]
        public string AdditionalRolesJSON { get; set; }
    }
}
