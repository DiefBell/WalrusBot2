namespace WalrusBot2.Data
{
    using System;
    using System.Data.Entity;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;

    public partial class dbContextWalrus : DbContext
    {
        public dbContextWalrus()
            : base("name=dbContextWalrus")
        {
        }

        public virtual DbSet<WalrusConf> WalrusConfs { get; set; }
        public virtual DbSet<WalrusMembershipList> WalrusMembershipLists { get; set; }
        public virtual DbSet<WalrusRoleId> WalrusRoleIds { get; set; }
        public virtual DbSet<WalrusString> WalrusStrings { get; set; }
        public virtual DbSet<WalrusUserInfo> WalrusUserInfoes { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<WalrusConf>()
                .Property(e => e.Key)
                .IsUnicode(false);

            modelBuilder.Entity<WalrusConf>()
                .Property(e => e.Value)
                .IsUnicode(false);

            modelBuilder.Entity<WalrusMembershipList>()
                .Property(e => e.Email)
                .IsUnicode(false);

            modelBuilder.Entity<WalrusMembershipList>()
                .Property(e => e.Membership)
                .IsUnicode(false);

            modelBuilder.Entity<WalrusRoleId>()
                .Property(e => e.Role)
                .IsUnicode(false);

            modelBuilder.Entity<WalrusString>()
                .Property(e => e.StringKey)
                .IsUnicode(false);

            modelBuilder.Entity<WalrusString>()
                .Property(e => e.StringValue)
                .IsUnicode(false);

            modelBuilder.Entity<WalrusUserInfo>()
                .Property(e => e.UserId)
                .IsUnicode(false);

            modelBuilder.Entity<WalrusUserInfo>()
                .Property(e => e.Username)
                .IsUnicode(false);

            modelBuilder.Entity<WalrusUserInfo>()
                .Property(e => e.Email)
                .IsUnicode(false);

            modelBuilder.Entity<WalrusUserInfo>()
                .Property(e => e.IGNsJSON)
                .IsUnicode(false);

            modelBuilder.Entity<WalrusUserInfo>()
                .Property(e => e.AdditionalRolesJSON)
                .IsUnicode(false);
        }
    }
}
