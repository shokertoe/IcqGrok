using ICQ.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace ICQ.Server.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<Chat> Chats => Set<Chat>();
    public DbSet<ChatParticipant> ChatParticipants => Set<ChatParticipant>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<DeviceToken> DeviceTokens => Set<DeviceToken>();
    public DbSet<WebPushSubscription> WebPushSubscriptions => Set<WebPushSubscription>();
    public DbSet<UserKeyBundle> UserKeyBundles => Set<UserKeyBundle>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Nickname).IsUnique();
            e.HasIndex(u => u.Uin).IsUnique();
            // Portable unique index on Email (nulls allowed on both SQLite and PostgreSQL)
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Status).HasConversion<int>();
        });

        // Contact
        modelBuilder.Entity<Contact>(e =>
        {
            e.HasOne(c => c.Owner)
                .WithMany(u => u.Contacts)
                .HasForeignKey(c => c.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(c => c.ContactUser)
                .WithMany()
                .HasForeignKey(c => c.ContactUserId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(c => new { c.OwnerId, c.ContactUserId }).IsUnique();
            e.Property(c => c.Status).HasConversion<int>();
        });

        // Chat
        modelBuilder.Entity<Chat>(e =>
        {
            e.Property(c => c.Type).HasConversion<int>();
        });

        // ChatParticipant
        modelBuilder.Entity<ChatParticipant>(e =>
        {
            e.HasOne(p => p.Chat)
                .WithMany(c => c.Participants)
                .HasForeignKey(p => p.ChatId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(p => p.User)
                .WithMany(u => u.ChatParticipants)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(p => new { p.ChatId, p.UserId }).IsUnique();
            e.Property(p => p.Role).HasConversion<int>();
        });

        // Message
        modelBuilder.Entity<Message>(e =>
        {
            e.HasOne(m => m.Chat)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ChatId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(m => m.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(m => m.ChatId);
            e.HasIndex(m => m.SentAt);
            e.HasIndex(m => m.ClientMessageId);
            e.Property(m => m.Type).HasConversion<int>();
        });

        // RefreshToken
        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(t => t.Token).IsUnique();
        });

        // DeviceToken
        modelBuilder.Entity<DeviceToken>(e =>
        {
            e.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(d => d.Token).IsUnique();
            e.HasIndex(d => d.UserId);
        });

        // WebPushSubscription
        modelBuilder.Entity<WebPushSubscription>(e =>
        {
            e.HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(s => s.Endpoint).IsUnique();
            e.HasIndex(s => s.UserId);
        });

        // UserKeyBundle
        modelBuilder.Entity<UserKeyBundle>(e =>
        {
            e.HasOne(k => k.User)
                .WithOne()
                .HasForeignKey<UserKeyBundle>(k => k.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
