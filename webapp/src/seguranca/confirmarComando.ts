import { api, ApiError } from '../api/client';
import { navigationRef } from '../navigation/navigationRef';
import { biometria } from './biometria';

/**
 * Sprint 27 (ADR 0043) — confirmação de comandos sensíveis (desarmar, acionar saída, abrir acesso).
 *
 * O servidor decide se o PIN é exigido (quem ainda não criou PIN passa sem confirmação) e sempre o
 * confere. Aqui só se escolhe COMO obter o PIN: digital/rosto do aparelho (libera o PIN guardado)
 * ou a folha de PIN. Cada toque leva uma chave de idempotência: se a rede repetir a requisição,
 * o comando não é enviado duas vezes ao equipamento.
 */
export type CabecalhosComando = Record<string, string>;

export interface PedidoPin {
  tipo: 'pin';
  acao: string;
  erro?: string;
  responder: (pin: string | null) => void;
}

export interface PedidoConvite {
  tipo: 'convite';
  acao: string;
  responder: (escolha: 'criar' | 'continuar' | null) => void;
}

export type PedidoConfirmacao = PedidoPin | PedidoConvite;

let mostrarPedido: ((pedido: PedidoConfirmacao | null) => void) | null = null;

/** Chamado pelo `ConfirmacaoComandoHost` (montado uma vez em App.tsx). */
export function registrarHostConfirmacao(mostrar: ((pedido: PedidoConfirmacao | null) => void) | null): void {
  mostrarPedido = mostrar;
}

const pedirPin = (acao: string, erro?: string) =>
  new Promise<string | null>((resolve) => {
    if (!mostrarPedido) {
      resolve(null);
      return;
    }
    mostrarPedido({ tipo: 'pin', acao, erro, responder: resolve });
  });

const pedirConvite = (acao: string) =>
  new Promise<'criar' | 'continuar' | null>((resolve) => {
    if (!mostrarPedido) {
      resolve('continuar');
      return;
    }
    mostrarPedido({ tipo: 'convite', acao, responder: resolve });
  });

let situacaoPin: { definido: boolean; lidoEm: number } | null = null;
let conviteJaMostrado = false;

/** Depois de criar/trocar o PIN, ou ao trocar de conta. */
export function invalidarSituacaoPin(): void {
  situacaoPin = null;
  conviteJaMostrado = false;
}

async function pinDefinido(): Promise<boolean> {
  if (situacaoPin && Date.now() - situacaoPin.lidoEm < 30_000) {
    return situacaoPin.definido;
  }
  try {
    const r = await api.get<{ definido: boolean }>('/api/minha-conta/pin');
    situacaoPin = { definido: r.definido, lidoEm: Date.now() };
    return r.definido;
  } catch {
    // Sem conseguir consultar, tenta sem PIN: se o servidor exigir (PIN_OBRIGATORIO), cai no fluxo do PIN.
    return false;
  }
}

export function novaChave(): string {
  return `${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 12)}`;
}

const codigoDe = (e: unknown) => (e instanceof ApiError ? e.codigo : undefined);

/**
 * Executa `executar` com os cabeçalhos de confirmação. Devolve null se a pessoa cancelou.
 * `executar` pode ser chamado mais de uma vez (PIN errado): os cabeçalhos trazem a mesma chave.
 */
export async function confirmarComando<T>(acao: string, executar: (cabecalhos: CabecalhosComando) => Promise<T>): Promise<T | null> {
  const chave = novaChave();
  const base: CabecalhosComando = { 'Idempotency-Key': chave };

  if (!(await pinDefinido())) {
    if (!conviteJaMostrado) {
      conviteJaMostrado = true;
      const escolha = await pedirConvite(acao);
      if (escolha === null) {
        return null;
      }
      if (escolha === 'criar') {
        if (navigationRef.isReady()) {
          navigationRef.navigate('Seguranca');
        }
        return null;
      }
    }
    try {
      return await executar(base);
    } catch (e) {
      if (codigoDe(e) !== 'PIN_OBRIGATORIO') {
        throw e;
      }
      situacaoPin = { definido: true, lidoEm: Date.now() }; // criado em outro aparelho
    }
  }

  const pinBiometria = await biometria.obterPin(`Confirmar: ${acao}`);
  if (pinBiometria) {
    try {
      return await executar({ ...base, 'X-Confirmacao-Pin': pinBiometria, 'X-Confirmacao-Biometria': '1' });
    } catch (e) {
      if (codigoDe(e) !== 'PIN_INCORRETO') {
        throw e;
      }
      // O PIN mudou em outro aparelho: esquece o guardado e pede de novo.
      await biometria.desativarConfirmacao();
    }
  }

  let erro: string | undefined;
  for (;;) {
    const pin = await pedirPin(acao, erro);
    if (pin === null) {
      return null;
    }
    try {
      return await executar({ ...base, 'X-Confirmacao-Pin': pin });
    } catch (e) {
      if (codigoDe(e) === 'PIN_INCORRETO' || codigoDe(e) === 'PIN_OBRIGATORIO') {
        erro = (e as ApiError).message;
        continue;
      }
      throw e;
    }
  }
}
