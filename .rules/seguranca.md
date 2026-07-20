# Regras: Segurança

## Objetivo

Garantir que o nível de segurança do sistema nunca regrida, independentemente da pressão por
velocidade de entrega — autenticação, autorização, segredos e proteção contra abuso permanecem
como pré-requisitos inegociáveis de qualquer funcionalidade nova.

## Princípios

- Nenhum segredo (chave de assinatura, senha de acesso a serviço, string de conexão) vive no
  código-fonte ou em arquivo versionado — sempre em mecanismo de configuração local seguro em
  desenvolvimento, e variável de ambiente/cofre de segredos em produção.
- Mensagens voltadas ao usuário nunca revelam mais informação do que o necessário — erro de
  autenticação nunca distingue qual campo especificamente estava errado; erro de autorização
  nunca confirma a existência de um recurso que não pertence a quem pediu.
- Toda superfície de entrada pública é validada antes de alcançar qualquer regra de negócio.
- Toda operação sensível a abuso (autenticação, criação de conta) tem mitigação de força bruta
  ativa por padrão, não como adição posterior.
- Autorização é sempre verificada no servidor — nunca confiar em um identificador de recurso
  enviado pelo cliente sem confirmar a posse dele.

## Convenções

- Senhas nunca são armazenadas em texto puro — sempre com função de hash com custo computacional
  deliberado, nunca reduzido "para performance" sem avaliação explícita.
- Tokens de sessão de longa duração nunca são armazenados em texto puro no servidor — sempre um
  hash comparável, nunca reversível.
- Ambientes de desenvolvimento e produção nunca compartilham segredo — cada ambiente tem o seu
  próprio, gerado de forma independente.
- Documentação e superfícies de introspecção (exploração interativa de API) ficam disponíveis
  apenas em ambiente de desenvolvimento, nunca em produção.

## Boas práticas

- Revisar toda nova superfície de entrada de usuário contra os riscos mais comuns de segurança
  (injeção, autenticação quebrada, exposição de dado sensível) antes de considerá-la pronta.
- Manter controle de origem cruzada restritivo por padrão — nenhuma origem é liberada sem
  necessidade explícita e configurada.
- Forçar tráfego criptografado e cabeçalhos de segurança em qualquer ambiente voltado ao público.
- Auditar periodicamente onde segredos são lidos e garantir que nenhum novo caminho de leitura
  os exponha em log ou resposta de erro.

## Padrões obrigatórios

- Toda funcionalidade que lida com autenticação, autorização, ou dado sensível é avaliada contra
  os riscos mais comuns de segurança web antes de ser considerada pronta.
- Toda tentativa de autenticação malsucedida é contabilizada com limite de tentativas e bloqueio
  temporário — nunca tentativas ilimitadas.
- Toda operação de escrita que afeta um recurso pertencente a um usuário confirma a posse desse
  recurso no servidor antes de executar.
- Nenhum segredo é logado, mesmo em nível de depuração.

## Anti-padrões

- Reduzir o custo de uma função de hash ou o rigor de um bloqueio por tentativas "para acelerar
  testes", esquecendo de reverter.
- Liberar origem cruzada de forma ampla "temporariamente" para destravar uma integração.
- Mensagem de erro que expõe detalhe interno (identificador de linha de banco, nome de tabela,
  rastro de exceção) diretamente ao cliente.
- Confiar em um identificador de recurso vindo do cliente sem verificar a posse dele no servidor.

## Checklist

- [ ] Algum segredo real está em um arquivo versionado ou hardcoded no código?
- [ ] Mensagens de erro de autenticação/autorização mantêm a ambiguidade proposital?
- [ ] Toda operação sensível a força bruta tem limite de tentativas e/ou bloqueio ativo?
- [ ] Toda escrita em um recurso de um usuário confirma a posse desse recurso no servidor?
- [ ] Controle de origem cruzada permanece restritivo, sem liberação ampla?
- [ ] Superfícies de introspecção de API estão desabilitadas fora de desenvolvimento?

## Exemplos

- Um endpoint de login retorna a mesma mensagem genérica de erro tanto para e-mail inexistente
  quanto para senha incorreta, e contabiliza tentativas falhas até acionar um bloqueio temporário.
- Uma chave de assinatura de token é lida exclusivamente de configuração local segura em
  desenvolvimento e de variável de ambiente em produção — nunca aparece em um arquivo de
  configuração versionado.
- Um endpoint que atualiza um recurso pertencente a um usuário verifica no servidor que o
  identificador recebido realmente pertence a quem fez a requisição, retornando a mesma mensagem
  de "não encontrado" tanto para recurso inexistente quanto para recurso de outro dono.
