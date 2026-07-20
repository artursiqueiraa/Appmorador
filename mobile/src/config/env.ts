/**
 * Configuração centralizada — nenhuma URL deve aparecer hardcoded em outro lugar
 * do app. Vem de EXPO_PUBLIC_API_URL (.env na raiz do projeto mobile; use
 * .env.local, gitignorado, para sobrescrever na sua máquina — ex.: IP da rede
 * local para testar em dispositivo físico, ou 10.0.2.2 no emulador Android).
 */
const apiUrl = process.env.EXPO_PUBLIC_API_URL;

if (!apiUrl) {
  throw new Error(
    'EXPO_PUBLIC_API_URL não configurada. Defina no arquivo .env (ou .env.local) na raiz do projeto mobile.',
  );
}

export const env = {
  apiUrl,
};
