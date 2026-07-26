import React, { useCallback, useEffect, useState } from 'react';
import { Alert, FlatList, Pressable, StyleSheet, Text, View } from 'react-native';
import { useNavigation, useRoute, type RouteProp } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { CalendarClock, ChevronLeft, Pencil, Trash2 } from 'lucide-react-native';
import { api, ApiError } from '../../api/client';
import type { DiaSemanaToken, PermissaoAcessoResponse, PontoAcessoResponse } from '../../api/types';
import { PrimaryButton } from '../../components/PrimaryButton';
import { TextField } from '../../components/TextField';
import { EstadoVazio } from '../../components/EstadoVazio';
import { Skeleton } from '../../components/Skeleton';
import type { RootStackParamList } from '../../navigation/types';
import { colors, fontSize, fontWeight, radius, spacing } from '../../theme/theme';

type PermissoesRouteProp = RouteProp<RootStackParamList, 'Permissoes'>;

const DIAS: { valor: DiaSemanaToken; rotulo: string }[] = [
  { valor: 'Segunda', rotulo: 'Seg' },
  { valor: 'Terca', rotulo: 'Ter' },
  { valor: 'Quarta', rotulo: 'Qua' },
  { valor: 'Quinta', rotulo: 'Qui' },
  { valor: 'Sexta', rotulo: 'Sex' },
  { valor: 'Sabado', rotulo: 'Sáb' },
  { valor: 'Domingo', rotulo: 'Dom' },
];

const HORARIO_REGEX = /^([01]?\d|2[0-3]):([0-5]\d)$/;

function formatarHorario(horario?: string | null): string {
  return horario ? horario.slice(0, 5) : '';
}

/** "Segunda, Terca" -> ["Segunda","Terca"]; "Todos" (ou vazio) -> todos os 7 dias. */
function parseDias(diasPermitidos: string): Set<DiaSemanaToken> {
  const tokens = new Set(DIAS.map((d) => d.valor));
  if (diasPermitidos === 'Todos' || !diasPermitidos) {
    return tokens;
  }

  const selecionados = diasPermitidos.split(',').map((s) => s.trim()) as DiaSemanaToken[];
  return new Set(selecionados);
}

function descreverDias(dias: Set<DiaSemanaToken>): string {
  if (dias.size === 7) {
    return 'Todos os dias';
  }

  return DIAS.filter((d) => dias.has(d.valor))
    .map((d) => d.rotulo)
    .join(', ');
}

function descreverHorario(inicial?: string | null, final?: string | null): string | null {
  if (!inicial && !final) {
    return null;
  }

  return `${formatarHorario(inicial) || '00:00'} às ${formatarHorario(final) || '23:59'}`;
}

/**
 * Sprint 7 — Controle de Acesso. Regras de dia/horário de uma Credencial em um Ponto
 * de Acesso. DataInicial/DataFinal (vigência) não têm editor aqui ainda — exigiriam um
 * seletor de data que o Design System não tem hoje, mesma decisão já tomada para o
 * filtro de período da Central de Eventos (sem dependência nova); registrado em
 * docs/DIVIDA_TECNICA.md.
 */
