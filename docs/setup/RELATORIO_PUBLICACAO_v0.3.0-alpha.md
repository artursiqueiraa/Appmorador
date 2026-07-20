# Relatório — Preparação para Publicação da v0.3.0-alpha

**Data**: 2026-07-20
**Natureza**: auditoria/limpeza/organização do repositório — nenhuma funcionalidade nova.

## Relatório da auditoria (apresentado antes de qualquer remoção)

| # | Achado | Ação |
|---|---|---|
| 1 | `admin/` — diretório vazio (nem arquivo oculto) | Removido |
| 2 | `mobile/LICENSE` — licença MIT do Expo/650 Industries, mal-atribuída ao projeto (resquício do `create-expo-app`) | Removido |
| 3 | `backend/.gitignore` ignorava `**/appsettings.Development.json` em qualquer projeto — esse arquivo hoje só tem `Logging`/`Cors`, zero segredo; se ficasse ignorado, um clone novo nunca receberia o `Cors:AllowedOrigins` necessário pro Mobile-web funcionar | Corrigido — só `appsettings.*.local.json`/`secrets.json` continuam ignorados (convenção certa pra segredo local) |
| 4 | `mobile/.gitignore` ignorava só `.env*.local`, não `.env` — `mobile/.env` ficaria versionado | Corrigido: `.env`/`.env.*` ignorados, `.env.example` criado e versionado no lugar |
| 5 | Nenhum segredo real versionável encontrado (`Jwt:Key`/connection string real só em `user-secrets`, fora da árvore do repo) | Nenhuma ação — já correto |
| 6 | `snapshots/` (imagens de câmera capturadas em disparo de alarme) não existia ainda, mas é dado sensível por natureza | Adicionado ao `.gitignore` preventivamente |
| 7 | `bin/`/`obj/` (5 projetos .NET), `node_modules/`/`.expo/` (Mobile) | Já cobertos pelos `.gitignore`s existentes — nenhuma ação manual necessária |
| 8 | Não existia `README.md` nem `LICENSE` na raiz | `README.md` criado; `LICENSE` pendente (ver Segurança/Pendências) |
| 9 | Não existe `tests/` nem `scripts/` | Não criadas vazias/fake — o projeto genuinamente não tem suíte automatizada ainda (toda homologação foi via `curl` real, documentada em `docs/TESTES_FUNCIONAIS.md`/`docs/reviews/`). Documentado no README como estado real, não escondido. |
| 10 | `mobile/package.json` tinha `"version": "57.0.9"` (resquício da versão do SDK Expo do template) | Atualizado para `"0.3.0-alpha"` |
| 11 | Código não utilizado | Não foi feita varredura profunda de símbolos mortos (exigiria ferramenta dedicada tipo `ts-prune`/analyzer Roslyn) — fora do escopo desta auditoria de repo-hygiene sem ferramenta própria; nenhum arquivo órfão óbvio encontrado na navegação manual |

## Ajustes no `.gitignore` (3 arquivos)

