/**
 * Sprint 16 (ADR 0019, UX001) — durações de animação oficiais. Toda animação
 * precisa ter uma finalidade funcional (ver ADR 0019) — nunca só decorativa.
 * `motion` (nomes já usados desde a Sprint 2: `duration.base/ambient` + curvas
 * bezier para Reanimated) continua existindo — nunca removido.
 */
export type Animation = {
  fast: number;
  normal: number;
  slow: number;
  easing: string;
};

export const animation: Animation = {
  fast: 150,
  normal: 250,
  slow: 400,
  easing: 'ease-in-out',
};

/** Nomes de conveniência já usados por todas as telas existentes desde a Sprint 2. */
export const motion = {
  duration: {
    fast: 150,
    base: 300,
    slow: 500,
    /** Pulso lento e contínuo (ex.: anel "respirando" do HeroCard) — categoria
     * própria porque fast/base/slow são todas feedback de interação. */
    ambient: 1800,
  },
  easing: {
    standard: [0.4, 0, 0.2, 1] as const,
    decelerate: [0, 0, 0.2, 1] as const,
    accelerate: [0.4, 0, 1, 1] as const,
  },
};
