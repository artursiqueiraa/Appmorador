# Relatório — Sprint 1: Plataforma Segurança Conectada (Auth + Propriedade + Dashboard + Mobile)

Data: 2026-07-19
Status: **implementada, testada, e rodando. Migration aplicada. Build backend e mobile limpos.**

Plano aprovado em `C:\Users\artur\.claude\plans\floofy-coalescing-gizmo.md`.

---

## 1. Backend — arquitetura

Novo projeto `AppMorador.Application` (Clean Architecture): Domain fica puro, Application orquestra
os casos de uso, Infrastructure só implementa (EF, hashing, JWT), Api fica fina.

```
AppMorador.Domain/
  Entities/Property.cs (renomeado de Site), User.cs, RefreshToken.cs
  Repositories/IUserRepository.cs, IPropertyRepository.cs
  ContactId/ContactIdDefinition.cs (+ campo FriendlyMessage novo)

AppMorador.Application/
  Auth/ (IAuthService, AuthService, IPasswordHasher, ITokenService, DTOs)
  Properties/ (IPropertyService, PropertyService, DTOs)
  Dashboard/ (IDashboardService, DashboardService, IDashboardQueryService, DTOs)
  Common/Result.cs

AppMorador.Infrastructure/
  Identity/ (BCryptPasswordHasher, JwtTokenService, JwtOptions, AuthServiceCollectionExtensions)
  Persistence/ (UserRepository, PropertyRepository — novos; AppDbContext atualizado)
  Dashboard/DashboardQueryService.cs

AppMorador.Api/
  Controllers/ (AuthController, PropertiesController, DashboardController)
  Middleware/ (SecurityHeadersMiddleware, ExceptionHandlingMiddleware)
  Auth/ClaimsPrincipalExtensions.cs
  RateLimiterPolicies.cs
```

## 2. Modelo de domínio

- **`Site` → `Property`** (rename real, ver seção 4) + campos novos `Endereco` (opcional) e
  `OwnerUserId` (dono único nesta sprint).
- **`User`**: Nome, Email (único), PasswordHash, FailedLoginAttempts, LockedUntilUtc, SecurityStamp,
  CreatedAtUtc.
- **`RefreshToken`**: nunca guarda o token cru — só `TokenHash` (SHA-256), com `RevokedAtUtc` e
  `ReplacedByTokenHash` para rastrear rotação.

## 3. Endpoints

| Rota | Método | Auth | Rate limit |
|---|---|---|---|
| `/api/auth/register` | POST | não | sim (10/min) |
| `/api/auth/login` | POST | não | sim (10/min) |
| `/api/auth/refresh` | POST | não | não |
| `/api/auth/logout` | POST | sim | não |
| `/api/properties` | POST/GET | sim | não |
| `/api/properties/{id}` | PUT | sim | não |
| `/api/properties/{id}/dashboard` | GET | sim | não |

Toda checagem de posse (`property.OwnerUserId == usuário atual`) fica nos serviços de Application,
nunca no controller — 404 genérico tanto para "não existe" quanto para "não é seu" (não revela a
outro usuário que uma propriedade alheia existe).

## 4. Migration — o que foi feito e as duas correções que você pediu

**Antes de aplicar**, você revisou o resumo técnico (operações, impacto nos dados, destrutivo,
segurança, recomendação — formato que virou regra permanente, ver `feedback_migration_review_protocol`
na memória) e pediu 2 ajustes, ambos aplicados:

1. **Removido `defaultValue: Guid.Empty`** de `Property.OwnerUserId` — a tabela estava vazia, não
   havia necessidade de um valor fake.
2. **`DeleteBehavior` de `Property → User` mudado de `Cascade` para `Restrict`** — apagar um usuário
   nunca apaga as propriedades dele em cascata.

Para manter os 3 arquivos de migration consistentes (`.cs`, `.Designer.cs`, `ModelSnapshot.cs`),
regerei do zero em vez de remendar à mão: mudei `AppDbContext.cs` para `Restrict`, rodei
`migrations remove` + `migrations add` de novo, e reapliquei manualmente a correção de
`RenameTable`/`AddColumn` (EF não detecta rename de tabela sozinho, só de coluna).

**Aplicada com sucesso**: `ALTER TABLE Sites RENAME Properties` (não drop+create), `Users` e
`RefreshTokens` criadas, `SiteId → PropertyId` renomeado em `AlarmPanels`/`Cameras`/`Dvrs`/
`Occurrences`, FK `Property → User` confirmada como `RESTRICT` via `information_schema`.

## 5. Segurança — checklist aplicado e verificado

| Item pedido | Como foi feito | Verificado |
|---|---|---|
| Sem segredo no código | `Jwt:Key` e `ConnectionStrings:DefaultConnection` só em user-secrets | sim — grep confirma nenhum dos dois em `appsettings*.json` |
| Hash de senha seguro | BCrypt (work factor 12) | sim — `Verify`/`Hash` testados via login real |
| Senha forte | Regex (8+ caracteres, maiúscula, minúscula, dígito) | sim — testado, 400 com mensagem clara |
| Rate limit no login | Fixed window, 10/min | sim — testado ao vivo, 11ª tentativa retorna 429 |
| Lockout | 5 tentativas erradas → 15 min bloqueado | revisado no código (`AuthService.LoginAsync`); não testado ao vivo para não estourar o rate limit da sessão de teste |
| Mensagens genéricas (sem enumeration) | "E-mail ou senha inválidos." sempre igual | sim — testado com e-mail inexistente e senha errada, mesma mensagem |
| Nunca confiar em Id do cliente | Toda ação de propriedade valida `OwnerUserId` no servidor | sim — testado com 2º usuário tentando ver propriedade do 1º → 404 |
| HTTPS/HSTS | `UseHttpsRedirection`/`UseHsts` (fora de Development) | configurado; não testado com certificado real neste ambiente |
| CORS restritivo | Sem origem configurada = nada liberado (nunca `AllowAnyOrigin`) | revisado no código |
| Swagger só em dev | `if (IsDevelopment())` no registro e no middleware | revisado no código |
| Migrations com constraints/FKs/índices | Ver seção 4 | sim |
| Logs nunca incluem senha/token/Authorization | grep dedicado, zero ocorrência | sim |
| Erros nunca vazam stack trace/connection string | `ExceptionHandlingMiddleware` genérico | revisado no código |
| Mobile: nunca AsyncStorage para token | `expo-secure-store` (Keychain/Keystore) | sim — ver nota sobre fallback web abaixo |

