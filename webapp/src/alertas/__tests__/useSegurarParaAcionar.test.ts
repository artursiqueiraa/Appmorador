/**
 * Sprint 24 (ADR 0040) — o pânico só dispara depois de 3 s pressionado; soltar antes cancela.
 */
import { act, renderHook } from '@testing-library/react-native';
import { useSegurarParaAcionar } from '../useSegurarParaAcionar';

describe('useSegurarParaAcionar', () => {
  beforeEach(() => jest.useFakeTimers());
  afterEach(() => jest.useRealTimers());

  it('segurando o tempo todo, aciona uma vez', async () => {
    const aoCompletar = jest.fn();
    const { result } = await renderHook(() => useSegurarParaAcionar(3000, aoCompletar));

    await act(async () => result.current.iniciar());
    await act(async () => {
      jest.advanceTimersByTime(3100);
    });

    expect(aoCompletar).toHaveBeenCalledTimes(1);
    expect(result.current.segurando).toBe(false);
  });

  it('soltando antes de 3 s, nunca aciona e zera o progresso', async () => {
    const aoCompletar = jest.fn();
    const { result } = await renderHook(() => useSegurarParaAcionar(3000, aoCompletar));

    await act(async () => result.current.iniciar());
    await act(async () => {
      jest.advanceTimersByTime(2000);
    });
    expect(result.current.progresso).toBeGreaterThan(0.5);

    await act(async () => result.current.cancelar());
    await act(async () => {
      jest.advanceTimersByTime(2000);
    });

    expect(aoCompletar).not.toHaveBeenCalled();
    expect(result.current.progresso).toBe(0);
  });
});
