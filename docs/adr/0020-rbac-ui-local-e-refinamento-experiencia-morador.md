# ADR 0020 — RBAC de UI Local e Refinamento da Experiência do Morador (Sprint 17)

**Data**: 2026-07-25

## Contexto

A Sprint 16 (ADR 0019) entregou o Design System Mobile Oficial UX001, mas uma validação em
dispositivo real ("Sprint 16.1") encontrou 7 fricções reais que a auditoria estática da Sprint 16
não pegou: erros técnicos vazando para o morador (mensagens com `ex.Message` cru, termos como
"partição"/"offline"); cadastro de credencial facial permitindo duplicidade e sem nenhuma captura
de foto; a aba Acessos e a tela de Detalhes do Equipamento continuando 100% técnicas (CRUD de
cadastro, IP/Usuário/Identificador, botões de sincronização); Empty States sem CTA em boa parte
das telas; e o HeroCard mostrando "Online"/"Offline" cru em vez de uma frase útil.

Esta Sprint corrige as 7 fricções, mas esbarra em 3 lacunas arquiteturais que a missão original
assumia resolvidas e que precisaram de decisão explícita do usuário antes de qualquer código:

1. **Não existe endpoint para persistir foto de morador** (`Morador.FotoPath` existe no domínio
   desde a Sprint 6, mas nunca ganhou rota de escrita — dívida técnica item 30/pré-existente).
2. **Não existe nenhum campo de perfil de usuário** em nenhuma resposta de API — `EntrarResponse`/
   `StoredUser` são só `{id, nome, email}`. O domínio não tem RBAC (dívida técnica item 6).
3. **Só centrais JFL têm PGM real** — Intelbras nunca implementou PGM (Sprint 15, dívida técnica
   item 28) e Control iD é um leitor de controle de acesso, não uma central de alarme; não tem
   nenhum conceito de PGM.

## Decisão 1 — Captura facial: pré-visualização real, persistência ainda não

Em vez de esconder o cadastro facial (opção mais simples, mas que não resolve a fricção real do
morador) ou fingir que a foto é salva (dishonesto), a Sprint implementa a captura real via
`expo-image-picker` (câmera prioritária, galeria como fallback) com pré-visualização real da foto
escolhida, mas **cria a credencial sem persistir a foto no backend** — a imagem fica só localmente
(`expo-secure-store`, mesmo padrão de `onboardingStorage.ts`), associada à credencial por id
(`credenciais/fotoFacialLocal.ts`). Uma mensagem explícita informa que o armazenamento seguro da
foto chega numa atualização futura. Se o app for reinstalado, a miniatura desaparece, mas a
credencial em si (no backend) continua intacta.

`expo-camera` não foi adicionado como dependência nova — `expo-image-picker` já cobre tanto
câmera (`launchCameraAsync`) quanto galeria (`launchImageLibraryAsync`) sozinho, tornando uma
segunda dependência nativa redundante para o mesmo resultado (captura de uma foto). A prioridade
de captura definida na Discovery (1. fluxo do equipamento/Control iD; 2. câmera; 3. galeria; 4.
indisponível) colapsa para: item 1 não existe (nenhuma tela/endpoint de "capturar no dispositivo"
hoje); item 2 e 3 cobertos por `expo-image-picker`; item 4 aparece quando nenhuma permissão é
concedida.

