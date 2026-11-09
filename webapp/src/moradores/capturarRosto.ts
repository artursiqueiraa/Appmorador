import * as ImagePicker from 'expo-image-picker';

const OPCOES: ImagePicker.ImagePickerOptions = { mediaTypes: ['images'], quality: 0.7, allowsEditing: true, aspect: [1, 1] };

/**
 * Akteck Seg (ADR 0036/0037) — foto pela câmera (no computador, o navegador abre a webcam ou o
 * seletor de arquivo). Recorte quadrado, como o leitor facial espera. Nulo = cancelado.
 */
export async function capturarDaCamera(): Promise<string | null> {
  const permissao = await ImagePicker.requestCameraPermissionsAsync();
  if (!permissao.granted) throw new Error('SEM_PERMISSAO');
  const resultado = await ImagePicker.launchCameraAsync(OPCOES);
  return resultado.canceled ? null : resultado.assets[0].uri;
}

/** Foto já existente, escolhida da galeria (ou de uma pasta, no computador). */
export async function escolherDaGaleria(): Promise<string | null> {
  const permissao = await ImagePicker.requestMediaLibraryPermissionsAsync();
  if (!permissao.granted) throw new Error('SEM_PERMISSAO');
  const resultado = await ImagePicker.launchImageLibraryAsync(OPCOES);
  return resultado.canceled ? null : resultado.assets[0].uri;
}

/** Câmera primeiro; galeria se a câmera não for permitida (prioridade da Sprint 17). */
export async function capturarRosto(): Promise<string | null> {
  try {
    return await capturarDaCamera();
  } catch {
    return escolherDaGaleria();
  }
}
