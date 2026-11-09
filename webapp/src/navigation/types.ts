import type { NavigatorScreenParams } from '@react-navigation/native';

/** Akteck Seg (ADR 0038) — abas do dia a dia; o resto fica no menu lateral (`itensMenu.ts`). */
export type MainTabParamList = {
  Inicio: undefined;
  Cameras: undefined;
  Alarme: undefined;
  Acessos: undefined;
};

export type RootStackParamList = {
  Login: undefined;
  Cadastro: undefined;
  SelecionarPropriedade: undefined;
  Onboarding: { propriedadeId?: string } | undefined;
  MainTabs: NavigatorScreenParams<MainTabParamList> | undefined;
  Eventos: undefined;
  MinhaPropriedade: undefined;
  Notificacoes: undefined;
  Unidades: { propriedadeId: string; nomePropriedade: string };
  Moradores: { unidadeId: string; identificacaoUnidade: string; propriedadeId: string };
  Credenciais: { moradorId: string; nomeMorador: string; propriedadeId: string };
  Permissoes: { credencialId: string; tituloCredencial: string; propriedadeId: string };
  PontosAcesso: { propriedadeId: string; nomePropriedade: string };
  /** Sprint 24.5 — câmera do acesso + Abrir / Agora não. */
  AcessoPorta: { pontoAcessoId: string; propriedadeId: string; nome: string };
  Visitantes: { propriedadeId: string; nomePropriedade: string };
  Autorizacoes: { visitanteId: string; nomeVisitante: string; propriedadeId: string };
  Veiculos: { moradorId: string; nomeMorador: string; propriedadeId: string };
  Vagas: { propriedadeId: string; nomePropriedade: string };
  Entregas: { propriedadeId: string; nomePropriedade: string };
  DetalhesEntrega: { entregaId: string };
  Equipamentos: { propriedadeId: string; nomePropriedade: string };
  DetalhesEquipamento: { equipamentoId: string };
  CentraisJfl: { propriedadeId: string; nomePropriedade: string };
  DetalhesCentralJfl: { equipamentoId: string };
  CentraisIntelbras: { propriedadeId: string; nomePropriedade: string };
  DetalhesCentralIntelbras: { equipamentoId: string };
  CentralOperacional: { propriedadeId: string; nomePropriedade: string };
  SaudePropriedade: { propriedadeId: string; nomePropriedade: string };
  DetalheCamera: { cameraId: string; nomeCamera: string };
  /** Akteck Seg (ADR 0036) — detalhe de uma pessoa (celular; no computador o detalhe fica ao lado da lista). */
  Pessoa: { moradorId: string; unidadeId: string };
  NovaPessoa: { unidadeId: string };
  /** Akteck Seg (ADR 0038) — vai direto para a captura do rosto (escolhe a pessoa antes, se não vier). */
  CadastroFacial: { moradorId?: string } | undefined;
  /** Akteck Seg (ADR 0038) — antes era uma aba; agora abre pelo menu lateral. */
  Ajustes: undefined;
  /** Sprint 24 (ADR 0040) — contatos de emergência, tempo até avisar o próximo e "Não perturbe". */
  ContatosEmergencia: undefined;
  /** Sprint 25 (ADR 0041) — só o Proprietário vê. */
  Pessoas: undefined;
  MinhaConta: undefined;
  /** Sprint 27 (ADR 0043) — PIN, digital/rosto, bloqueio do app e registro de comandos. */
  Seguranca: undefined;
  /** Sprint 26 (ADR 0042) — abrem com ou sem login. */
  Termos: undefined;
  Privacidade: undefined;
};
