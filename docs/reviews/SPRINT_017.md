# Relatório — Sprint 17 (Refinamento da Experiência do Morador)

**Data de conclusão**: 2026-07-25

## Resumo executivo

Sprint corretiva sobre a Sprint 16 (UX001): 7 fricções encontradas numa validação em dispositivo
real foram corrigidas sem alterar backend, domínio, contratos de API, integrações, SignalR,
Providers, Snapshot, Timeline ou Eventos. Três lacunas arquiteturais que a missão original
assumia resolvidas (upload de foto de morador, campo de perfil de usuário, PGM real em todo
fabricante) foram verificadas por leitura direta de código antes de qualquer implementação e
resolvidas com decisão explícita do usuário (ver ADR 0020) em vez de assumidas ou faladas por
cima.

## Relatório de Auditoria (Fase 0)

Ver `docs/sprints/SPRINT_017.md` para a tabela completa dos 7 achados com o arquivo/linha
verificado para cada um. Todos os 7 achados foram confirmados reais por leitura direta de código
(nenhum assumido só pela descrição da missão).

## Relatório do Discovery (Fase 1)

Ver ADR 0020 para as 4 decisões de arquitetura tomadas nesta Sprint (captura facial sem
persistência, perfil como preferência local, Painel de Controle só JFL, mapeamento de erro
centralizado) — cada uma resultado de uma pergunta explícita respondida pelo usuário antes da
implementação começar.

## Fluxo de Erros — antes × depois

**Antes**: cada tela decidia sozinha o que mostrar quando `ApiError` acontecia; falhas de rede
(sem `fetch` bem-sucedido) propagavam como exceção JS crua e não tratada; mensagens do backend
(`ex.Message`, "partição", "(offline)") apareciam direto na tela.

**Depois**: `api/client.ts` sempre produz um `ApiError.message` já amigável (classificado por
`utils/errorMapper.ts`) — toda tela que já fazia `err instanceof ApiError ? err.message : fallback`
ganha o benefício automaticamente, sem edição própria. Mensagem técnica original vai só para
`console.warn` em `__DEV__`. Falhas de ação pontual (ex.: acionar um comando) mostram `Toast` com
"Tentar novamente"; falhas de renderização mostram `ErrorBoundary` com "Não se preocupe, estamos
registrando este problema."

## Fluxo Facial — antes × depois

**Antes**: `TipoCredencialSelector` permitia escolher "Facial" quantas vezes quisesse, sem nenhuma
verificação; a credencial era criada sem nenhuma captura de foto — nenhum feedback visual de que
"cadastrar o rosto" significava qualquer coisa além de escolher um chip.

**Depois**: ao escolher "Facial" com uma credencial `Facial` já existente, mostra "Você já possui
uma credencial facial" com [Atualizar foto] [Remover] [Cancelar]. Sem credencial existente, abre
câmera (`expo-image-picker`, fallback galeria) → pré-visualização real → confirmação com aviso
explícito de que a foto ainda não é persistida no backend (dívida técnica) → credencial criada +
foto salva localmente (`credenciais/fotoFacialLocal.ts`) associada ao id da credencial. A lista
mostra a miniatura da foto local, status "Ativa", botões "Atualizar foto"/"Remover".

## Aba Acessos — antes × depois

**Antes**: lista de Moradores/Visitantes com CRUD, sem nenhum comando executável.

**Depois**: nova seção "Painel de Controle" no topo, com um cartão por PGM `permitida=true` de
cada central JFL da propriedade (ícone + nome amigável configurável + estado + botão de ação +
feedback "Feito ✓"), desabilitado com estado visual "Sem comunicação com o equipamento" quando o
equipamento está offline. Sem nenhuma central JFL com PGM permitida, mostra Empty State honesto
("Nenhum comando disponível ainda", CTA "Entendi") — nunca inventa comandos para Intelbras/Control
iD. Moradores/Visitantes preservados abaixo, sem alteração.

## Detalhes do Equipamento — morador × técnico

**Morador**: ícone + nome do equipamento + estado (Online/Offline) + botão único "Ver histórico"
(navega para a Central de Eventos da propriedade).

