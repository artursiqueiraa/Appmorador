import { Platform } from 'react-native';
import { colors } from '../theme/theme';

/**
 * Web App — no celular não existe foco por teclado; no navegador, quem navega com
 * Tab precisa ver onde está. O contorno padrão do react-native-web ficava com
 * contraste ~1:1 sobre o fundo escuro (achado do audit Hallmark/sloplint, regra
 * 26a). Aplicado só em `:focus-visible`, então clique/toque não mostra o anel e o
 * visual continua idêntico ao app mobile.
 */
export function aplicarEstilosGlobaisWeb(): void {
  if (Platform.OS !== 'web' || typeof document === 'undefined') {
    return;
  }
  // Akteck Seg (ADR 0035) — mesma família tipográfica do Painel (Geist).
  const fonte = document.createElement('link');
  fonte.rel = 'stylesheet';
  fonte.href = 'https://fonts.googleapis.com/css2?family=Geist:wght@400;500;600;700;800&display=swap';
  document.head.appendChild(fonte);

  const estilo = document.createElement('style');
  estilo.textContent = `
    body, body * { font-family: "Geist", "Segoe UI", system-ui, sans-serif !important; }
    :focus-visible { outline: 2px solid ${colors.marca} !important; outline-offset: 2px; }
    input:focus-visible, textarea:focus-visible { outline-offset: 0; }
  `;
  document.head.appendChild(estilo);
}
