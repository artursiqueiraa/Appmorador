/**
 * Web App — tokens de layout responsivo. O app mobile não precisa disso (a tela
 * sempre tem largura de celular); no navegador, a mesma interface precisa se
 * comportar em celular, tablet e desktop sem mudar o visual dos componentes.
 *
 * - Abaixo de `breakpoints.desktop`: idêntico ao mobile (largura total + navegação inferior).
 * - A partir de `breakpoints.desktop`: navegação vira barra lateral e o conteúdo
 *   fica numa coluna centralizada (nunca estica cards a 1920px).
 */
export type Layout = {
  breakpoints: { desktop: number };
  contentMaxWidth: number;
  /** Akteck Seg (ADR 0036) — telas principais no computador (Início, Moradores, Acessos, Câmeras): usam a largura de verdade. */
  wideMaxWidth: number;
  /** Coluna da lista no layout lista + detalhe. */
  listaMestreLargura: number;
  formMaxWidth: number;
  sideNavWidth: number;
};

export const layout: Layout = {
  breakpoints: { desktop: 1024 },
  contentMaxWidth: 760,
  wideMaxWidth: 1180,
  listaMestreLargura: 400,
  formMaxWidth: 480,
  sideNavWidth: 240,
};