- **`.gitignore`** (raiz, novo): IDE (`.vscode/*` com exceção de `extensions.json`/`settings.json`, `.idea/`), OS (`.DS_Store`, `Thumbs.db`), logs, `*.mdf`/`*.ldf`/`*.db`/`*.sqlite`, `.env`/`.env.*`, e uma regra preventiva contra pastas `*_backup_*/` (motivada pela cópia duplicada acidental encontrada e removida numa tarefa anterior).
- **`backend/.gitignore`**: adicionado `TestResults/`; parou de ignorar `appsettings.Development.json` (ver achado #3); `snapshots/` adicionado.
- **`mobile/.gitignore`**: `.env`/`.env.*` ignorados com exceção de `.env.example`.

## Verificação de segurança

- `appsettings.json`: só tem `Password=CHANGE_ME` (placeholder, nunca usado de verdade).
- `Jwt:Key` e a connection string real: confirmados só em `dotnet user-secrets`
  (`%APPDATA%\Microsoft\UserSecrets\<guid>\secrets.json`), fisicamente fora da árvore do
  repositório — nunca em risco de serem versionados independente do `.gitignore`.
- Nenhuma senha, token, API key ou certificado (`.pfx`/`.p12`/`.key`/`.crt`) encontrado em
  qualquer arquivo versionável do repositório.
- `mobile/.env` (config local, não segredo) agora corretamente ignorado, com `.env.example`
  versionado no lugar.

## Organização

Estrutura final:

```
appmorador/
├── .agents/  .rules/  CLAUDE.md   (roteador de engenharia)
├── .gitignore
├── README.md
├── backend/        (.gitignore próprio)
├── mobile/         (.gitignore próprio, .env.example)
└── docs/
    ├── adr/  roadmap/  reviews/  sprints/  setup/
    ├── CHANGELOG.md  DIVIDA_TECNICA.md  ALTERACOES_BANCO.md  TESTES_FUNCIONAIS.md
```

`tests/` e `scripts/` não foram criadas — ver achado #9.

## README

Criado do zero na raiz. Cobre: descrição do produto, arquitetura (camadas + fronteira pt-BR/
inglês), tecnologias, requisitos, instalação, configuração (banco + segredos + mobile),
execução, credenciais de desenvolvimento (as 4 personas, com a ressalva de que não há RBAC),
estrutura do projeto (incluindo a nota honesta sobre ausência de testes automatizados), roadmap
e mapa de toda a documentação.

## Licença

**Pendência intencional**: perguntado ao usuário se deveria criar uma licença proprietária ou
MIT — decisão adiada por ele. `README.md` e `docs/DIVIDA_TECNICA.md` (item 9) registram o estado
como pendente, não definido silenciosamente.

## Versionamento

- `docs/CHANGELOG.md`: nova entrada `[v0.3.0-alpha] — 2026-07-20` no topo, resumindo esta
  preparação de publicação.
- `mobile/package.json`: `version` alinhada para `0.3.0-alpha`.
- Tag Git `v0.3.0-alpha` **não foi criada por mim** — está na lista de comandos sugeridos abaixo,
  para o usuário decidir quando rodar.

## Validação (pós-limpeza, sem regressão)

- `dotnet build` (solução completa): 0 erros, 0 warnings.
- `npx tsc --noEmit` (mobile): limpo.
- Backend e Mobile reiniciados em janelas visíveis próprias na máquina — ambos sobem
  corretamente (`Sistema pronto.` no log do Backend; Metro respondendo em `:8081`).
- Swagger: `GET /swagger/index.html` → 200.
- Login (conta Morador/Fernanda Oliveira): 200, token emitido.
- Propriedades: `GET /api/properties` → 200, retorna "Residencial Jardim das Flores".
- Dashboard: `GET /api/properties/{id}/dashboard` → 200, dados corretos.
- Eventos: `GET /api/properties/{id}/eventos` → 200, 5 ocorrências paginadas corretamente.

Nenhuma regressão encontrada — todos os resultados idênticos aos das validações anteriores
(Sprint 3.1 e tarefa de ambiente de desenvolvimento).

## Pendências

- **Licença**: decisão de negócio adiada pelo usuário (ver seção Licença).
- **Testes automatizados**: não existem; se/quando forem criados, `tests/` passa a existir com
  conteúdo real, não antes.
- **`scripts/`**: não criada; sem necessidade identificada hoje (setup é só `dotnet run`/
  `npx expo start`, documentado no README).

## Status final do repositório

Pronto para virar um repositório Git: sem arquivo temporário, sem segredo versionável, `.gitignore`
revisado em 3 níveis, `README.md` completo, estrutura organizada, build e fluxos revalidados sem
regressão. **Ainda não é um repositório Git** — nenhum comando Git foi executado nesta tarefa
(nem `git init`), por instrução explícita. Comandos sugeridos abaixo, para o usuário decidir
quando rodar.

## Comandos Git sugeridos (nenhum executado automaticamente)

Seguindo a recomendação do usuário de usar uma branch de release em vez de commitar direto em
`main`:

```bash
git init
git branch -M main
git remote add origin https://github.com/artursiqueiraa/Appmorador.git

git checkout -b release/v0.3.0-alpha
git add .
git status                          # conferir o que sera versionado antes de commitar
git commit -m "release: v0.3.0-alpha"
git tag v0.3.0-alpha
git push origin release/v0.3.0-alpha
git push origin v0.3.0-alpha
```

Depois de revisar a branch de release, criar `develop` (ou `feature/sprint-4-...`) a partir dela
para a Sprint 4, mantendo `release/v0.3.0-alpha`/a tag como ponto de retorno estável.

## Parecer do Reviewer

| Pilar | Avaliação |
|---|---|
| **Arquitetura** | ✅ Aprovado. Nenhuma mudança de código de domínio; só organização de repositório. |
| **Segurança** | ✅ Aprovado. Nenhum segredo real encontrado versionável; correção real feita (`.env` do Mobile passou a ser ignorado); regra do `appsettings.Development.json` corrigida sem reintroduzir risco (arquivo permanece sem segredo por convenção de processo já estabelecida). |
| **Produto** | ✅ Aprovado. Escopo respeitado — nenhuma funcionalidade nova; decisão de licenciamento (que é de negócio, não técnica) foi levantada ao usuário, não assumida. |
| **UX/UI** | ➖ N/A — nenhuma tela alterada. |
| **Performance** | ➖ N/A — mudanças são só de repositório/documentação. |
| **Manutenibilidade** | ✅ Aprovado. README único e completo dá a um desenvolvedor novo tudo que precisa sem contexto prévio; estrutura de `.gitignore` em 3 níveis é explícita sobre o que cobre cada um. |
| **Documentação** | ✅ Aprovado. README, CHANGELOG, DIVIDA_TECNICA (2 itens novos) e este relatório refletem o estado real do repositório, incluindo lacunas (sem testes automatizados, sem licença definida) sem escondê-las. |
| **Regressões** | ✅ Nenhuma encontrada. Build/`tsc` limpos; Swagger/Login/Propriedades/Dashboard/Eventos revalidados via requests reais após toda a limpeza, resultados idênticos aos anteriores. |

**Conclusão**: sem pendência bloqueadora para publicação.

---

**O projeto está pronto para ser publicado como v0.3.0-alpha no Git.**
