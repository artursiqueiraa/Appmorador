/** Sprint 16 (ADR 0019, UX001) — elevação oficial. Mesmos valores já usados desde a Sprint 2 (shadow.sm/md/lg) — só ganhou o tipo `Shadow` exigido pelo Design System oficial. */
export type Shadow = {
  sm: object;
  md: object;
  lg: object;
};

export const shadow: Shadow = {
  sm: { shadowColor: '#000', shadowOffset: { width: 0, height: 1 }, shadowOpacity: 0.2, shadowRadius: 2, elevation: 2 },
  md: { shadowColor: '#000', shadowOffset: { width: 0, height: 4 }, shadowOpacity: 0.25, shadowRadius: 8, elevation: 4 },
  lg: { shadowColor: '#000', shadowOffset: { width: 0, height: 8 }, shadowOpacity: 0.3, shadowRadius: 16, elevation: 8 },
};
