using Microsoft.EntityFrameworkCore;

namespace SyncEmailAttachments.Infrastructure.Data.EmailContext;

public sealed class EmailContext : DbContext
{
	private EmailContext() { }
	public EmailContext(DbContextOptions<EmailContext> options) : base(options) { }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<EmailHistory>(e =>
		{
			e.ToTable("EmailHistory");

			e.HasKey(e => e.EmailAddress);
			e.Property(e => e.EmailAddress).HasMaxLength(300);
		});
	}
}