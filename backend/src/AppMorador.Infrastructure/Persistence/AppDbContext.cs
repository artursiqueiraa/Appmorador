using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
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

    public DbSet<DispositivoPush> DispositivosPush => Set<DispositivoPush>();

    // Sprint 21 (ADR 0021) — RBAC Master.
    public DbSet<UsuarioPropriedade> UsuariosPropriedade => Set<UsuarioPropriedade>();

    public DbSet<UsuarioPropriedadePermissao> UsuariosPropriedadePermissao => Set<UsuarioPropriedadePermissao>();

    public DbSet<PropriedadeFeatureFlag> PropriedadesFeatureFlag => Set<PropriedadeFeatureFlag>();

    public DbSet<ModeloEquipamento> ModelosEquipamento => Set<ModeloEquipamento>();

    public DbSet<ModeloEquipamentoCapacidade> ModelosEquipamentoCapacidade => Set<ModeloEquipamentoCapacidade>();

    public DbSet<Provisionamento> Provisionamentos => Set<Provisionamento>();

    public DbSet<AuditoriaMaster> AuditoriaMaster => Set<AuditoriaMaster>();

    // Sprint 22B (ADR 0031)
    public DbSet<VinculoEquipamentoPropriedade> VinculosEquipamentoPropriedade => Set<VinculoEquipamentoPropriedade>();

    // Sprint 24 (ADR 0040) — alertas confiáveis.
    public DbSet<AlertaCritico> AlertasCriticos => Set<AlertaCritico>();

    public DbSet<ContatoEmergencia> ContatosEmergencia => Set<ContatoEmergencia>();

    public DbSet<EventoConectividade> EventosConectividade => Set<EventoConectividade>();

    public DbSet<ControleEnvioNotificacao> ControlesEnvioNotificacao => Set<ControleEnvioNotificacao>();

    // Sprint 24.5 — portões e portas.
    public DbSet<AberturaPontoAcesso> AberturasPontoAcesso => Set<AberturaPontoAcesso>();

    // Sprint 25 — contas e multiusuário.
    public DbSet<ConvitePropriedade> ConvitesPropriedade => Set<ConvitePropriedade>();

    // Sprint 26 — facial ponta a ponta + LGPD.
    public DbSet<ConsentimentoBiometrico> ConsentimentosBiometricos => Set<ConsentimentoBiometrico>();

    public DbSet<SincronizacaoFoto> SincronizacoesFoto => Set<SincronizacaoFoto>();

    /// <summary>Sprint 27 (ADR 0043) — auditoria de comandos.</summary>
    public DbSet<ComandoExecutado> ComandosExecutados => Set<ComandoExecutado>();

    /// <summary>Sprint 27 (ADR 0043) — resultado dos backups agendados e testes de restauração.</summary>
    public DbSet<RegistroBackup> RegistrosBackup => Set<RegistroBackup>();

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

        // Sprint 20 — mesmo padrao de StatusEquipamento/StatusResolucao: enum de negocio
        // trafega/persiste como texto legivel, nunca como numero interno (ADR 0005).
        modelBuilder.Entity<Camera>()
            .Property(c => c.Status)
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

        // Akteck Seg (ADR 0036) — titular/dependente. Default "Titular" no banco faz o
        // backfill de todos os moradores já existentes na própria migration.
        modelBuilder.Entity<Morador>()
            .Property(m => m.Tipo)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(TipoMorador.Titular);

        modelBuilder.Entity<Morador>()
            .Property(m => m.Parentesco)
            .HasConversion<string>()
            .HasMaxLength(20);

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

        modelBuilder.Entity<Equipamento>()
            .Property(e => e.EstadoOperacional)
            .HasConversion<string>();

        // Sprint 22B (ADR 0031) — "Numero de Serie" (coluna real: Identificador, nunca
        // renomeada — ver ADR 0031 sobre por que nao renomear um campo ja usado pela
        // correlacao de sessao JFL) unico por Propriedade. MySQL/InnoDB nao considera dois
        // NULLs iguais num indice unico, entao equipamentos sem Identificador continuam
        // permitidos (nem todo fabricante expoe um, ver Equipamento.cs).
        modelBuilder.Entity<Equipamento>()
            .HasIndex(e => new { e.PropriedadeId, e.Identificador })
            .IsUnique();

        // Sprint 22B (ADR 0031) — vínculo Equipamento<->Propriedade com histórico. FK Restrict
        // nos dois lados (mesmo padrão de Equipamento/EventoEquipamento acima) — nunca cascade,
        // o histórico de vínculo tem que sobreviver mesmo que o equipamento/propriedade seja
        // excluído logicamente.
        modelBuilder.Entity<VinculoEquipamentoPropriedade>()
            .HasOne(v => v.Equipamento)
            .WithMany()
            .HasForeignKey(v => v.EquipamentoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<VinculoEquipamentoPropriedade>()
            .HasOne(v => v.Propriedade)
            .WithMany()
            .HasForeignKey(v => v.PropriedadeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<VinculoEquipamentoPropriedade>()
            .HasIndex(v => v.EquipamentoId);

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

        // Sprint 19 — Notificacoes Push (ADR 0023). Um dispositivo pertence a um
        // Usuario (nunca compartilhado); Propriedade e opcional (token pode ser
        // registrado antes de qualquer Propriedade estar selecionada). Sem soft
        // delete: "Ativo=false" ja e o mecanismo de desativacao (logout, token
        // invalido) — nunca removido fisicamente, para historico (mesmo racional do
        // RefreshToken).
        modelBuilder.Entity<DispositivoPush>()
            .HasOne(d => d.Usuario)
            .WithMany()
            .HasForeignKey(d => d.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DispositivoPush>()
            .HasOne(d => d.Propriedade)
            .WithMany()
            .HasForeignKey(d => d.PropriedadeId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<DispositivoPush>()
            .Property(d => d.Plataforma)
            .HasConversion<string>();

        modelBuilder.Entity<DispositivoPush>()
            .HasIndex(d => d.Token)
            .IsUnique();

        modelBuilder.Entity<DispositivoPush>()
            .HasIndex(d => new { d.UsuarioId, d.Ativo });

        // Sprint 21 (ADR 0021) — RBAC Master. RoleGlobal e nullable (so preenchido
        // para internos) — HasConversion<string> funciona normalmente em enum
        // nullable, convertendo null para null.
        modelBuilder.Entity<Usuario>()
            .Property(u => u.RoleGlobal)
            .HasConversion<string>();

        // UsuarioPropriedade: um vinculo unico por (Usuario, Propriedade) — nunca
        // duplicado. Cascade em ambos os lados (Sprint 21 so cria 1 linha por
        // Propriedade, o Administrador/dono — ver ADR 0021).
        modelBuilder.Entity<UsuarioPropriedade>()
            .HasOne(v => v.Usuario)
            .WithMany()
            .HasForeignKey(v => v.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UsuarioPropriedade>()
            .HasOne(v => v.Propriedade)
            .WithMany()
            .HasForeignKey(v => v.PropriedadeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UsuarioPropriedade>()
            .Property(v => v.Perfil)
            .HasConversion<string>();

        modelBuilder.Entity<UsuarioPropriedade>()
            .HasIndex(v => new { v.UsuarioId, v.PropriedadeId })
            .IsUnique();

        modelBuilder.Entity<UsuarioPropriedadePermissao>()
            .HasOne(p => p.UsuarioPropriedade)
            .WithMany()
            .HasForeignKey(p => p.UsuarioPropriedadeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UsuarioPropriedadePermissao>()
            .Property(p => p.Permissao)
            .HasConversion<string>();

        modelBuilder.Entity<UsuarioPropriedadePermissao>()
            .HasIndex(p => new { p.UsuarioPropriedadeId, p.Permissao })
            .IsUnique();

        modelBuilder.Entity<PropriedadeFeatureFlag>()
            .HasOne(f => f.Propriedade)
            .WithMany()
            .HasForeignKey(f => f.PropriedadeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PropriedadeFeatureFlag>()
            .Property(f => f.Feature)
            .HasConversion<string>();

        modelBuilder.Entity<PropriedadeFeatureFlag>()
            .HasIndex(f => new { f.PropriedadeId, f.Feature })
            .IsUnique();

        // ModeloEquipamento: Fabricante+Nome unico (mesma chave usada pelo
        // get-or-create transparente em EquipamentoServico, ver ADR 0027).
        modelBuilder.Entity<ModeloEquipamento>()
            .Property(m => m.Fabricante)
            .HasConversion<string>();

        modelBuilder.Entity<ModeloEquipamento>()
            .HasIndex(m => new { m.Fabricante, m.Nome })
            .IsUnique();

        modelBuilder.Entity<ModeloEquipamentoCapacidade>()
            .HasOne(c => c.ModeloEquipamento)
            .WithMany()
            .HasForeignKey(c => c.ModeloEquipamentoId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ModeloEquipamentoCapacidade>()
            .Property(c => c.Capacidade)
            .HasConversion<string>();

        modelBuilder.Entity<ModeloEquipamentoCapacidade>()
            .HasIndex(c => new { c.ModeloEquipamentoId, c.Capacidade })
            .IsUnique();

        // Equipamento.ModeloEquipamentoId — SetNull (nunca Cascade): remover um
        // modelo do catalogo nao pode apagar o equipamento que aponta pra ele.
        modelBuilder.Entity<Equipamento>()
            .HasOne(e => e.ModeloEquipamento)
            .WithMany()
            .HasForeignKey(e => e.ModeloEquipamentoId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Provisionamento>()
            .HasOne(p => p.Propriedade)
            .WithMany()
            .HasForeignKey(p => p.PropriedadeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Provisionamento>()
            .Property(p => p.Template)
            .HasConversion<string>();

        modelBuilder.Entity<Provisionamento>()
            .Property(p => p.Status)
            .HasConversion<string>();

        // AuditoriaMaster e trilha generica (mesmo espirito de HistoricoCredencial/
        // RegistroEventoAlarme): sem FK para Usuario de proposito — o registro
        // precisa sobreviver mesmo que a conta interna seja excluida no futuro,
        // entao guarda UsuarioNome como snapshot de texto, nunca via navegacao.
        modelBuilder.Entity<AuditoriaMaster>()
            .Property(a => a.Acao)
            .HasConversion<string>();

        modelBuilder.Entity<AuditoriaMaster>()
            .HasIndex(a => a.DataHoraUtc);

        modelBuilder.Entity<AuditoriaMaster>()
            .HasIndex(a => a.UsuarioId);

        ConfigurarAlertasConfiaveis(modelBuilder);
        ConfigurarPortoesEPortas(modelBuilder);
        ConfigurarMultiusuario(modelBuilder);
        ConfigurarFacial(modelBuilder);
        ConfigurarSegurancaConta(modelBuilder);
        MarcarDatasUtc(modelBuilder);
    }

    /// <summary>
    /// Sprint 24.5 — ponto de acesso ligado ao que abre (equipamento + saída) e à câmera que mostra
    /// a porta. Equipamento tem soft delete (Restrict físico, como o resto do domínio); câmera
    /// excluída só desliga o vínculo.
    /// </summary>
    private static void ConfigurarPortoesEPortas(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PontoAcesso>(e =>
        {
            e.HasOne(p => p.EquipamentoAcionador).WithMany().HasForeignKey(p => p.EquipamentoAcionadorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.Camera).WithMany().HasForeignKey(p => p.CameraId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AberturaPontoAcesso>(e =>
        {
            e.HasOne(a => a.PontoAcesso).WithMany().HasForeignKey(a => a.PontoAcessoId).OnDelete(DeleteBehavior.Restrict);
            e.Property(a => a.Mensagem).HasMaxLength(200);
            e.HasIndex(a => new { a.PontoAcessoId, a.OcorridoEmUtc });
        });
    }

    /// <summary>Sprint 25 (ADR 0041) — convites de acesso; código guardado só como hash (único).</summary>
    private static void ConfigurarMultiusuario(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConvitePropriedade>(e =>
        {
            e.HasOne(c => c.Propriedade).WithMany().HasForeignKey(c => c.PropriedadeId).OnDelete(DeleteBehavior.Restrict);
            e.Property(c => c.Papel).HasConversion<string>().HasMaxLength(20);
            e.Property(c => c.Email).HasMaxLength(200);
            e.Property(c => c.CodigoHash).HasMaxLength(64);
            e.HasIndex(c => c.CodigoHash).IsUnique();
            e.HasIndex(c => c.PropriedadeId);
        });

        modelBuilder.Entity<Usuario>().Property(u => u.Telefone).HasMaxLength(20);
    }

    /// <summary>Sprint 27 (ADR 0043) — auditoria de comandos (chave de idempotência única por usuário) e backups.</summary>
    private static void ConfigurarSegurancaConta(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ComandoExecutado>(e =>
        {
            e.Property(c => c.Tipo).HasConversion<string>().HasMaxLength(20);
            e.Property(c => c.Confirmacao).HasConversion<string>().HasMaxLength(20);
            e.Property(c => c.Parametros).HasMaxLength(100);
            e.Property(c => c.Mensagem).HasMaxLength(200);
            e.Property(c => c.ChaveIdempotencia).HasMaxLength(64);
            e.Property(c => c.RespostaJson).HasColumnType("longtext");
            e.HasIndex(c => new { c.PropriedadeId, c.OcorridoEmUtc });
            // MySQL aceita várias linhas com NULL num índice único: só comandos com chave competem.
            e.HasIndex(c => new { c.UsuarioId, c.ChaveIdempotencia }).IsUnique();
        });

        modelBuilder.Entity<RegistroBackup>(e =>
        {
            e.Property(r => r.Tipo).HasConversion<string>().HasMaxLength(20);
            e.Property(r => r.Arquivo).HasMaxLength(300);
            e.Property(r => r.Mensagem).HasMaxLength(500);
            e.HasIndex(r => new { r.Tipo, r.OcorridoEmUtc });
        });

        modelBuilder.Entity<Usuario>().Property(u => u.PinConfirmacaoHash).HasMaxLength(100);
    }

    /// <summary>Sprint 26 (ADR 0042) — consentimento biométrico e situação da foto em cada leitor.</summary>
    private static void ConfigurarFacial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConsentimentoBiometrico>(e =>
        {
            e.HasOne(c => c.Morador).WithMany().HasForeignKey(c => c.MoradorId).OnDelete(DeleteBehavior.Restrict);
            e.Property(c => c.VersaoTermo).HasMaxLength(20);
            e.HasIndex(c => new { c.MoradorId, c.RevogadoEmUtc });
        });

        modelBuilder.Entity<SincronizacaoFoto>(e =>
        {
            e.HasOne(s => s.Equipamento).WithMany().HasForeignKey(s => s.EquipamentoId).OnDelete(DeleteBehavior.Restrict);
            e.Property(s => s.Situacao).HasConversion<string>().HasMaxLength(20);
            e.Property(s => s.Operacao).HasMaxLength(20);
            e.Property(s => s.Mensagem).HasMaxLength(200);
            e.HasIndex(s => new { s.MoradorId, s.EquipamentoId }).IsUnique();
            e.HasIndex(s => s.Situacao);
        });
    }

    private static readonly ValueConverter<DateTime, DateTime> ConversorUtc =
        new(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    private static readonly ValueConverter<DateTime?, DateTime?> ConversorUtcOpcional =
        new(v => v, v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

    /// <summary>
    /// Sprint 24 (achado na homologação) — o MySQL devolve DateTime sem fuso (Kind = Unspecified), e a
    /// API serializava "2026-11-08T19:01:06" sem o "Z": o celular no Brasil lia como hora local e
    /// mostrava tudo 3 horas adiantado ("agora mesmo" por 3 horas, alerta "às 19:00" às 16:00). Toda
    /// propriedade cujo nome termina em "Utc" (convenção do projeto) — e o Timestamp do registro de
    /// alarme — volta do banco marcada como UTC e sai na API com "Z". Datas de calendário digitadas
    /// pelo usuário (ex.: DataInicial/DataFinal de autorizações) ficam de fora de propósito.
    /// </summary>
    private static void MarcarDatasUtc(ModelBuilder modelBuilder)
    {
        foreach (var entidade in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var propriedade in entidade.GetProperties())
            {
                var ehUtc = propriedade.Name.EndsWith("Utc", StringComparison.Ordinal)
                    || (entidade.ClrType == typeof(RegistroEventoAlarme) && propriedade.Name == nameof(RegistroEventoAlarme.Timestamp));
                if (!ehUtc || propriedade.GetValueConverter() is not null)
                {
                    continue;
                }

                if (propriedade.ClrType == typeof(DateTime))
                {
                    propriedade.SetValueConverter(ConversorUtc);
                }
                else if (propriedade.ClrType == typeof(DateTime?))
                {
                    propriedade.SetValueConverter(ConversorUtcOpcional);
                }
            }
        }
    }

    /// <summary>
    /// Sprint 24 (ADR 0040). AlertaCritico e EventoConectividade são registro de segurança (sem soft
    /// delete, nunca apagados); OcorrenciaId/EquipamentoId do alerta são referência solta, sem FK —
    /// mesmo espírito de Ocorrencia, o alerta precisa existir mesmo que a origem suma.
    /// </summary>
    private static void ConfigurarAlertasConfiaveis(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Propriedade>()
            .Property(p => p.MinutosEscalonamento)
            .HasDefaultValue(2);

        modelBuilder.Entity<AlertaCritico>(e =>
        {
            e.HasOne(a => a.Propriedade).WithMany().HasForeignKey(a => a.PropriedadeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(a => a.ConfirmadoPorUsuario).WithMany().HasForeignKey(a => a.ConfirmadoPorUsuarioId).OnDelete(DeleteBehavior.Restrict);
            e.Property(a => a.Tipo).HasConversion<string>().HasMaxLength(20);
            e.Property(a => a.NivelEscalonamento).HasConversion<string>().HasMaxLength(20);
            e.Property(a => a.Descricao).HasMaxLength(200);
            e.HasIndex(a => new { a.PropriedadeId, a.CriadoEmUtc });
            e.HasIndex(a => a.ProximoEscalonamentoEmUtc);
        });

        modelBuilder.Entity<ContatoEmergencia>(e =>
        {
            e.HasOne(c => c.Propriedade).WithMany().HasForeignKey(c => c.PropriedadeId).OnDelete(DeleteBehavior.Restrict);
            e.Property(c => c.Nome).HasMaxLength(80);
            e.Property(c => c.Telefone).HasMaxLength(20);
            e.HasIndex(c => new { c.PropriedadeId, c.Ordem });
            e.HasQueryFilter(c => !c.Excluido);
        });

        modelBuilder.Entity<EventoConectividade>(e =>
        {
            e.HasOne(c => c.Equipamento).WithMany().HasForeignKey(c => c.EquipamentoId).OnDelete(DeleteBehavior.Restrict);
            e.Property(c => c.Tipo).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(c => new { c.PropriedadeId, c.OcorridoEmUtc });
            e.HasIndex(c => new { c.EquipamentoId, c.OcorridoEmUtc });
        });

        modelBuilder.Entity<ControleEnvioNotificacao>(e =>
        {
            e.HasKey(c => c.Chave);
            e.Property(c => c.Chave).HasMaxLength(80);
        });
    }
}
