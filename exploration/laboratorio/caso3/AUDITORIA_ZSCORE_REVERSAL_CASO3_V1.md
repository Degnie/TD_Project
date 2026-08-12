# Auditoría de Cierre — Z-Score Reversal (Caso 3A, primera familia)

Estado: **documento de cierre de sub-fase — Caso 3A, primera de las 2 familias requeridas por
D-086**. Consolida evidencia verificada del ciclo especificación → decisión → implementación →
pruebas → auditoría para `EstrategiaZScoreReversion` antes de evaluar la segunda familia. Mismo
patrón que las auditorías de cierre de sub-fase de Caso 2 (`AUDITORIA_CASO_2_3_GESTION_CAPITAL_
V1.md`).

---

## 1. Alcance auditado

Documentos de origen: `PROPUESTA_CASO3_V1.md`, `DECISIONES_CASO3_V1.md` (D-086 a D-090),
`ESPECIFICACION_FAMILIA_ESTRATEGIA_CASO3_V1.md`, `ESPECIFICACION_IMPLEMENTACION_ZSCORE_
REVERSAL_V1.md`. Implementación: `exploration/EstrategiaZScoreReversion.cs`,
`exploration/laboratorio/protocolo/EjecutorProtocolo.cs` (extendido), `exploration/laboratorio/
caso3/` (`PresentadorResolucionIntentos.cs`, `TestsCaso3.cs`, `Caso3.csproj`, `Program.cs`).

---

## 2. Verificación de arquitectura

**Separación motor/laboratorio (D-015)**: confirmado por `git status --porcelain -- src/ tests/`
vacío en todo el ciclo de esta sub-fase — ningún archivo de `src/`/`tests/` fue creado ni
modificado. Toda la implementación vive en `exploration/`.

**Contrato `IStrategy` intacto**: `EstrategiaZScoreReversion : IStrategy` implementa únicamente
`Observar(DataSlice) → IReadOnlyList<OrderRequest>` — sin miembros adicionales expuestos por la
interfaz, verificado por lectura directa de `src/Domain/Strategy/IStrategy.cs` (sin cambios).

**Sin dependencia de capital ni motor financiero**: la estrategia no referencia
`Instrumento`/`ConfiguracionCostes`/`ConfiguracionSizing`/`PortfolioState` en ningún punto —
verificado por ausencia de esos `using`/tipos en `EstrategiaZScoreReversion.cs`.

---

## 3. Diseño estadístico

Parámetros congelados por convención externa (D-030): `Ventana=20`, `UmbralEntrada=2.0`,
`UmbralSalida=0.5` — no calibrados sobre el dataset, no ajustados tras ver resultados. Ventana
deslizante O(1) por vela (suma/suma de cuadrados incremental), sin posiciones simultáneas
(verificado por P3), neutralidad representada por `Array.Empty<OrderRequest>()` — mismo patrón que
las 4 estrategias existentes.

**Ningún parámetro fue ajustado durante la implementación** — la única corrección aplicada durante
el ciclo (sección 6) fue al arnés de prueba, no a `Ventana`/`UmbralEntrada`/`UmbralSalida` ni a la
lógica de la estrategia.

---

## 4. D-090 — Metadata de capacidades

`CaracteristicasEstrategia(bool UsaMartingala)` (`protocolo/EjecutorProtocolo.cs`) implementada
como record externo a `IStrategy`, consumido vía `EntradaProtocolo.Caracteristicas`/
`ResultadoProtocolo.Caracteristicas`, ambos opcionales con default `null` ("no declarado", un
tercer estado honesto — no se asume `true` ni `false` por defecto). Verificado por P8: agregar el
campo no altera `ResultadoProtocolo` de una corrida sin `Caracteristicas` declarado.

---

## 5. D-055/D-088 — Presentación "no aplica"

`PresentadorResolucionIntentos.Formatear` (`caso3/PresentadorResolucionIntentos.cs`) implementa la
capa de presentación sin tocar `AnalizadorOperacional.cs` — verificado por ausencia de diff en ese
archivo. Distingue 3 estados: `UsaMartingala=false` → `"no aplica"`; `UsaMartingala=true` → valores
reales formateados; `Caracteristicas=null` (no declarado) → valores reales sin asumir
aplicabilidad, verificado explícitamente por P7 (los 3 casos probados en la misma prueba).

