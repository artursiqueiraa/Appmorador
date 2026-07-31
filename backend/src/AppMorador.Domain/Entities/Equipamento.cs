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

    /// <summary>Sprint 22B — endereço físico da interface de rede, opcional (nem todo equipamento expõe/precisa).</summary>
    public string? MacAddress { get; set; }

    /// <summary>Sprint 22B — anotação livre do Técnico/Master sobre o equipamento (cadastro no Painel Web).</summary>
    public string? Observacoes { get; set; }

    public required StatusEquipamento Status { get; set; }

    /// <summary>
    /// Sprint 22B (ADR 0031) — estado administrativo/de ciclo de vida (Ativo/EmManutencao/
    /// Inativo/Defeituoso), decidido por um Técnico/Master via Painel Web — nunca confundir com
    /// <see cref="Status"/> (conectividade). Default Ativo para equipamentos já cadastrados
    /// (backfill na migration) e para novos cadastros.
    /// </summary>
    public EstadoOperacionalEquipamento EstadoOperacional { get; set; } = EstadoOperacionalEquipamento.Ativo;

    /// <summary>Preenchida só por uma sincronização manual bem-sucedida — nunca por job automático (fora de escopo).</summary>
    public DateTime? UltimaSincronizacaoUtc { get; set; }

    /// <summary>
    /// Sprint 22C.2 — dicionário chave/valor (serializado em JSON) com o que o Provider do
    /// fabricante conseguiu descobrir de verdade sobre o equipamento (ex.: Control iD:
    /// Firmware/Hostname; JFL: Modelo/MAC, vindos do handshake da central). Deliberadamente
    /// livre (não um conjunto fixo de colunas) porque cada fabricante descobre coisas
    /// diferentes, e a lista de fabricantes cresce — nunca inventar aqui um campo que o
    /// Provider não devolveu de verdade (ver ADR 0031, mesmo princípio de nunca fabricar dado).
    /// </summary>
    public string? InformacoesDescobertasJson { get; set; }

    /// <summary>Quando `InformacoesDescobertasJson` foi preenchido pela última vez — nulo se nunca houve descoberta automática bem-sucedida.</summary>
    public DateTime? UltimaDescobertaUtc { get; set; }

    public required DateTime CreatedAtUtc { get; set; }
}
