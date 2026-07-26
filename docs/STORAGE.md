# Inventário de Arquivos Persistentes / Não Versionados

Sprint 17.5 (Release 0.9.0). Tudo que existe fora do controle de versão (git) e por quê, para que
uma recuperação de desastre saiba exatamente o que precisa ser recriado/restaurado e o que é só
cache descartável.

## Backend

| Item | Local | Versionado? | Natureza |
|---|---|---|---|
| Segredos (`Jwt:Key`, connection string real) | `%APPDATA%\Microsoft\UserSecrets\752195b8-1e51-4397-bc77-b66da4cb95f9\secrets.json` (Windows, dev) | Não (por design — fora da árvore do repo) | Configuração sensível. Perdida ao trocar de máquina — precisa ser reconfigurada (`docs/setup/SETUP_AMBIENTE.md` seção 2), nunca restaurada de um backup (é específica da máquina/ambiente). |
| Snapshots de câmera no disparo de alarme | `backend/src/AppMorador.Api/snapshots/` (relativo, `Snapshots:BasePath`) | Não (`**/snapshots/` no `.gitignore`) | Dado sensível (imagem real de propriedade/residência) gerado em runtime pelo `SnapshotCaptureService`. Hoje vazio neste ambiente (nenhum disparo real capturado) — se existir em produção, é dado de cliente, não um artefato de build. |
| Build output (`bin/`, `obj/`) | `backend/src/*/bin`, `backend/src/*/obj` | Não | Reconstruível via `dotnet build`/`dotnet restore`. |
| Banco de dados (MySQL) | Serviço MySQL local/servidor (fora da árvore do projeto) | Não (nem poderia — é um SGBD externo) | Ver `database/README.md` e `docs/DISASTER_RECOVERY.md` para backup/restore. |
| Logs de execução | Console (stdout), sem arquivo por padrão nesta versão | Não aplicável | Nenhuma configuração de `Serilog`/arquivo de log persistente hoje — só `ILogger` padrão do ASP.NET Core no console. Se isso mudar no futuro, atualizar esta tabela. |

## Mobile

| Item | Local | Versionado? | Natureza |
|---|---|---|---|
| `.env` (URL da API local) | `mobile/.env` | Não (`.env`/`.env.*` no `.gitignore`, exceto `.env.example`) | Configuração de máquina/rede do desenvolvedor — cada pessoa aponta para o próprio Backend. |
| `node_modules/` | `mobile/node_modules` | Não | Reconstruível via `npm install`. |
| Cache do Metro/Expo | `mobile/.expo`, `mobile/dist` | Não | Reconstruível, seguro apagar a qualquer momento (`scripts/clean_project.ps1`). |
| Progresso do Onboarding Wizard | `expo-secure-store` **no dispositivo do usuário final** (chave por Propriedade) | Não aplicável (dado do usuário final, não do desenvolvedor) | Sprint 16. Perdido se o app for desinstalado do celular — sem impacto no backend (a Propriedade em si continua existindo no banco). |
| Preferência de perfil (morador/técnico) | `expo-secure-store` no dispositivo, chave `perfilPreferencia` | Não aplicável | Sprint 17. Nunca uma fronteira de segurança real (ver ADR 0020) — perdida ao reinstalar, volta ao padrão `'morador'`. |
| Rótulos amigáveis de PGM | `expo-secure-store` no dispositivo, chave `pgmLabels_{equipamentoId}` | Não aplicável | Sprint 17. Perdida ao reinstalar/trocar de dispositivo — volta ao rótulo padrão `Comando N` (ver `docs/DIVIDA_TECNICA.md` item 35). |
| Foto de credencial facial (pré-visualização) | `expo-secure-store` no dispositivo, chave `fotoFacialLocal_{credencialId}` | Não aplicável | Sprint 17. **Não existe no backend** — é só uma miniatura local, perdida ao reinstalar (ver `docs/DIVIDA_TECNICA.md` item 32). |
| Credenciais de assinatura Android (keystore) | Servidores da Expo (EAS — "Using remote Android credentials (Expo server)") | Não (gerenciado pela Expo, fora do repositório e da máquina local) | Necessário para gerar um APK/AAB assinado com a mesma identidade em builds futuras. Ver `docs/DISASTER_RECOVERY.md` seção "Geração de APK". |

## Outros

| Item | Local | Versionado? | Natureza |
|---|---|---|---|
| Dumps de banco (backup completo pontual) | `database/backup/*.sql` | Não (só a estrutura da pasta, via `.gitkeep`) | Ver `database/README.md` — o dump oficial da Release 0.9.0 fica anexado como asset da Release no GitHub, não no histórico do git. |
| Material de referência/protótipo (`ft/`) | `ft/` (raiz do repo) | Não (`/ft/` adicionado ao `.gitignore` nesta Sprint) | Imagens e um componente `.jsx` de protótipo (Sprint 16, UX001) deixados soltos na raiz pelo usuário — não fazem parte do código do app. Mantidos localmente por referência, nunca fizeram parte de nenhuma tela real; excluídos do versionamento para não confundir "isto é código do produto" com "isto é material de referência visual". |

## Regra geral

Nada nesta lista precisa ser versionado no git — tudo é (a) reconstruível por uma ferramenta
(`dotnet restore`, `npm install`), (b) específico da máquina/ambiente (segredos, `.env`), (c) dado
de runtime gerado pelo próprio sistema (snapshots, banco), ou (d) material de referência solto,
não código. Se um item novo passar a existir fora dessas categorias, ele deveria ser versionado —
revisar esta tabela quando isso acontecer.
