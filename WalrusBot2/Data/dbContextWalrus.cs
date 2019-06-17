namespace WalrusBot2.Data
{
    using System;
    using System.Data.Entity;
    using System.Linq;

    public partial class dbContextWalrus : DbContext
    {
        protected static string connectionString;
        public static void SetConnectionString(string s)
        {
            connectionString = s;
        }

        public dbContextWalrus() : base("name=dbContextWalrus")
        {
            Database.Connection.ConnectionString = connectionString;
        }

        public virtual DbSet<WalrusConf> WalrusConfs { get; set; }
        public virtual DbSet<WalrusMembershipList> WalrusMembershipLists { get; set; }
        public virtual DbSet<WalrusRoleId> WalrusRoleIds { get; set; }
        public virtual DbSet<WalrusString> WalrusStrings { get; set; }
        public virtual DbSet<WalrusUserInfo> WalrusUserInfoes { get; set; }

        public string this[string d, string k]
        {
            get
            {
                switch (d)
                {
                    case "config":
                        return (from c in WalrusConfs where c.Key == k select c.Value).FirstOrDefault();
                    case "string":
                        return (from s in WalrusStrings where s.StringKey == k select s.StringValue).FirstOrDefault();
                    case "role":
                        return (from r in WalrusRoleIds where r.Role == k select r.Id).FirstOrDefault();
                    default:
                        return "";
                }
            }
        }

        private object await(IQueryable<WalrusConf> queryable)
        {
            throw new NotImplementedException();
        }

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
