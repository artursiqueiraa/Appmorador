# ADR 0002 — Conectividade com o DVR na LAN do cliente

- Status: **Em aberto — decisão não tomada**
- Data: 2026-07-18

## Contexto

O produto precisa, a partir da nuvem, alcançar o DVR (Intelbras/Dahua via CGI, Hikvision via ISAPI)
que fica num IP privado dentro da rede do cliente (residência/loja), para:

1. Mostrar live-view no momento de um disparo de zona.
2. Buscar e baixar um clipe gravado no intervalo `[T-pré, T+pós]` quando uma ocorrência é aberta.

Restrições do produto (não negociáveis neste ADR):
- **Não depender de configuração de MikroTik/roteador** pelo cliente final (não é um instalador de rede).
- **Não expor o DVR diretamente na internet** (sem port-forward manual, sem IP público no DVR).

Isso é diferente do problema já resolvido na integração com a central de alarme JFL: lá, é a
própria central que abre a conexão TCP de saída para o nosso servidor (push/report), então não há
porta para abrir no cliente — ver inventário da Fase 0. O DVR, ao contrário, não tem esse
comportamento nativo: os protocolos CGI (Dahua/Intelbras) e ISAPI (Hikvision) são desenhados para
serem *consultados* (pull), assumindo que quem pergunta está na mesma rede ou tem rota direta até o
IP do equipamento.

## Opções candidatas (nenhuma escolhida ainda)

1. **P2P do fabricante** (ex.: Dahua/Intelbras P2P cloud, Hikvision Hik-Connect/EZVIZ P2P).
   - Prós: zero hardware extra, já embutido no firmware dos DVRs.
   - Contras: depende da nuvem do fabricante (disponibilidade, rate limit, mudança de API sem
     aviso), pode não suportar download de clipe por intervalo arbitrário (muitas vezes só
     live-view), autenticação/cadastro do dispositivo na nuvem do fabricante é um passo de
     configuração que o cliente final teria que fazer (ou nós, remotamente).

2. **Túnel reverso de saída a partir de um ponto da instalação** (ex.: um pequeno agente/gateway
   que abre uma conexão de saída — WireGuard, mTLS/gRPC reverso, ou algo tipo frp/ngrok
   self-hosted — para o nosso backend, e faz proxy do tráfego CGI/ISAPI para o DVR local).
   - Prós: protocolo real (CGI/ISAPI) preservado ponta a ponta, não depende de nuvem de terceiro,
     mesma filosofia do modelo JFL (saída, sem porta aberta no cliente).
   - Contras: exige um processo rodando em algum lugar da rede do cliente (o próprio DVR, se
     suportar rodar binário/script; um Raspberry Pi/mini-PC; ou o celular do cliente, o que não
     serve para clipes pós-fato). Precisa de gestão de identidade/certificado por instalação.

3. **Agente de instalação futuro** (hardware/software dedicado instalado pela equipe técnica no ato
   da instalação, propósito único: manter um túnel de saída vivo e falar CGI/ISAPI localmente).
   - Prós: controle total do comportamento, pode também fazer buffer local, health-check do DVR,
     etc.
   - Contras: custo de hardware e logística de instalação; é essencially a Opção 2 formalizada como
     produto.

## Decisão

**Ainda não tomada.** Este ADR será atualizado quando a decisão for confirmada com o usuário.

## Consequência da indecisão (mitigação adotada agora)

Para não bloquear o desenvolvimento do pipeline de clipe e do live-view enquanto a decisão não é
tomada, o backend definirá a interface:

```csharp
public interface IDvrConnectivity
{
    Task<DvrMediaEndpoint> ResolveMediaEndpointAsync(Guid deviceId, CancellationToken ct);
}
```

`DvrMediaEndpoint` encapsula o suficiente para o conector CGI/ISAPI operar (base URL alcançável,
credenciais, e opcionalmente um `HttpMessageHandler`/proxy a usar na chamada) sem que o chamador
saiba *como* essa rota foi resolvida (P2P, túnel, ou IP direto).

Uma implementação **stub/mock** (`DirectLanDvrConnectivity`, que hoje apenas devolve o IP:porta
cadastrado do DVR — válido em ambiente de desenvolvimento/rede local ou quando o cliente já tem
IP público/VPN de terceiros) será registrada por padrão, permitindo construir e testar o pipeline de
clipe e o live-view (Fase de `Clips`/`Devices`) sem esperar a decisão final. Trocar de
implementação depois não deve exigir mudança em `Clips`, `Devices` ou nos controllers — só na
composition root (DI).

## Revisão

Retomar esta decisão antes da Fase que implementa `Clips` de verdade contra hardware real (não
apenas contra o stub). Critérios a levantar antes de decidir: SLA de disponibilidade de cada
fabricante para P2P, custo de manter um agente de túnel por instalação, e quantos clientes-piloto
já têm algum tipo de acesso remoto configurado (VPN corporativa, IP fixo, etc.) que tornaria a
questão moot no curto prazo.
