# Sprint 23 — Base única (web + Android + iOS)

> Fase 1 (planejamento, sem código) — 2026-11-08. Origem: [Plano de Ação de Produto](../roadmap/PLANO_ACAO_PRODUTO.md).
> Decisão do usuário: unificar num app só, com o `webapp/` como base.

## 1. Resumo

Hoje existem dois apps quase iguais: `mobile/` (identidade antiga, gera APK) e `webapp/`
(identidade Akteck Seg, só navegador). O `webapp/` foi copiado do `mobile/` e evoluiu; o mobile
parou. Esta Sprint faz o `webapp/` gerar também o Android (e deixa o iOS configurado), para que
toda funcionalidade nova — a começar pelos alertas críticos da Sprint 24 — exista num lugar só.

## 2. Levantamento (estado real)

| Ponto | Situação |
|---|---|
| Dependências | Idênticas nos dois `package.json` — nenhuma lib nativa a adicionar. |
| Código | `webapp/src` contém tudo de `mobile/src` (nenhum arquivo existe só no mobile) + 20 arquivos novos. |
| Recursos nativos | Preservados atrás de `Platform.OS` (armazenamento seguro, push, canais Android). Só foram desligados no navegador, não removidos. |
| `app.json` | O do webapp perdeu os blocos `android`/`ios`, os plugins nativos (secure-store, splash, build-properties com cleartext, notifications, edge-to-edge) e o `projectId` do EAS. |
| `eas.json` | Não existe no webapp. |
| Scripts | Webapp só tem `start --web` e `build` web. |
| Ícones | Os dois usam ícones antigos (idênticos); o Android sairia sem a marca Akteck. |

**Código escrito só para navegador (quebra no celular):**

1. `api/useImagemAutenticada.ts` — usa `URL.createObjectURL` (não existe no React Native). Afeta
   foto do morador e imagens das câmeras (`CameraCard`, `CameraMini`, `useFotoMorador`).
2. `moradores/servicoPessoas.ts` — envio da foto converte a URI em `Blob` via `fetch(uri).blob()`;
   no Android o `FormData` precisa de `{ uri, name, type }`.

O restante (`DialogoAlerta` com `Modal`, estilos globais web, armazenamento local) já é seguro nas
duas plataformas.

## 3. Estratégia

- **Uma base, arquivos por plataforma** só onde a API difere: `arquivo.web.ts` / `arquivo.native.ts`
  (resolução nativa do Metro). Nada de `if (Platform.OS)` novo espalhado nas telas.
- **Configuração nativa restaurada** no `app.json` do webapp a partir do `mobile/`, já com nome,
  ícones e splash Akteck Seg.
- **`mobile/` intocado durante a Sprint.** Só é arquivado depois que o APK da base única for
  aprovado na homologação em dispositivo.

## 4. Arquitetura proposta

```
webapp/  (base única)
├── app.json        ← web + android + ios + plugins nativos + projectId EAS
├── eas.json        ← perfis development / preview / production
├── assets/         ← ícones e splash Akteck Seg
└── src/
    ├── api/
    │   ├── useImagemAutenticada.web.ts     (blob: URL — comportamento atual)
    │   └── useImagemAutenticada.native.ts  ({ uri, headers: Authorization })
    └── moradores/
        ├── arquivoFoto.web.ts     (Blob)
        └── arquivoFoto.native.ts  ({ uri, name, type })
```

O hook passa a devolver uma fonte de imagem (`ImageSourcePropType`) em vez de uma string, para que
o nativo envie o token no header da própria `<Image>` — sem baixar a imagem duas vezes.

## 5. Arquivos

**Novos**
- `webapp/eas.json`
- `webapp/src/api/useImagemAutenticada.web.ts`, `useImagemAutenticada.native.ts`
- `webapp/src/moradores/arquivoFoto.web.ts`, `arquivoFoto.native.ts`
- `webapp/assets/` — ícone, ícone adaptativo (frente/fundo/monocromático) e splash com o "A" Akteck
- `docs/adr/0039-base-unica-web-android-ios.md`
- `docs/testing/Sprint23.md`

**Modificados**
- `webapp/app.json`, `webapp/package.json` (scripts `android`/`ios`/`start`)
- `webapp/src/cameras/CameraCard.tsx`, `CameraMini.tsx`, `moradores/useFotoMorador.ts` (consumir fonte de imagem)
- `webapp/src/moradores/servicoPessoas.ts`
- `webapp/README.md`, `README.md` raiz, `CLAUDE.md` (onde o app vive)
- `docs/CHANGELOG.md`, `docs/DIVIDA_TECNICA.md` (fecha itens 52 e 59), `docs/roadmap/ROADMAP.md`, `docs/adr/README.md`

**Removidos:** `webapp/src/api/useImagemAutenticada.ts` (substituído pelos dois arquivos).

**Sem alteração de backend e sem migration.**

## 6. Ordem de implementação

1. `app.json` + `eas.json` + scripts — `expo-doctor` e `expo prebuild --no-install` (dry run) limpos.
2. Ícones e splash Akteck Seg.
3. `useImagemAutenticada` por plataforma + consumidores — jest/typecheck/lint, web conferido no navegador.
4. Envio de foto por plataforma — teste de unidade dos dois formatos.
5. APK preview (EAS) — **feito pelo usuário** ou por mim com confirmação (o build roda na conta EAS).
6. Homologação em dispositivo (checklist abaixo) → só então arquivar `mobile/` (tag git
   `mobile-legado` + remoção da pasta).
7. Documentação + Reviewer.

## 7. Riscos

| Risco | Mitigação |
|---|---|
| Layout de computador aparecer em tablet | Breakpoints atuais são por largura; conferir em tablet na homologação. |
| Código só-web ainda não detectado | Busca por APIs de navegador já feita; o APK é a prova final. |
| `cleartext` (HTTP) no APK | Mantido só porque a API de desenvolvimento é HTTP local, igual ao mobile; produção exige HTTPS (registrar na dívida). |
| Arquivar o mobile cedo demais | Só após homologação aprovada, e com tag git para restaurar. |

## 8. Rollback

`mobile/` continua intacto até a homologação. Se o APK da base única for reprovado, o APK do
`mobile/` segue sendo o oficial e a Sprint volta para correção — nada é perdido.

## 9. Testes

- Automatizados: `jest`, `tsc --noEmit`, `eslint`, `expo-doctor`, `expo export --platform web`.
- APK preview (EAS, `--profile preview`).
- Homologação no Android físico — checklist da Sprint:
  - App instala e abre com nome/ícone/splash Akteck Seg.
  - Foto do morador e imagens de câmera carregam.
  - Cadastro facial: câmera abre, foto é enviada e aparece na lista.
  - Comandos de alarme e portão no Início.
  - Menu lateral e abas de baixo.
- Regressão obrigatória: login, logout, troca de propriedade, Início, Acessos, push (registro do
  dispositivo), tempo real (SignalR).
- Web: as mesmas telas conferidas no navegador, para garantir que nada regrediu.

## 10. Critérios de aceite

1. Um só código gera o site e o APK.
2. APK aprovado na homologação, sem bug Blocker.
3. Site sem regressão.
4. `mobile/` arquivado com tag de restauração.
5. ADR 0039 registrada; itens 52 e 59 da dívida técnica fechados.
