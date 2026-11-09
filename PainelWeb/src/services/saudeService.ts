import { httpClient } from './httpClient';

/** Sprint 27 (ADR 0043) — espelho de `SaudeSistemaResponse` (backend). */
export type SituacaoComponente = 'Ok' | 'Atencao' | 'Fora';

export interface ComponenteSaude {
  chave: string;
  nome: string;
  situacao: SituacaoComponente;
  detalhe?: string | null;
  foraDesdeUtc?: string | null;
}

export interface RegistroBackup {
  sucesso: boolean;
  arquivo?: string | null;
  tamanhoBytes?: number | null;
  mensagem?: string | null;
  ocorridoEmUtc: string;
}

export interface SaudeSistema {
  geral: SituacaoComponente;
  componentes: ComponenteSaude[];
  ultimoBackup?: RegistroBackup | null;
  ultimoTesteRestauracao?: RegistroBackup | null;
  verificadoEmUtc: string;
}

export const saudeService = {
  obter: () => httpClient.get<SaudeSistema>('/api/painel/saude').then((r) => r.data),
};
