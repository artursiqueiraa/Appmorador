import React, { useCallback, useEffect, useState } from 'react';
import { Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { Car, ChevronRight, DoorOpen, Package, ParkingSquare, UserPlus, Users, Zap } from 'lucide-react-native';
import { useAuth } from '../../auth/AuthContext';
import { api, ApiError } from '../../api/client';
import { useToast } from '../../components/Toast';
import type { EquipamentoResponse, MoradorResponse, ResultadoComandoJfl, UnidadeResponse, VisitanteResponse } from '../../api/types';
import type { RootStackParamList } from '../../navigation/types';
import { EstadoVazio } from '../../components/EstadoVazio';
import { Skeleton } from '../../components/Skeleton';
import { PrimaryButton } from '../../components/PrimaryButton';
import { CommandCard } from '../../acessos/CommandCard';
import { obterRotulos, rotuloPadrao, type RotuloPgm } from '../../acessos/pgmLabels';
import { colors, fontSize, fontWeight, iconSize, radius, spacing } from '../../theme/theme';

type Aba = 'moradores' | 'visitantes';

interface ComandoPainel {
  equipamentoId: string;
  numeroPgm: number;
  rotulo: RotuloPgm;
  conectado: boolean;
  descricaoEstado: string;
}

/**
 * Sprint 17 (ADR 0020) — só centrais JFL têm PGM de verdade hoje (Intelbras/Control
 * iD não suportam, ver auditoria da Sprint 17): nunca inventar comandos para
 * fabricantes que não têm essa capacidade.
 */
async function carregarComandosJfl(propriedadeId: string): Promise<ComandoPainel[]> {
  const equipamentos = await api.get<EquipamentoResponse[]>(`/api/properties/${propriedadeId}/equipamentos`);
  const centraisJfl = equipamentos.filter((eq) => eq.fabricante === 'Jfl');

  const porCentral = await Promise.all(
    centraisJfl.map(async (equipamento) => {
      try {
        const resultado = await api.get<ResultadoComandoJfl>(`/api/equipamentos/${equipamento.id}/jfl/status`);
        if (!resultado.sucesso || !resultado.statusResultante) {
          return [];
        }

        const rotulos = await obterRotulos(equipamento.id);
        const conectado = equipamento.status === 'Online';
        return resultado.statusResultante.pgms
          .filter((pgm) => pgm.permitida)
          .map((pgm): ComandoPainel => ({
            equipamentoId: equipamento.id,
            numeroPgm: pgm.numero,
            rotulo: rotulos[pgm.numero] ?? rotuloPadrao(pgm.numero),
            conectado,
            descricaoEstado: conectado ? 'Pronto' : 'Sem comunicação com o equipamento',
          }));
      } catch {
        return [];
      }
    }),
  );

  return porCentral.flat();
}

/**
 * Sprint 16 (ADR 0019, UX001) — aba "Acessos": quem pode entrar e como. Diverge
 * conscientemente do protótipo UX001 num ponto: o cartão "Cadastrar meu rosto" não
 * foi implementado — exigiria captura de câmera + upload de foto, que não existem
 * nesta Sprint nem no backend (ver DIVIDA_TECNICA). Nunca fingir uma funcionalidade
 * que não existe de verdade.
 */
export function AccessScreen() {
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { selectedProperty } = useAuth();
  const { mostrarErro } = useToast();
  const [aba, setAba] = useState<Aba>('moradores');
  const [moradores, setMoradores] = useState<(MoradorResponse & { nomeUnidade: string })[]>([]);
  const [visitantes, setVisitantes] = useState<VisitanteResponse[]>([]);
  const [comandos, setComandos] = useState<ComandoPainel[]>([]);
  const [painelDispensado, setPainelDispensado] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const carregar = useCallback(async () => {
    if (!selectedProperty) return;

    setLoading(true);
    setError(null);
    try {
      const unidades = await api.get<UnidadeResponse[]>(`/api/properties/${selectedProperty.id}/unidades`);
      const listasPorUnidade = await Promise.all(
        unidades.map((unidade) =>
          api
            .get<MoradorResponse[]>(`/api/unidades/${unidade.id}/moradores`)
            .then((lista) => lista.map((m) => ({ ...m, nomeUnidade: unidade.identificacao }))),
        ),
      );
      setMoradores(listasPorUnidade.flat());

      const listaVisitantes = await api.get<VisitanteResponse[]>(`/api/properties/${selectedProperty.id}/visitantes`);
      setVisitantes(listaVisitantes);

      setComandos(await carregarComandosJfl(selectedProperty.id));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Não foi possível carregar os acessos.');
    } finally {
      setLoading(false);
    }
  }, [selectedProperty]);

  const acionarComando = useCallback(
    async (comando: ComandoPainel) => {
      try {
        const resultado = await api.post<ResultadoComandoJfl>(`/api/equipamentos/${comando.equipamentoId}/jfl/pgm/acionar`, {
          pgmNumero: comando.numeroPgm,
        });
        if (!resultado.sucesso) {
          mostrarErro({
            titulo: 'Não foi possível acionar',
            mensagem: resultado.mensagemErro ?? 'O equipamento não respondeu. Tente novamente.',
            onRetry: () => acionarComando(comando),
          });
        }
        return resultado.sucesso;
      } catch (err) {
        mostrarErro({
          titulo: 'Não foi possível acionar',
          mensagem: err instanceof ApiError ? err.message : 'Algo deu errado. Tente novamente.',
          onRetry: () => acionarComando(comando),
        });
        return false;
      }
    },
    [mostrarErro],
  );

  useEffect(() => {
    carregar();
  }, [carregar]);

  if (!selectedProperty) {
    return null;
  }

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      <Text style={styles.titulo}>Acessos</Text>
      <Text style={styles.subtitulo}>Quem pode entrar e como</Text>

      {!loading && (
        <View style={styles.painel}>
          <Text style={styles.secaoTitulo}>Painel de Controle</Text>
          {comandos.length > 0 ? (
            <View style={styles.lista}>
              {comandos.map((comando) => (
                <CommandCard
                  key={`${comando.equipamentoId}-${comando.numeroPgm}`}
                  icone={comando.rotulo.icone}
                  label={comando.rotulo.label}
                  conectado={comando.conectado}
                  descricaoEstado={comando.descricaoEstado}
                  onAcionar={() => acionarComando(comando)}
                />
              ))}
            </View>
          ) : !painelDispensado ? (
            <EstadoVazio
              icon={Zap}
              titulo="Nenhum comando disponível ainda"
              descricao="Assim que sua central tiver comandos liberados (como abrir o portão), eles aparecem aqui."
              cta={{ label: 'Entendi', onPress: () => setPainelDispensado(true) }}
            />
          ) : null}
        </View>
      )}

      <View style={styles.abas}>
        <AbaBotao label="Moradores" ativo={aba === 'moradores'} onPress={() => setAba('moradores')} />
        <AbaBotao label="Visitantes" ativo={aba === 'visitantes'} onPress={() => setAba('visitantes')} />
      </View>

      {error ? <Text style={styles.error}>{error}</Text> : null}

      {loading ? (
        <View style={{ gap: spacing.sm, marginTop: spacing.md }}>
          <Skeleton height={64} radius={radius.lg} />
          <Skeleton height={64} radius={radius.lg} />
        </View>
      ) : aba === 'moradores' ? (
        <View style={styles.lista}>
          {moradores.length === 0 ? (
            <EstadoVazio
              icon={Users}
              titulo="Nenhum morador ainda"
              descricao="Cadastre quem mora com você para liberar acesso e credenciais."
              cta={{ label: 'Adicionar morador', onPress: () => navigation.navigate('MinhaPropriedade') }}
            />
          ) : (
            moradores.map((morador) => (
              <View key={morador.id} style={styles.itemLinha}>
                <Pressable
                  style={styles.itemPrincipal}
                  onPress={() =>
                    navigation.navigate('Credenciais', {
                      moradorId: morador.id,
                      nomeMorador: morador.nome,
                      propriedadeId: selectedProperty.id,
                    })
                  }
                >
                  <View style={styles.avatar} />
                  <View style={styles.itemTextoWrap}>
                    <Text style={styles.itemTitulo}>{morador.nome}</Text>
                    <Text style={styles.itemSubtitulo}>{morador.nomeUnidade}</Text>
                  </View>
                  <ChevronRight size={16} color={colors.mute} />
                </Pressable>
                <Pressable
                  style={styles.itemAcao}
                  accessibilityLabel={`Veículos de ${morador.nome}`}
                  onPress={() =>
                    navigation.navigate('Veiculos', { moradorId: morador.id, nomeMorador: morador.nome, propriedadeId: selectedProperty.id })
                  }
                >
                  <Car size={16} color={colors.sub} />
                </Pressable>
              </View>
            ))
          )}
          <PrimaryButton
            label="Adicionar morador"
            variant="secondary"
            onPress={() => navigation.navigate('MinhaPropriedade')}
          />
        </View>
      ) : (
        <View style={styles.lista}>
          {visitantes.length === 0 ? (
            <EstadoVazio
              icon={UserPlus}
              titulo="Nenhum visitante ainda"
              descricao="Cadastre um visitante para liberar uma autorização de entrada."
              cta={{ label: 'Adicionar visitante', onPress: () => navigation.navigate('Visitantes', { propriedadeId: selectedProperty.id, nomePropriedade: selectedProperty.nome }) }}
            />
          ) : (
            visitantes.map((visitante) => (
              <Pressable
                key={visitante.id}
                style={styles.itemLinhaSimples}
                onPress={() => navigation.navigate('Visitantes', { propriedadeId: selectedProperty.id, nomePropriedade: selectedProperty.nome })}
              >
                <View style={styles.avatar} />
                <View style={styles.itemTextoWrap}>
                  <Text style={styles.itemTitulo}>{visitante.nome}</Text>
                  <Text style={styles.itemSubtitulo}>Ver autorizações</Text>
                </View>
                <ChevronRight size={16} color={colors.mute} />
              </Pressable>
            ))
          )}
          <PrimaryButton
            label="Adicionar visitante"
            variant="secondary"
            onPress={() => navigation.navigate('Visitantes', { propriedadeId: selectedProperty.id, nomePropriedade: selectedProperty.nome })}
          />
        </View>
      )}

      <Text style={styles.secaoMais}>Mais</Text>
      <View style={styles.lista}>
        <MenuLinha
          icon={DoorOpen}
          label="Portões"
          onPress={() => navigation.navigate('PontosAcesso', { propriedadeId: selectedProperty.id, nomePropriedade: selectedProperty.nome })}
        />
        <MenuLinha
          icon={ParkingSquare}
          label="Vagas"
          onPress={() => navigation.navigate('Vagas', { propriedadeId: selectedProperty.id, nomePropriedade: selectedProperty.nome })}
        />
        <MenuLinha
          icon={Package}
          label="Entregas"
          onPress={() => navigation.navigate('Entregas', { propriedadeId: selectedProperty.id, nomePropriedade: selectedProperty.nome })}
        />
      </View>
    </ScrollView>
  );
}

