using AppMorador.Application.Auditoria;
using AppMorador.Application.Autenticacao;
using AppMorador.Application.Autorizacoes;
using AppMorador.Application.Rbac;
using AppMorador.Application.Cameras;
using AppMorador.Application.ControlId;
using AppMorador.Application.Credenciais;
using AppMorador.Application.Dashboard;
using AppMorador.Application.Entregas;
using AppMorador.Application.Equipamentos;
using AppMorador.Application.Eventos;
using AppMorador.Application.Intelbras;
using AppMorador.Application.Jfl;
using AppMorador.Application.Moradores;
using AppMorador.Application.Operacional;
using AppMorador.Application.Painel;
using AppMorador.Application.Painel.Diagnostico;
using AppMorador.Application.Painel.Equipamentos;
using AppMorador.Application.Painel.VinculosEquipamento;
using AppMorador.Application.PermissoesAcesso;
using AppMorador.Application.PermissoesVeiculares;
using AppMorador.Application.PontosAcesso;
using AppMorador.Application.Propriedades;
using AppMorador.Application.Provisionamentos;
using AppMorador.Application.Unidades;
using AppMorador.Application.Vagas;
using AppMorador.Application.Veiculos;
using AppMorador.Application.Visitantes;
using AppMorador.Application.VinculosVeiculoVaga;
using AppMorador.Domain.Repositories;
using AppMorador.Infrastructure.ControlId;
using AppMorador.Infrastructure.Dashboard;
using AppMorador.Infrastructure.Eventos;
using AppMorador.Infrastructure.Intelbras;
using AppMorador.Infrastructure.Jfl;
using AppMorador.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace AppMorador.Infrastructure.Identity;

/// <summary>Registra Autenticacao/Propriedade/Dashboard (Application + Infrastructure) no DI, mesmo padrao de AddJflServer/AddSnapshotCapture.</summary>
public static class AuthServiceCollectionExtensions
{
    public static IServiceCollection AddAuthModule(this IServiceCollection services, Action<JwtOptions> configure)
    {
        var options = new JwtOptions { Key = string.Empty, Issuer = string.Empty, Audience = string.Empty };
        configure(options);

        if (string.IsNullOrWhiteSpace(options.Key))
        {
            throw new InvalidOperationException(
                "Jwt:Key nao configurada. Configure via user-secrets em dev (dotnet user-secrets set \"Jwt:Key\" \"...\") " +
                "ou variavel de ambiente Jwt__Key em producao — nunca no appsettings.json.");
        }

        services.AddSingleton(options);

        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
        services.AddScoped<IPropriedadeRepositorio, PropriedadeRepositorio>();
        services.AddScoped<IUnidadeRepositorio, UnidadeRepositorio>();
        services.AddScoped<IMoradorRepositorio, MoradorRepositorio>();
        services.AddScoped<ICredencialRepositorio, CredencialRepositorio>();
        services.AddScoped<IPontoAcessoRepositorio, PontoAcessoRepositorio>();
        services.AddScoped<IPermissaoAcessoRepositorio, PermissaoAcessoRepositorio>();
        services.AddScoped<IHistoricoCredencialRepositorio, HistoricoCredencialRepositorio>();
        services.AddScoped<IVisitanteRepositorio, VisitanteRepositorio>();
        services.AddScoped<IAutorizacaoRepositorio, AutorizacaoRepositorio>();
        services.AddScoped<IHistoricoVisitanteRepositorio, HistoricoVisitanteRepositorio>();
        services.AddScoped<IVeiculoRepositorio, VeiculoRepositorio>();
        services.AddScoped<IVagaRepositorio, VagaRepositorio>();
        services.AddScoped<IVinculoVeiculoVagaRepositorio, VinculoVeiculoVagaRepositorio>();
        services.AddScoped<IPermissaoVeicularRepositorio, PermissaoVeicularRepositorio>();
        services.AddScoped<IHistoricoVeiculoRepositorio, HistoricoVeiculoRepositorio>();
        services.AddScoped<IHistoricoVagaRepositorio, HistoricoVagaRepositorio>();
        services.AddScoped<IEntregaRepositorio, EntregaRepositorio>();
        services.AddScoped<IHistoricoEntregaRepositorio, HistoricoEntregaRepositorio>();
        services.AddScoped<IEquipamentoRepositorio, EquipamentoRepositorio>();
        services.AddScoped<IEventoEquipamentoRepositorio, EventoEquipamentoRepositorio>();
        services.AddScoped<IStatusCentralJflRepositorio, StatusCentralJflRepositorio>();
        services.AddScoped<ICentralRepositorio, CentralRepositorio>();
        services.AddScoped<ISnapshotOperacionalRepositorio, SnapshotOperacionalRepositorio>();
        services.AddScoped<ICameraRepositorio, CameraRepositorio>();
        services.AddScoped<ICameraServico, CameraServico>();

        // Sprint 21 (ADR 0021/0025/0026/0027/0028) — RBAC Master.
        services.AddScoped<IUsuarioPropriedadeRepositorio, UsuarioPropriedadeRepositorio>();
        services.AddScoped<IUsuarioPropriedadePermissaoRepositorio, UsuarioPropriedadePermissaoRepositorio>();
        services.AddScoped<IPropriedadeFeatureFlagRepositorio, PropriedadeFeatureFlagRepositorio>();
        services.AddScoped<IModeloEquipamentoRepositorio, ModeloEquipamentoRepositorio>();
        services.AddScoped<IProvisionamentoRepositorio, ProvisionamentoRepositorio>();
        services.AddScoped<IAuditoriaMasterRepositorio, AuditoriaMasterRepositorio>();
        services.AddScoped<IAuditoriaService, AuditoriaService>();
        services.AddScoped<IPermissaoService, PermissaoService>();
        services.AddScoped<IUsuarioInternoServico, UsuarioInternoServico>();
        services.AddScoped<IModeloEquipamentoServico, ModeloEquipamentoServico>();
        services.AddScoped<IProvisionamentoServico, ProvisionamentoServico>();
        services.AddScoped<IPropriedadeFeatureFlagServico, PropriedadeFeatureFlagServico>();
        services.AddScoped<IUsuarioPropriedadePermissaoServico, UsuarioPropriedadePermissaoServico>();
        services.AddScoped<IImpersonationServico, ImpersonationServico>();

        // Sprint 22A (ADR 0029) — leitura global cross-tenant para o Painel Web (Master/Suporte).
        services.AddScoped<IProprietarioServico, ProprietarioServico>();
        services.AddScoped<IDashboardOperacionalServico, DashboardOperacionalServico>();

        // Sprint 22B (ADR 0031) — módulos novos do Painel Web (Equipamentos globais + Provisionamentos).
        services.AddScoped<IEquipamentoAdminServico, EquipamentoAdminServico>();
        services.AddScoped<IVinculoEquipamentoPropriedadeRepositorio, VinculoEquipamentoPropriedadeRepositorio>();
        services.AddScoped<IVinculoEquipamentoServico, VinculoEquipamentoServico>();
        services.AddScoped<IDiagnosticoEquipamentoRepositorio, DiagnosticoEquipamentoRepositorio>();
        services.AddScoped<IDiagnosticoServico, DiagnosticoServico>();
        services.AddScoped<IConsultaDashboardServico, ConsultaDashboardServico>();
        services.AddScoped<IFonteEventos, JflFonteEventos>();
        services.AddScoped<IFonteEventos, EquipamentoFonteEventos>();
        services.AddScoped<IFonteEventos, IntelbrasFonteEventos>();

        // Sprint 11 — Migracao da Integracao Control iD (ADR 0014). Timeout curto:
        // equipamentos podem estar offline/inacessiveis, e o teste de conexao precisa
        // falhar rapido em vez de travar a requisicao do usuario.
        services.AddHttpClient(ControlIdProvider.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(5);
        });
        services.AddScoped<ICriptografiaSimetrica, DataProtectionCriptografiaSimetrica>();
        services.AddScoped<IControlIdProvider, ControlIdProvider>();

