import { createNavigationContainerRef } from '@react-navigation/native';
import type { RootStackParamList } from './types';

/**
 * Sprint 18 (ADR 0022, Regra 1 — Toast Inteligente) — precisa saber qual tela
 * está em foco para decidir se um evento em tempo real já é visível (e então
 * não duplicar com um toast) ou se aconteceu "fora do contexto atual" (e então
 * um toast discreto faz sentido). Padrão oficial do React Navigation para isso
 * (ref + `onStateChange`, ver `RootNavigator.tsx`) — não usa hooks porque quem
 * decide (o "bridge" de toast) não é um descendente de nenhuma `Screen`.
 */
export const navigationRef = createNavigationContainerRef<RootStackParamList>();
