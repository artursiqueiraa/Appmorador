import React, { useState } from 'react';
import { Image, StyleSheet, Text, View } from 'react-native';
import { Camera, Glasses, ImageIcon, ScanFace, Smile, Sun } from 'lucide-react-native';
import { ApiError } from '../api/client';
import { PrimaryButton } from '../components/PrimaryButton';
import { CartaoNeon } from '../components/neon/CartaoNeon';
import { colors, efeitos, fontSize, fontWeight, iconSize, radius, spacing } from '../theme/theme';
import { capturarDaCamera, escolherDaGaleria } from './capturarRosto';
import { servicoPessoas } from './servicoPessoas';

const DICAS = [
  { icone: Sun, texto: 'Ambiente\nbem iluminado' },
  { icone: Smile, texto: 'Rosto de\nfrente' },
  { icone: Glasses, texto: 'Sem óculos,\nboné ou máscara' },
];

interface Props {
  moradorId: string;
  /** URI local de uma foto já salva (para mostrar no enquadramento). */
  fotoAtual?: string | null;
  onSalvo: (uri: string) => void;
}

/**
 * Akteck Seg (ADR 0037) — "Cadastro Facial" da referência: enquadramento com cantos verdes,
 * dicas de captura e duas formas de obter a foto. A foto vai para o servidor (ADR 0036). O envio
 * ao leitor físico ainda não existe, e a tela não diz que existe.
 */
export function CapturaFacial({ moradorId, fotoAtual, onSalvo }: Props) {
  const [previa, setPrevia] = useState<string | null>(fotoAtual ?? null);
  const [enviando, setEnviando] = useState<'camera' | 'galeria' | null>(null);
  const [erro, setErro] = useState<string | null>(null);

  const obter = async (origem: 'camera' | 'galeria') => {
    setErro(null);
    let uri: string | null;
    try {
      uri = origem === 'camera' ? await capturarDaCamera() : await escolherDaGaleria();
    } catch {
      setErro(origem === 'camera' ? 'Permita o acesso à câmera, ou use uma foto da galeria.' : 'Permita o acesso às fotos para escolher uma imagem.');
      return;
    }
    if (!uri) return;
    setPrevia(uri);
    setEnviando(origem);
    try {
      await servicoPessoas.cadastrarRosto(moradorId, uri);
      onSalvo(uri);
    } catch (e) {
      setErro(e instanceof ApiError ? e.message : 'Não foi possível salvar a foto. Tente de novo.');
    } finally {
      setEnviando(null);
    }
  };

  return (
    <View style={styles.container}>
      <CartaoNeon style={styles.moldura}>
        <View style={styles.area}>
          <View style={[styles.canto, styles.cantoSE]} />
          <View style={[styles.canto, styles.cantoSD]} />
          <View style={[styles.canto, styles.cantoIE]} />
          <View style={[styles.canto, styles.cantoID]} />
          <View style={styles.circulo}>
            {previa ? (
              <Image source={{ uri: previa }} style={styles.foto} accessibilityLabel="Prévia da foto do rosto" />
            ) : (
              <ScanFace size={96} color={colors.marcaLine} strokeWidth={1.2} />
            )}
          </View>
        </View>
      </CartaoNeon>

      <CartaoNeon semPadding>
        <Text style={styles.dicasTitulo}>Posicione o rosto no enquadramento</Text>
        <View style={styles.dicas}>
          {DICAS.map((d, i) => (
            <View key={d.texto} style={[styles.dica, i > 0 && styles.dicaDivisor]}>
              <d.icone size={iconSize.lg} color={colors.marca} />
              <Text style={styles.dicaTexto}>{d.texto}</Text>
            </View>
          ))}
        </View>
      </CartaoNeon>

      {erro ? <Text style={styles.erro}>{erro}</Text> : null}
      <PrimaryButton label="Capturar Foto" icone={Camera} onPress={() => obter('camera')} loading={enviando === 'camera'} disabled={enviando !== null} />
      <PrimaryButton label="Usar uma foto da galeria" icone={ImageIcon} variant="secondary" onPress={() => obter('galeria')} loading={enviando === 'galeria'} disabled={enviando !== null} />
    </View>
  );
}

const CANTO = 44;

const styles = StyleSheet.create({
  container: { gap: spacing.md },
  moldura: { padding: spacing.xl },
  area: { aspectRatio: 1, width: '100%', maxWidth: 360, alignSelf: 'center', alignItems: 'center', justifyContent: 'center' },
  canto: { position: 'absolute', width: CANTO, height: CANTO, borderColor: colors.marca },
  cantoSE: { top: 0, left: 0, borderTopWidth: 4, borderLeftWidth: 4, borderTopLeftRadius: radius.lg },
  cantoSD: { top: 0, right: 0, borderTopWidth: 4, borderRightWidth: 4, borderTopRightRadius: radius.lg },
  cantoIE: { bottom: 0, left: 0, borderBottomWidth: 4, borderLeftWidth: 4, borderBottomLeftRadius: radius.lg },
  cantoID: { bottom: 0, right: 0, borderBottomWidth: 4, borderRightWidth: 4, borderBottomRightRadius: radius.lg },
  circulo: {
    width: '76%',
    aspectRatio: 1,
    borderRadius: radius.pill,
    borderWidth: 2,
    borderColor: colors.marca,
    boxShadow: efeitos.brilhoMarca,
    overflow: 'hidden',
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: colors.bg2,
  },
  foto: { width: '100%', height: '100%' },
  dicasTitulo: { color: colors.text, fontSize: fontSize.body, fontWeight: fontWeight.bold, textAlign: 'center', paddingVertical: spacing.md, borderBottomWidth: 1, borderBottomColor: colors.lineSoft },
  dicas: { flexDirection: 'row', paddingVertical: spacing.md },
  dica: { flex: 1, alignItems: 'center', gap: spacing.sm, paddingHorizontal: spacing.xs },
  dicaDivisor: { borderLeftWidth: 1, borderLeftColor: colors.lineSoft },
  dicaTexto: { color: colors.sub, fontSize: fontSize.secondary, textAlign: 'center' },
  erro: { color: colors.danger, fontSize: fontSize.secondary, textAlign: 'center' },
});
