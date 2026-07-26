# Sprint 7 — Controle de Acesso Inteligente (Domínio)

## Missão

Execute esta Sprint seguindo integralmente o fluxo definido em `CLAUDE.md`. Antes de iniciar:
ler `CLAUDE.md`, revisar `ROADMAP.md`, revisar as ADRs, revisar a Sprint 6, carregar apenas os
agentes necessários. Não implementar funcionalidades fora deste escopo.

## Objetivo

Construir o domínio completo de Controle de Acesso do AppMorador. Esta Sprint NÃO realizará
integração com equipamentos físicos. O objetivo é preparar toda a estrutura de negócio para
futuras integrações com: Control iD, Intelbras, Hikvision, JFL. Ao final desta Sprint o sistema
deverá possuir toda a inteligência de permissões, credenciais e regras de acesso. A integração
física ocorrerá apenas em Sprint futura.

## Escopo

1. **Credenciais** — tipos: Facial, Tag RFID, QR Code, PIN, Biometria, Chave Virtual (estrutura
   preparada). Cada credencial pertence obrigatoriamente a um morador.
2. **Permissões de Acesso** — cada credencial poderá possuir: Ativa, Suspensa, Expirada,
   Revogada. Preparar o domínio para futuras sincronizações.
3. **Regras de Acesso** — cada credencial poderá possuir restrições de: Dias da semana, Horário
   inicial, Horário final, Data inicial, Data final. Não comunicar com equipamentos nesta Sprint.
4. **Pontos de Acesso** — exemplos: Portão Principal, Garagem, Hall, Academia, Piscina, Salão de
   Festas, Entrada de Serviço, Loja, Escritório. Cada credencial poderá possuir acesso a um ou
   mais pontos.
5. **Relacionamentos** — Propriedade → Unidade → Morador → Credenciais → Permissões → Pontos de
   Acesso. Garantir consistência em todo o agregado.
6. **Histórico** — registrar internamente todas as alterações (Credencial criada/revogada/
   reativada, Permissão alterada, Horário alterado). Não implementar auditoria visual — preparar
   apenas o domínio.
7. **Dashboard** — indicadores: Total de credenciais, Credenciais ativas, Credenciais suspensas,
   Pontos de acesso cadastrados. Reutilizar componentes existentes.
8. **Mobile** — telas: Credenciais, Permissões, Pontos de Acesso. Seguir integralmente o Design
   System existente. Nenhum redesign.
9. **UX** — Loading, Empty State, Validação, Feedback de sucesso, Feedback de erro, Confirmação
   antes de revogar credenciais.
10. **Arquitetura** — Clean Architecture, ADRs existentes, Design System, estrutura atual. Não
    criar soluções paralelas, não duplicar regras de negócio.

## Exclusão

Toda exclusão continuará seguindo o padrão definido na Sprint 6. Soft Delete. Nunca excluir
fisicamente: Credenciais, Permissões, Pontos de acesso. Registrar: Excluido, DataExclusaoUtc,
ExcluidoPorUsuarioId.

## Fora de Escopo

Não implementar: Comunicação TCP, Comunicação HTTP com equipamentos, SDKs de fabricantes,
Reconhecimento facial, Cadastro facial real, QR Code funcional, Tag física, Leitores biométricos,
Push Notification, WebSocket, Analytics, IA. Registrar qualquer necessidade como backlog.

## Processo Obrigatório

Executar a Sprint por etapas. Após cada etapa: Build Backend, Build Mobile, Validar regressões,
Atualizar documentação quando necessário. Somente avançar quando a etapa anterior estiver
completamente validada.

## Critérios de Aceite

Backend compilando; Mobile compilando; CRUD de Credenciais funcional; CRUD de Permissões
funcional; CRUD de Pontos de Acesso funcional; Dashboard utilizando dados reais; Soft Delete
funcionando; Sem regressões; Documentação atualizada; Reviewer aprovando todos os pilares.

## Diretriz de Produto

Esta Sprint deverá preparar completamente o domínio de Controle de Acesso. Ao final deverá ser
possível executar o seguinte fluxo: Criar uma propriedade → Criar uma unidade → Cadastrar um
morador → Cadastrar uma credencial → Definir permissões → Definir pontos de acesso → Visualizar
todas essas informações refletidas no Dashboard. Sem qualquer comunicação com equipamentos
físicos. Toda a arquitetura deverá permanecer preparada para futuras integrações com Control iD,
Intelbras, Hikvision e JFL. A integração ocorrerá apenas em Sprint futura. O objetivo desta Sprint
é construir o cérebro do Controle de Acesso, não os conectores.

## Decisões tomadas na Fase 1 (aprovação do usuário antes da implementação)

1. **`Credencial.Status` (kill-switch geral) + `PermissaoAcesso` como entidade de vínculo
   própria** — resolve a ambiguidade entre a seção 2 do escopo (status na Credencial) e o
   diagrama de relacionamento da seção 5 (Permissões como nível hierárquico próprio): `Status`
   (Ativa/Suspensa/Expirada/Revogada) fica na `Credencial`; as regras de dia/horário/data da
   seção 3 ficam em `PermissaoAcesso`, um vínculo `Credencial` ↔ `PontoAcesso`. Ver ADR 0010.
2. **`PontoAcesso` pertence direto à Propriedade, nunca à Unidade** — pontos de acesso como
   "Portão Principal" ou "Piscina" são infraestrutura compartilhada de toda a propriedade.
