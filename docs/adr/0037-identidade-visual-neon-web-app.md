# ADR 0037 — Identidade visual oficial Akteck Seg (verde neon) no Web App

**Data**: 2026-11-03

## Contexto

O usuário enviou a referência visual oficial da Akteck Seg e pediu fidelidade a ela no Web App, preservando backend, APIs, rotas, regras de negócio e integrações reais, sem dados fictícios. A referência traz fundo preto-esverdeado, verde neon (#00E69A), cartões com borda verde e brilho, botões em degradê, um logo "A" com órbita e as telas:
- Login, Início e Pessoas;
- Cadastro de pessoa e Cadastro facial;
- Portas e Acessos, Câmeras e Alarme;
- Eventos e Informações da Unidade.

## Decisão

- **Paleta:** o azul provisório do ADR 0035 sai do Web App e entra o verde neon como cor de marca. Os contrastes foram medidos (AA). A borda de campo passou de 2.90:1 para `#0A7F57`, que dá ≥ 3:1. O Painel Web mantém o ADR 0035.
- **Efeitos:**
  - brilho com `boxShadow`, que o react-native-web aceita;
  - degradê em SVG (`components/neon/Degrade.tsx`), sem dependência nova.
  - **Lição técnica:** o SVG precisa de `width`/`height` 100%, senão fica com a largura de antes do layout. Ícones soltos ficam atrás de irmãos posicionados no navegador, por isso vão dentro de uma `View`.
- **Componentes compartilhados** (`components/neon/`): `TelaCabecalho`, `BotaoRedondo`, `CartaoNeon`, `IconeQuadrado`, `AbasSegmentadas`, `NumeroDestaque`, `AcaoRapida`, `LinhaEvento` e `TituloSecao`.
  - `PrimaryButton` ganha degradê, ícone e as variantes `danger` e `compacto`.
  - `TextField` ganha ícone e o botão de mostrar/ocultar senha.
  - Com isso, todas as telas existentes herdam o visual sem mudar de lógica.
- **Navegação:** Início / Pessoas / Acessos / Câmeras / Mais, com a aba ativa iluminada.
- **Fidelidade com honestidade.** Mantive o que a referência mostra **e o sistema tem**; o resto foi adaptado ou omitido:
  - **Início:** "Acessos hoje" não existe no backend, então vira "Pessoas". Os antigos botões "Armar total/Noturno/Desarmar" do Início eram só visuais e foram removidos. "Armar Alarme" leva à tela **Alarme**, que usa os comandos reais da central JFL (status, armar e desarmar por partição).
  - **Portas:** só "Abrir" (o pulso da PGM). "Manter Aberta" e "Fechar" não têm comando real.
  - **Vínculos:** Morador, Dependente e Visitante. Lojista e Prestador não existem no domínio.
  - **Câmeras:** mostram a última imagem e o estado Online/Offline, nunca "ao vivo" (Regra de Vocabulário, ADR 0024). Gravar, áudio e falar não existem.
  - **Eventos:** o filtro é por período (real, no servidor). Não há categoria nos eventos, então não há abas Alarmes/Acessos/Câmeras.
  - **"Esqueceu sua senha?":** explica que a recuperação ainda é pelo atendimento. Não existe envio por e-mail.
  - **Fotos de prédio e câmera** da referência não foram inventadas: o banner usa degradê e um ícone.
- **Sessão:** `AuthContext.trocarPropriedade()` volta para "Suas propriedades" sem sair da conta (seletor do Início).

## Consequências

- O Web App e o app mobile agora têm identidades diferentes. Levar a identidade ao mobile exige uma Sprint própria, com APK e homologação em dispositivo (Regra de Validação em Dispositivo).
- Na web, as imagens autenticadas (foto, câmera) passam por `useImagemAutenticada`, que busca com token e gera uma URL `blob:`, porque `<Image>` não envia header de autenticação no navegador.