        // Sprint 12 — Migracao JFL Active 100 Bus (ADR 0015). Nao registra nenhum
        // HttpClient (a central e quem disca para o AppMorador, nunca o contrario) —
        // JflProvider so consome SessionManager e os *CommandService ja registrados
        // por AddJflServer (AppMorador.Jfl).
        services.AddScoped<IJflProvider, JflProvider>();
        services.AddScoped<IJflComandoServico, JflComandoServico>();

        // Sprint 15 — Integracao Intelbras: Prova Definitiva da Arquitetura (ADR 0018).
        // Modelada via API HTTP local (dial-out, mesmo padrao do Control iD) com
        // vocabulario de comando de central de alarme (mesmo padrao do JFL) — prova
        // que os dois eixos sao independentes. Timeout curto pelo mesmo motivo do
        // Control iD: a central pode estar offline/inacessivel.
        services.AddHttpClient(IntelbrasProvider.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(5);
        });
        services.AddScoped<IIntelbrasProvider, IntelbrasProvider>();
        services.AddScoped<IIntelbrasComandoServico, IntelbrasComandoServico>();

        services.AddScoped<IAutenticacaoServico, AutenticacaoServico>();
        services.AddScoped<IPropriedadeServico, PropriedadeServico>();
        services.AddScoped<IUnidadeServico, UnidadeServico>();
        services.AddScoped<IMoradorServico, MoradorServico>();
        services.AddScoped<ICredencialServico, CredencialServico>();
        services.AddScoped<IPontoAcessoServico, PontoAcessoServico>();
        services.AddScoped<IPermissaoAcessoServico, PermissaoAcessoServico>();
        services.AddScoped<IVisitanteServico, VisitanteServico>();
        services.AddScoped<IAutorizacaoServico, AutorizacaoServico>();
        services.AddScoped<IVeiculoServico, VeiculoServico>();
        services.AddScoped<IVagaServico, VagaServico>();
        services.AddScoped<IVeiculoVagaServico, VeiculoVagaServico>();
        services.AddScoped<IPermissaoVeicularServico, PermissaoVeicularServico>();
        services.AddScoped<IEntregaServico, EntregaServico>();
        services.AddScoped<IEquipamentoServico, EquipamentoServico>();
        services.AddScoped<IEquipamentoIntegracaoServico, EquipamentoIntegracaoServico>();
        services.AddScoped<IDashboardServico, DashboardServico>();
        services.AddScoped<IEventosServico, EventosServico>();

        // Sprint 13 — Camada Operacional Unificada (ADR 0016). Fluxo obrigatorio:
        // Estado Bruto -> Classificador Operacional -> Snapshot Operacional ->
        // Dashboard/Mobile. Nenhum Provider e consumido aqui.
        services.AddScoped<IClassificadorOperacionalServico, ClassificadorOperacionalServico>();
        services.AddScoped<ISnapshotOperacionalServico, SnapshotOperacionalServico>();

        return services;
    }
}
