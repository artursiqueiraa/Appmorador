# Sprint 9 — Veículos e Garagens

## Missão

Execute esta Sprint seguindo integralmente o fluxo definido em `CLAUDE.md`. Antes de iniciar:
ler `CLAUDE.md`, revisar `ROADMAP.md`, revisar as ADRs, revisar a Sprint 8, carregar apenas os
agentes necessários. Não implementar funcionalidades fora deste escopo.

## Objetivo

Construir o domínio completo de Veículos e Garagens do AppMorador. Esta Sprint deverá consolidar
toda a estrutura responsável pelo gerenciamento de veículos, vagas e permissões veiculares.
Nenhuma integração física será realizada. Todo o domínio deverá permanecer preparado para futuras
integrações com Control iD, Intelbras, Hikvision, JFL e OCR de placas.

## Escopo

1. **Veículos** — cada veículo pertence obrigatoriamente a um morador. Campos mínimos: Placa,
   Marca, Modelo, Cor, Ano, Observações, Tipo, Status (Ativo/Suspenso/Inativo). Preparar estrutura
   para futuras permissões automáticas.
2. **Tipos de Veículo** — Carro, Moto, Caminhonete, Van, Caminhão, Bicicleta, Outro.
3. **Vagas** — domínio independente. A vaga pertence diretamente à Propriedade, nunca ao Morador.
   Número, Bloco, Andar, Coberta, Tipo (Morador/Visitante/Comercial/Serviço), Status (Livre/
   Ocupada/Bloqueada/Reservada), Observações.
4. **Vinculação Veículo ↔ Vaga** — relacionamento independente. Associar veículo à vaga, alterar
   vaga, remover vínculo, histórico da alteração. Não armazenar a vaga diretamente dentro da
   entidade Veículo. Preparar para futuras vagas rotativas.
5. **Permissões Veiculares** — cada veículo poderá possuir permissões para Garagem Principal,
   Garagem Secundária, Visitantes, Área Comercial. Nesta Sprint apenas estruturar o domínio, sem
   comunicar com equipamentos.
6. **Histórico** — registrar automaticamente: Veículo criado/alterado/removido, Vaga criada/
   alterada/bloqueada, Veículo vinculado/desvinculado, Permissão alterada. Preparar apenas o
   domínio, sem interface de auditoria.
7. **Dashboard** — indicadores com dados reais: Total de veículos, Veículos ativos, Vagas
   cadastradas, Vagas livres, Vagas ocupadas. Reutilizar componentes existentes.
8. **Mobile** — telas: Veículos, Vagas. Fluxos: Cadastro, Edição, Exclusão lógica, Vinculação
   veículo ↔ vaga. Seguir integralmente o Design System existente.
9. **UX** — Loading, Empty State, Estados de erro, Feedback de sucesso, Feedback de exclusão,
   Confirmação antes da exclusão, Validação de placa duplicada, Validação de vaga inexistente.
10. **Arquitetura** — Clean Architecture, ADRs existentes, Design System, estrutura atual. Não
    duplicar regras, não criar serviços paralelos. Toda regra de negócio deve ter um único ponto
    de manutenção.

## Exclusão

Seguir integralmente o padrão da Sprint 6. Soft Delete. Nunca excluir fisicamente: Veículos,
Vagas, Permissões, Vínculos. Registrar: Excluido, DataExclusaoUtc, ExcluidoPorUsuarioId.

## Fora de Escopo

Não implementar: OCR, Leitura automática de placas, Portão automático, Câmeras, Reconhecimento
veicular, Integração Control iD, Integração Intelbras, Integração Hikvision, Integração JFL, IA,
Analytics, Push Notification, WebSocket. Registrar qualquer necessidade como backlog.

## Processo Obrigatório

Executar a Sprint em pequenas etapas. Após cada etapa: Build Backend, Build Mobile, Validar
regressões, Atualizar documentação. Somente avançar quando a etapa anterior estiver completamente
validada.

## Critérios de Aceite

Backend compilando; Mobile compilando; CRUD de Veículos funcional; CRUD de Vagas funcional;
Vinculação Veículo ↔ Vaga funcional; Dashboard utilizando dados reais; Soft Delete funcionando;
Histórico funcionando; Sem regressões; ADR atualizada; CHANGELOG atualizado; ROADMAP atualizado;
Reviewer aprovando todos os pilares.

## Diretriz de Produto

Ao final desta Sprint deverá ser possível executar o fluxo: Criar uma propriedade → Criar uma
unidade → Cadastrar um morador → Cadastrar um veículo → Cadastrar uma vaga → Vincular veículo à
vaga → Alterar a vaga quando necessário → Visualizar todas as informações refletidas no
Dashboard. Sem qualquer comunicação com hardware. Toda a arquitetura deverá permanecer preparada
para futuras integrações com OCR de placas, portões automáticos, Control iD, Intelbras, Hikvision
e JFL.

## Decisões tomadas na Fase 1 (aprovação do usuário antes da implementação)

1. **Status da Vaga é híbrido** — Livre/Ocupada são derivados automaticamente da existência de
   vínculos ativos entre veículos e vagas (computados em tempo de leitura, sem job/scheduler);
   Bloqueada/Reservada representam decisões operacionais e sempre têm prioridade sobre o cálculo
   automático. Ver ADR 0012.
2. **Permissões Veiculares reaproveitam `PontoAcesso`** (Sprint 7, ADR 0010) em vez de um enum
   próprio de áreas — consistente com as ADRs anteriores, evita duplicação de conceitos.
   `PontoAcesso` ganhou o campo `Tipo` (Geral/Veicular); as permissões veiculares só podem apontar
   para um Ponto de Acesso do tipo Veicular. Ver ADR 0012.
