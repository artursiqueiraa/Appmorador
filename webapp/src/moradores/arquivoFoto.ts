/**
 * Base única (ADR 0039) — no Android/iOS, o seletor de imagem devolve um caminho `file://`. O
 * `FormData` do React Native não aceita `Blob` de arquivo local: ele envia o arquivo a partir de
 * `{ uri, name, type }`. A versão do navegador fica em `arquivoFoto.web.ts`.
 */
export async function anexarFoto(formulario: FormData, campo: string, uri: string): Promise<void> {
  const png = uri.toLowerCase().endsWith('.png');
  const arquivo = { uri, name: png ? 'rosto.png' : 'rosto.jpg', type: png ? 'image/png' : 'image/jpeg' };
  // Formato próprio do React Native; a tipagem DOM do FormData só conhece Blob/string.
  formulario.append(campo, arquivo as unknown as Blob);
}
