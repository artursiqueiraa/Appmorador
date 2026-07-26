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

    public DbSet<Unidade> Unidades => Set<Unidade>();

    public DbSet<Morador> Moradores => Set<Morador>();

    public DbSet<Credencial> Credenciais => Set<Credencial>();

    public DbSet<PontoAcesso> PontosAcesso => Set<PontoAcesso>();

    public DbSet<PermissaoAcesso> PermissoesAcesso => Set<PermissaoAcesso>();

    public DbSet<HistoricoCredencial> HistoricoCredenciais => Set<HistoricoCredencial>();

    public DbSet<Visitante> Visitantes => Set<Visitante>();

    public DbSet<Autorizacao> Autorizacoes => Set<Autorizacao>();

    public DbSet<HistoricoVisitante> HistoricoVisitantes => Set<HistoricoVisitante>();

    public DbSet<Veiculo> Veiculos => Set<Veiculo>();

    public DbSet<Vaga> Vagas => Set<Vaga>();

    public DbSet<VinculoVeiculoVaga> VinculosVeiculoVaga => Set<VinculoVeiculoVaga>();

    public DbSet<PermissaoVeicular> PermissoesVeiculares => Set<PermissaoVeicular>();

    public DbSet<HistoricoVeiculo> HistoricoVeiculos => Set<HistoricoVeiculo>();

    public DbSet<HistoricoVaga> HistoricoVagas => Set<HistoricoVaga>();

    public DbSet<Entrega> Entregas => Set<Entrega>();

    public DbSet<HistoricoEntrega> HistoricoEntregas => Set<HistoricoEntrega>();

    public DbSet<Equipamento> Equipamentos => Set<Equipamento>();

    public DbSet<EventoEquipamento> EventosEquipamento => Set<EventoEquipamento>();

    public DbSet<StatusCentralJfl> StatusCentraisJfl => Set<StatusCentralJfl>();

    public DbSet<SnapshotOperacional> SnapshotsOperacionais => Set<SnapshotOperacional>();

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

        // Sprint 6 — dominio principal (Propriedade > Unidade > Morador). Exclusao e
        // sempre logica (ADR 0009): Restrict aqui e so uma trava de integridade fisica
        // contra um FK orfao improvavel — quem apaga de verdade (marca Excluido) e o
        // PropriedadeServico, nunca o banco.
        modelBuilder.Entity<Unidade>()
            .HasOne(u => u.Propriedade)
            .WithMany()
            .HasForeignKey(u => u.PropriedadeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Unidade>()
            .Property(u => u.Tipo)
            .HasConversion<string>();

        modelBuilder.Entity<Morador>()
            .HasOne(m => m.Unidade)
            .WithMany()
            .HasForeignKey(m => m.UnidadeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Morador>()
            .Property(m => m.Status)
            .HasConversion<string>();

        // Query filter global: nenhuma consulta normal ve registro excluido
        // logicamente — quem implementa uma consulta nova sobre estas 3 entidades nao
        // precisa lembrar de filtrar Excluido manualmente. Uma futura tela de Lixeira
        // usaria IgnoreQueryFilters() explicitamente, nunca por acidente.
        modelBuilder.Entity<Propriedade>().HasQueryFilter(p => !p.Excluido);
        modelBuilder.Entity<Unidade>().HasQueryFilter(u => !u.Excluido);
        modelBuilder.Entity<Morador>().HasQueryFilter(m => !m.Excluido);

        // Sprint 7 — Controle de Acesso Inteligente (dominio, sem integracao real).
        // Mesmo padrao de soft delete (ADR 0009/0010): Restrict fisico + Excluido
        // logico controlado pela aplicacao.
        modelBuilder.Entity<Credencial>()
            .HasOne(c => c.Morador)
            .WithMany()
            .HasForeignKey(c => c.MoradorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Credencial>()
            .Property(c => c.Tipo)
            .HasConversion<string>();

        modelBuilder.Entity<Credencial>()
            .Property(c => c.Status)
            .HasConversion<string>();

        modelBuilder.Entity<PontoAcesso>()
            .HasOne(p => p.Propriedade)
            .WithMany()
            .HasForeignKey(p => p.PropriedadeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Sprint 9 — distingue pontos Gerais de Veiculares (PermissaoVeicular só aponta para Veicular).
        modelBuilder.Entity<PontoAcesso>()
            .Property(p => p.Tipo)
            .HasConversion<string>();

        // PermissaoAcesso e entidade de vinculo (mesmo padrao de VinculoZonaCamera):
        // uma Credencial pode ter varias, uma por PontoAcesso que acessa.
        modelBuilder.Entity<PermissaoAcesso>()
            .HasOne(p => p.Credencial)
            .WithMany()
            .HasForeignKey(p => p.CredencialId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PermissaoAcesso>()
            .HasOne(p => p.PontoAcesso)
            .WithMany()
            .HasForeignKey(p => p.PontoAcessoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PermissaoAcesso>()
            .Property(p => p.DiasPermitidos)
            .HasConversion<string>();

        // HistoricoCredencial e auditoria pura (mesmo espirito de RegistroEventoAlarme):
        // sem soft delete, sem query filter — nunca excluido.
        modelBuilder.Entity<HistoricoCredencial>()
            .HasOne(h => h.Credencial)
            .WithMany()
            .HasForeignKey(h => h.CredencialId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HistoricoCredencial>()
            .Property(h => h.TipoEvento)
            .HasConversion<string>();

        modelBuilder.Entity<Credencial>().HasQueryFilter(c => !c.Excluido);
        modelBuilder.Entity<PontoAcesso>().HasQueryFilter(p => !p.Excluido);
        modelBuilder.Entity<PermissaoAcesso>().HasQueryFilter(p => !p.Excluido);

        // Sprint 8 — Visitantes e Autorizacoes (dominio, sem integracao real). Mesmo
        // padrao de soft delete (ADR 0009) + entidade de vinculo (ADR 0010/0011).
        modelBuilder.Entity<Visitante>()
            .HasOne(v => v.Propriedade)
            .WithMany()
            .HasForeignKey(v => v.PropriedadeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Autorizacao>()
            .HasOne(a => a.MoradorResponsavel)
            .WithMany()
            .HasForeignKey(a => a.MoradorResponsavelId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Autorizacao>()
            .HasOne(a => a.Unidade)
            .WithMany()
            .HasForeignKey(a => a.UnidadeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Autorizacao>()
            .HasOne(a => a.Visitante)
            .WithMany()
            .HasForeignKey(a => a.VisitanteId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Autorizacao>()
            .Property(a => a.Tipo)
            .HasConversion<string>();

        modelBuilder.Entity<Autorizacao>()
            .Property(a => a.StatusManual)
            .HasConversion<string>();

        // HistoricoVisitante e auditoria pura (mesmo espirito de HistoricoCredencial):
        // sem soft delete, sem query filter — nunca excluido. AutorizacaoId e opcional
        // (nulo so no evento VisitanteRemovido), por isso SetNull em vez de Restrict.
        modelBuilder.Entity<HistoricoVisitante>()
            .HasOne(h => h.Visitante)
            .WithMany()
            .HasForeignKey(h => h.VisitanteId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HistoricoVisitante>()
            .HasOne(h => h.Autorizacao)
            .WithMany()
            .HasForeignKey(h => h.AutorizacaoId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<HistoricoVisitante>()
            .Property(h => h.TipoEvento)
            .HasConversion<string>();

        modelBuilder.Entity<Visitante>().HasQueryFilter(v => !v.Excluido);
        modelBuilder.Entity<Autorizacao>().HasQueryFilter(a => !a.Excluido);

        // Sprint 9 — Veiculos e Garagens (dominio, sem integracao real). Mesmo padrao
        // de soft delete (ADR 0009) + entidade de vinculo (ADR 0010/0011).
        modelBuilder.Entity<Veiculo>()
            .HasOne(v => v.Morador)
            .WithMany()
            .HasForeignKey(v => v.MoradorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Veiculo>()
            .Property(v => v.Tipo)
            .HasConversion<string>();

        modelBuilder.Entity<Veiculo>()
            .Property(v => v.Status)
            .HasConversion<string>();

        // Unicidade de Placa e verificada em codigo de aplicacao (GetByPlacaAsync),
        // nao por indice unico no banco: um veiculo excluido logicamente pode liberar
        // a placa para recadastro (ex.: carro vendido), e um indice unico no banco nao
        // sabe distinguir excluido de ativo sem um indice filtrado — nao suportado de
        // forma portavel pelo Pomelo/MySQL nesta versao.
        modelBuilder.Entity<Vaga>()
            .HasOne(v => v.Propriedade)
            .WithMany()
            .HasForeignKey(v => v.PropriedadeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Vaga>()
            .Property(v => v.Tipo)
            .HasConversion<string>();

        modelBuilder.Entity<Vaga>()
            .Property(v => v.StatusManual)
            .HasConversion<string>();

        // VinculoVeiculoVaga e entidade de vinculo temporal (mesmo padrao de
        // PermissaoAcesso) — cada linha e um periodo de ocupacao, nunca sobrescrita.
        modelBuilder.Entity<VinculoVeiculoVaga>()
            .HasOne(v => v.Veiculo)
            .WithMany()
            .HasForeignKey(v => v.VeiculoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<VinculoVeiculoVaga>()
            .HasOne(v => v.Vaga)
            .WithMany()
            .HasForeignKey(v => v.VagaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PermissaoVeicular>()
            .HasOne(p => p.Veiculo)
            .WithMany()
            .HasForeignKey(p => p.VeiculoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PermissaoVeicular>()
            .HasOne(p => p.PontoAcesso)
            .WithMany()
            .HasForeignKey(p => p.PontoAcessoId)
            .OnDelete(DeleteBehavior.Restrict);

        // HistoricoVeiculo/HistoricoVaga sao auditoria pura (mesmo espirito de
        // HistoricoCredencial): sem soft delete, sem query filter — nunca excluidos.
        modelBuilder.Entity<HistoricoVeiculo>()
            .HasOne(h => h.Veiculo)
            .WithMany()
            .HasForeignKey(h => h.VeiculoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HistoricoVeiculo>()
            .Property(h => h.TipoEvento)
            .HasConversion<string>();

        modelBuilder.Entity<HistoricoVaga>()
            .HasOne(h => h.Vaga)
            .WithMany()
            .HasForeignKey(h => h.VagaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HistoricoVaga>()
            .Property(h => h.TipoEvento)
            .HasConversion<string>();

        modelBuilder.Entity<Veiculo>().HasQueryFilter(v => !v.Excluido);
        modelBuilder.Entity<Vaga>().HasQueryFilter(v => !v.Excluido);
        modelBuilder.Entity<VinculoVeiculoVaga>().HasQueryFilter(v => !v.Excluido);
        modelBuilder.Entity<PermissaoVeicular>().HasQueryFilter(p => !p.Excluido);

        // Sprint 10 — Entregas e Correspondencias (dominio, sem integracao real).
        // Mesmo padrao de soft delete (ADR 0009). Status e 100% manual — sem
        // StatusManual/calculadora hibrida (ADR 0013): diferente de Autorizacao/Vaga,
        // aqui nao ha noção de "efetivo computado", apenas transicoes explicitas.
        modelBuilder.Entity<Entrega>()
            .HasOne(e => e.MoradorDestinatario)
            .WithMany()
            .HasForeignKey(e => e.MoradorDestinatarioId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Entrega>()
            .HasOne(e => e.Unidade)
            .WithMany()
            .HasForeignKey(e => e.UnidadeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Entrega>()
            .Property(e => e.Tipo)
            .HasConversion<string>();

        modelBuilder.Entity<Entrega>()
            .Property(e => e.Status)
            .HasConversion<string>();

        // HistoricoEntrega e auditoria pura (mesmo espirito de HistoricoCredencial):
        // sem soft delete, sem query filter — nunca excluido.
        modelBuilder.Entity<HistoricoEntrega>()
            .HasOne(h => h.Entrega)
            .WithMany()
            .HasForeignKey(h => h.EntregaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HistoricoEntrega>()
            .Property(h => h.TipoEvento)
            .HasConversion<string>();

        modelBuilder.Entity<Entrega>().HasQueryFilter(e => !e.Excluido);

        // Sprint 11 — Migracao da Integracao Control iD (ADR 0014). Equipamento
        // pertence direto a Propriedade (mesmo padrao de PontoAcesso/Vaga/Visitante).
        // Providers de fabricante (ControlIdProvider) nunca aparecem aqui — a
        // persistencia so conhece a entidade generica.
        modelBuilder.Entity<Equipamento>()
            .HasOne(e => e.Propriedade)
            .WithMany()
            .HasForeignKey(e => e.PropriedadeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Equipamento>()
            .Property(e => e.Fabricante)
            .HasConversion<string>();

        modelBuilder.Entity<Equipamento>()
            .Property(e => e.Status)
            .HasConversion<string>();

        // EventoEquipamento e auditoria pura (mesmo espirito de Ocorrencia): sem soft
        // delete, sem query filter — nunca excluido.
        modelBuilder.Entity<EventoEquipamento>()
            .HasOne(e => e.Equipamento)
            .WithMany()
            .HasForeignKey(e => e.EquipamentoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Equipamento>().HasQueryFilter(e => !e.Excluido);

        // Sprint 12 — Migracao JFL Active 100 Bus (ADR 0015). StatusCentralJfl e um
        // rollup 1:1 com Equipamento (Fabricante=Jfl) — indice unico garante upsert
        // seguro. Sem soft delete/query filter: e um snapshot substituivel, nao um
        // registro de auditoria.
        modelBuilder.Entity<StatusCentralJfl>()
            .HasOne(s => s.Equipamento)
            .WithMany()
            .HasForeignKey(s => s.EquipamentoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StatusCentralJfl>()
            .HasIndex(s => s.EquipamentoId)
            .IsUnique();

        // Sprint 13 — Camada Operacional Unificada (ADR 0016). SnapshotOperacional e
        // um rollup 1:1 com Propriedade (indice unico garante upsert seguro), sempre
        // gerado a partir de dados ja persistidos — nunca por um Provider.
        modelBuilder.Entity<SnapshotOperacional>()
            .HasOne(s => s.Propriedade)
            .WithMany()
            .HasForeignKey(s => s.PropriedadeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SnapshotOperacional>()
            .Property(s => s.Saude)
            .HasConversion<string>();

        modelBuilder.Entity<SnapshotOperacional>()
            .HasIndex(s => s.PropriedadeId)
            .IsUnique();
    }
}
