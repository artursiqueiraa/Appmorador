using AppMorador.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AppMorador.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Propriedade> Propriedades => Set<Propriedade>();

    public DbSet<Central> Centrais => Set<Central>();

    public DbSet<Zona> Zonas => Set<Zona>();

    public DbSet<Ocorrencia> Ocorrencias => Set<Ocorrencia>();

    public DbSet<RegistroEventoAlarme> RegistrosEventoAlarme => Set<RegistroEventoAlarme>();

    public DbSet<Gravador> Gravadores => Set<Gravador>();

    public DbSet<Camera> Cameras => Set<Camera>();

    public DbSet<VinculoZonaCamera> VinculosZonaCamera => Set<VinculoZonaCamera>();

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Central>()
            .HasIndex(c => c.NumeroSerie)
            .IsUnique();

        modelBuilder.Entity<Zona>()
            .HasIndex(z => new { z.CentralId, z.Numero })
            .IsUnique();

        // Ocorrencia e deliberadamente robusta a dados nao provisionados: os FKs sao
        // nullable e SetNull no delete, nunca Cascade/Restrict — a ocorrencia nunca
        // deve deixar de existir por causa de uma central/zona removida depois.
        modelBuilder.Entity<Ocorrencia>()
            .HasOne(o => o.Central)
            .WithMany()
            .HasForeignKey(o => o.CentralId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Ocorrencia>()
            .HasOne(o => o.Propriedade)
            .WithMany()
            .HasForeignKey(o => o.PropriedadeId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Ocorrencia>()
            .HasOne(o => o.Zona)
            .WithMany()
            .HasForeignKey(o => o.ZonaId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indices para as consultas de suporte/diagnostico mais obvias (Fase 1.1):
        // ocorrencias recentes em geral, e ocorrencias recentes de uma zona especifica.
        modelBuilder.Entity<Ocorrencia>()
            .HasIndex(o => o.CreatedAtUtc);

        modelBuilder.Entity<Ocorrencia>()
            .HasIndex(o => new { o.ZonaId, o.CreatedAtUtc });

        // Sprint 3 — Central de Eventos: consulta paginada por propriedade ordenada por
        // data (JflFonteEventos) e o principal padrao de acesso a esta tabela agora.
        modelBuilder.Entity<Ocorrencia>()
            .HasIndex(o => new { o.PropriedadeId, o.CreatedAtUtc });

        modelBuilder.Entity<Ocorrencia>()
            .Property(o => o.StatusResolucao)
            .HasConversion<string>();

        // RegistroEventoAlarme e tabela de auditoria/diagnostico pura: guardar o
        // resultado como string legivel favorece consulta SQL direta por quem for investigar.
        modelBuilder.Entity<RegistroEventoAlarme>()
            .Property(l => l.ResultadoProcessamento)
            .HasConversion<string>();

        // VinculoZonaCamera e deliberadamente uma entidade de vinculo (nao FK cravada
        // em Zona): sem indice/constraint de unicidade, para permitir zona com mais de
        // uma camera (ou o inverso) no futuro sem mudar o schema.
        modelBuilder.Entity<Gravador>()
            .Property(g => g.Fabricante)
            .HasConversion<string>();

        // Sprint 1 — Autenticacao/Propriedade.
        modelBuilder.Entity<Usuario>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(t => t.TokenHash)
            .IsUnique();

        modelBuilder.Entity<RefreshToken>()
            .HasOne(t => t.Usuario)
            .WithMany()
            .HasForeignKey(t => t.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict (nao Cascade): apagar um usuario nunca pode apagar de tabela as
        // propriedades dele em cascata — evita destruir dados de uma residencia/
        // comercio por acidente ao remover uma conta. Exclusao de propriedade, se
        // necessaria, deve ser uma acao explicita e separada.
        modelBuilder.Entity<Propriedade>()
            .HasOne(p => p.Proprietario)
            .WithMany()
            .HasForeignKey(p => p.ProprietarioId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Propriedade>()
            .Property(p => p.Tipo)
            .HasConversion<string>();
    }
}
