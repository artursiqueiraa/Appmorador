import React, { useCallback, useEffect, useState } from 'react';
import { RefreshControl, ScrollView, StyleSheet, Text } from 'react-native';
import Animated, { FadeIn } from 'react-native-reanimated';
import { Package } from 'lucide-react-native';
import { useAuth } from '../../auth/AuthContext';
import { api, ApiError } from '../../api/client';
import type { DashboardResponse } from '../../api/types';
import { colors, motion, spacing } from '../../theme/theme';
import { EstadoVazio } from '../../components/EstadoVazio';
import { HeaderDashboard } from './HeaderDashboard';
import { CardSaude } from './CardSaude';
import { CardResumoInstalacao } from './CardResumoInstalacao';
import { CardUltimaAtividade } from './CardUltimaAtividade';
import { AcoesRapidas } from './AcoesRapidas';
import { AtalhoEventos } from './AtalhoEventos';
import { SkeletonDashboard } from './SkeletonDashboard';

/**
 * Orquestrador: busca o dashboard e decide qual estado renderizar (skeleton/vazio/
 * conteúdo). Toda a apresentação vive nos componentes filhos — nenhuma regra visual
 * fica aqui.
 */
export function DashboardScreen() {
  const { user, selectedProperty, logout } = useAuth();
  const [dashboard, setDashboard] = useState<DashboardResponse | null>(null);
  const [armado, setArmado] = useState(true);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadDashboard = useCallback(async () => {
    if (!selectedProperty) {
      return;
    }

    setLoading(true);
    setError(null);
    try {
      const data = await api.get<DashboardResponse>(`/api/properties/${selectedProperty.id}/dashboard`);
      setDashboard(data);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível carregar o dashboard.');
    } finally {
      setLoading(false);
    }
  }, [selectedProperty]);

  useEffect(() => {
    loadDashboard();
  }, [loadDashboard]);

  if (loading && !dashboard) {
    return <SkeletonDashboard />;
  }

  const instalacaoVazia =
    !!dashboard && dashboard.quantidadeCentrais === 0 && dashboard.quantidadeCameras === 0 && dashboard.quantidadeSensores === 0;

  return (
    <ScrollView
      style={styles.container}
      contentContainerStyle={styles.content}
      refreshControl={<RefreshControl refreshing={loading} onRefresh={loadDashboard} tintColor={colors.safe} />}
    >
      <HeaderDashboard
        primeiroNome={user?.nome?.split(' ')[0] ?? ''}
        nomePropriedade={dashboard?.nome ?? selectedProperty?.nome ?? ''}
        tipoPropriedade={dashboard?.tipo ?? selectedProperty?.tipo ?? 'Outro'}
        onLogout={logout}
      />

      {error ? <Text style={styles.error}>{error}</Text> : null}

      {dashboard ? (
        <Animated.View entering={FadeIn.duration(motion.duration.base)}>
          <CardSaude pontuacaoSaude={dashboard.pontuacaoSaude} protegido={dashboard.statusSeguranca === 'Protegido'} />

          {instalacaoVazia ? (
            <EstadoVazio
              icon={Package}
              titulo="Sua instalação está sendo preparada"
              descricao="Assim que seus dispositivos forem adicionados, eles aparecerão aqui."
            />
          ) : (
            <CardResumoInstalacao
              quantidadeCentrais={dashboard.quantidadeCentrais}
              quantidadeGravadores={dashboard.quantidadeGravadores}
              quantidadeCameras={dashboard.quantidadeCameras}
              quantidadeSensores={dashboard.quantidadeSensores}
            />
          )}

          <CardUltimaAtividade ultimoEvento={dashboard.ultimoEvento} ultimoEventoEmUtc={dashboard.ultimoEventoEmUtc} />

          <AcoesRapidas armado={armado} onChange={setArmado} />

          <AtalhoEventos />
        </Animated.View>
      ) : null}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.bg },
  content: { padding: spacing.xl, paddingBottom: spacing.xxl * 2 },
  error: { color: colors.danger, fontSize: 13, marginBottom: spacing.md, textAlign: 'center' },
});
