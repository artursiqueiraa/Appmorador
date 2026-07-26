// https://docs.expo.dev/guides/using-eslint/
const { defineConfig } = require('eslint/config');
const expoConfig = require("eslint-config-expo/flat");

module.exports = defineConfig([
  expoConfig,
  {
    ignores: ["dist/*"],
  },
  {
    rules: {
      // Todo o app busca dados com o padrão "carregar() dentro de um useEffect"
      // (sem React Query/Suspense, por decisão deliberada de simplicidade) — a regra
      // nova do eslint-plugin-react-hooks (voltada para o modelo do React Compiler)
      // marca esse padrão padrão como erro em ~20 telas que já funcionam
      // corretamente. Desligada aqui em vez de reescrever toda a camada de
      // carregamento de dados do app por causa de uma regra de um preset novo.
      'react-hooks/set-state-in-effect': 'off',
      // Reanimated exige mutar `sharedValue.value` diretamente (é a própria API) —
      // a regra ainda não reconhece esse padrão como uma exceção válida.
      'react-hooks/immutability': 'off',
    },
  },
]);
