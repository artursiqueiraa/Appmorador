# ADR 0010 — Domínio de Controle de Acesso: Credencial + PermissaoAcesso + PontoAcesso

**Data**: 2026-07-21

## Contexto

A Sprint 7 pediu o domínio completo de Controle de Acesso do AppMorador — credenciais (Facial,
Tag RFID, QR Code, PIN, Biometria, Chave Virtual), permissões (status Ativa/Suspensa/Expirada/
Revogada), regras de acesso (dia da semana/horário/data) e pontos de acesso (Portão Principal,
Garagem, Piscina, etc.) — **sem** nenhuma integração com equipamento físico (Control iD,
Intelbras, Hikvision, JFL fica para uma Sprint futura).

## Problema

A missão descreve dois conceitos de "permissão" de formas aparentemente conflitantes: a seção de
escopo fala em credenciais que "poderão possuir" um status (Ativa/Suspensa/Expirada/Revogada),
enquanto o diagrama de relacionamento lista "Permissões" como um nível hierárquico próprio entre
Credenciais e Pontos de Acesso, portador das regras de dia/horário. Modelar isso como uma única
entidade (`Credencial` carregando também as regras de dia/horário) obrigaria uma credencial a ter
no máximo um conjunto de regras — mas o mesmo morador pode precisar de regras diferentes por
ponto de acesso (ex.: acesso à Garagem 24h, mas à Academia só em horário comercial).

## Alternativas consideradas

- **Só `Credencial.Status`, sem entidade de permissão separada**: mais simples, mas impede regras
  diferentes por Ponto de Acesso para a mesma credencial — não atende ao requisito de "cada
  credencial poderá possuir acesso a um ou mais pontos" com regras próprias por ponto.
- **Só uma entidade `PermissaoAcesso` (sem `Credencial.Status`)**: o status Ativa/Suspensa/
  Expirada/Revogada ficaria implícito por ausência/presença de permissões, sem um "kill switch"
  único e explícito para a credencial inteira — dificultaria suspender uma credencial em todos os
  pontos de uma vez sem apagar/recriar N registros de permissão.
- **`Credencial.Status` (kill-switch geral) + `PermissaoAcesso` como entidade de vínculo por Ponto
  de Acesso, com as regras de dia/horário/data** (decisão adotada): resolve os dois requisitos ao
  mesmo tempo — `Status` responde "esta credencial pode ser usada, em qualquer lugar?" e
  `PermissaoAcesso` responde "neste ponto específico, em quais dias/horários?". Confirmado
  explicitamente com o usuário antes da implementação (Fase 1 da Sprint 7).

## Decisão

- **`Credencial`**: pertence a um `Morador` (obrigatório), tem `Tipo` (enum `TipoCredencial`:
  Facial/TagRfid/QrCode/Pin/Biometria/ChaveVirtual, imutável após criação) e `Status` (enum
  `StatusCredencial`: Ativa/Suspensa/Expirada/Revogada).
- **`PontoAcesso`**: pertence direto à `Propriedade` (não à `Unidade`) — confirmado explicitamente
  com o usuário na Fase 1. Justificativa: pontos de acesso como "Portão Principal" ou "Piscina"
  são infraestrutura compartilhada de toda a propriedade, não de uma unidade específica.
- **`PermissaoAcesso`**: entidade de vínculo `Credencial` ↔ `PontoAcesso`, carregando
  `DiasPermitidos` (enum `[Flags] DiaSemana`), `HorarioInicial`/`HorarioFinal` (`TimeOnly?`) e
  `DataInicial`/`DataFinal` (`DateTime?`, vigência). Uma credencial pode ter N `PermissaoAcesso`,
  uma por Ponto de Acesso (ou várias para o mesmo ponto com regras diferentes, se necessário no
  futuro — nada no modelo impede). Validação de negócio: o `PontoAcesso` de uma `PermissaoAcesso`
  precisa pertencer à mesma `Propriedade` da `Credencial` (via `Morador→Unidade→Propriedade`) —
  nunca é possível vincular uma credencial a um ponto de acesso de outra propriedade.
