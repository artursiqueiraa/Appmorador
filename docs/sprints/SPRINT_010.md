# Sprint 10 — Entregas e Correspondências

## Missão

Execute esta Sprint seguindo integralmente o fluxo definido em `CLAUDE.md`. Antes de iniciar:
ler `CLAUDE.md`, revisar `ROADMAP.md`, revisar as ADRs, revisar a Sprint 9, carregar apenas os
agentes necessários. Não implementar funcionalidades fora deste escopo.

## Objetivo

Construir o domínio completo de Entregas e Correspondências. Esta Sprint deverá permitir
registrar, acompanhar e finalizar entregas destinadas aos moradores. Nenhuma integração física
será realizada.

## Escopo

1. **Entregas** — cada entrega deverá possuir: Morador destinatário, Unidade, Tipo, Descrição,
   Recebido por, Data de recebimento, Data de retirada, Observações, Status.
2. **Tipos** — Correspondência, Encomenda, Delivery, Documento, Mercado, Outro.
3. **Status** — Aguardando Recebimento, Disponível para Retirada, Retirada, Cancelada. Não
   utilizar jobs automáticos — todo o fluxo é comandado por ações do usuário.
4. **Histórico** — registrar automaticamente: Entrega cadastrada, Entrega atualizada, Entrega
   recebida, Entrega retirada, Entrega cancelada. Preparar apenas o domínio, sem tela de
   auditoria.
5. **Dashboard** — indicadores com dados reais: Entregas pendentes, Entregas retiradas, Entregas
   disponíveis, Correspondências cadastradas. Reutilizar componentes existentes.
6. **Mobile** — telas: Entregas, Detalhes da Entrega. Fluxos: Cadastro, Consulta, Alteração de
   status, Exclusão lógica. Seguir integralmente o Design System existente.
7. **UX** — Loading, Empty State, Estados de erro, Feedback de sucesso, Feedback de erro,
   Confirmação antes de cancelar.
8. **Arquitetura** — Clean Architecture, ADRs existentes, Design System, estrutura atual. Não
   duplicar regras de negócio. Toda lógica deve ter um único ponto de manutenção.

## Exclusão

Seguir o padrão estabelecido na Sprint 6. Soft Delete. Nunca excluir fisicamente: Entregas,
Histórico. Registrar: Excluido, DataExclusaoUtc, ExcluidoPorUsuarioId.

## Fora de Escopo

Não implementar: QR Code, Assinatura digital, Foto da entrega, Integração com transportadoras,
Push Notification, WebSocket, Analytics, IA. Registrar qualquer necessidade como backlog.

## Processo Obrigatório

Executar por etapas. Após cada etapa: Build Backend, Build Mobile, Validar regressões, Atualizar
documentação. Somente avançar quando a etapa anterior estiver validada.

## Critérios de Aceite

Backend compilando; Mobile compilando; CRUD de Entregas funcional; Histórico funcional; Dashboard
utilizando dados reais; Soft Delete funcionando; Sem regressões; ADR atualizada; CHANGELOG
atualizado; ROADMAP atualizado; Reviewer aprovando todos os pilares.

## Diretriz de Produto

Ao final desta Sprint deverá ser possível executar o fluxo: Criar uma propriedade → Criar uma
unidade → Cadastrar um morador → Registrar uma entrega → Marcar como disponível para retirada →
Registrar a retirada → Visualizar os indicadores atualizados no Dashboard. Sem qualquer
comunicação com hardware. Toda a arquitetura deverá permanecer preparada para futuras integrações
com portarias virtuais, sistemas de notificações e automações.

## Decisões tomadas na Fase 1

Diferente das Sprints 8 e 9, a própria missão já resolveu as principais ambiguidades no texto —
nenhuma pergunta de esclarecimento foi necessária:

1. **Status 100% manual, sem calculadora híbrida** — a missão fecha explicitamente essa porta
   ("não utilizar jobs automáticos... todo o fluxo será comandado por ações do usuário"), ao
   contrário de Autorizacao (ADR 0011) e Vaga (ADR 0012). `DataRecebimento`/`DataRetirada`
   começam nulas e são preenchidas pelas ações explícitas "marcar disponível"/"registrar
   retirada" — resolvido diretamente pela Diretriz de Produto, que descreve os 3 passos em
   sequência. Ver ADR 0013.
2. **Entregas são criadas/listadas no nível da Propriedade**, não por morador individual — visão
   unificada de "central de entregas", variação deliberada do padrão de aninhamento sob o
   Morador usado em Credencial/Veículo (Sprints 7/9). Ver ADR 0013.
