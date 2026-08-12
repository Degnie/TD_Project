# Especificación — Reporte de Incapacidades (Caso 4.4, D-096/D-097)

Estado: **documento de diseño implementable — previo a implementación**. Traduce D-096 (exposición
de `Incapacidades`) + D-097 (semántica: restricción económica observable, no error) a diseño
concreto. No modifica código en este documento.

---

## 1. Ubicación del dato

**`ResultadoCorridaTimeframe`** (`exploration/laboratorio/protocolo/EjecutorProtocolo.cs:27-33`),
extendido con un campo nuevo opcional — mismo patrón exacto que `MetricasFinancieras` (D-072/
D-077): campo `IReadOnlyList<RegistroIncapacidad>? Incapacidades = null`, poblado solo en la rama
`Success`, ubicado en el mismo `record` que ya agrupa la evidencia de una corrida por timeframe.

**Por qué no un DTO nuevo separado**: `RegistroIncapacidad` ya es un tipo completo y estable
(`src/Domain/Broker/RegistroIncapacidad.cs`, congelado desde Caso 2, D-059) — no requiere
reestructurarse para el reporte, solo dejar de descartarse. Introducir un DTO nuevo duplicaría
información sin necesidad, contrario al principio de "una sola fuente de verdad" ya aplicado en
D-077.

**Por qué no un reporte completamente separado**: `ReporteFinancieroGenerador.Generar` ya recibe
`ResultadoProtocolo` completo (que contiene `Corridas`, cada una con su
`ResultadoCorridaTimeframe`) — agregar una sección nueva a ese mismo reporte (sección 2 de este
documento) reutiliza la lectura ya existente, sin requerir que quien ejecuta el protocolo invoque
un generador adicional.

**Cambio exacto en `EjecutorProtocolo.cs:158-160`**:

```csharp
// Antes:
var metricasFinancieras = CalculadoraMetricasFinancieras.Calcular(resultado1, entrada.CapitalInicial);
return new ResultadoCorridaTimeframe(tf, EstadoCorridaTimeframe.Success, null, perfil, anexo, metricasFinancieras);

// Despues:
var metricasFinancieras = CalculadoraMetricasFinancieras.Calcular(resultado1, entrada.CapitalInicial);
return new ResultadoCorridaTimeframe(tf, EstadoCorridaTimeframe.Success, null, perfil, anexo, metricasFinancieras, resultado1.IncapacidadesEfectivas);
```

`resultado1.IncapacidadesEfectivas` ya existe (`ResultadoBacktest.cs:21`, retorna lista vacía si
`Incapacidades` es `null`) — no requiere lógica nueva de manejo de nulos.

---

## 2. Formato del reporte

**Nueva sección en `ReporteFinancieroGenerador.Generar`** (`exploration/laboratorio/
modelo_financiero/ReporteFinancieroGenerador.cs`), insertada después de la sección 3 (Métricas
financieras) y antes de la sección 4 (Límites) — mismo documento, no uno nuevo, siguiendo la
estructura de secciones numeradas ya establecida.

**Contenido, siguiendo D-097 (lenguaje neutral, sin afirmar falla)**:

```
4. Restricciones de capacidad observadas (D-096/D-097)
   Una "incapacidad" registra que una orden operativamente válida habría requerido más capacidad
   económica disponible de la que el modelo permitía en ese momento — no indica que la estrategia
   falló, que el resultado es inválido, ni que la corrida debe descartarse (D-097). El motor no
   bloquea ni modifica ninguna orden por esta razón (D-059/D-060, vigentes).

   {timeframe}
     Total de incapacidades: {N}
     {si N == 0: "Ninguna restricción de capacidad observada." — sin tabla}
     {si N > 0, resumen agrupado:}
       Por lado:  Buy={n}, Sell={n}
       Reserva requerida promedio: {F2}
       Reserva requerida máxima:   {F2}
```

**Resumen, no detalle exhaustivo por defecto**: con corridas de miles de operaciones (ej. dataset
1m), listar cada `RegistroIncapacidad` individualmente produciría un reporte impracticable —
mismo criterio que ya rigió `ReporteFinancieroGenerador` (no repite el detalle de cada `Trade`,
solo métricas agregadas). El detalle completo (cada registro con `Timestamp`/`Request`) queda
disponible en el anexo (sección 3), no en el resumen — mismo patrón que `ReporteEscenariosGenerador`
ya separa resumen de anexo en el pipeline existente.

