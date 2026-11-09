/**
 * Base única (ADR 0039) — a foto do rosto entra no FormData em formatos diferentes por plataforma:
 * `{ uri, name, type }` no Android/iOS e `Blob` no navegador.
 */
// O resolvedor do lint é o do navegador e enxerga os dois imports como o mesmo arquivo `.web.ts`.
// eslint-disable-next-line import/no-duplicates
import { anexarFoto as anexarNativo } from '../arquivoFoto';
// eslint-disable-next-line import/no-duplicates
import { anexarFoto as anexarWeb } from '../arquivoFoto.web';

const novoFormulario = () => ({ append: jest.fn() }) as unknown as FormData & { append: jest.Mock };

describe('anexarFoto (Android/iOS)', () => {
  it('envia o arquivo local como { uri, name, type } em JPEG', async () => {
    const formulario = novoFormulario();
    await anexarNativo(formulario, 'foto', 'file:///cache/ImagePicker/abc.jpg');
    expect(formulario.append).toHaveBeenCalledWith('foto', {
      uri: 'file:///cache/ImagePicker/abc.jpg',
      name: 'rosto.jpg',
      type: 'image/jpeg',
    });
  });

  it('reconhece PNG pela extensão', async () => {
    const formulario = novoFormulario();
    await anexarNativo(formulario, 'foto', 'file:///cache/abc.PNG');
    expect(formulario.append).toHaveBeenCalledWith('foto', expect.objectContaining({ name: 'rosto.png', type: 'image/png' }));
  });
});

describe('anexarFoto (navegador)', () => {
  const fetchOriginal = globalThis.fetch;
  afterEach(() => {
    globalThis.fetch = fetchOriginal;
  });

  it('converte a URI em Blob e nomeia pelo tipo', async () => {
    const blob = { type: 'image/png' };
    globalThis.fetch = jest.fn().mockResolvedValue({ blob: () => Promise.resolve(blob) }) as unknown as typeof fetch;
    const formulario = novoFormulario();
    await anexarWeb(formulario, 'foto', 'blob:http://localhost/123');
    expect(globalThis.fetch).toHaveBeenCalledWith('blob:http://localhost/123');
    expect(formulario.append).toHaveBeenCalledWith('foto', blob, 'rosto.png');
  });
});
