using AppMorador.Application.Notificacoes;
using AppMorador.Infrastructure.Notifications;
using Xunit;

namespace AppMorador.Tests.Notificacoes;

// Sprint 19 (Fase 8.2) — a janela de debounce (60s) e fixa dentro da classe (sem
// relogio injetavel), entao so testamos o comportamento imediato aqui: passar 60s de
// verdade num teste seria lento/fragil. O reset apos a janela fica coberto pela
// leitura do codigo (DateTime.UtcNow - ultimoEnvioUtc >= Janela), nao por um teste
// automatizado — registrado como limitacao conhecida, nao como lacuna escondida.
public class DebounceNotificacaoEmMemoriaTests
{
    [Fact]
    public void PodeNotificar_PrimeiraChamada_RetornaTrue()
    {
        var debounce = new DebounceNotificacaoEmMemoria();

        Assert.True(debounce.PodeNotificar(EventoNotificacaoTipo.EquipamentoOffline, Guid.NewGuid()));
    }

    [Fact]
    public void PodeNotificar_AposRegistrarEnvio_RetornaFalseImediatamente()
    {
        var debounce = new DebounceNotificacaoEmMemoria();
        var chave = Guid.NewGuid();

        debounce.RegistrarEnvio(EventoNotificacaoTipo.EquipamentoOffline, chave);

        Assert.False(debounce.PodeNotificar(EventoNotificacaoTipo.EquipamentoOffline, chave));
    }

    [Fact]
    public void PodeNotificar_ChaveDiferente_NaoEAfetadaPeloDebounceDeOutraChave()
    {
        var debounce = new DebounceNotificacaoEmMemoria();
        debounce.RegistrarEnvio(EventoNotificacaoTipo.EquipamentoOffline, Guid.NewGuid());

        Assert.True(debounce.PodeNotificar(EventoNotificacaoTipo.EquipamentoOffline, Guid.NewGuid()));
    }

    [Fact]
    public void PodeNotificar_MesmaChaveTipoDiferente_NaoEAfetadaPeloDebounceDeOutroTipo()
    {
        var debounce = new DebounceNotificacaoEmMemoria();
        var chave = Guid.NewGuid();
        debounce.RegistrarEnvio(EventoNotificacaoTipo.EquipamentoOffline, chave);

        Assert.True(debounce.PodeNotificar(EventoNotificacaoTipo.AlarmeDisparado, chave));
    }
}