**"Tipo de incapacidad"**: verificado en código que `RegistroIncapacidad` no tiene un campo de
"tipo" — solo `Side` (vía `Request.Side`), `ReservaRequerida`, `CashDisponible`. La agrupación
"por tipo" de esta especificación es "por `Side`" (Buy/Sell), el único eje de clasificación que el
dato ya soporta sin inferir nada nuevo — no se inventa una taxonomía de tipos no respaldada por el
registro existente.

---

## 3. Anexo — detalle completo (opcional, no obligatorio en el resumen)

Si se decide incluir detalle línea por línea (fuera del resumen), va en un anexo separado por
timeframe, mismo patrón que `REPORTE_FINANCIERO_V1_ANEXO_{Timeframe}.md` ya usa para
`ReporteEscenariosGenerador`. No se implementa en este ciclo salvo que el auditor lo solicite
explícitamente — el resumen agregado (sección 2) ya satisface D-096 (exposición) sin necesidad de
listar cada evento individual.

---

## 4. Compatibilidad

- **`src/` sin cambios**: `ValidadorCapacidad.cs`, `RegistroIncapacidad.cs`, `BacktestRunner.cs`,
  `ResultadoBacktest.cs` permanecen intactos — el campo `Incapacidades`/`IncapacidadesEfectivas`
  ya existe desde Caso 2, solo deja de descartarse en `exploration/`.
- **`ResultadoCorridaTimeframe`**: campo nuevo opcional con default `null` (o lista vacía, a
  definir en implementación) — mismo patrón D-061/D-072 que preserva compatibilidad con cualquier
  código existente que construya el record sin ese argumento.
- **3 baselines congelados**: no regeneran — ninguno usa sizing activo, y aunque `Incapacidades`
  puede tener entradas incluso sin sizing (una orden puede carecer de capacidad por razones no
  relacionadas con sizing), agregar la sección al reporte no cambia ningún hash de identidad
  (`HashCompuesto`/`HashConfiguracionEconomica` no derivan de `Incapacidades`).
- **Nota para un ciclo futuro, fuera de esta especificación**: `ReporteFinancieroGenerador.cs`
  §5 (línea 66-71) todavía cita "D-085, no resuelta en Caso 2 V1" — desactualizado desde el cierre
  de D-085 en Caso 4.3. No se corrige en este documento (pertenece a Caso 2, congelado; corregirlo
  requiere su propia decisión), se deja registrado para no perder el hallazgo.

---

## 5. Pruebas obligatorias antes de cerrar

- **P1 — Corrida sin incapacidades**: `Incapacidades` vacío o `null` → el reporte muestra "Ninguna
  restricción de capacidad observada", sin tabla ni cifras.
- **P2 — Corrida con incapacidades**: al menos 1 `RegistroIncapacidad` → el reporte muestra el
  total y el resumen agrupado por `Side`, con reserva promedio/máxima calculadas correctamente
  contra los valores reales de `ReservaRequerida`.
- **P3 — Determinismo**: 2 corridas idénticas producen el mismo conteo/resumen de incapacidades —
  mismo criterio de determinismo ya verificado para el resto del pipeline (`VerificarDeterminismo`,
  `EjecutorProtocolo.cs:163-179`, sin modificar esa función).
- **P4 — No regresión de baselines**: `HashCompuesto`/`HashConfiguracionEconomica` de los 3
  baselines congelados sin cambio; 126/126 (o el conteo vigente) tests de producción sin cambio.
- **P5 — Lenguaje neutral verificado**: el texto del reporte no contiene palabras que impliquen
  falla ("error", "inválido", "descartar") en la sección de incapacidades — verificable por
  inspección del texto generado, coherente con D-097.

---

## Fuera de alcance de este documento

No se implementa código. No se modifica `ValidadorCapacidad.cs`, `RegistroIncapacidad.cs`,
`BacktestRunner.cs`, `ResultadoBacktest.cs`. No se implementa el modo estricto (Opción B de D-096,
declarada como evolución posible, no parte de este ciclo). No se corrige la referencia
desactualizada a D-085 en `ReporteFinancieroGenerador.cs` §5.

---

## Próximo paso

Autorización de implementación bajo el alcance de este documento: campo nuevo en
`ResultadoCorridaTimeframe`, línea nueva en `EjecutorProtocolo.cs:160`, sección nueva en
`ReporteFinancieroGenerador.Generar`, P1-P5 como criterio de cierre.
