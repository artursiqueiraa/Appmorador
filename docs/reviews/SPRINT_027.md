# Reviewer — Sprint 27 (Segurança da conta e confiabilidade)

Data: 2026-11-08. Referências: [plano](../sprints/SPRINT_027.md), [ADR 0043](../adr/0043-seguranca-conta-confiabilidade.md), [homologação](../testing/Sprint27.md).

| Pilar | Avaliação |
|---|---|
| **Arquitetura** | Os comandos têm uma porta única (`ExecucaoComandoServico`, na Application). Os controllers só traduzem os cabeçalhos HTTP (`ComandoHttp`). A saúde usa portas (`IVerificadorBanco`, `IEstadoRecepcaoJfl`), sem o Domain conhecer o JFL. Ressalva: o batimento das rotinas é um singleton em memória, aceitável enquanto houver uma instância só (dívida 64). |
| **Segurança** | O PIN fica só como hash, é sempre validado no servidor e bloqueia após 5 erros. A biometria nunca sai do aparelho. A idempotência usa um índice único, que resolve a concorrência. O cookie de sessão é HttpOnly, Strict e restrito a `/api/auth`. O CORS só aceita credenciais das origens configuradas. `/health` público não expõe detalhes. A senha do backup fica em DPAPI. Pendentes: painel admin (dívida 68) e rotação da chave Resend exposta no terminal. |
| **Produto** | O dono sabe quem desarmou ou abriu. Quem não tem PIN é convidado a criar, sem bloqueio. "Esqueci o PIN" usa a senha da conta. |
| **UX/UI** | A folha de PIN tem teclado próprio, tentativas restantes e Cancelar. O convite explica o porquê em uma frase. A tela de Segurança mostra o que está ativo. Tudo foi testado no web e no emulador. |
| **Performance** | A auditoria acrescenta 1 a 2 escritas por comando. A resposta guardada (`RespostaJson`) só é gravada quando há chave. O vigia roda a cada 1 minuto com consultas leves. |
| **Acessibilidade** | As teclas do PIN têm rótulos, o erro usa `accessibilityRole="alert"` e os toques têm alvo de 56 px. Os interruptores têm rótulo. |
| **Manutenção** | Testes: 21 novos no backend (PIN, execução, idempotência, saúde) e 7 no app (fluxo de confirmação). Os scripts de backup são parametrizados e registram em log. O `.easignore` está documentado. |
| **Documentação** | Atualizados: ADR 0043, CHANGELOG, ALTERACOES_BANCO (incluído o registro da Sprint 26, que faltava) e DIVIDA_TECNICA (37 e 51 resolvidas, 68 e 69 novas). Homologação descrita em `docs/testing/Sprint27.md`. |

**Parecer:** aprovada para homologação em dispositivo. O que bloqueia o encerramento é o checklist
do celular, principalmente a digital/rosto e o bloqueio do app, que o emulador não cobre sem uma
digital cadastrada.