**Achado durante a revisão, corrigido nesta mesma sprint**: a connection string com a senha real do
MySQL estava em `appsettings.Development.json`, sem `.gitignore` protegendo (backend não tinha
`.gitignore` nenhum ainda). Movi para user-secrets (mesmo tratamento do `Jwt:Key`) e criei
`backend/.gitignore` excluindo `appsettings.Development.json`/`appsettings.Local.json` como defesa
extra.

**Limitação conhecida, não implementada nesta sprint**: `User.SecurityStamp` existe no modelo mas
ainda não é validado a cada request (isso exigiria consultar o banco por request autenticada, como o
`Teste-portaria-main1` faz). Sem endpoint de troca de senha nesta sprint, isso não tem efeito prático
ainda — vira relevante quando essa funcionalidade for adicionada.

## 6. Mobile — scaffold e telas

**Achado técnico**: `create-expo-app` tem um bug neste ambiente — sua chamada interna a
`npm pack --dry-run` falha ao parsear a saída (mesmo com `--json` limpo quando testado
isoladamente). Contornei baixando o tarball do template (`npm pack expo-template-blank-typescript`)
e extraindo manualmente — resultado idêntico ao que o CLI geraria.

Dependências instaladas: `@react-navigation/native` + `native-stack`, `react-native-screens`,
`react-native-safe-area-context`, `expo-secure-store`, `react-native-svg`, `lucide-react-native`
(mesma API/ícones do mockup fornecido), e `react-dom`/`react-native-web`/`@expo/metro-runtime` (só
para permitir testar no navegador durante o desenvolvimento).

```
mobile/src/
  theme/theme.ts          — paleta, raios, espaçamento extraídos do mockup
  config/env.ts           — EXPO_PUBLIC_API_URL centralizada (.env)
  api/client.ts, types.ts — fetch wrapper com refresh automático em 401
  auth/secureStorage.ts   — SecureStore (Keychain/Keystore); fallback localStorage só quando Platform.OS === 'web' (não é o alvo de produção)
  auth/AuthContext.tsx    — sessão, login/registro/logout, propriedade selecionada (em memória)
  navigation/RootNavigator.tsx — troca de stack conforme estado (sem token → Login/Cadastro; com token sem propriedade → SelecionarPropriedade; com as duas → Dashboard)
  screens/ — SplashScreen, LoginScreen, CadastroScreen, SelecionarPropriedadeScreen, DashboardScreen
```

**Dashboard**: exatamente os 7 itens pedidos (status, Armar/Desarmar visuais — sem endpoint de
comando nesta sprint, por decisão sua —, atalhos Câmeras/Acessos marcados "em breve" já que essas
telas não existem ainda, último evento, Health Score). Nenhuma tela extra do mockup (grade de
câmeras, tela de Acessos, overlay de alerta) foi construída.

## 7. Testes realizados

- **Backend**: `dotnet build` limpo (0/0). Fluxo completo via `curl`: registro → login → criar
  propriedade → listar → dashboard (retornou "Configuração pendente"/`healthScore: 0` corretamente,
  já que não há `AlarmPanel` cadastrado ainda — comportamento correto). Rate limit confirmado ao
  vivo. E-mail duplicado, senha fraca, e acesso cross-user todos retornaram o comportamento esperado.
- **Mobile**: `npx tsc --noEmit` limpo (0 erros). `npx expo export -p web` gerou o bundle sem erros
  (2267 módulos). Rodei `expo start --web` e abri no navegador — a aplicação sobe e renderiza sem
  erro de runtime (corrigi um erro real nesse processo: `expo-secure-store` não tem implementação
  para web, adicionei fallback só para essa plataforma de teste).
  **Limitação da verificação**: não tenho uma ferramenta de captura de tela neste ambiente, então não
  confirmei visualmente que o layout bate pixel a pixel com o mockup — só que compila, builda, e
  roda sem erro. Recomendo você abrir `http://localhost:8082` (ou rodar `npx expo start` e escanear
  o QR code num celular) para validar visualmente.

## 8. Serviços rodando agora

- Backend: `http://localhost:5000` (API) + porta `8085` (TCP JFL) — processo em background.
- Mobile web (dev): `http://localhost:8082` — processo em background.

## 9. Pendências / próximos passos (não fazer sem seu OK)

- Validar `SecurityStamp` por request (quando "trocar senha" existir).
- Testar lockout ao vivo (código revisado, não exercitado por causa do rate limit).
- Testar em dispositivo real via Expo Go (só web foi verificado aqui).
- Endpoint de comando armar/desarmar (Sprint futura, envolve protocolo JFL 0x4E/0x4F).
- Telas de Câmeras/Acessos (fora do escopo desta sprint).
