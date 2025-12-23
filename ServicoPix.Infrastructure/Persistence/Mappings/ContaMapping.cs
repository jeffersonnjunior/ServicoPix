using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicoPix.Domain.Entities;

namespace ServicoPix.Infrastructure.Persistence.Mappings;

public class ContaMapping : IEntityTypeConfiguration<Conta>
{
    public void Configure(EntityTypeBuilder<Conta> builder)
    {
        builder.ToTable("Contas");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .IsRequired();

        builder.Property(c => c.Numero)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(c => c.Saldo)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.HasIndex(c => c.Numero)
            .IsUnique();
    }
}
