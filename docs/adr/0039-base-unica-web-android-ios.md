# ADR 0039 — Base única: o `webapp/` gera web, Android e iOS

**Data**: 2026-11-08

## Contexto

Desde a ADR 0034 existiam dois apps quase iguais. O `mobile/` gerava o APK, mas tinha parado na
identidade antiga (ADR 0019) por pedido do usuário de não mexer nele. O `webapp/`, uma cópia do
mobile, recebeu toda a evolução: identidade Akteck Seg, moradores, rosto, Início do protótipo
(ADRs 0035–0038).

O [Plano de Ação de Produto](../roadmap/PLANO_ACAO_PRODUTO.md) depende de recursos que só existem
no app nativo: alerta crítico com som, desbloqueio por biometria, localização e widgets. Diante
disso, o usuário escolheu entre três caminhos:

1. liberar mudanças no `mobile/`;
2. ficar só no navegador;
3. unificar num app só.

Ele escolheu unificar, com o `webapp/` como base.

## Levantamento que sustentou a decisão

- As dependências eram idênticas nos dois `package.json`.
- Nenhum arquivo existia só no `mobile/`; o `webapp/` tinha 20 arquivos a mais.
- Os recursos nativos continuavam no `webapp/`, atrás de `Platform.OS`, desligados só no navegador.
- Apenas dois trechos usavam APIs exclusivas do navegador:
  - `useImagemAutenticada`, que usa `URL.createObjectURL`;
  - o envio da foto do rosto, com `Blob` no `FormData`.

## Decisão

- O `webapp/` é a **base oficial** do app. O mesmo código gera o site (`expo export --platform
  web`), o APK/AAB (EAS) e, mais adiante, o iOS.
- Diferenças de plataforma ficam em **arquivos por plataforma**, pela resolução do Metro:
  - `arquivo.ts` é a versão Android/iOS e também a que o TypeScript enxerga;
  - `arquivo.web.ts` é a versão do navegador.
  - Nada de novos `if (Platform.OS)` espalhados nas telas.
- Primeiros pares de arquivos por plataforma:
  - `api/useImagemAutenticada`: no nativo, `data:` URI via `FileReader`; no navegador, `blob:` URL.
    A assinatura é a mesma, então as telas não mudaram;
  - `moradores/arquivoFoto`: no nativo, `{ uri, name, type }`; no navegador, `Blob`.
- **Identificador Android/iOS `com.akteck.seg`** (decisão do usuário). Ele instala como um app novo,
  e o Firebase da Sprint 24 será criado já com esse identificador.
- O projeto EAS continua o mesmo (`ostresmosqueteiros/appmorador`), com um keystore novo criado
  para o identificador novo.
- `app.json` ganhou:
  - os blocos `android`/`ios` e os plugins nativos do mobile;
  - os textos de permissão de câmera e fotos em pt-BR;
  - ícones e splash com o símbolo Akteck.
- Dependências alinhadas ao Expo SDK 57 (patches). Isso corrige a regressão de memória do Hermes
  apontada pelo `expo-doctor`.
- `react-native-edge-to-edge` removido: o pacote não tem mais manutenção e o React Native 0.86 já é
  borda a borda nativamente. O `expo-doctor` passa 21/21.
- O `mobile/` fica **intocado até o APK da base única ser aprovado** na homologação em
  dispositivo. Depois disso, é arquivado (tag git `mobile-legado` + remoção da pasta).

## Alternativas descartadas

- **Liberar o `mobile/`:** manteria duas bases. Cada funcionalidade nova teria de ser feita duas
  vezes, que é a causa do item 52 da dívida técnica.
- **Pacote compartilhado (`packages/app`):** é a solução clássica de monorepo, mas acrescenta
  ferramenta (workspaces, configuração de Metro) sem ganho real, porque só existe um app consumidor.
- **Só navegador:** inviabiliza o núcleo do produto (alerta crítico, biometria, localização).

## Consequências

- Toda Sprint de app passa a ser feita uma vez só, e a Regra de Validação em Dispositivo se aplica ao
  `webapp/`.
- O nome da pasta `webapp/` deixa de descrever o conteúdo. A renomeação para `app/` fica para depois
  do arquivamento do `mobile/`, para não mexer em dois lugares ao mesmo tempo.
- Build EAS a partir desta máquina usa `EAS_NO_VCS=1` + `EAS_PROJECT_ROOT=webapp`. Sem isso, o EAS
  copia o repositório inteiro e falha num symlink de `.claude/skills` que o Windows não permite
  recriar. Bônus: o envio cai para ~0,5 MB.
- O APK de teste continua usando HTTP na rede local (`usesCleartextTraffic`), como o mobile.
  Produção exige HTTPS (dívida técnica).
