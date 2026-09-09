using Microsoft.EntityFrameworkCore;
using ICQ.Server.Models;

namespace ICQ.Server.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Chat> Chats => Set<Chat>();
    public DbSet<ChatParticipant> ChatParticipants => Set<ChatParticipant>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserKeyBundle> UserKeyBundles => Set<UserKeyBundle>();
    public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();
    public DbSet<FileAttachment> FileAttachments => Set<FileAttachment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Uin).IsUnique();
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Nickname).HasMaxLength(64);
            e.Property(u => u.Status).HasMaxLength(32).HasDefaultValue("offline");
            e.Property(u => u.StatusMessage).HasMaxLength(256);
        });

        modelBuilder.Entity<Chat>(e =>
        {
            e.Property(c => c.Title).HasMaxLength(128);
            e.Property(c => c.Type).HasMaxLength(16).HasDefaultValue("private");
        });

        modelBuilder.Entity<ChatParticipant>(e =>
        {
            e.HasKey(cp => new { cp.ChatId, cp.UserId });
            e.HasOne(cp => cp.Chat).WithMany(c => c.Participants).HasForeignKey(cp => cp.ChatId);
            e.HasOne(cp => cp.User).WithMany().HasForeignKey(cp => cp.UserId);
        });

        modelBuilder.Entity<Message>(e =>
        {
            e.HasIndex(m => new { m.ChatId, m.CreatedAt });
            e.Property(m => m.Content).HasMaxLength(8000);
            e.Property(m => m.EncryptedPayload).HasMaxLength(16000);
            e.HasOne(m => m.Chat).WithMany(c => c.Messages).HasForeignKey(m => m.ChatId);
            e.HasOne(m => m.Sender).WithMany().HasForeignKey(m => m.SenderId);
        });

        modelBuilder.Entity<Contact>(e =>
        {
            e.HasKey(c => new { c.OwnerId, c.ContactUserId });
            e.HasOne(c => c.Owner).WithMany(u => u.Contacts).HasForeignKey(c => c.OwnerId);
            e.HasOne(c => c.ContactUser).WithMany().HasForeignKey(c => c.ContactUserId);
        });

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasIndex(rt => rt.Token).IsUnique();
            e.HasOne(rt => rt.User).WithMany().HasForeignKey(rt => rt.UserId);
        });

        modelBuilder.Entity<UserKeyBundle>(e =>
        {
            e.HasKey(k => k.UserId);
            e.HasOne(k => k.User).WithOne().HasForeignKey<UserKeyBundle>(k => k.UserId);
            e.Property(k => k.IdentityKeyPublic).HasMaxLength(256);
            e.Property(k => k.SignedPreKeyPublic).HasMaxLength(256);
            e.Property(k => k.SignedPreKeySignature).HasMaxLength(512);
            e.Property(k => k.OneTimePreKeysJson).HasMaxLength(8000);
        });

        modelBuilder.Entity<PushSubscription>(e =>
        {
            e.HasIndex(p => new { p.UserId, p.Endpoint });
            e.HasOne(p => p.User).WithMany().HasForeignKey(p => p.UserId);
            e.Property(p => p.Endpoint).HasMaxLength(1024);
            e.Property(p => p.P256dh).HasMaxLength(256);
            e.Property(p => p.Auth).HasMaxLength(128);
            e.Property(p => p.Platform).HasMaxLength(32);
        });

        modelBuilder.Entity<FileAttachment>(e =>
        {
            e.HasOne(f => f.Message).WithMany().HasForeignKey(f => f.MessageId).IsRequired(false);
            e.Property(f => f.FileName).HasMaxLength(256);
            e.Property(f => f.ContentType).HasMaxLength(128);
            e.Property(f => f.StoragePath).HasMaxLength(512);
        });
    }
}
