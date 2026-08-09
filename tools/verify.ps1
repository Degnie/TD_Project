#!/usr/bin/env pwsh
# Contrato de verificación única — ADR-002.
# Seis comprobaciones: build, tests, trazabilidad spec<->test, frontera de
# arquitectura, mutación sobre módulos modificados, alcance de archivos.

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$failed = $false

function Section($title) {
    Write-Host ""
    Write-Host "=== $title ===" -ForegroundColor Cyan
}

# 1. Compilación sin errores
Section "1. Compilacion"
dotnet build --configuration Release
if ($LASTEXITCODE -ne 0) { Write-Host "FALLA: build" -ForegroundColor Red; $failed = $true }
else { Write-Host "OK: build" -ForegroundColor Green }

# 2. Suite de tests en verde
Section "2. Tests"
dotnet test --configuration Release
if ($LASTEXITCODE -ne 0) { Write-Host "FALLA: tests" -ForegroundColor Red; $failed = $true }
else { Write-Host "OK: tests" -ForegroundColor Green }

# 3. Trazabilidad: todo test cita un ID existente; toda RN/CU/EC/RNF verificable activa tiene test
Section "3. Trazabilidad spec <-> test"

# Ancla a lineas de declaracion canonica ("**RN-04 ..." o "* **CU-01: ...")
# para no capturar menciones sueltas en tablas/prosa (ej. IDs retirados, "Fuera de alcance").
# Dentro de una linea de declaracion, extrae TODOS los IDs (una declaracion
# puede agrupar varios, ej. "**RNF-01, RNF-02, RNF-03 * ...").
$specIds = Select-String -Path "SPEC.md" -Pattern '^\*\s*\*{0,2}(RN|CU|EC|RNF)-[0-9]{2}' |
    ForEach-Object {
        [regex]::Matches($_.Line, '(RN|CU|EC|RNF)-[0-9]{2}') | ForEach-Object { $_.Value }
    } |
    Sort-Object -Unique

$testFiles = Get-ChildItem -Path "tests" -Filter "*.cs" -Recurse -ErrorAction SilentlyContinue

$testIds = @()
$untaggedTests = @()

foreach ($file in $testFiles) {
    $lines = Get-Content $file.FullName
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match '\[(Fact|Theory)\]') {
            $citation = $null
            # Busca hacia atras mientras el bloque de comentario "//" sea contiguo
            # (sin longitud fija), para no depender de cuantas lineas ocupe la cita.
            $j = $i - 1
            while ($j -ge 0 -and $lines[$j].TrimStart().StartsWith('//')) {
                if ($lines[$j] -match 'spec:\s*((?:(?:RN|CU|EC|RNF)-[0-9]{2}(?:,\s*)?)+)') {
                    $citation = $Matches[1]
                }
                $j--
            }
            if ($null -eq $citation) {
                $untaggedTests += "$($file.FullName):$($i+1)"
            } else {
                $ids = [regex]::Matches($citation, '(RN|CU|EC|RNF)-[0-9]{2}') | ForEach-Object { $_.Value }
                $testIds += $ids
            }
        }
    }
}
$testIds = $testIds | Sort-Object -Unique

$unknownIds = $testIds | Where-Object { $specIds -notcontains $_ }
$uncoveredIds = $specIds | Where-Object { $testIds -notcontains $_ }

if ($unknownIds.Count -gt 0) {
    Write-Host "FALLA: tests citan IDs inexistentes en SPEC.md:" -ForegroundColor Red
    $unknownIds | ForEach-Object { Write-Host "  $_" }
    $failed = $true
}
if ($untaggedTests.Count -gt 0) {
    Write-Host "FALLA: tests sin cita 'spec:' adyacente:" -ForegroundColor Red
    $untaggedTests | ForEach-Object { Write-Host "  $_" }
    $failed = $true
}
if ($uncoveredIds.Count -gt 0) {
    Write-Host "IDs de SPEC.md sin ningun test (puede ser esperado si aun no hay tests, o si el ID es RNF cuantitativo pendiente):" -ForegroundColor Yellow
    $uncoveredIds | ForEach-Object { Write-Host "  $_" }
}
if ($unknownIds.Count -eq 0 -and $untaggedTests.Count -eq 0) {
    Write-Host "OK: extraccion y citas de IDs consistentes ($($specIds.Count) IDs en SPEC.md, $($testIds.Count) IDs citados en tests)" -ForegroundColor Green
}

# 4. Frontera de arquitectura respetada (Domain no depende de Application/Infrastructure)
Section "4. Frontera de arquitectura"
dotnet test tests/Application.Tests/Application.Tests.csproj --configuration Release --filter "FullyQualifiedName~ArchitectureTests"
if ($LASTEXITCODE -ne 0) { Write-Host "FALLA: regla de arquitectura" -ForegroundColor Red; $failed = $true }
else { Write-Host "OK: frontera de arquitectura (o sin tests de arquitectura aun)" -ForegroundColor Green }

# 5. Umbral de mutacion sobre modulos modificados (Stryker.NET) — placeholder hasta que exista codigo de dominio
Section "5. Umbral de mutacion"
Write-Host "PENDIENTE: se activa cuando exista implementacion en src/Domain (Etapa 3)" -ForegroundColor Yellow

# 6. Alcance: archivos modificados coinciden con los declarados
Section "6. Alcance de archivos"
Write-Host "PENDIENTE: comparacion manual contra el manifiesto de la Etapa 0 (no automatizado en este script)" -ForegroundColor Yellow

Write-Host ""
if ($failed) {
    Write-Host "VERIFY: FALLA" -ForegroundColor Red
    exit 1
} else {
    Write-Host "VERIFY: OK (con puntos pendientes marcados arriba, ver 5 y 6)" -ForegroundColor Green
    exit 0
}