**Técnico** (sem alteração de comportamento): tela completa — IP/Porta/Usuário/Identificador,
Testar conexão, Consultar informações, Sincronizar moradores/credenciais/permissões, Importar
eventos.

Extensão de coerência (mesma decisão/mecanismo, não escopo novo): a seção "Proteção" de
`MinhaPropriedadeScreen` (Controladores de acesso/Centrais JFL/Centrais Intelbras) também fica
visível só para `perfil === 'tecnico'` — sem isso, o morador ainda alcançaria as telas mais
técnicas do app (armar/desarmar partição, PGM cru, inibir zona por número) por outro caminho.

## HeroCard — conectividade

Novo chip abaixo do subtítulo do HeroCard: "🟢 Conectado" (equipamento sincronizou há ≤2 min),
"🟡 Última comunicação há X min" (sincronizou há mais tempo, mas ainda há equipamento online),
"⚪ Sem comunicação desde HH:MM" ou "Sem comunicação ainda" (todos os equipamentos offline).
Derivado de `dashboard.quantidadeEquipamentosOnline/Offline` e
`ultimaAtualizacaoOperacionalUtc` (Sprint 13/14, já atualizados via SignalR) — nenhum campo novo no
backend.

## Empty States revisados

`cta` adicionado em: `EntregasScreen` (Registrar entrega), `PermissoesScreen` (Adicionar
permissão), `VisitantesScreen` (Adicionar visitante), `PontosAcessoScreen` (Adicionar ponto de
acesso), `UnidadesScreen` (Adicionar unidade), `VeiculosScreen` (Adicionar veículo),
`CentraisJflScreen`/`CentraisIntelbrasScreen` (Adicionar central), `MoradoresScreen` (Adicionar
morador), `CredenciaisScreen` (Adicionar credencial), `EquipamentosScreen` (Adicionar
equipamento), `VagasScreen` (Adicionar vaga), `AutorizacoesScreen` (Adicionar autorização),
`SaudePropriedadeScreen` (Ir para Minha Propriedade). `AccessScreen` (Painel de Controle) ganhou
CTA "Entendi" nesta Sprint (Escopo 3).

**Exceção deliberada, documentada no próprio arquivo**: `CamerasScreen` permanece sem `cta` — não
existe nenhuma ação real disponível (sem CRUD de câmera, sem fabricante com suporte real de
vídeo); um botão aqui só fingiria uma funcionalidade inexistente, o que a Sprint 16 já havia
decidido conscientemente evitar. `HomeScreen` (Atividade recente) e `EventosScreen` também ficam
sem `cta` pelo mesmo motivo: são feeds gerados pelo sistema, sem ação de "adicionar" que faça
sentido para o morador.

## Ajustes — antes × depois

**Antes**: Perfil → PropertyCard → Notificações/Termos (sem agrupamento) → Sair.

**Depois**: Perfil → seção "Propriedade" (PropertyCard, primeiro item acionável) → seção "Conta"
(Notificações + toggle "Modo técnico") → seção "Legal" (Termos e Privacidade) → Sair.

## Arquivos criados

`src/utils/errorMapper.ts`, `src/components/Toast.tsx`, `src/auth/profilePreference.ts`,
`src/acessos/pgmLabels.ts`, `src/acessos/CommandCard.tsx`, `src/credenciais/fotoFacialLocal.ts`.

## Arquivos modificados

`src/api/client.ts` (erro centralizado), `src/components/ErrorBoundary.tsx` (copy), `App.tsx`
(`ToastProvider`), `src/auth/AuthContext.tsx` (`perfil`/`setPerfil`), `src/screens/acessos/
AccessScreen.tsx` (Painel de Controle), `src/screens/credenciais/CredenciaisScreen.tsx` (fluxo
facial completo), `src/screens/equipamentos/DetalhesEquipamentoScreen.tsx` (RBAC de UI),
`src/screens/ajustes/MinhaPropriedadeScreen.tsx` (seção Proteção gated), `src/components/
HeroCard.tsx` (prop `conectividade`), `src/screens/home/HomeScreen.tsx` (`computarConectividade`),
`src/screens/ajustes/SettingsScreen.tsx` (reorganização + toggle), e os ~13 arquivos de tela
listados em "Empty States revisados".

