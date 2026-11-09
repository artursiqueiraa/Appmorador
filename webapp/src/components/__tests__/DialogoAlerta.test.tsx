/**
 * Web App — o `Alert.alert` do react-native-web não faz nada; o substituto precisa
 * mostrar o diálogo e executar o `onPress` do botão escolhido (ex.: "Sair").
 */
import React from 'react';
import { act, fireEvent, render, screen } from '@testing-library/react-native';
import { Alert, DialogoAlertaHost } from '../DialogoAlerta';

describe('DialogoAlerta', () => {
  it('mostra título/mensagem e executa o botão escolhido', async () => {
    const sair = jest.fn();
    await render(<DialogoAlertaHost />);

    await act(async () => {
      Alert.alert('Sair da conta?', 'Você pode entrar de novo quando quiser.', [
        { text: 'Cancelar', style: 'cancel' },
        { text: 'Sair', style: 'destructive', onPress: sair },
      ]);
    });

    expect(screen.getByText('Sair da conta?')).toBeTruthy();
    expect(screen.getByText('Você pode entrar de novo quando quiser.')).toBeTruthy();

    await fireEvent.press(screen.getByText('Sair'));
    expect(sair).toHaveBeenCalledTimes(1);
    expect(screen.queryByText('Sair da conta?')).toBeNull();
  });

  it('cancelar fecha sem executar a ação destrutiva', async () => {
    const excluir = jest.fn();
    await render(<DialogoAlertaHost />);

    await act(async () => {
      Alert.alert('Excluir?', undefined, [
        { text: 'Cancelar', style: 'cancel' },
        { text: 'Excluir', style: 'destructive', onPress: excluir },
      ]);
    });

    await fireEvent.press(screen.getByText('Cancelar'));
    expect(excluir).not.toHaveBeenCalled();
    expect(screen.queryByText('Excluir?')).toBeNull();
  });

  it('sem botões, mostra só "OK"', async () => {
    await render(<DialogoAlertaHost />);
    await act(async () => {
      Alert.alert('Salvo');
    });
    await fireEvent.press(screen.getByText('OK'));
    expect(screen.queryByText('Salvo')).toBeNull();
  });
});
