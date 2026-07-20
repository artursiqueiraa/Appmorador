const MINUTO = 60_000;
const HORA = 60 * MINUTO;
const DIA = 24 * HORA;

/**
 * Formata uma data UTC como tempo relativo em pt-BR ("há 2 minutos", "ontem"). Isolado
 * num único utilitário para que uma futura internacionalização troque só este arquivo,
 * sem tocar em quem o consome.
 */
export function formatRelativeTime(isoDate: string, now: Date = new Date()): string {
  const data = new Date(isoDate);
  const diffMs = now.getTime() - data.getTime();

  if (diffMs < MINUTO) {
    return 'agora mesmo';
  }
  if (diffMs < HORA) {
    const minutos = Math.floor(diffMs / MINUTO);
    return `há ${minutos} ${minutos === 1 ? 'minuto' : 'minutos'}`;
  }
  if (diffMs < DIA) {
    const horas = Math.floor(diffMs / HORA);
    return `há ${horas} ${horas === 1 ? 'hora' : 'horas'}`;
  }
  if (diffMs < 2 * DIA) {
    return 'ontem';
  }
  if (diffMs < 7 * DIA) {
    const dias = Math.floor(diffMs / DIA);
    return `há ${dias} dias`;
  }

  return data.toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit', year: 'numeric' });
}