export function PermissoesScreen() {
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { params } = useRoute<PermissoesRouteProp>();
  const { credencialId, tituloCredencial, propriedadeId } = params;

  const [permissoes, setPermissoes] = useState<PermissaoAcessoResponse[]>([]);
  const [pontos, setPontos] = useState<PontoAcessoResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [editando, setEditando] = useState<PermissaoAcessoResponse | null>(null);
  const [pontoAcessoId, setPontoAcessoId] = useState<string | null>(null);
  const [dias, setDias] = useState<Set<DiaSemanaToken>>(new Set(DIAS.map((d) => d.valor)));
  const [horarioInicial, setHorarioInicial] = useState('');
  const [horarioFinal, setHorarioFinal] = useState('');
  const [erroHorario, setErroHorario] = useState<string | null>(null);
  const [salvando, setSalvando] = useState(false);

  const carregar = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [listaPermissoes, listaPontos] = await Promise.all([
        api.get<PermissaoAcessoResponse[]>(`/api/credenciais/${credencialId}/permissoes`),
        api.get<PontoAcessoResponse[]>(`/api/properties/${propriedadeId}/pontos-acesso`),
      ]);
      setPermissoes(listaPermissoes);
      setPontos(listaPontos);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível carregar as permissões.');
    } finally {
      setLoading(false);
    }
  }, [credencialId, propriedadeId]);

  useEffect(() => {
    carregar();
  }, [carregar]);

  const abrirNovo = () => {
    setEditando(null);
    setPontoAcessoId(pontos[0]?.id ?? null);
    setDias(new Set(DIAS.map((d) => d.valor)));
    setHorarioInicial('');
    setHorarioFinal('');
    setErroHorario(null);
    setShowForm(true);
  };

  const abrirEdicao = (permissao: PermissaoAcessoResponse) => {
    setEditando(permissao);
    setPontoAcessoId(permissao.pontoAcessoId);
    setDias(parseDias(permissao.diasPermitidos));
    setHorarioInicial(formatarHorario(permissao.horarioInicial));
    setHorarioFinal(formatarHorario(permissao.horarioFinal));
    setErroHorario(null);
    setShowForm(true);
  };

  const alternarDia = (dia: DiaSemanaToken) => {
    setDias((prev) => {
      const proximo = new Set(prev);
      if (proximo.has(dia)) {
        proximo.delete(dia);
      } else {
        proximo.add(dia);
      }
      return proximo;
    });
  };

  const salvar = async () => {
    if (!editando && !pontoAcessoId) {
      return;
    }

    if (horarioInicial && !HORARIO_REGEX.test(horarioInicial)) {
      setErroHorario('Use o formato HH:MM, ex.: 08:00');
      return;
    }
    if (horarioFinal && !HORARIO_REGEX.test(horarioFinal)) {
      setErroHorario('Use o formato HH:MM, ex.: 18:00');
      return;
    }
    setErroHorario(null);

    const payload = {
      diasPermitidos: dias.size === 7 ? undefined : DIAS.filter((d) => dias.has(d.valor)).map((d) => d.valor).join(', '),
      horarioInicial: horarioInicial ? `${horarioInicial}:00` : undefined,
      horarioFinal: horarioFinal ? `${horarioFinal}:00` : undefined,
    };

    setSalvando(true);
    setError(null);
    try {
      if (editando) {
        const atualizada = await api.put<PermissaoAcessoResponse>(`/api/permissoes/${editando.id}`, payload);
        setPermissoes((prev) => prev.map((p) => (p.id === atualizada.id ? atualizada : p)));
      } else {
        const criada = await api.post<PermissaoAcessoResponse>(`/api/credenciais/${credencialId}/permissoes`, {
          pontoAcessoId,
          ...payload,
        });
        setPermissoes((prev) => [...prev, criada]);
      }
      setShowForm(false);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível salvar a permissão.');
    } finally {
      setSalvando(false);
    }
  };

  const confirmarExclusao = (permissao: PermissaoAcessoResponse) => {
    Alert.alert('Remover permissão?', `O acesso a "${permissao.pontoAcessoNome}" será removido desta credencial.`, [
      { text: 'Cancelar', style: 'cancel' },
      {
        text: 'Remover',
        style: 'destructive',
        onPress: async () => {
          try {
            await api.delete(`/api/permissoes/${permissao.id}`);
            setPermissoes((prev) => prev.filter((p) => p.id !== permissao.id));
          } catch (err) {
            setError(err instanceof ApiError ? err.message : 'Não foi possível remover a permissão.');
          }
        },
      },
    ]);
  };

  const semPontosCadastrados = !loading && pontos.length === 0;

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <Pressable onPress={() => navigation.goBack()} style={styles.iconBtn} accessibilityLabel="Voltar">
          <ChevronLeft size={20} color={colors.text} />
        </Pressable>
        <View style={styles.headerTextWrap}>
          <Text style={styles.title}>Permissões</Text>
          <Text style={styles.subtitle}>{tituloCredencial}</Text>
        </View>
      </View>

      {error ? <Text style={styles.error}>{error}</Text> : null}

      {loading ? (
        <View style={{ gap: spacing.sm }}>
          <Skeleton height={72} radius={radius.lg} />
          <Skeleton height={72} radius={radius.lg} />
        </View>
      ) : (
        <FlatList
          data={permissoes}
          keyExtractor={(item) => item.id}
          contentContainerStyle={styles.list}
          refreshing={loading}
          onRefresh={carregar}
          renderItem={({ item }) => {
            const horario = descreverHorario(item.horarioInicial, item.horarioFinal);
            return (
              <View style={styles.card}>
                <View style={styles.cardIcon}>
                  <CalendarClock size={18} color={colors.safe} />
                </View>
                <View style={styles.cardTextWrap}>
                  <Text style={styles.cardTitle}>{item.pontoAcessoNome}</Text>
                  <Text style={styles.cardSubtitle}>{descreverDias(parseDias(item.diasPermitidos))}</Text>
                  {horario ? <Text style={styles.cardSubtitle}>{horario}</Text> : null}
                </View>
                <View style={styles.cardActions}>
                  <Pressable onPress={() => abrirEdicao(item)} style={styles.actionBtn} accessibilityLabel="Editar permissão">
                    <Pencil size={16} color={colors.sub} />
                  </Pressable>
                  <Pressable onPress={() => confirmarExclusao(item)} style={styles.actionBtn} accessibilityLabel="Remover permissão">
                    <Trash2 size={16} color={colors.danger} />
                  </Pressable>
                </View>
              </View>
            );
          }}
          ListEmptyComponent={
            !showForm ? (
              <EstadoVazio
                icon={CalendarClock}
                titulo="Nenhuma permissão ainda"
                descricao="Defina em quais pontos de acesso e horários esta credencial pode ser usada."
                cta={{ label: 'Adicionar permissão', onPress: abrirNovo }}
              />
            ) : null
          }
        />
      )}

      {showForm ? (
        <View style={styles.form}>
          {editando ? (
            <Text style={styles.pontoFixo}>Ponto de acesso: {editando.pontoAcessoNome}</Text>
          ) : (
            <View style={styles.container2}>
              <Text style={styles.label}>Ponto de acesso</Text>
              <View style={styles.chipsRow}>
                {pontos.map((ponto) => {
                  const ativo = ponto.id === pontoAcessoId;
                  return (
                    <Pressable
                      key={ponto.id}
                      onPress={() => setPontoAcessoId(ponto.id)}
                      style={[styles.chip, ativo && styles.chipAtivo]}
                    >
                      <Text style={[styles.chipLabel, ativo && styles.chipLabelAtivo]}>{ponto.nome}</Text>
                    </Pressable>
                  );
                })}
              </View>
            </View>
          )}

          <View style={styles.container2}>
            <Text style={styles.label}>Dias permitidos</Text>
            <View style={styles.chipsRow}>
              {DIAS.map((dia) => {
                const ativo = dias.has(dia.valor);
                return (
                  <Pressable
                    key={dia.valor}
                    onPress={() => alternarDia(dia.valor)}
                    style={[styles.chip, ativo && styles.chipAtivo]}
                  >
                    <Text style={[styles.chipLabel, ativo && styles.chipLabelAtivo]}>{dia.rotulo}</Text>
                  </Pressable>
                );
              })}
            </View>
          </View>

          <TextField
            label="Horário inicial (opcional)"
            value={horarioInicial}
            onChangeText={setHorarioInicial}
            placeholder="08:00"
            keyboardType="numbers-and-punctuation"
            error={erroHorario ?? undefined}
          />
          <TextField
            label="Horário final (opcional)"
            value={horarioFinal}
            onChangeText={setHorarioFinal}
            placeholder="18:00"
            keyboardType="numbers-and-punctuation"
          />

          <PrimaryButton
            label={editando ? 'Salvar alterações' : 'Adicionar permissão'}
            onPress={salvar}
            loading={salvando}
            disabled={!editando && !pontoAcessoId}
          />
          <PrimaryButton label="Cancelar" variant="secondary" onPress={() => setShowForm(false)} />
        </View>
      ) : semPontosCadastrados ? (
        <Text style={styles.avisoSemPontos}>Cadastre um ponto de acesso na propriedade antes de criar uma permissão.</Text>
      ) : (
        <PrimaryButton label="Adicionar permissão" variant="secondary" onPress={abrirNovo} />
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.bg, padding: spacing.xl },
  container2: { marginBottom: spacing.lg },
  header: { flexDirection: 'row', alignItems: 'center', gap: spacing.md, marginBottom: spacing.lg },
  iconBtn: {
    width: 38,
    height: 38,
    borderRadius: radius.md,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
    alignItems: 'center',
    justifyContent: 'center',
  },
  headerTextWrap: { flex: 1 },
  title: { color: colors.text, fontSize: fontSize.title, fontWeight: fontWeight.bold },
  subtitle: { color: colors.sub, fontSize: fontSize.secondary, marginTop: 2 },
  list: { paddingBottom: spacing.lg, gap: spacing.sm },
  card: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
    padding: spacing.md,
    borderRadius: radius.lg,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
    marginBottom: spacing.sm,
  },
  cardIcon: {
    width: 38,
    height: 38,
    borderRadius: radius.md,
    backgroundColor: colors.safeDim,
    alignItems: 'center',
    justifyContent: 'center',
  },
  cardTextWrap: { flex: 1 },
  cardTitle: { color: colors.text, fontSize: fontSize.cardTitle, fontWeight: fontWeight.medium },
  cardSubtitle: { color: colors.mute, fontSize: fontSize.tiny, marginTop: 2 },
  cardActions: { flexDirection: 'row', gap: spacing.xs },
  actionBtn: { width: 32, height: 32, alignItems: 'center', justifyContent: 'center' },
  form: { gap: spacing.sm },
  error: { color: colors.danger, fontSize: fontSize.secondary, marginBottom: spacing.md, textAlign: 'center' },
  label: {
    color: colors.sub,
    fontSize: fontSize.meta,
    fontWeight: fontWeight.medium,
    marginBottom: spacing.xs + 2,
  },
  pontoFixo: { color: colors.text, fontSize: fontSize.body, fontWeight: fontWeight.medium, marginBottom: spacing.md },
  chipsRow: { flexDirection: 'row', flexWrap: 'wrap', gap: spacing.xs },
  chip: {
    paddingVertical: spacing.xs + 2,
    paddingHorizontal: spacing.md,
    borderRadius: radius.pill,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
  },
  chipAtivo: { backgroundColor: colors.safeDim, borderColor: colors.safeLine },
  chipLabel: { color: colors.sub, fontSize: fontSize.secondary, fontWeight: fontWeight.medium },
  chipLabelAtivo: { color: colors.safe },
  avisoSemPontos: { color: colors.mute, fontSize: fontSize.secondary, textAlign: 'center', marginTop: spacing.sm },
});
