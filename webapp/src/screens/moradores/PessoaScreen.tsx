import React, { useCallback, useEffect, useState } from 'react';
import { Pressable, ScrollView, StyleSheet, Text } from 'react-native';
import { useNavigation, useRoute, type RouteProp } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { ChevronLeft, Users } from 'lucide-react-native';
import type { MoradorResponse } from '../../api/types';
import { useAuth } from '../../auth/AuthContext';
import { EstadoVazio } from '../../components/EstadoVazio';
import { Skeleton } from '../../components/Skeleton';
import { PainelPessoa } from '../../moradores/PainelPessoa';
import { servicoPessoas } from '../../moradores/servicoPessoas';
import type { RootStackParamList } from '../../navigation/types';
import { colors, fontSize, iconSize, radius, spacing } from '../../theme/theme';

/** Akteck Seg (ADR 0036) — detalhe de uma pessoa no celular (no computador o mesmo painel fica ao lado da lista). */
export function PessoaScreen() {
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { params } = useRoute<RouteProp<RootStackParamList, 'Pessoa'>>();
  const { selectedProperty } = useAuth();
  const [pessoa, setPessoa] = useState<MoradorResponse | null | undefined>(undefined);

  const carregar = useCallback(async () => {
    try {
      const lista = await servicoPessoas.listarPessoas(params.unidadeId);
      setPessoa(lista.find((p) => p.id === params.moradorId) ?? null);
    } catch {
      setPessoa(null);
    }
  }, [params.moradorId, params.unidadeId]);

  useEffect(() => {
    carregar();
  }, [carregar]);

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      <Pressable onPress={() => navigation.goBack()} style={styles.voltar} accessibilityRole="button" accessibilityLabel="Voltar para Moradores">
        <ChevronLeft size={iconSize.md} color={colors.sub} />
        <Text style={styles.voltarTexto}>Moradores</Text>
      </Pressable>
      {pessoa === undefined ? (
        <Skeleton height={240} radius={radius.lg} />
      ) : pessoa === null ? (
        <EstadoVazio icon={Users} titulo="Pessoa não encontrada" descricao="Ela pode ter sido removida." cta={{ label: 'Voltar', onPress: () => navigation.goBack() }} />
      ) : (
        <PainelPessoa
          pessoa={pessoa}
          onAlterada={setPessoa}
          onRemovida={() => navigation.goBack()}
          onAbrirCredenciais={() =>
            selectedProperty && navigation.navigate('Credenciais', { moradorId: pessoa.id, nomeMorador: pessoa.nome, propriedadeId: selectedProperty.id })
          }
        />
      )}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.bg },
  content: { padding: spacing.xl, paddingBottom: spacing.xxxl, gap: spacing.lg },
  voltar: { flexDirection: 'row', alignItems: 'center', gap: spacing.xs, minHeight: 44, alignSelf: 'flex-start' },
  voltarTexto: { color: colors.sub, fontSize: fontSize.body },
});
