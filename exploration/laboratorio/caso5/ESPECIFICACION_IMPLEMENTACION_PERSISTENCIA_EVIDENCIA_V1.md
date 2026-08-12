# Especificación de Implementación — Persistencia de Evidencia Comparativa (Caso 5C, Capa 1)

Estado: **especificación previa a implementación — cubre exclusivamente Capa 1**. Traduce D-116 y
D-117 (`DECISIONES_CASO5C_V1.md`) a diseño de código concreto. **Ningún código se modifica en este
documento.** No cubre Capa 2 (análisis/recomendación, D-118 a D-120) — queda condicionada a que
esta capa produzca corpus real, tal como decidió el auditor.

---

## 1. Ubicación exacta del persistidor

**Archivo**: `exploration/laboratorio/caso5/PersistidorComparaciones.cs` — mismo módulo satélite
que ya aloja `ComparadorGestores.cs`/gestores/pruebas de Caso 5A/5B (`caso5/Caso5.csproj`), no una
carpeta nueva. Mismo criterio de ubicación ya usado para Caso 5B respecto a Caso 5A.

**Namespace**: `TD_Project.Caso5`.

**No modifica `ComparadorGestores.cs`**: `PersistidorComparaciones` recibe un
`ResultadoComparativoGestores` ya calculado — lo envuelve, no lo extiende. `ComparadorGestores.
Comparar` sigue siendo una función pura sin tocar disco (D-116).

**`Caso5.csproj`**: no requiere ningún `<Compile Include>` nuevo — `System.IO`/`System.Text` ya
están disponibles por `ImplicitUsings`.

---

## 2. Contrato de almacenamiento

```csharp
namespace TD_Project.Caso5;

// spec: Caso 5C D-116 (DECISIONES_CASO5C_V1.md) — envuelve ComparadorGestores, no lo modifica.
// Extension directa del patron ya verificado en protocolo/Program.cs:48-72 (carpeta timestamped +
// JSON de identidad + reporte). No falla la comparacion en memoria si la escritura a disco falla
// (D-059/D-096: fallo de capa secundaria no bloquea la capa principal).
public static class PersistidorComparaciones
{
    // Devuelve la ruta de la carpeta escrita, para que el llamador pueda reportarla (mismo patron
    // que protocolo/Program.cs:71, "Artefactos escritos en: {carpeta}").
    public static string Persistir(string dirResultados, ResultadoComparativoGestores resultado);
}
```

**Por qué no un método `void`**: el precedente (`protocolo/Program.cs`) imprime la ruta escrita
tras persistir — devolver la ruta permite que cualquier `Program.cs` que use este componente haga
lo mismo sin necesitar reconstruir la lógica del nombre de carpeta por su cuenta.

**Por qué no recibe `ConfiguracionSizing`/`EntradaProtocolo`**: `ResultadoComparativoGestores` ya
contiene todo lo necesario (`Estrategia`, `Timeframe`, `NombreDataset`, `Filas` con
`IdentidadGestor` por fila) — el persistidor no necesita conocer cómo se construyó la comparación,
solo su resultado (mismo principio de que `ReporteFinancieroGenerador` no conoce `BacktestRunner`).

---

## 3. Formato de identidad

**`IDENTIDAD_COMPARACION.json`** — mismo estilo de interpolación manual de string que
`protocolo/Program.cs:58-68` (sin `JsonSerializer`, consistente con el resto del proyecto, que no
usa una librería de serialización en ningún punto de `exploration/laboratorio/`):

```json
{
  "estrategia": "Tres Mosqueteros",
  "timeframe": "1D",
  "nombreDataset": "BTCUSDT_2024-01-02_2025-01-02",
  "gestores": [
    { "identidad": "fixed-fractional:v1:riesgo=0.1", "estado": "Success" },
    { "identidad": "fixed-risk:v1:monto=50", "estado": "Success" },
    { "identidad": "volatility-sizing:v1:ventana=20:base=0.1:desviacionReferencia=2", "estado": "Incomplete" }
  ],
  "fechaGeneracionUtc": "2026-08-12T14:32:00Z"
}
```

**Campos y su origen** (D-117, sin ningún dato derivado o inferido):
- `estrategia`/`timeframe`/`nombreDataset`: directos de `ResultadoComparativoGestores`.
- `gestores[].identidad`: `FilaComparacionGestor.IdentidadGestor` (ya calculado por
  `IIdentidadGestorRiesgo`, Caso 5A/D-109).
