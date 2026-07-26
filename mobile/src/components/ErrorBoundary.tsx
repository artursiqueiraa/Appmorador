import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { AlertTriangle } from 'lucide-react-native';
import { PrimaryButton } from './PrimaryButton';
import { colors, fontSize, fontWeight, iconSize, spacing } from '../theme/theme';

interface Props {
  children: React.ReactNode;
}

interface State {
  hasError: boolean;
}

/**
 * Auditoria mobile — sem isso, qualquer erro de renderização em qualquer tela
 * derrubava o app inteiro (tela branca/vermelha), sem nenhuma forma de recuperação
 * a não ser fechar e reabrir o app. "Tentar novamente" reseta só o estado local do
 * boundary — não corrige a causa, mas evita perder a sessão/navegação por um erro
 * de renderização pontual.
 */
export class ErrorBoundary extends React.Component<Props, State> {
  state: State = { hasError: false };

  static getDerivedStateFromError(): State {
    return { hasError: true };
  }

  componentDidCatch(error: unknown) {
    if (__DEV__) {
      console.error('ErrorBoundary capturou um erro de renderização:', error);
    }
  }

  private tentarNovamente = () => this.setState({ hasError: false });

  render() {
    if (!this.state.hasError) {
      return this.props.children;
    }

    return (
      <View style={styles.container}>
        <View style={styles.iconWrap}>
          <AlertTriangle size={iconSize.xl} color={colors.warn} />
        </View>
        <Text style={styles.titulo}>Algo deu errado</Text>
        <Text style={styles.descricao}>
          Não se preocupe, estamos registrando este problema. Tente novamente — se continuar acontecendo, feche e abra o app de novo.
        </Text>
        <PrimaryButton label="Tentar novamente" onPress={this.tentarNovamente} />
      </View>
    );
  }
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.bg,
    alignItems: 'center',
    justifyContent: 'center',
    padding: spacing.xxl,
  },
  iconWrap: {
    width: 64,
    height: 64,
    borderRadius: 32,
    backgroundColor: colors.warnDim,
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: spacing.lg,
  },
  titulo: { color: colors.text, fontSize: fontSize.title, fontWeight: fontWeight.bold, marginBottom: spacing.sm },
  descricao: { color: colors.sub, fontSize: fontSize.secondary, textAlign: 'center', marginBottom: spacing.lg },
});
