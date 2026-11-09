# ADR 0038 — Web App volta ao protótipo original (grafite + status) com navegação reorganizada

**Data**: 2026-11-03

## Contexto

O usuário não aprovou o visual verde neon (ADR 0037) e pediu que o Web App seguisse a ideia
original do produto, isto é, o protótipo UX001 ("Segurança Conectada"). Os pontos centrais do pedido:

- base grafite-azulada, com a semântica de status como assinatura;
- Início com o cartão "PROTEGIDO" e o anel pulsando, os três botões de arme, câmeras e atividade;
- aba Acessos enxuta;
- abas importantes embaixo e um menu lateral para o resto.

## Decisão

- **Paleta do protótipo** (`theme/colors.ts`):
  - verde para protegido e ação, âmbar para aberto e atenção, vermelho para disparo;
  - único desvio: o cinza `mute` passou de 3.0:1 para 5.4:1, para passar em contraste AA;
  - efeitos discretos, sem brilho neon, e botão principal em verde sólido.
- **Navegação:**
  - abas Início, Câmeras, Alarme e Moradores, mais o botão **Menu**, que abre um menu lateral;
  - menu lateral com histórico, portões e portas, visitantes, vagas, entregas, minha propriedade, notificações, ajustes e trocar de propriedade;
  - no computador, a barra lateral mostra as abas e o menu inteiro, além do seletor de propriedade;
  - fonte única dos itens: `navigation/itensMenu.ts`.
- **Início = protótipo, com dados reais:**
  - `HeroStatus` e `BotoesArme` usam `useCentralAlarme`, com status e comandos reais da central JFL:
    "Armar total" chama `armar`, "Noturno" chama `armar-stay` e "Desarmar" chama `desarmar`;
  - os botões, que até a Sprint 16 só mudavam um estado local, agora agem de verdade;
  - "Abrir portão" (`useComandosPortao`, a lógica que estava na aba Acessos) foi para o Início;
  - câmeras mostram a última imagem, nunca "ao vivo".
- **Aba Moradores = "Acessos" do protótipo:** o cartão "Cadastrar rosto" e as abas Moradores (selo
  Facial ou Pendente) e Provisórios (visitantes). Portões, vagas e entregas saíram dela.
- **`TelaCabecalho`:** volta ao padrão do protótipo (voltar quadrado, título à esquerda, sem repetir o logo).

## Consequências

- O ADR 0037 (neon) fica substituído no Web App. O logo Akteck Seg continua.
- "Ajustes" deixou de ser aba: virou tela da pilha, aberta pelo menu.
- Mobile continua intocado (Sprint própria, com APK e homologação em dispositivo).
