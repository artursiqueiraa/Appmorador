import React, { useEffect, useState } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { CheckCircle2, CircleAlert } from 'lucide-react-native';
import { api } from '../api/client';
import { colors, fontSize, spacing } from '../theme/theme';

interface SituacaoLeitor {
  equipamentoId: string;
  nomeEquipamento: string;
  situacao: 'Enviada' | 'Falhou' | 'Removida';
  mensagem?: string | null;
}

/**
 * Sprint 26 (ADR 0042) — onde o rosto está: "Enviado ao leitor da portaria" ou "Falhou" (o servidor
 * tenta de novo sozinho a cada minuto). Sem leitores faciais na propriedade, não mostra nada.
 */
export function SituacaoLeitores({ moradorId, versao }: { moradorId: string; versao: number }) {
  const [leitores, setLeitores] = useState<SituacaoLeitor[]>([]);

  useEffect(() => {
    api
      .get<SituacaoLeitor[]>(`/api/moradores/${moradorId}/foto/leitores`)
      .then(setLeitores)
      .catch(() => setLeitores([]));
  }, [moradorId, versao]);

  if (leitores.length === 0) {
    return null;
  }

  return (
    <View style={styles.lista}>
      {leitores.map((l) => (
        <View key={l.equipamentoId} style={styles.linha}>
          {l.situacao === 'Enviada' ? <CheckCircle2 size={16} color={colors.safe} /> : <CircleAlert size={16} color={colors.warn} />}
          <Text style={styles.texto}>
            {l.situacao === 'Enviada'
              ? `Enviado ao ${l.nomeEquipamento}`
              : `Não chegou ao ${l.nomeEquipamento} — tentaremos de novo automaticamente`}
          </Text>
        </View>
      ))}
    </View>
  );
}

const styles = StyleSheet.create({
  lista: { gap: spacing.xs },
  linha: { flexDirection: 'row', alignItems: 'center', gap: spacing.sm },
  texto: { color: colors.sub, fontSize: fontSize.secondary, flex: 1 },
});