function AbaBotao({ label, ativo, onPress }: { label: string; ativo: boolean; onPress: () => void }) {
  return (
    <Pressable onPress={onPress} style={[styles.aba, ativo && styles.abaAtiva]}>
      <Text style={[styles.abaLabel, ativo && styles.abaLabelAtiva]}>{label}</Text>
    </Pressable>
  );
}

function MenuLinha({ icon: Icon, label, onPress }: { icon: typeof DoorOpen; label: string; onPress: () => void }) {
  return (
    <Pressable style={styles.itemLinhaSimples} onPress={onPress}>
      <View style={styles.menuIconWrap}>
        <Icon size={iconSize.sm} color={colors.accent} />
      </View>
      <Text style={styles.itemTitulo}>{label}</Text>
      <View style={{ flex: 1 }} />
      <ChevronRight size={16} color={colors.mute} />
    </Pressable>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.bg },
  content: { padding: spacing.xl, paddingBottom: spacing.xxxl },
  titulo: { color: colors.text, fontSize: fontSize.title, fontWeight: fontWeight.black },
  subtitulo: { color: colors.sub, fontSize: fontSize.secondary, marginTop: 3, marginBottom: spacing.lg },
  painel: { marginBottom: spacing.xl },
  secaoTitulo: { color: colors.sub, fontSize: fontSize.secondary, fontWeight: fontWeight.medium, marginBottom: spacing.sm },
  abas: {
    flexDirection: 'row',
    gap: spacing.xs,
    backgroundColor: colors.bg2,
    padding: 4,
    borderRadius: radius.md,
    borderWidth: 1,
    borderColor: colors.lineSoft,
  },
  aba: { flex: 1, paddingVertical: spacing.sm + 1, borderRadius: radius.sm, alignItems: 'center' },
  abaAtiva: { backgroundColor: colors.surface2 },
  abaLabel: { fontSize: fontSize.secondary, fontWeight: fontWeight.bold, color: colors.sub },
  abaLabelAtiva: { color: colors.text },
  lista: { marginTop: spacing.md, gap: spacing.sm },
  itemLinha: {
    flexDirection: 'row',
    alignItems: 'center',
    borderRadius: radius.lg,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
  },
  itemLinhaSimples: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    padding: spacing.md,
    borderRadius: radius.lg,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
  },
  itemPrincipal: { flex: 1, flexDirection: 'row', alignItems: 'center', gap: spacing.md, padding: spacing.md },
  itemAcao: { paddingHorizontal: spacing.md, alignSelf: 'stretch', justifyContent: 'center' },
  avatar: { width: 42, height: 42, borderRadius: 999, backgroundColor: colors.surface2 },
  itemTextoWrap: { flex: 1, minWidth: 0 },
  itemTitulo: { color: colors.text, fontSize: fontSize.cardTitle, fontWeight: fontWeight.medium },
  itemSubtitulo: { color: colors.mute, fontSize: fontSize.tiny, marginTop: 2 },
  menuIconWrap: {
    width: 34,
    height: 34,
    borderRadius: radius.sm,
    backgroundColor: colors.surface2,
    alignItems: 'center',
    justifyContent: 'center',
  },
  secaoMais: { color: colors.sub, fontSize: fontSize.secondary, fontWeight: fontWeight.medium, marginTop: spacing.xl, marginBottom: spacing.sm },
  error: { color: colors.danger, fontSize: fontSize.secondary, marginTop: spacing.md, textAlign: 'center' },
});
