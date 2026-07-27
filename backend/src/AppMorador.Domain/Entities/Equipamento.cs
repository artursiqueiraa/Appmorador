using AppMorador.Domain.Common;

namespace AppMorador.Domain.Entities;

/// <summary>
/// Um equipamento físico de integração (controlador de acesso, central, câmera com
/// SDK próprio) pertencente diretamente a uma <see cref="Propriedade"/> — nunca a uma
/// Unidade. Sprint 11: primeiro fabricante com Provider real é Control iD; os campos
/// aqui são deliberadamente genéricos (Ip/Porta/Usuario/Senha/Identificador) para que
/// futuros fabricantes (Intelbras, Hikvision, Dahua, JFL) reutilizem a mesma entidade,
/// só trocando o Provider por trás — ver ADR 0014.
/// </summary>
/// <remarks>
/// Sprint 12 — Migração JFL: Ip/Porta/Usuario/SenhaCriptografada viraram opcionais.
/// Fabricantes que discam para o equipamento (Control iD) sempre os preenchem; JFL é
/// o oposto — a central é quem disca para o AppMorador, então não há IP/porta/usuário
/// de conexão de saída (só <see cref="Identificador"/>, o número de série, correlaciona
/// com a sessão TCP já aberta). Ver ADR 0015.
/// </remarks>
public class Equipamento : EntidadeComSoftDelete
{
    public Guid Id { get; set; }

    public Guid PropriedadeId { get; set; }

    public Propriedade? Propriedade { get; set; }

    public required string Nome { get; set; }

    /// <summary>
    /// Sprint 21 (ADR 0027) — substitui o antigo campo <c>Modelo</c> (texto livre) por
    /// uma referência ao catálogo (<see cref="Entities.ModeloEquipamento"/>), de onde
    /// vêm as capacidades reais do equipamento. Nullable: nem todo equipamento
    /// cadastrado (inclusive dados anteriores a esta Sprint) tem um modelo resolvido.
    /// </summary>
    public Guid? ModeloEquipamentoId { get; set; }

    public ModeloEquipamento? ModeloEquipamento { get; set; }

    public required FabricanteEquipamento Fabricante { get; set; }

    public string? Ip { get; set; }

    public int? Porta { get; set; }

    public string? Usuario { get; set; }

    /// <summary>
    /// Criptografada em repouso via <see cref="Microsoft.AspNetCore.DataProtection.IDataProtector"/>
    /// — nunca armazenada nem devolvida em texto puro. Só é decifrada dentro do
    /// Provider do fabricante correspondente, no momento exato da chamada ao
    /// equipamento. Opcional — nem todo fabricante exige uma (ex.: JFL Active 100
    /// Bus não usa senha nos comandos de arme/desarme/PGM/inibição hoje).
    /// </summary>
    public string? SenhaCriptografada { get; set; }

    /// <summary>Identificador interno do equipamento no fabricante (ex.: número de série) — opcional, nem todo fabricante expõe um.</summary>
    public string? Identificador { get; set; }

    public required StatusEquipamento Status { get; set; }

    /// <summary>Preenchida só por uma sincronização manual bem-sucedida — nunca por job automático (fora de escopo).</summary>
    public DateTime? UltimaSincronizacaoUtc { get; set; }

    public required DateTime CreatedAtUtc { get; set; }
}
