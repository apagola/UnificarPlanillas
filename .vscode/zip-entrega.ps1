# ---------------------------------------------------------------------------
#  Deja un ZIP listo para pasar al servidor de CAF y abre la carpeta con el ZIP
#  seleccionado.
#
#  En CAF NO se instala por ClickOnce: en el escritorio del servidor hay una
#  carpeta PlanillasApp que es una copia de bin\Debug y el exe se lanza desde
#  ahi. Por eso se empaqueta la carpeta bin\Debug ENTERA (como el
#  "Unificador planillas.zip" que se venia entregando).
#
#  COMPILA SIEMPRE en Debug antes de empaquetar (MSBuild de VS 2022, via
#  vswhere): como en el servidor PlanillasApp es una copia de bin\Debug, si se
#  compilaba solo en Release el exe de Debug se quedaba viejo y se entregaba sin
#  los cambios (paso el 05/10 y el 06/10/2026). Si la compilacion falla, no se
#  genera el ZIP. Con -SinCompilar se empaqueta lo que haya en bin\Debug.
#
#  De la copia se quita lo que no se sube o machacaria datos del servidor:
#    - Logs\           (logs locales)
#    - planillasBases\ (datos de trabajo, el servidor tiene los suyos)
#    - *.zip           (entregas anteriores que se colarian dentro)
#
#  En el servidor: cerrar el programa y descomprimir ENCIMA de PlanillasApp,
#  sobrescribiendo.
#
#  El ZIP se deja en bin\, al lado de Debug\.
# ---------------------------------------------------------------------------
param(
    [switch]$NoOpen,
    [switch]$SinCompilar
)

$ErrorActionPreference = 'Stop'

$raiz    = Split-Path -Parent $PSScriptRoot
$salida  = Join-Path $raiz 'UnificarPlanillas\bin\Debug'
$sello   = Get-Date -Format 'yyyyMMdd_HHmm'
$zip     = Join-Path (Split-Path -Parent $salida) "UnificadorPlanillas_CAF_$sello.zip"
$staging = Join-Path $env:TEMP "zip_entrega_unificar_$sello"

$exe = Join-Path $salida 'Unificador planillas.exe'

if ($SinCompilar) {
    Write-Host ""
    Write-Host "  -SinCompilar: se empaqueta bin\Debug tal cual." -ForegroundColor Yellow
} else {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (-not (Test-Path $vswhere)) { throw "No existe $vswhere. No se puede localizar MSBuild." }
    $msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
    if (-not $msbuild -or -not (Test-Path $msbuild)) { throw "vswhere no encuentra el MSBuild.exe de Visual Studio." }

    $vbproj = Join-Path $raiz 'UnificarPlanillas\UnificarPlanillas.vbproj'
    Write-Host ""
    Write-Host "  Compilando Debug con $msbuild ..." -ForegroundColor Cyan
    & $msbuild $vbproj /t:Build /p:Configuration=Debug /v:m /nologo
    if ($LASTEXITCODE -ne 0) {
        throw "La compilacion en Debug ha FALLADO (MSBuild exit code $LASTEXITCODE). No se genera el ZIP."
    }
}

if (-not (Test-Path $exe)) { throw "No existe $exe. Compila en Debug antes de empaquetar." }

# Red de seguridad (con la compilacion de arriba deberia pasar siempre). Se empaqueta bin\Debug: si se compilo en Release, el exe de Debug se queda viejo y se entrega sin
# los cambios (paso el 05/10/2026: dos entregas salieron con el exe del 24/07).
$fuentes = Get-ChildItem (Join-Path $raiz 'UnificarPlanillas') -Recurse -Include '*.vb', '*.config', '*.vbproj' |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }
$ultimaFuente = $fuentes | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($ultimaFuente.LastWriteTime -gt (Get-Item $exe).LastWriteTime) {
    throw "El exe de bin\Debug ($((Get-Item $exe).LastWriteTime.ToString('dd/MM/yyyy HH:mm'))) es mas viejo que $($ultimaFuente.Name) ($($ultimaFuente.LastWriteTime.ToString('dd/MM/yyyy HH:mm'))). Compila en DEBUG antes de empaquetar."
}

Write-Host ""
Write-Host "  Preparando el ZIP para CAF (exe compilado el $((Get-Item $exe).LastWriteTime.ToString('dd/MM/yyyy HH:mm')))..." -ForegroundColor Cyan

if (Test-Path $staging) { Remove-Item $staging -Recurse -Force }
Copy-Item $salida $staging -Recurse

foreach ($d in @('Logs', 'planillasBases')) {
    $p = Join-Path $staging $d
    if (Test-Path $p) { Remove-Item $p -Recurse -Force }
}
Get-ChildItem $staging -Include '*.zip' -Recurse | Remove-Item -Force

if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path (Join-Path $staging '*') -DestinationPath $zip -CompressionLevel Optimal

$ficheros = (Get-ChildItem $staging -Recurse -File).Count
Remove-Item $staging -Recurse -Force

# El exe que va dentro del ZIP tiene que ser el de bin\Debug (el ZIP guarda la fecha con 2 s de resolucion).
$fechaExe = (Get-Item $exe).LastWriteTime
Add-Type -AssemblyName System.IO.Compression.FileSystem
$z = [System.IO.Compression.ZipFile]::OpenRead($zip)
try {
    $entrada = $z.Entries | Where-Object { $_.FullName -eq 'Unificador planillas.exe' } | Select-Object -First 1
    if (-not $entrada) { throw "El ZIP no contiene 'Unificador planillas.exe'." }
    $fechaZip = $entrada.LastWriteTime.LocalDateTime
} finally { $z.Dispose() }
if ([math]::Abs(($fechaZip - $fechaExe).TotalSeconds) -gt 2) {
    throw "El exe del ZIP ($($fechaZip.ToString('dd/MM/yyyy HH:mm:ss'))) no coincide con el de bin\Debug ($($fechaExe.ToString('dd/MM/yyyy HH:mm:ss')))."
}

$mb = [math]::Round((Get-Item $zip).Length / 1MB, 1)
Write-Host ""
Write-Host "  ZIP listo para pasar a CAF: $ficheros ficheros, $mb MB" -ForegroundColor Green
Write-Host "  Exe del ZIP: $($fechaZip.ToString('dd/MM/yyyy HH:mm:ss'))  =  exe de bin\Debug: $($fechaExe.ToString('dd/MM/yyyy HH:mm:ss'))" -ForegroundColor Green
Write-Host "  $zip"
Write-Host "  En el servidor: cerrar el programa y descomprimir encima de PlanillasApp (sobrescribir)"
Write-Host ""

if (-not $NoOpen) { explorer.exe "/select,$zip" }
