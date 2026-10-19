param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[a-z0-9-]+$')]
    [string]$Fase
)

$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$destino = Join-Path $PSScriptRoot 'evidencias'
New-Item -ItemType Directory -Path $destino -Force | Out-Null
$trx = Join-Path $destino "$Fase.trx"
if (Test-Path -LiteralPath $trx) { throw "Ya existe $trx. Usa otro nombre para conservar la evidencia." }

$rutas = @(
    'Dfit/Models/ReglasMembresia.cs',
    'Dfit/Models/Patterns/Strategy/PaymentStrategy.cs',
    'Dfit/Models/Patterns/Proxy/GymAccessProxy.cs',
    'Dfit/Controllers/HomeController.cs',
    'Dfit/Utils/EnvLoader.cs',
    'PruebasUnitarias/PruebasUnitarias.csproj',
    'PruebasUnitarias/MSTestSettings.cs',
    'PruebasUnitarias/PruebaDocumentada.cs',
    'PruebasUnitarias/CalculoVigenciaTests.cs',
    'PruebasUnitarias/MembresiaAccesoTests.cs',
    'PruebasUnitarias/Test1.cs',
    'PruebasUnitarias/EnvLoaderTests.cs'
)
$fuentes = foreach ($ruta in $rutas) {
    $archivo = Join-Path $repo $ruta
    [ordered]@{ ruta = $ruta; sha256 = (Get-FileHash $archivo -Algorithm SHA256).Hash; contenido = [IO.File]::ReadAllText($archivo) }
}
$fuentes | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $destino "$Fase-fuentes.json") -Encoding utf8

# Configuracion del proceso de ejecucion, sin modificar configuracion del usuario.
$env:DOTNET_CLI_HOME = Join-Path $env:LOCALAPPDATA 'Temp\dfit-dotnet-cli'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = '0'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$env:NUGET_PACKAGES = Join-Path $env:USERPROFILE '.nuget\packages'
$argumentos = @(
    'run', '--project', (Join-Path $repo 'PruebasUnitarias/PruebasUnitarias.csproj'),
    '-p:UseVSTest=false', '-p:EnableMicrosoftTestingExtensionsCodeCoverage=false',
    '-p:RestoreIgnoreFailedSources=true', '-p:NuGetAudit=false',
    '--', '--report-trx', '--report-trx-filename', "$Fase.trx", '--results-directory', $destino
)
$inicio = [DateTime]::UtcNow.ToString('O')
& dotnet @argumentos 2>&1 | Tee-Object -FilePath (Join-Path $destino "$Fase-salida.txt")
$codigo = $LASTEXITCODE
[ordered]@{
    fase = $Fase; inicioUtc = $inicio; finUtc = [DateTime]::UtcNow.ToString('O')
    ejecutable = 'dotnet'; argumentos = $argumentos; codigoSalida = $codigo
    nota = 'Runner oficial MSTest directo. NuGetAudit=false solo evita consulta de red en esta ejecucion.'
} | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $destino "$Fase-ejecucion.json") -Encoding utf8
if (Test-Path -LiteralPath $trx) {
    [xml]$resultado = Get-Content -LiteralPath $trx -Raw
    $resultado.TestRun.ResultSummary.Counters | Format-List total, executed, passed, failed, notExecuted
}
exit $codigo