- `gestores[].estado`: `FilaComparacionGestor.Estado` — permite reconocer, sin abrir el reporte,
  qué filas tienen métricas válidas.
- `fechaGeneracionUtc`: único campo no presente en `ResultadoComparativoGestores`, necesario para
  la diversidad temporal que D-119 (futura) necesitará — capturado en el momento de persistir, no
  inventado ni derivado de ningún cálculo.

**No incluye `HashCompuesto`/`HashConfiguracionEconomica`** de cada corrida individual — D-114 ya
fijó que la fuente es `MetricasFinancieras`, no la identidad de cada corrida por separado; incluir
los hashes individuales aquí sería una ampliación no autorizada por D-117 (que no lista hashes como
insumo). Si una fase futura los necesita, requiere una decisión explícita, no una inclusión
oportunista en esta especificación.

**`COMPARACION_GESTORES_V1.md`**: exactamente el `string` que `RenderizadorComparacionGestores.
Generar` ya produce (Caso 5B, sin modificación) — escrito tal cual a disco.

---

## 4. Estructura de archivos

```
caso5/resultados/{Estrategia}_{Timeframe}_{timestamp}/
 ├── IDENTIDAD_COMPARACION.json
 └── COMPARACION_GESTORES_V1.md
```

Mismo patrón de nombre que `protocolo/Program.cs:50`
(`{Estrategia.Replace(" ", "")}_{DateTime.UtcNow:yyyyMMddTHHmmssZ}`), extendido con `{Timeframe}`
para distinguir comparaciones de la misma estrategia sobre timeframes distintos en el mismo
segundo — necesario porque `ComparadorGestores` opera sobre un único timeframe por invocación
(D-113), a diferencia de `EjecutorProtocolo` que ya agrupa todos los timeframes en una sola
carpeta.

**`caso5/resultados/` excluido de git** — mismo criterio que `protocolo/resultados/`
(`.gitignore:5`) y `validacion_integral/datasets_generados/`. Requiere agregar una línea al
`.gitignore` raíz o un `.gitignore` propio dentro de `caso5/resultados/` (mismo patrón que
`validacion_integral/.gitignore`) — decisión de implementación menor, cualquiera de las dos formas
cumple D-116.

---

## 5. Qué datos se persisten

- Identidad de la comparación (estrategia, timeframe, dataset).
- Identidad de cada gestor comparado y su estado de corrida.
- La tabla de texto ya generada por Caso 5B (`RenderizadorComparacionGestores.Generar`).
- Momento de generación (timestamp UTC).

---

## 6. Qué datos NO se persisten

- **Ningún valor numérico de métricas fuera de lo que ya está en `COMPARACION_GESTORES_V1.md`** —
  `IDENTIDAD_COMPARACION.json` no duplica `PnLTotal`/`DrawdownMaximoPct`/etc.; esos valores viven
  únicamente en el reporte de texto, evitando dos fuentes de verdad para el mismo número (D-072/
  D-077 extendido a esta capa).
- **`Fills`/`Trades`/`EquityCurve` de ninguna corrida individual** — fuera del alcance de
  `MetricasFinancieras` (D-114), y por tanto fuera del alcance de esta persistencia.
- **Ningún dato de régimen de mercado ni clasificación inferida** (D-117, exclusión explícita).
- **Ninguna interpretación, análisis, ranking, ni recomendación** — esta especificación cubre
  exclusivamente Capa 1; ningún archivo escrito por `PersistidorComparaciones` contiene una
  conclusión, solo evidencia cruda ya calculada por Caso 5B.

---

## 7. Reproducibilidad

- **`Persistir` no re-ejecuta ninguna corrida** — recibe el `ResultadoComparativoGestores` ya
  calculado, escribe exactamente lo que contiene. Dos llamadas con el mismo `ResultadoComparativoGestores`
  producen el mismo `IDENTIDAD_COMPARACION.json` salvo `fechaGeneracionUtc` (que refleja el momento
  real de persistencia, no del cálculo — mismo criterio que `protocolo/resultados/` ya acepta,
  donde el nombre de carpeta también varía por timestamp entre corridas idénticas).