- **`HistoricoCredencial`**: registro de auditoria pura (criação/suspensão/reativação/revogação/
  expiração de credencial; criação/alteração/exclusão de permissão) — não estende
  `EntidadeComSoftDelete` (mesmo padrão de `RegistroEventoAlarme`: histórico nunca é excluído,
  nem logicamente).
- Todas as 3 entidades operacionais (`Credencial`, `PontoAcesso`, `PermissaoAcesso`) herdam
  `EntidadeComSoftDelete` (ADR 0009) — mesmo padrão de exclusão lógica do domínio principal,
  cascateado em código de aplicação: excluir uma `Propriedade`/`Unidade`/`Morador` cascateia até
  suas Credenciais e PermissoesAcesso; excluir um `PontoAcesso` cascateia até as PermissoesAcesso
  que apontavam para ele; excluir uma `Credencial` cascateia até suas PermissoesAcesso.

## Consequências

- O domínio está pronto para receber integrações reais (Control iD, Intelbras, Hikvision, JFL)
  numa Sprint futura sem redesenho: uma integração real precisaria apenas 1) popular/sincronizar
  `Credencial`/`PermissaoAcesso` a partir do equipamento (ou o inverso) e 2) traduzir
  `PermissaoAcesso` para o formato de regra do fabricante — a "inteligência" de negócio (quem tem
  acesso a onde, quando) já existe e não muda.
- `Credencial.Tipo` é imutável por design — trocar o tipo de uma credencial (ex.: de Facial para
  PIN) significa criar uma credencial nova, não editar a existente. Reduz ambiguidade sobre o que
  "editar tipo" significaria para uma integração real futura (um cadastro facial não vira PIN).
- Nenhuma comunicação com hardware, SDK de fabricante, reconhecimento facial real, QR Code
  funcional, leitor biométrico real ou Tag física foi implementada nesta Sprint — tudo registrado
  como backlog em `docs/roadmap/ROADMAP.md` e `docs/DIVIDA_TECNICA.md`.

## Impactos

`Domain/Entities/{Credencial,TipoCredencial,StatusCredencial,PontoAcesso,PermissaoAcesso,
DiaSemana,HistoricoCredencial,TipoEventoHistorico}.cs`; `Domain/Repositories/{ICredencialRepositorio,
IPontoAcessoRepositorio,IPermissaoAcessoRepositorio,IHistoricoCredencialRepositorio}.cs`;
`Infrastructure/Persistence/{CredencialRepositorio,PontoAcessoRepositorio,
PermissaoAcessoRepositorio,HistoricoCredencialRepositorio,AppDbContext}.cs`;
`Application/{Credenciais,PontosAcesso,PermissoesAcesso}/*.cs`; `Application/Propriedades/
PropriedadeServico.cs`, `Application/Unidades/UnidadeServico.cs`, `Application/Moradores/
MoradorServico.cs` (cascade expandido); `Api/Controllers/{CredenciaisController,
PontosAcessoController,PermissoesAcessoController}.cs`.

## Como revisar futuramente

Ao implementar a integração real com Control iD/Intelbras/Hikvision/JFL (fase futura já anunciada
no `ROADMAP.md`), reaproveitar `Credencial`/`PermissaoAcesso`/`PontoAcesso` como estão — não criar
um modelo paralelo. Se a integração exigir campos específicos de fabricante (ex.: ID do usuário no
equipamento Control iD, caminho da foto sincronizada), avaliar se entram nas entidades existentes
(campo opcional, populado só quando a integração existir) ou numa tabela de mapeamento à parte
(`CredencialIntegracaoExterna` ou similar) — decisão dessa Sprint futura, não desta.
