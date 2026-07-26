# Sprint 5 — Alinhamento Visual ao Protótipo

## Missão

Utilizar o protótipo anexado (`ft/seguranca-conectada-prototipo (1).jsx` + 3 imagens de
referência) apenas como referência visual. Preservar toda a arquitetura, lógica, navegação e
contratos existentes do AppMorador. O objetivo é aproximar a interface do produto ao design
apresentado, sem reescrever o aplicativo nem substituir a implementação atual por uma nova.

## Achado inicial

A paleta de cores do protótipo (`c = { bg, surface, safe, warn, danger, accent, ... }`) é
idêntica, valor por valor, a `mobile/src/theme/tokens.ts` — o Design System atual já nasceu deste
protótipo (ou de um ancestral idêntico). O trabalho desta Sprint é composição/estilo visual dos
componentes existentes, não tokens novos.

## Escopo — polimento visual seguro (dado/contrato/navegação inalterados)

- `CardSaude`: hero com anel "respirando" (Reanimated) e glow radial, ícone maior.
- `HeaderDashboard`: aproximar layout do protótipo.
- `CardResumoInstalacao`: ícones em caixa arredondada (consistência visual).
- `ItemEvento`/`CardUltimaAtividade`: ícone em caixa arredondada (protótipo) em vez de círculo.
- `AcoesRapidas`: refinar visual; **adicionar 3º estado "Noturno"** (visual-only, sem comando
  real à central — mesmo padrão já usado em Armar/Desarmar), decisão confirmada com o usuário.
- Nova seção discreta "em breve" para visualização ao vivo de câmeras (mesmo padrão de
  `RecursosFuturos` da Sprint 4), decisão confirmada com o usuário.

## Fora de escopo (presente no protótipo, não implementado)

- Grade de câmeras com thumbnail/vídeo "AO VIVO" real.
- Tela "Acessos" (cadastro facial, QR code provisório).
- Overlay de "Alarme disparado" com timeline de vídeo pré/pós-roll, sirene, ligar 190.
- Navegação por abas fixas (Início/Câmeras/Acessos/Ajustes) — hoje é stack simples.

Todos já registrados como backlog em Sprints anteriores ou nesta.

## Processo

Mesmo processo de duas fases das Sprints anteriores de produto: Fase 1 (plano, sem código,
mapeamento protótipo × implementação real, perguntas de decisão) → Fase 2 (implementação em
etapas pequenas, validadas).
