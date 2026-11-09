# Homologação — Sprint 23 (Base única web + Android + iOS)

## Build

| Campo | Valor |
|---|---|
| Build ID | `934faa0a-177b-4389-9c1a-0b345024dd8b` |
| Link do EAS Build | https://expo.dev/accounts/ostresmosqueteiros/projects/appmorador/builds/934faa0a-177b-4389-9c1a-0b345024dd8b |
| APK Preview | https://expo.dev/artifacts/eas/oq-N9lCnA2ZFKSX-gtCWfp-iBWX7zzp9iGTBM1pqBWs.apk |
| Versão do app | 1.0.0 (`webapp/app.json`) |
| Identificador | `com.akteck.seg` (app novo; o antigo "Segurança Conectada" pode continuar instalado) |
| Perfil | `preview` (`webapp/eas.json`), API em `http://192.168.100.2:5027` |

## Release Notes

```
Release Notes — Sprint 23

Novidades
• O app de celular agora sai da mesma base do app web: nome, ícone e visual Akteck Seg
• Tela inicial do protótipo (PROTEGIDO, armar/noturno/desarmar, portões, câmeras)
• Aba Moradores com cadastro de rosto direto pela câmera do celular
• Menu lateral com histórico, visitantes, vagas, entregas, notificações e ajustes

Correções
• Corrigida regressão de memória conhecida do motor JavaScript (Hermes) do Expo SDK 57

Observação técnica
• Sem mudança no servidor nem no banco de dados
```

## Antes de testar

1. Computador e celular na mesma rede; IP da máquina ainda `192.168.100.2`.
2. Backend rodando e acessível na rede (`0.0.0.0:5027`).
3. Instalar o APK pelo link acima (permitir "fontes desconhecidas" se pedir).

## Checklist da Sprint

| # | Verificação | Resultado |
|---|---|---|
| 1 | App instala com nome **Akteck Seg** e ícone do "A" verde | |
| 2 | Splash com o símbolo Akteck no fundo grafite | |
| 3 | Login abre e entra | |
| 4 | Início: cartão de status, botões Armar total / Noturno / Desarmar | |
| 5 | Início: imagens das câmeras aparecem (miniaturas) | |
| 6 | Aba Câmeras: última imagem de cada câmera carrega | |
| 7 | Aba Moradores: fotos (avatares) das pessoas com rosto cadastrado aparecem | |
| 8 | Cadastrar rosto → a câmera do celular abre (pede permissão com texto em português) | |
| 9 | Foto tirada é enviada e a pessoa passa a mostrar o selo "Facial" | |
| 10 | "Usar uma foto da galeria" também envia | |
| 11 | Menu (aba da direita) abre o menu lateral com todos os itens | |
| 12 | Tela gira/teclado não cobre campos no login e nos formulários | |

## Regressão obrigatória

| # | Fluxo | Resultado |
|---|---|---|
| R1 | Login | |
| R2 | Logout | |
| R3 | Troca de propriedade | |
| R4 | Início (Dashboard) | |
| R5 | Acessos / Moradores | |
| R6 | Push: permissão pedida e dispositivo registrado (envio real depende do Firebase — Sprint 24) | |
| R7 | Tempo real (SignalR): mudança de status aparece sem recarregar | |

## Resultado

**Aprovada** (rodada 2).

### Rodada 1 — build `934faa0a` (2026-11-08)

Reprovada.

| Severidade | Bug | Ação |
|---|---|---|
| High | Botões principais: o verde não preenche a largura (SVG em "100%" medido antes do layout no Android) | `Degrade` mede pelo `onLayout`; cores iguais viram fundo sólido. Mesmo ajuste no brilho do `HeroStatus` |
| High | Título das telas por baixo da barra de status (Android de borda a borda, sem recuo superior) | Recuo da área segura aplicado uma vez em `ColunaConteudo`, que envolve todas as telas; removido o recuo duplicado de `DetalheCamera` e `SelecionarPropriedade` |

### Rodada 2 — build `e6713e1d-b31b-4def-b4e8-0345d30ee45f`

APK: https://expo.dev/artifacts/eas/CaAvp44PvT9M6moT2ha678k8EGyJ32otnz9ewgmDIb4.apk

**Aprovada** pelo usuário em 2026-11-08 ("validado"). Os dois bugs High da rodada 1 foram
confirmados como corrigidos; nenhum bug novo relatado.

## Encerramento

- `mobile/` arquivado na tag git `mobile-legado` (commit `e300e89`) e removido da árvore.
- Dívidas técnicas 52 e 59 resolvidas.
- Pendências que seguem: dívida 60 (HTTP + IP fixo no APK de teste) e 61 (`EAS_NO_VCS`).