- **No requiere ningún hash nuevo**: la identidad de cada gestor (`IIdentidadGestorRiesgo`) ya es
  determinista y estable (verificado en Caso 5A P10) — dos comparaciones idénticas producen el
  mismo array `gestores` en `IDENTIDAD_COMPARACION.json`, salvo el timestamp.
- **No modifica `HashCompuesto`/`HashConfiguracionEconomica` de ninguna corrida** — esta capa no
  toca `IdentidadExperimentoCompleta`, solo persiste lo que Caso 5B ya produjo.

---

## 8. Pruebas necesarias

Ubicación: `caso5/TestsPersistidorComparaciones.cs`, mismo patrón runner manual que
`TestsComparadorGestores.cs`, agregado a `caso5/Program.cs`.

1. **P1 — Estructura de carpeta correcta**: `Persistir` crea `caso5/resultados/
   {Estrategia}_{Timeframe}_{timestamp}/` con exactamente 2 archivos
   (`IDENTIDAD_COMPARACION.json`, `COMPARACION_GESTORES_V1.md`).
2. **P2 — Contenido de `IDENTIDAD_COMPARACION.json` coincide con el resultado en memoria**: cada
   campo (`estrategia`, `timeframe`, `nombreDataset`, `gestores[].identidad`,
   `gestores[].estado`) coincide exactamente con los valores del `ResultadoComparativoGestores`
   recibido — sin transformación ni pérdida de datos.
3. **P3 — Contenido de `COMPARACION_GESTORES_V1.md` idéntico al render de Caso 5B**: el archivo
   escrito es exactamente igual al `string` que `RenderizadorComparacionGestores.Generar` produce
   para el mismo resultado — confirma que no hay una segunda ruta de formateo divergente.
4. **P4 — Reproducibilidad de contenido salvo timestamp**: dos llamadas a `Persistir` con el mismo
   `ResultadoComparativoGestores` producen `IDENTIDAD_COMPARACION.json` idéntico salvo
   `fechaGeneracionUtc`.
5. **P5 — No persiste ninguna métrica numérica en el JSON**: verificación estructural — el JSON
   deserializado no contiene ninguna clave de métrica financiera (`pnlTotal`, `drawdownMaximoPct`,
   etc.), confirmando D-116/§6.
6. **P6 — Fallo de escritura no invalida el resultado en memoria**: con un `dirResultados` inválido
   (ej. ruta con caracteres no permitidos, o permisos denegados si es viable simular en el entorno
   de test), `Persistir` puede lanzar una excepción propia, pero el `ResultadoComparativoGestores`
   original permanece intacto y utilizable — confirma que la persistencia es una capa secundaria
   sin efecto retroactivo sobre el cálculo ya hecho.
7. **P7 — `ComparadorGestores.cs` sin cambios**: verificación de que la firma pública de
   `ComparadorGestores.Comparar`/`RenderizadorComparacionGestores.Generar` sigue siendo idéntica a
   la de Caso 5B (mismo criterio de no reabrir esa fase, D-116) — comparación de firma vía
   reflexión, mismo patrón que P6 de `TestsComparadorGestores.cs` ya usa para verificar ausencia de
   métodos no autorizados.

Suite de producción (`dotnet test -c Release`) debe permanecer en 126/126 — ningún archivo de
`src/`/`tests/` se modifica en esta fase. Suite de Caso 5 (`caso5/Caso5.csproj`) debe permanecer en
18/18 (Caso 5A + 5B) más las nuevas pruebas de esta especificación, todas en verde.

---

## 9. Fuera de alcance de esta especificación

No se implementó código. No se especifica Capa 2 (algoritmo de recomendación, ranking, selección,
análisis estadístico, umbrales de evidencia D-119) — queda condicionada a que esta implementación
produzca corpus real. No se decide si el `.gitignore` de `caso5/resultados/` es una línea nueva en
el `.gitignore` raíz o un archivo propio dentro de la carpeta (decisión de implementación menor,
§4).

---

## Próximo paso

Autorización explícita del auditor para implementar: `PersistidorComparaciones.Persistir`,
`.gitignore` para `caso5/resultados/`, y la suite de pruebas (§8, 7 pruebas) — todo en
`exploration/laboratorio/caso5/`, sin tocar `ComparadorGestores.cs` ni ningún archivo de
`src/`/`tests/`. Tras la implementación, Capa 2 queda pendiente de una propuesta futura, una vez
exista corpus real generado por esta capa.