## Evidências dos testes

- `dotnet build` (backend): 0 erros, 0 avisos — confirmando zero alteração/regressão no backend
  (nenhum arquivo de `backend/` tocado nesta Sprint).
- `npm run typecheck` (`tsc --noEmit`): 0 erros em cada etapa (rodado após cada Escopo).
- `npm run lint` (`expo lint`): 0 erros, 0 avisos em cada etapa.
- `npx expo-doctor`: 20/20 checks aprovados.
- Build EAS (preview/APK): ver seção seguinte.
- **Validação funcional em dispositivo físico real** (abrir o app, testar Fluxo Feliz, repetir os
  7 cenários da Sprint 16.1): pendente de o usuário instalar o build e reportar — mesmo padrão já
  estabelecido nas Sprints 15/16 (sem emulador/dispositivo conectado neste ambiente de execução).

## Build EAS

Build `2e6f2f69-0d24-43a2-8050-3a09e92254c4` (perfil `preview`, Android, SDK 57), finalizado em
14min13s. APK: https://expo.dev/artifacts/eas/Sw-6EmPAQrG-kcAQ9s9dZWGwM5TgLkAVWGDap5hUnJY.apk —
logs: https://expo.dev/accounts/ostresmosqueteiros/projects/appmorador/builds/2e6f2f69-0d24-43a2-8050-3a09e92254c4

## Dívidas técnicas

Ver `docs/DIVIDA_TECNICA.md` itens 32-36 (foto facial sem persistência no backend, `expo-camera`
deliberadamente não instalado, perfil sem RBAC real, rótulos de PGM só locais, Painel de Controle
só JFL).

## Parecer do Reviewer — 8 Pilares (desta Sprint)

| Pilar | Avaliação |
|---|---|
| **1. Experiência** | ✅ Aprovado. As 7 fricções da validação em dispositivo real foram corrigidas sem introduzir vocabulário técnico novo na superfície diária. |
| **2. Erros** | ✅ Aprovado. Nenhuma mensagem técnica (stacktrace, status HTTP, "partição"/"PGM"/"offline") sobrevive ao mapeamento — confirmado por leitura direta das 3 fontes reais de mensagem (`ArmCommandService`, `ControlIdProvider`, `IntelbrasProvider`) antes de escrever os padrões de classificação. |
| **3. Acessos** | ✅ Aprovado. Painel de Controle mostra só comandos reais (JFL, `permitida=true`); Empty State honesto quando vazio, nunca fabricado para Intelbras/Control iD. |
| **4. Facial** | ✅ Aprovado, com ressalva documentada. Captura e pré-visualização são reais; persistência da foto no backend continua pendente (dívida técnica pré-existente, disclosure explícito ao usuário — nunca finge que salvou). |
| **5. RBAC de UI** | ✅ Aprovado, com ressalva documentada. `perfil` é só preferência de UI (nunca fronteira de segurança real) — documentado em 3 lugares (`profilePreference.ts`, ADR 0020, `DetalhesEquipamentoScreen.tsx`) para que nenhuma Sprint futura confunda isso com autorização de verdade. |
| **6. Empty States** | ✅ Aprovado. CTA adicionado em 13 telas; 3 exceções deliberadas e documentadas (Câmeras, Atividade recente, Eventos) por não haver ação real de "adicionar" nesses contextos. |
| **7. Performance** | ✅ Aprovado. Painel de Controle busca status só das centrais JFL (filtro por fabricante antes da chamada), nenhuma chamada desnecessária a Intelbras/Control iD. |
| **8. Fluxo Feliz** | ✅ Aprovado por inspeção de código (abrir app → Hero → abrir portão via Painel de Controle → feedback "Feito ✓" → fechar) — validação cronometrada real em dispositivo físico ainda pendente do usuário. |

**Conclusão**: Sprint aprovada. Ressalva não-bloqueante: validação funcional em dispositivo físico
real (mesmo padrão das Sprints 15/16) fica para o usuário reportar depois de instalar o build.
