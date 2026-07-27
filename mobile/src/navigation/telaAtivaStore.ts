import { useSyncExternalStore } from 'react';

/**
 * Sprint 18 (ADR 0022) — armazém mínimo (sem biblioteca nova) do nome da tela em
 * foco, atualizado por `RootNavigator` via `navigationRef.onStateChange`. Usa
 * `useSyncExternalStore` (já disponível nesta versão do React) para expor via
 * hook sem re-renderizar nada além de quem realmente lê a tela ativa.
 */
type Ouvinte = () => void;

let telaAtivaAtual: string | undefined;
const ouvintes = new Set<Ouvinte>();

export function definirTelaAtiva(nome: string | undefined): void {
  if (telaAtivaAtual === nome) {
    return;
  }
  telaAtivaAtual = nome;
  ouvintes.forEach((ouvinte) => ouvinte());
}

function obterTelaAtiva(): string | undefined {
  return telaAtivaAtual;
}

function inscrever(ouvinte: Ouvinte): () => void {
  ouvintes.add(ouvinte);
  return () => ouvintes.delete(ouvinte);
}

export function useTelaAtiva(): string | undefined {
  return useSyncExternalStore(inscrever, obterTelaAtiva);
}
