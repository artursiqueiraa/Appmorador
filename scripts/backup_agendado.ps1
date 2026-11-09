#Requires -Version 5.1
<#
.SYNOPSIS
    Backup diario do banco do AppMorador + teste de restauracao (Sprint 27, ADR 0043).
.DESCRIPTION
    Roda pelo Agendador de Tarefas do Windows (ver scripts\agendar_backup.ps1), todo dia as 3h:
      1. mysqldump completo -> database\backup\diario\appmorador_AAAAMMDD_HHmm.zip
      2. Aos domingos, copia para database\backup\semanal\
      3. Retencao: 7 diarios e 4 semanais (os mais antigos sao apagados)
      4. Teste de restauracao: carrega o dump no banco appmorador_restore_teste e confere se as
         tabelas e a quantidade de usuarios batem com o banco de origem
      5. Grava o resultado de cada etapa na tabela RegistrosBackup (o painel admin e o vigia de
         saude leem dali)

    Senha do MySQL: variavel APPMORADOR_DB_PASSWORD ou, se ausente, o arquivo protegido por DPAPI
    criado por agendar_backup.ps1 (%LOCALAPPDATA%\AppMorador\backup.cred, legivel so pelo mesmo
    usuario do Windows). Nunca fica no repositorio.

    O banco de teste precisa existir (o usuario appmorador nao tem CREATE DATABASE, ver ADR 0008).
    Preparo unico, com um usuario privilegiado:
      CREATE DATABASE appmorador_restore_teste CHARACTER SET utf8mb4;
      GRANT ALL PRIVILEGES ON appmorador_restore_teste.* TO 'appmorador'@'localhost';
.EXAMPLE
    .\scripts\backup_agendado.ps1