Anti-duplicação (achado #2) é feita no cliente: antes de iniciar a captura, a tela verifica se já
existe uma credencial `Facial` para o morador — se sim, mostra "Você já possui uma credencial
facial" com [Atualizar foto] [Remover] [Cancelar], nunca permitindo uma segunda.

## Decisão 2 — Perfil (morador/técnico): preferência local, nunca uma fronteira de segurança

Sem alterar o domínio ou a resposta de autenticação, `auth/profilePreference.ts` introduz uma
preferência 100% local (`expo-secure-store`, chave única `perfilPreferencia`), com **padrão
`'morador'`** (o mais restritivo, esconde telas técnicas) e um toggle discreto em Ajustes ("Modo
técnico"). Qualquer pessoa com acesso ao próprio celular pode alternar livremente — isso é
esperado e documentado: a preferência organiza a UI, nunca protege dado algum (o backend continua
validando posse/autenticação normalmente, sem relação nenhuma com este campo).

Duas telas passam a variar por `perfil`:
- **`DetalhesEquipamentoScreen`** (achado #5): morador vê só ícone + nome do equipamento + estado
  + "Ver histórico" (navega para a Central de Eventos da propriedade); técnico continua vendo
  IP/Usuário/Identificador e os 6 botões de sincronização/teste/importação, sem nenhuma mudança de
  comportamento.
- **`MinhaPropriedadeScreen`**: a seção "Proteção" (Controladores de acesso/Centrais JFL/Centrais
  Intelbras — telas de gestão bruta de fabricante: armar/desarmar partição, PGM, inibir zona por
  número) fica visível só para `perfil === 'tecnico'`. Extensão natural do mesmo mecanismo já
  aprovado para a Decisão 2 — sem essa mudança, um morador ainda alcançaria as telas técnicas mais
  cruas do app por outro caminho, incoerente com o objetivo desta Sprint.

## Decisão 3 — Painel de Controle: só comandos reais, nunca inventados por fabricante

A aba Acessos ganha uma seção "Painel de Controle" acima das abas Moradores/Visitantes,
mostrando um cartão por PGM com `permitida === true`, buscando `GET /api/equipamentos/{id}/jfl/
status` de cada equipamento `Fabricante === 'Jfl'` da propriedade — nunca para Intelbras/Control
iD, que não têm PGM real. Rótulos amigáveis ("Abrir portão" em vez de "PGM 3") são armazenados
localmente por equipamento (`acessos/pgmLabels.ts`, mesmo padrão de `expo-secure-store`), com
fallback `Comando N` quando ainda não configurado — o backend (`PgmStatusInfo`) não tem campo de
nome amigável, e criar uma coluna nova para isso seria mudança de contrato de API fora do escopo
desta Sprint. Quando nenhuma central JFL com PGM permitida existe, um Empty State honesto aparece
("Nenhum comando disponível ainda", CTA "Entendi") em vez de esconder a seção ou fabricar dados.

## Decisão 4 — Erros: mapeamento centralizado, uma única passagem por toda a superfície de API

Em vez de retrofitar dezenas de telas individualmente, `api/client.ts` passou a produzir sempre um
`ApiError.message` já amigável — toda tela que já fazia `err instanceof ApiError ? err.message :
fallback` (padrão estabelecido desde a Sprint 7) ganha o benefício automaticamente, sem edição.
`utils/errorMapper.ts` classifica por prioridade: sem internet (`@react-native-community/netinfo`)
→ falha de servidor (status ausente/5xx) → dispositivo não responde (substrings de mensagens reais
de `ArmCommandService`/`ControlIdProvider`/`IntelbrasProvider`, confirmadas por leitura direta do
código) → não encontrado (404) → vazamento técnico genérico (stacktrace/termos de protocolo) →
mensagem já amigável do backend passa direto (validações de domínio pt-BR). A mensagem técnica
original nunca é descartada — vai para `console.warn` só em `__DEV__`, nunca para a tela.

Toast (`components/Toast.tsx`) cobre feedback de falha de **ação** (ex.: acionar um comando),
distinto do `ErrorBoundary` (erro de renderização) e do estado `error` de cada tela (falha ao
carregar dados) — sempre com "Tentar novamente" quando a ação permitir.

## Fora de escopo (reafirmado)

RBAC real no domínio/backend, novos comandos no backend, qualquer alteração em contratos de API,
integrações, SignalR, Providers, Snapshot, Timeline, Eventos, push notification, analytics, novos
fabricantes, câmeras ao vivo, biometria real (o backend já suporta o tipo `Facial`, mas a captura
depende do ambiente).

## Impactos

Ver relatório de entrega (`docs/reviews/SPRINT_017.md`) para a lista completa de arquivos criados/
modificados. Zero alteração em backend, domínio, contratos de API, integrações, SignalR, Providers,
Snapshot, Timeline, Eventos ou ADRs 0014-0019 — confirmado por `dotnet build` limpo sem tocar
nenhum arquivo do backend nesta Sprint.
