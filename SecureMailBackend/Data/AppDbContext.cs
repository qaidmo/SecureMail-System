using Microsoft.EntityFrameworkCore;
using SecureMailBackend.Models;

namespace SecureMailBackend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

      

        public DbSet<User> Users { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }

        public DbSet<Organization> Organizations { get; set; }
        public DbSet<OrganizationMember> OrganizationMembers { get; set; }

        public DbSet<EmailIntegration> EmailIntegrations { get; set; }
        public DbSet<EmailMessage> EmailMessages { get; set; }
        public DbSet<MessageUrl> MessageUrls { get; set; }
        public DbSet<MessageAttachment> MessageAttachments { get; set; }

        public DbSet<AddressCheck> AddressChecks { get; set; }
        public DbSet<Scan> Scans { get; set; }

        public DbSet<DeviceToken> DeviceTokens { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<DomainPolicy> DomainPolicies { get; set; }
        public DbSet<ScanRule> ScanRules { get; set; }
        public DbSet<VirusTotalQuota> VirusTotalQuotas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            
            modelBuilder.Entity<OrganizationMember>()
                .HasIndex(m => new { m.OrgId, m.UserId })
                .IsUnique();

            
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

           
            modelBuilder.Entity<EmailIntegration>()
                .HasIndex(e => new { e.UserId, e.Provider, e.ProviderAccountEmail })
                .IsUnique();

           
            modelBuilder.Entity<EmailMessage>()
                .HasIndex(m => new { m.IntegrationId, m.ProviderMessageId })
                .IsUnique();

           
            modelBuilder.Entity<DomainPolicy>()
                .HasIndex(p => new { p.OrgId, p.PolicyType, p.Domain })
                .IsUnique();

            modelBuilder.Entity<DeviceToken>()
                .HasIndex(t => t.FcmToken)
                .IsUnique();
                
            modelBuilder.Entity<VirusTotalQuota>()
                .HasIndex(q => q.Date)
                .IsUnique();
        }
    }
}