#>
param(
    [string]$Usuario = "appmorador",
    [string]$Banco = "appmorador",
    [string]$BancoTeste = "appmorador_restore_teste",
    [int]$ManterDiarios = 7,
    [int]$ManterSemanais = 4,
    [string]$MysqlBin = "C:\Program Files\MySQL\MySQL Server 8.0\bin"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$dirDiario = Join-Path $repoRoot "database\backup\diario"
$dirSemanal = Join-Path $repoRoot "database\backup\semanal"
$dirLog = Join-Path $repoRoot "database\backup\logs"
New-Item -ItemType Directory -Force -Path $dirDiario, $dirSemanal, $dirLog | Out-Null
$log = Join-Path $dirLog ("backup_" + (Get-Date -Format "yyyyMMdd") + ".log")

function Escrever([string]$texto) {
    $linha = "[{0}] {1}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $texto
    Write-Host $linha
    Add-Content -Path $log -Value $linha -Encoding UTF8
}

function Resolver-Ferramenta([string]$nome) {
    $noPath = Get-Command $nome -ErrorAction SilentlyContinue
    if ($noPath) { return $noPath.Source }
    $padrao = Join-Path $MysqlBin "$nome.exe"
    if (Test-Path $padrao) { return $padrao }
    throw "$nome.exe nao encontrado no PATH nem em '$MysqlBin'."
}

function Obter-Senha {
    if ($env:APPMORADOR_DB_PASSWORD) { return $env:APPMORADOR_DB_PASSWORD }
    $cred = Join-Path $env:LOCALAPPDATA "AppMorador\backup.cred"
    if (Test-Path $cred) {
        $seguro = Get-Content $cred | ConvertTo-SecureString
        return [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($seguro))
    }
    throw "Senha do MySQL nao encontrada: defina APPMORADOR_DB_PASSWORD ou rode scripts\agendar_backup.ps1."
}

function Sql([string]$texto) {
    # Aspas simples escapadas para uso dentro de literal SQL.
    if ([string]::IsNullOrEmpty($texto)) { return "NULL" }
    # Regex '\\' = uma barra; na substituicao do .NET a barra e literal, entao '\\' vira duas.
    return "'" + ($texto -replace '\\', '\\' -replace "'", "''") + "'"
}

function Registrar([string]$tipo, [bool]$sucesso, [string]$arquivo, $tamanho, [string]$mensagem) {
    $id = [guid]::NewGuid().ToString()
    $quando = (Get-Date).ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.ffffff")
    $tam = if ($null -eq $tamanho) { "NULL" } else { [string]$tamanho }
    if ($mensagem -and $mensagem.Length -gt 500) { $mensagem = $mensagem.Substring(0, 500) }
    $cmd = "INSERT INTO RegistrosBackup (Id, Tipo, Sucesso, Arquivo, TamanhoBytes, Mensagem, OcorridoEmUtc) VALUES ('$id', '$tipo', $([int]$sucesso), $(Sql $arquivo), $tam, $(Sql $mensagem), '$quando');"
    try {
        & $script:mysql -u $Usuario $Banco -e $cmd 2>&1 | Out-Null
        if ($LASTEXITCODE -ne 0) { Escrever "Aviso: nao foi possivel registrar '$tipo' no banco." }
    } catch {
        Escrever "Aviso: nao foi possivel registrar '$tipo' no banco: $($_.Exception.Message)"
    }
}

function Contar([string]$banco, [string]$consulta) {
    $saida = & $script:mysql -u $Usuario $banco -N -B -e $consulta 2>&1
    if ($LASTEXITCODE -ne 0) { throw "Consulta falhou em ${banco}: $saida" }
    return [int64](($saida | Select-Object -Last 1).ToString().Trim())
}

$script:mysql = Resolver-Ferramenta "mysql"
$mysqldump = Resolver-Ferramenta "mysqldump"
$env:MYSQL_PWD = Obter-Senha

$data = Get-Date -Format "yyyyMMdd_HHmm"
$sqlTemp = Join-Path $env:TEMP "appmorador_$data.sql"
$zip = Join-Path $dirDiario "appmorador_$data.zip"

try {
    # --- 1. Backup -----------------------------------------------------------------------
    try {
        Escrever "Gerando backup de '$Banco'..."
        # --result-file evita a conversao de encoding do pipeline do PowerShell.
        & $mysqldump -u $Usuario --single-transaction --no-tablespaces --routines --triggers --skip-comments --complete-insert --result-file="$sqlTemp" $Banco 2>&1 | ForEach-Object { Escrever "mysqldump: $_" }
        if ($LASTEXITCODE -ne 0) { throw "mysqldump terminou com codigo $LASTEXITCODE." }
        Compress-Archive -Path $sqlTemp -DestinationPath $zip -Force
        $tamanho = (Get-Item $zip).Length
        Escrever "Backup gerado: $zip ($([math]::Round($tamanho / 1KB)) KB)"
        Registrar "Backup" $true (Split-Path $zip -Leaf) $tamanho $null
    } catch {
        Escrever "FALHA no backup: $($_.Exception.Message)"
        Registrar "Backup" $false $null $null $_.Exception.Message
        exit 1
    }

    # --- 2. Semanal e retencao -------------------------------------------------------------
    if ((Get-Date).DayOfWeek -eq [DayOfWeek]::Sunday) {
        Copy-Item $zip -Destination $dirSemanal -Force
        Escrever "Copia semanal guardada."
    }
    Get-ChildItem $dirDiario -Filter "appmorador_*.zip" | Sort-Object Name -Descending | Select-Object -Skip $ManterDiarios | ForEach-Object {
        Remove-Item $_.FullName -Force
        Escrever "Diario antigo apagado: $($_.Name)"
    }
    Get-ChildItem $dirSemanal -Filter "appmorador_*.zip" | Sort-Object Name -Descending | Select-Object -Skip $ManterSemanais | ForEach-Object {
        Remove-Item $_.FullName -Force
        Escrever "Semanal antigo apagado: $($_.Name)"
    }

    # --- 3. Teste de restauracao ------------------------------------------------------------
    try {
        Escrever "Testando restauracao em '$BancoTeste'..."
        $existe = & $script:mysql -u $Usuario -N -B -e "SHOW DATABASES LIKE '$BancoTeste';" 2>&1
        if (-not ($existe -match $BancoTeste)) {
            throw "O banco de teste '$BancoTeste' nao existe ou o usuario '$Usuario' nao tem acesso. Preparo unico (com root): CREATE DATABASE $BancoTeste CHARACTER SET utf8mb4; GRANT ALL PRIVILEGES ON $BancoTeste.* TO '$Usuario'@'localhost';"
        }

        # Restaura a partir do .zip (o mesmo arquivo que ficaria guardado), nao do .sql temporario.
        $dirExtraido = Join-Path $env:TEMP "appmorador_restore_$data"
        Expand-Archive -Path $zip -DestinationPath $dirExtraido -Force
        $sqlRestaurar = (Get-ChildItem $dirExtraido -Filter "*.sql" | Select-Object -First 1).FullName
        $caminhoMysql = $sqlRestaurar -replace "\\", "/"
        & $script:mysql -u $Usuario $BancoTeste -e "source $caminhoMysql" 2>&1 | ForEach-Object { Escrever "mysql: $_" }
        if ($LASTEXITCODE -ne 0) { throw "A carga do backup no banco de teste falhou (codigo $LASTEXITCODE)." }
        Remove-Item $dirExtraido -Recurse -Force

        $consultaTabelas = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE();"
        $tabelasOrigem = Contar $Banco $consultaTabelas
        $tabelasTeste = Contar $BancoTeste $consultaTabelas
        $usuariosTeste = Contar $BancoTeste "SELECT COUNT(*) FROM Usuarios;"
        $migracaoTeste = & $script:mysql -u $Usuario $BancoTeste -N -B -e "SELECT MAX(MigrationId) FROM __EFMigrationsHistory;" 2>&1

        if ($tabelasTeste -ne $tabelasOrigem) {
            throw "Restaurou $tabelasTeste tabelas, mas a origem tem $tabelasOrigem."
        }
        if ($usuariosTeste -lt 1) {
            throw "O banco restaurado nao tem usuarios."
        }
        $resumo = "$tabelasTeste tabelas, $usuariosTeste usuarios, ultima migration $migracaoTeste"
        Escrever "Restauracao OK: $resumo"
        Registrar "TesteRestauracao" $true (Split-Path $zip -Leaf) $null $resumo
    } catch {
        Escrever "FALHA no teste de restauracao: $($_.Exception.Message)"
        Registrar "TesteRestauracao" $false (Split-Path $zip -Leaf) $null $_.Exception.Message
        exit 2
    }
}
finally {
    Remove-Item $sqlTemp -ErrorAction SilentlyContinue
    Remove-Item Env:\MYSQL_PWD -ErrorAction SilentlyContinue
}
