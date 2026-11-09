# Akteck Seg — App (web, Android e iOS)

Base única do app do cliente (ADR 0039): o mesmo código gera o site e o app de celular, via Expo
(React Native + react-native-web). A pasta ainda se chama `webapp/` por histórico. O antigo
`../mobile` será arquivado depois que o APK desta base for homologado.

## Rodar localmente

Pré-requisito: a Api rodando em `http://localhost:5027` (`scripts/start_backend.ps1`).

```bash
npm install
npm run web        # navegador — http://localhost:8081
npm start          # Metro para Android/iOS (Expo Go ou development build)
```

A URL da Api vem de `EXPO_PUBLIC_API_URL` (`.env`, mesmo formato de `.env.example`). No celular,
use o IP da máquina na rede local, não `localhost`.

## Diferenças por plataforma

O código é um só. Onde a API muda entre plataformas, existe um par de arquivos resolvido pelo Metro:

| Arquivo | Android/iOS (`.ts`) | Navegador (`.web.ts`) |
|---|---|---|
| `api/useImagemAutenticada` | imagem vira `data:` URI | imagem vira `blob:` URL |
| `moradores/arquivoFoto` | `{ uri, name, type }` no FormData | `Blob` no FormData |

Recursos que só existem no celular continuam atrás de `Platform.OS`:

- **Notificações push**: no navegador, os avisos chegam em tempo real com a página aberta (SignalR).
- **Sessão**: no celular fica no Keychain/Keystore (`expo-secure-store`); no navegador, no
  armazenamento local (ver dívida técnica 51).
- **Vibração**: sem efeito no computador.

## Builds

```bash
npm run build      # site estático em dist/ (single page)
```

Publique `dist/` em qualquer hospedagem estática e adicione a origem dela em
`Cors:AllowedOrigins` da Api.

APK de teste (EAS, perfil `preview`, conta `ostresmosqueteiros`). Antes, confira o IP da máquina
em `eas.json` → `preview.env.EXPO_PUBLIC_API_URL`:

```powershell
$env:EAS_NO_VCS='1'; $env:EAS_PROJECT_ROOT=(Get-Location).Path
npx eas-cli build --platform android --profile preview
```

As duas variáveis evitam que o EAS copie o repositório inteiro (dívida técnica 61).
Identificador do app: `com.akteck.seg`.

## Verificação

```bash
npm run typecheck && npm run lint && npm test
npx expo-doctor
```

## Responsivo

| Largura | Layout |
|---|---|
| < 1024px | Igual ao celular: largura total + navegação inferior |
| ≥ 1024px | Barra lateral com as abas e o menu, conteúdo em colunas |
