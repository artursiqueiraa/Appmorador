/**
 * Base única (ADR 0039) — no navegador, o seletor de imagem devolve um `data:`/`blob:` URI; ele é
 * convertido em `Blob` para entrar no `FormData`. A versão Android/iOS fica em `arquivoFoto.ts`.
 */
export async function anexarFoto(formulario: FormData, campo: string, uri: string): Promise<void> {
  const arquivo = await (await fetch(uri)).blob();
  formulario.append(campo, arquivo, arquivo.type === 'image/png' ? 'rosto.png' : 'rosto.jpg');
}
