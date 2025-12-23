using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicoPix.Domain.Entities;

namespace ServicoPix.Infrastructure.Persistence.Mappings;

public class TransacaoMapping : IEntityTypeConfiguration<Transacao>
{
    public void Configure(EntityTypeBuilder<Transacao> builder)
    {
        builder.ToTable("Transacoes");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .IsRequired();

        builder.Property(t => t.ContaOrigemId)
            .IsRequired();

        builder.Property(t => t.ContaDestinoId)
            .IsRequired();

        builder.Property(t => t.Valor)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(t => t.DataCriacao)
            .IsRequired();

        builder.Property(t => t.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(t => t.MensagemErro)
            .HasMaxLength(500);

        builder.HasIndex(t => t.ContaOrigemId);
        builder.HasIndex(t => t.ContaDestinoId);
        builder.HasIndex(t => t.DataCriacao);
    }
}
