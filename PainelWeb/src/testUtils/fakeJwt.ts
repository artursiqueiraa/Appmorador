/** Sprint 22A — monta um JWT sem assinatura real (suficiente para `jwt-decode`, que nunca verifica assinatura). */
export function criarFakeJwt(payload: Record<string, unknown>): string {
  const header = { alg: 'HS256', typ: 'JWT' };
  const base64UrlEncode = (obj: object) =>
    btoa(JSON.stringify(obj)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');

  return `${base64UrlEncode(header)}.${base64UrlEncode(payload)}.fake-signature`;
}
