import { Drawer, Box, Typography, IconButton, Stack, Chip, Divider, Button } from '@mui/material';
import CloseIcon from '@mui/icons-material/Close';
import EditIcon from '@mui/icons-material/Edit';
import type { EquipamentoAdmin } from './types';

interface DetalheEquipamentoDrawerProps {
  equipamento: EquipamentoAdmin | null;
  onFechar: () => void;
  onEditar: (equipamento: EquipamentoAdmin) => void;
}

function Campo({ rotulo, valor }: { rotulo: string; valor?: string | null }) {
  if (!valor) {
    return null;
  }

  return (
    <Box>
      <Typography variant="caption" color="text.secondary">
        {rotulo}
      </Typography>
      <Typography variant="body1">{valor}</Typography>
    </Box>
  );
}

function formatarData(valor?: string | null) {
  return valor ? new Date(valor).toLocaleString('pt-BR') : null;
}

/**
 * Sprint 22C.2 — detalhe somente-leitura do Equipamento. Os campos exibidos dependem do
 * Fabricante (mission brief: "nunca mostrar campos vazios apenas porque outro fabricante possui
 * essas informações") — `Campo` já resolve isso sozinho ao não renderizar nada quando o valor é
 * nulo/vazio, então basta listar os campos relevantes por fabricante sem `if` extra.
 */
export function DetalheEquipamentoDrawer({ equipamento, onFechar, onEditar }: DetalheEquipamentoDrawerProps) {
  const descobertas = equipamento?.informacoesDescobertas ?? {};

  return (
    <Drawer anchor="right" open={Boolean(equipamento)} onClose={onFechar}>
      <Box sx={{ width: 380, p: 2 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 2 }}>
          <Typography variant="h3">{equipamento?.nome}</Typography>
          <IconButton onClick={onFechar} size="small">
            <CloseIcon fontSize="small" />
          </IconButton>
        </Box>

        {equipamento ? (
          <Stack spacing={2}>
            <Campo rotulo="Propriedade" valor={equipamento.propriedadeNome} />

            <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
              <Chip size="small" label={equipamento.fabricante} variant="outlined" />
              <Chip size="small" label={equipamento.status} color={equipamento.status === 'Online' ? 'success' : equipamento.status === 'Offline' ? 'error' : 'default'} />
              <Chip size="small" label={equipamento.estadoOperacional} variant="outlined" />
            </Box>

            <Divider />

            {equipamento.fabricante === 'Jfl' ? (
              <>
                <Campo rotulo="Número de Série" valor={equipamento.numeroSerie} />
                <Campo rotulo="MAC Address" valor={equipamento.macAddress} />
                <Campo rotulo="Modelo" valor={descobertas.Modelo} />
                <Campo rotulo="Firmware" valor={descobertas.Firmware} />
                <Campo rotulo="IMEI" valor={descobertas.Imei} />
                <Campo rotulo="Última conexão" valor={formatarData(equipamento.ultimaSincronizacaoUtc)} />
              </>
            ) : (
              <>
                <Campo rotulo="IP" valor={equipamento.ip} />
                <Campo rotulo="Porta" valor={equipamento.porta?.toString()} />
                <Campo rotulo="Usuário" valor={equipamento.usuario} />
                <Campo rotulo="Número de Série" valor={equipamento.numeroSerie} />
                <Campo rotulo="Versão" valor={descobertas.Versao} />
                <Campo rotulo="Nome do Dispositivo" valor={descobertas.NomeDispositivo} />
                <Campo rotulo="Última sincronização" valor={formatarData(equipamento.ultimaSincronizacaoUtc)} />
              </>
            )}

            <Campo rotulo="Última descoberta automática" valor={formatarData(equipamento.ultimaDescobertaUtc)} />
            <Campo rotulo="Observações" valor={equipamento.observacoes} />

            <Divider />

            <Button variant="outlined" startIcon={<EditIcon />} onClick={() => onEditar(equipamento)} fullWidth>
              Editar cadastro
            </Button>
          </Stack>
        ) : null}
      </Box>
    </Drawer>
  );
}
