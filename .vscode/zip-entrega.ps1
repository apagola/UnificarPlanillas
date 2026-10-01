# ---------------------------------------------------------------------------
#  Deja un ZIP listo para pasar al servidor de CAF y abre la carpeta con el ZIP
#  seleccionado.
#
#  En CAF NO se instala por ClickOnce: en el escritorio del servidor hay una
#  carpeta PlanillasApp que es una copia de bin\Debug y el exe se lanza desde
#  ahi. Por eso se empaqueta la carpeta bin\Debug ENTERA (como el
#  "Unificador planillas.zip" que se venia entregando).
#
#  NO compila: se compila antes desde Visual Studio.
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
    [switch]$NoOpen
)

$ErrorActionPreference = 'Stop'

$raiz    = Split-Path -Parent $PSScriptRoot
$salida  = Join-Path $raiz 'UnificarPlanillas\bin\Debug'
$sello   = Get-Date -Format 'yyyyMMdd_HHmm'
$zip     = Join-Path (Split-Path -Parent $salida) "UnificadorPlanillas_CAF_$sello.zip"
$staging = Join-Path $env:TEMP "zip_entrega_unificar_$sello"

$exe = Join-Path $salida 'Unificador planillas.exe'
if (-not (Test-Path $exe)) { throw "No existe $exe. Compila antes desde Visual Studio." }

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

$mb = [math]::Round((Get-Item $zip).Length / 1MB, 1)
Write-Host ""
Write-Host "  ZIP listo para pasar a CAF: $ficheros ficheros, $mb MB" -ForegroundColor Green
Write-Host "  $zip"
Write-Host "  En el servidor: cerrar el programa y descomprimir encima de PlanillasApp (sobrescribir)"
Write-Host ""

if (-not $NoOpen) { explorer.exe "/select,$zip" }
