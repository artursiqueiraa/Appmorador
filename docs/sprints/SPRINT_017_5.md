# Sprint 17.5 — Release 0.9.0, Backup, Portabilidade e Disaster Recovery

## Missão

Transformar o AppMorador num projeto totalmente reproduzível, documentado, versionado e
recuperável — sem alterar nenhuma funcionalidade, regra de negócio, API ou integração. Criar um
ponto de restauração completo (Release 0.9.0) para garantir rastreabilidade e continuidade do
desenvolvimento.

## Escopo entregue

1. **Auditoria do Ambiente** (`docs/AUDITORIA_AMBIENTE.md`) — versões confirmadas por inspeção
   direta desta máquina (não suposições): .NET 8.0.421, Node 24.16.0, MySQL 8.0.46, EAS CLI
   20.5.1, portas, dependências, ferramentas auxiliares (simuladores).
2. **Backup do Banco** (`database/`) — `schema.sql` e `seed_data.sql` versionados; dump completo
   (`appmorador_full_20260725.sql`) gerado e anexado como asset da Release (não versionado no git,
   decisão documentada em `database/README.md`).
3. **Arquivos Persistentes** (`docs/STORAGE.md`) — inventário completo do que existe fora do
   controle de versão e por quê (segredos, snapshots de câmera, cache local do Mobile,
   credenciais de assinatura Android geridas pela Expo).
4. **Variáveis de Ambiente** (`backend/.env.example` novo + `docs/ENVIRONMENT.md`) — cada
   variável documentada, com o mecanismo real de cada stack (env vars reais no Backend,
   `.env` real no Mobile via Expo).
5. **Scripts** (`scripts/`) — `backup_database.ps1`, `restore_database.ps1`, `setup_project.ps1`,
   `clean_project.ps1`, `start_backend.ps1`, `start_mobile.ps1`, `start_frontend.ps1` (documenta
   honestamente a ausência de um frontend web).
6. **Documentação** — `docs/SETUP.md` (ponto de entrada), `docs/ARCHITECTURE.md`,
   `docs/RELEASE_0.9.0.md`.
7. **Git** — `.gitignore` revisado (pasta de referência `ft/` excluída; dumps de banco pontuais
   excluídos); **bug crítico encontrado e corrigido** (ver "Achado crítico" abaixo).
8. **Build de Validação** — `dotnet build` (0 erros), `npm run typecheck`/`lint` (0 erros),
   `expo-doctor` (20/20) — antes e depois do fix do gitignore.
9. **Publicação** — commit único consolidando as Sprints 4-17 (nunca commitadas desde
   `v0.3.0-alpha`) + Sprint 17.5, tag `v0.9.0`, branch padrão do GitHub corrigido para `main`.
   Release do GitHub preparada (notas + asset) — criação bloqueada pelo classificador de
   segurança do ambiente de execução, comando entregue ao usuário para rodar diretamente.
10. **Verificação de Portabilidade** — clone real do repositório publicado, do zero, em diretório
    limpo — encontrou e validou a correção de 2 impedimentos reais (ver abaixo).
11. **Disaster Recovery** (`docs/DISASTER_RECOVERY.md`) — recuperação de banco/ambiente, troca de
    URL da Api, geração de APK, publicação de nova versão, atualização de dependências,
    checklist pós-restauração, plano de contingência, RTO/RPO.

## Achado crítico (encontrado pela própria verificação de portabilidade desta Sprint)

A regra `**/snapshots/` do `backend/.gitignore` (destinada só à pasta de runtime de imagens
capturadas no disparo de alarme) colidia — por causa do casamento de padrão sem diferenciar
maiúsculas/minúsculas do git no Windows — com os namespaces de código-fonte `Snapshots/` de
`AppMorador.Domain` e `AppMorador.Infrastructure`. **16 arquivos de código real (integração de
câmera: `CameraResolver`, providers CGI/ISAPI de Dahua/Hikvision/Intelbras,
`SnapshotCaptureService`) nunca haviam sido versionados**, desde que essa funcionalidade foi
construída (antes desta Sprint). O build local sempre funcionou normalmente (os arquivos existem
em disco) — só um clone limpo expunha o problema (`error CS0234: The type or namespace name
'Snapshots' does not exist`). Corrigido: regra reescrita para `src/AppMorador.Api/snapshots/`
(escopo específico, sem colisão possível), os 16 arquivos commitados, tag `v0.9.0` recriada
apontando para o commit corrigido.

Esse achado é exatamente o valor da Fase de Verificação de Portabilidade: nenhuma auditoria
estática do repositório local teria encontrado isso — só testar um clone genuinamente limpo expôs
o problema.

## Outros achados da verificação de portabilidade

- **`git config core.longpaths` não habilitado**: nomes de migration do EF Core (`*.Designer.cs`)
  em caminhos de clone razoavelmente aninhados ultrapassam 260 caracteres no Windows —
  `git checkout` falhava com `Filename too long`. Corrigido automaticamente por
  `scripts/setup_project.ps1`; documentado como pré-requisito em `docs/SETUP.md`.
- **Branch padrão do GitHub era `release/v0.3.0-alpha`**: um `git clone` sem especificar branch
  trazia código de antes da Sprint 4. Corrigido (`gh repo edit --default-branch main`).

## Fora de Escopo (confirmado, nada alterado)

Domínio, APIs, backend (lógica de negócio), integrações, SignalR, UX, funcionalidades, dados do
banco (exceto geração de backup). Confirmado por `dotnet build`/`npm typecheck`/`lint` idênticos
antes e depois (fora do fix do gitignore, que é puramente de versionamento, não de comportamento
em runtime).

## Processo

Executado em etapas pequenas, cada uma seguida de build de validação: Auditoria → Backup do Banco
→ Storage → Environment → Scripts → Documentação (Setup/Architecture/Release) → revisão de Git →
build final → commit único (Sprints 4-17.5) → push → tag → verificação de portabilidade (clone
real) → **achado crítico encontrado e corrigido** → tag recriada → segunda verificação de
portabilidade (confirmando o fix) → Disaster Recovery → documentação final.

## Critérios de Aceite

Ver `docs/reviews/SPRINT_017_5.md` para o relatório completo com evidências e o parecer do
Reviewer nos 8 pilares desta Sprint.
