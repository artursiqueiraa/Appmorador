import type { NavigatorScreenParams } from '@react-navigation/native';

/** Sprint 16 (ADR 0019, UX001) — as 4 abas fixas da navegação inferior. */
export type MainTabParamList = {
  Inicio: undefined;
  Cameras: undefined;
  Acessos: undefined;
  Ajustes: undefined;
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
};
