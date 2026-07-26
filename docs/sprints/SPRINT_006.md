# Sprint 6 — Domínio do Produto: Propriedades, Unidades e Moradores

## Missão

Execute esta Sprint seguindo integralmente o fluxo definido em `CLAUDE.md`. Antes de iniciar:
ler `CLAUDE.md`, revisar `ROADMAP.md`, revisar as ADRs, revisar a Sprint 5, carregar apenas os
agentes necessários. Não implementar funcionalidades fora deste escopo.

## Objetivo

Esta Sprint estabelece o domínio principal do AppMorador. O objetivo não é criar apenas CRUDs,
mas construir a base que sustentará todas as funcionalidades futuras do sistema. Ao final desta
Sprint o AppMorador deverá representar corretamente a estrutura de uma residência, condomínio ou
estabelecimento comercial. Todo desenvolvimento deve ser orientado ao domínio do negócio.

## Escopo

1. **Gestão de Propriedades** — tipos suportados: Residência, Condomínio, Loja, Escritório,
   Comércio. Cada propriedade pode ter múltiplas unidades.
2. **Gestão de Unidades** — exemplos: Casa, Apartamento, Loja, Sala Comercial, Bloco. Cada
   unidade pertence obrigatoriamente a uma propriedade.
3. **Gestão de Moradores** — cada morador pode ter: Nome, Foto (estrutura preparada), Telefone,
   E-mail, Documento, Unidade, Status, Observações. Não implementar biometria — só preparar o
   domínio.
4. **Relacionamentos** — Propriedade → Unidades → Moradores, com integridade e consistência.
5. **Dashboard** — usar dados reais: total de propriedades, total de unidades, total de
   moradores. Reutilizar componentes existentes, não criar novo Dashboard.
6. **Mobile** — telas Propriedades, Unidades, Moradores, seguindo integralmente o Design System
   existente. Nenhum redesign nesta Sprint.
7. **UX** — todos os fluxos com Loading, Empty State, Validação, Feedback de sucesso, Feedback
   de erro.
8. **Arquitetura** — respeitar Clean Architecture, ADRs existentes, Design System, estrutura do
   projeto. Não criar soluções paralelas, não duplicar regras de negócio.

## Fora de Escopo

Controle de acesso, Visitantes, Entregas, Veículos, QR Code, Reconhecimento facial, Push
Notification, WebSocket, Integrações com hardware, Analytics. Registrar qualquer necessidade
como backlog.

## Processo Obrigatório

Executar a Sprint por etapas. Após cada etapa: Build Backend, Build Mobile, Validar regressões,
Atualizar documentação quando necessário. Só avançar quando a etapa anterior estiver
completamente validada.

## Critérios de Aceite

Backend compilando; Mobile compilando; CRUD de propriedades funcional; CRUD de unidades
funcional; CRUD de moradores funcional; Dashboard utilizando dados reais; Sem regressões;
Documentação atualizada; Reviewer aprovando todos os pilares.

## Diretriz de Produto

Ao final deverá ser possível executar o fluxo: Criar uma propriedade → Cadastrar uma unidade →
Cadastrar moradores → Visualizar essas informações refletidas no Dashboard. Sem dados fictícios
na interface — sempre dados reais persistidos. Todo o desenvolvimento deve preparar naturalmente
as próximas Sprints de Visitantes, Veículos, Entregas, Controle de acesso, Reconhecimento facial,
Integrações com equipamentos, sem antecipar essas funcionalidades.

## Decisões tomadas na Fase 1 (aprovação do usuário antes da implementação)

1. **Exclusão lógica (soft delete), não física, não cascade físico** — `Excluido`/
   `DataExclusaoUtc`/`ExcluidoPorUsuarioId` em Propriedade/Unidade/Morador; nada excluído aparece
   nas consultas normais; cascade lógico explícito em código de aplicação; exclusão física
   reservada para rotina administrativa futura; arquitetura preparada para uma futura Lixeira/
   Restauração, sem implementá-la agora. Ver ADR 0009.
2. **"Total de propriedades" na tela Propriedades/seletor**, não forçado no contrato do
   Dashboard (que é por propriedade, não por conta).
3. **`TipoUnidade` mais amplo**: Casa, Apartamento, Loja, SalaComercial, Galpao, Quiosque,
   Escritorio, Outro — 8 valores em vez dos 5 exemplos literais da missão, para reduzir risco de
   nova migration de enum numa Sprint futura próxima.
4. **`TipoPropriedade` reaproveitado sem alteração** — "Loja/Escritório/Comércio" da missão já
   são cobertos pelo valor `Comercial` existente (mobile já descreve esse valor como "Lojas,
   escritórios, clínicas e pequenos negócios").