**Fórmulas no modificadas**: `AnalizadorOperacional.cs:62-67` (`PctSeguro`) permanece exactamente
igual — confirmado, no solo declarado.

---

## 6. Pruebas P1-P8

Las 8 pruebas pasan (`caso3/TestsCaso3.cs`, ejecutado vía `Program.cs`). Cobertura: señal de
entrada/salida (P1/P2), ausencia de posición simultánea (P3), equivalencia matemática de la ventana
incremental contra cálculo directo (P4), rendimiento (P5), determinismo (P6), metadata y
presentación (P7), regresión de compatibilidad (P8).

**Hallazgo durante implementación — arnés de prueba, no la estrategia**: la primera versión de P5
usaba `velas.Take(n + 1).ToArray()` dentro de un bucle de 100,000 iteraciones — O(n) por ciclo,
O(n²) total, indistinguible en síntoma de un bug real de la estrategia (colgó sin producir salida
durante varios minutos). Diagnóstico aplicado: detener la ejecución en vez de esperar
indefinidamente, medir CPU/tiempo real del proceso (confirmó progreso genuino, no bloqueo),
localizar el punto exacto de costo cuadrático, corregir a acumulación incremental
(`List<Candle>.Add`, O(1) amortizado). Mismo patrón de disciplina que D-062/D-083/D-084: nunca
asumir la causa, verificar con evidencia antes de corregir.

**No se registra como decisión nueva** (a diferencia de D-062/D-083/D-084): el hallazgo no reveló
ninguna limitación del pipeline, del motor ni de un parámetro congelado — fue exclusivamente un
defecto del código de prueba escrito en este mismo ciclo, corregido antes de cerrar. No hay
consecuencia de diseño que documentar más allá de esta auditoría.

---

## 7. Regresiones verificadas

- **107/107** tests de producción (`src/`/`tests/`), sin cambios.
- **7/7** pruebas del pipeline de Caso 1 (`TestsEjecutorProtocolo.cs`), `HashCompuesto` de
  `baseline_final/` intacto: `A48CCC57DA1919F533F4D532FDC0F945705681DCDA813B385BBFE7F44F40998E`.
- `BaselineFinanciero.csproj` (Caso 2) compila sin error tras la extensión de
  `EjecutorProtocolo.cs`; evidencia de `baseline_financiero_final/` no regenerada ni alterada
  (`git status --porcelain` vacío sobre esa carpeta).

---

## 8. Decisiones activadas por esta implementación

**D-044**: no activada — esta sub-fase no estudia interacción estrategia/régimen.

**D-084**: no activada — `GestorCapital`/sizing no interviene en ningún punto de esta
implementación.

**D-055**: parcialmente resuelta según el alcance ya fijado en D-089 — la presentación de "no
aplica" está implementada y probada; el rediseño completo del catálogo de métricas sigue fuera de
alcance de Caso 3A.

---

## Fuera de alcance de este documento

No se selecciona la segunda familia requerida por D-086 — queda para el próximo documento. No se
reabre D-044 ni D-084.

---

## Criterio de cierre de esta sub-fase

- ✓ Arquitectura verificada: `src/`/`tests/` intactos, `IStrategy` sin extender, sin dependencia
  financiera.
- ✓ Diseño estadístico sin calibración posterior a los resultados.
- ✓ D-090 implementada, metadata externa al contrato operativo.
- ✓ D-055/D-088 presentación "no aplica" implementada sin tocar fórmulas ni generadores congelados.
- ✓ 8/8 pruebas Caso 3 + 107/107 producción + hash de baseline Caso 1 intacto.
- ✓ Hallazgo de arnés de prueba documentado con causa raíz y corrección, sin decisión nueva
  requerida.
- ⏳ Auditoría revisa este documento — pendiente de confirmación antes de evaluar la segunda
  familia (D-087: máxima distancia estructural respecto a las 4 estrategias ya existentes,
  incluyendo Z-Score).
