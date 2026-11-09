# Sprint 24.5 — Portões e portas (tudo que abre, num lugar só)

> Fase 1 (planejamento, sem código) — 2026-11-08. Pedido do usuário durante a homologação da
> Sprint 24: "Portões e portas" deve mostrar todas as portas, seja leitor facial ou PGM da central.

## 1. Objetivo

O dono abre qualquer portão ou porta da propriedade pela mesma tela, sem precisar saber se quem
abre é a central de alarme ou o leitor facial.

## 2. Levantamento (estado real)

| Ponto | Situação |
|---|---|
| Tela "Portões e portas" | É o cadastro `PontoAcesso` da Sprint 7: só nome e tipo (ex.: "Recepcao"), **sem ligação com equipamento**. Não abre nada. É usado nas permissões de acesso (quem pode entrar onde). |
| PGM da central JFL | Abre de verdade (`POST /api/equipamentos/{id}/jfl/pgm/acionar`). Aparece só no Início, em "Portões", com nome dado no celular (`pgmLabels`, preferência local). |
| Leitor facial Control iD | Cadastra rosto e recebe eventos; **não existe comando de abrir porta** no provider. |
| Hikvision | Provider de controle de acesso ainda não existe (previsto na Sprint 26). |
| Intelbras | Central de alarme; sem PGM suportada hoje (ADR 0020). |

## 3. Proposta

1. **Ligar Ponto de Acesso a quem abre e a quem mostra.** O cadastro ganha dois vínculos opcionais:
   - "acionador": um equipamento mais o número da saída (PGM da JFL ou porta/relé do leitor);
   - **câmera do acesso**: a câmera que mostra aquela porta/portão.

   Um ponto pode existir sem nenhum dos dois (só para permissões, como hoje).
2. **Abrir pelo Ponto de Acesso.**
   - Endpoint `POST /api/pontos-acesso/{id}/abrir`.
   - O backend decide o comando pelo fabricante do acionador:
     - JFL → PGM (já existe);
     - Control iD → comando de porta (novo);
     - Hikvision → quando o provider existir.
   - Cada abertura fica no histórico com quem abriu.
3. **Comando de abrir porta no Control iD.** Novo método no `IControlIdProvider` (padrão ADR 0014),
   pela API HTTP do equipamento. Validado no simulador HTTP local; o teste no aparelho real entra na
   homologação física já pendente.
4. **Tela "Portões e portas" nova** (pedido do usuário, 2026-11-08).
   - Lista todos os pontos com o estado (disponível / equipamento sem comunicação).
   - **Tocar num acesso abre a tela do acesso**: imagem da câmera daquela porta (a última imagem,
     com "Atualizar"; vídeo ao vivo entra na Sprint 30), e dois botões: **Abrir** e **Agora não**.
     Assim o dono sempre vê quem está na porta antes de abrir. Essa tela também é a confirmação
     contra toque acidental (substitui o "toque duas vezes").
   - Sem câmera vinculada, a tela do acesso mostra só o nome e os botões.
   - O formulário passa a ter "O que abre esta porta?", com a lista de saídas reais da propriedade:
     PGMs da central e portas dos leitores.
5. **Início usa a mesma fonte.** O bloco "Portões" deixa de ler PGMs direto e mostra os pontos com
   acionador. O nome vem do cadastro (servidor), não mais da preferência local do celular; os nomes
   já salvos são migrados no primeiro acesso.
6. **Permissão.** Só quem tem `AbrirPortao` (Sprint 21) vê e usa o botão Abrir.

## 4. Fora do escopo

- Provider Hikvision (Sprint 26): a tela já fica pronta para ele, mas o comando só existe quando o
  provider for criado.
- Abrir por proximidade/geolocalização (Sprint 31).
- Plataforma genérica de comandos com idempotência (ADR 0033).

## 5. Modelo de dados (migration aditiva)

| Tabela | Mudança |
|---|---|
| `PontosAcesso` | + `EquipamentoAcionadorId` (opcional), + `NumeroSaida` (opcional), + `CameraId` (opcional) |
| `AberturasPontoAcesso` (nova) | Id, PontoAcessoId, UsuarioId, Sucesso, Mensagem, OcorridoEmUtc |

## 6. Riscos

| Risco | Mitigação |
|---|---|
| Comando Control iD diferente no aparelho real | Isolado no provider; validado no simulador; marcado como pendente de homologação física. |
| Nomes de portão salvos só no celular | Migração automática no primeiro acesso e, se falhar, o nome padrão "Saída N". |
| Abrir sem querer | A abertura só acontece na tela do acesso, depois de ver a câmera e tocar em **Abrir**. |

## 7. Decisões pedidas ao usuário

- Aprovar o escopo acima (Fase 2 em etapas: backend → Control iD → tela do acesso com câmera →
  Início).
- ~~Toque duplo ou toque único~~ — resolvido pelo pedido do usuário: abrir passa pela tela do
  acesso com a câmera.
