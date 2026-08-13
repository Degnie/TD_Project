# Especificación de Implementación — Auditoría Integral Post Caso 5C (D-127)

Estado: **especificación previa a ejecución**. Traduce D-127
(`DECISIONES_AUDITORIA_INTEGRAL_POST_CASO5C_V1.md`) a pasos concretos de verificación: qué comando
o inspección exacta corresponde a cada área A-H, en qué orden, con qué política de ejecución, qué
documento resulta, y qué hacer si aparece un hallazgo. **Ninguna verificación se ejecuta en este
documento. Ningún código se modifica.**

---

## 1. Mapeo de cada área A-H (verificación exacta, fuente exacta)

| Área | Verificación | Fuente exacta |
|---|---|---|
| **A. Motor base** | `dotnet build -c Release` (solución completa); `dotnet test -c Release` (suite de producción); inspección de `IdentidadExperimentoCompleta`/`HashCompuesto` ya cubierta por pruebas existentes, no recalculada aquí | `tests/Domain.Tests`, `tests/Application.Tests`, `tests/Infrastructure.Tests`, `tests/Presentation.Tests/TD_Project.Contracts.Tests`, `tests/Presentation.Tests/TD_Project.Api.Tests` |
| **B. Estrategias** | Las 6 estrategias congeladas se ejercitan dentro de la suite de producción (`Domain.Tests`) y ya fueron ejecutadas en las 67 comparaciones de Caso 5C — no se re-ejecutan aquí, se cita la evidencia ya verificada | `tests/Domain.Tests` (fixtures de estrategia), `exploration/laboratorio/caso5/resultados/` vía manifiesto (lectura, no re-ejecución) |
| **C. Motor financiero** | `dotnet run` sobre el ejecutable de pruebas del módulo, sin red ni escritura a `resultados/` | `exploration/laboratorio/modelo_financiero/TestsMetricasFinancieras.cs` (vía `ModeloFinanciero.csproj`) |
| **D. Gestores de riesgo** | `dotnet run` sobre `caso5/Program.cs` — genera únicamente la salida de pruebas P1-P10 de `TestsGestoresRiesgo.cs`; **la ejecución completa de `caso5/Program.cs` también corre Capa 1/5B, lo cual ya escribe a `resultados/` (ver §3, política de ejecución, y §5 sobre este caso específico)** | `exploration/laboratorio/caso5/TestsGestoresRiesgo.cs` (10/10 esperado) |
| **E. Comparador** | Mismo ejecutable que D — pruebas P1-P8 de `TestsComparadorGestores.cs`, corridas en la misma invocación de `caso5/Program.cs` | `exploration/laboratorio/caso5/TestsComparadorGestores.cs` (8/8 esperado) |
| **F. Persistencia de evidencia** | Mismo ejecutable — pruebas P1-P7 de `TestsPersistidorComparaciones.cs`; **más** reconciliación del manifiesto contra disco por conjunto (script de verificación ya usado 2 veces en Caso 5C, sin escritura) | `exploration/laboratorio/caso5/TestsPersistidorComparaciones.cs` (7/7 esperado), `caso5/MANIFIESTO_CORPUS_CASO5C_V1.json` vs `caso5/resultados/` (lectura) |
| **G. Capa analítica** | `dotnet run` sobre `analisis_corpus/` (11/11 esperado) y `analisis_interpretativo/` (8/8 esperado) — ambos leen el manifiesto existente, ninguno escribe a `resultados/` | `exploration/laboratorio/caso5/analisis_corpus/TestsAnalisisCorpus.cs`, `exploration/laboratorio/caso5/analisis_interpretativo/TestsAnalisisInterpretativo.cs` |
| **H. Datos** | Cálculo de SHA-256 de cada CSV congelado, comparado contra el `sha256` declarado en su `metadata.json` — lectura pura, sin red, sin regenerar nada | `datasets/reales/BTCUSDT/{13 timeframes}/metadata.json`, `datasets/reales/BTCUSDT_2022/{13 timeframes}/metadata.json`, `datasets/reales/ETHUSDT/{13 timeframes}/metadata.json` |

**Nota sobre D/E/F**: los 3 comparten el mismo ejecutable (`caso5/Program.cs`), que corre las 3
suites (Capa 5A/5B/5C-Capa1 = 10+8+7 = 25 pruebas) en una sola invocación — no hay forma de
ejecutar solo una sin las otras 2, mismo comportamiento ya documentado en Caso 5C. Ver §5 para
cómo se trata la escritura colateral a `resultados/` que esto produce.

---

## 2. Orden de ejecución

1. **Inspección estática**: `git status --porcelain -- src/ tests/ exploration/laboratorio/` antes
   de empezar — confirma que no hay cambios pendientes sin commitear que puedan confundir el
   resultado de la auditoría con trabajo en curso.
2. **`dotnet build -c Release`** sobre la solución completa (`src/`+`tests/`) — Área A, primer
   filtro (si no compila, la auditoría se detiene ahí para esa área).
3. **`dotnet test -c Release`** sobre la solución completa — Área A.
4. **Verificación H (datos)**: cálculo de SHA-256 sobre los 3 datasets, antes de tocar cualquier
   ejecutable — es pura lectura, no depende de que compile nada de `exploration/laboratorio/`.
5. **`dotnet run` sobre `modelo_financiero/`** — Área C.
6. **`dotnet run` sobre `caso5/Program.cs`** (única invocación) — Áreas D, E, F (pruebas). Genera
   escritura colateral a `resultados/` (ver §5).
7. **Verificación F (manifiesto vs disco)**: reconciliación por conjunto, inmediatamente después
   del paso 6, para detectar y clasificar cualquier carpeta nueva generada por él antes de seguir.
8. **`dotnet run` sobre `analisis_corpus/`** — Área G (parte 1).
9. **`dotnet run` sobre `analisis_interpretativo/`** — Área G (parte 2).
10. **Área B**: no requiere un paso de ejecución propio — se resuelve por referencia a la evidencia
    ya generada en los pasos 3 y 6 (las 6 estrategias ya se ejercitan ahí) y a la lectura del
    manifiesto (67 comparaciones, 6 estrategias, sin huecos, ya confirmado en Caso 5C).
11. **Consolidación**: con los resultados de 2-9, redactar `AUDITORIA_INTEGRAL_POST_CASO5C_V1.md`.

---

## 3. Política de ejecución (heredada de D-127, sin modificación)

**Orden de preferencia obligatorio**:
1. `dotnet test`, ejecutables con pruebas ya existentes, verificaciones estructurales,
   inspección/lectura de artefactos — ninguno escribe a `resultados/` **salvo el caso ya
   identificado en §5**.
2. Solo si resulta imprescindible una ejecución que sí escriba evidencia nueva: aislar el artefacto
   en `caso6/auditoria_integral/ejecuciones_tecnicas/`, nunca mezclado con `caso5/resultados/`,
   nunca incorporado al manifiesto ni a ningún análisis.

---

## 4. Salida esperada

**`caso6/AUDITORIA_INTEGRAL_POST_CASO5C_V1.md`**, con:

- Tabla por área (A-H): Funciona correctamente (Sí/No), Regresión (Sí/No), Evidencia válida
  (Sí/No), Problemas encontrados (lista), Requiere corrección (lista) — formato exacto ya fijado
  por D-127.
- Evidencia usada por área: comando/ruta exacta ejecutada, resultado numérico (N/N).
- Sección de hallazgos (si los hay), cada uno con el tratamiento de §5 de este documento.
- Estado final: apto / no apto para abrir `PROPUESTA_CASO6_RECOMENDADOR_V1.md`.

---

## 5. Gestión de hallazgos y del caso especial D/E/F

**Regla general (heredada de D-127, explícita aquí)**: si aparece un defecto durante cualquier
verificación,

```
Hallazgo
   |
   v
DETENER esa verificacion especifica (no corregir en el momento)
   |
   v
Registrar el hallazgo en AUDITORIA_INTEGRAL_POST_CASO5C_V1.md
   |
   v
Reportar al auditor antes de continuar con las demas areas
   |
   v
Decision de correccion aparte (si el auditor la autoriza),
con su propio registro — no como parte de esta auditoria
```

**No se corrige y continúa** — mismo criterio ya aplicado 2 veces en Caso 5C V2 (defectos de "3
periodos" e "instrumento único"), donde cada corrección fue reportada y autorizada explícitamente
antes de aplicarse, nunca aplicada silenciosamente dentro del flujo de otra tarea.

**Caso especial — escritura colateral de `caso5/Program.cs` (paso 6)**: esta única invocación
genera 1 carpeta en `caso5/resultados/` por cada comparación de Capa 1 ejercitada en sus pruebas
internas (mismo patrón ya documentado como "escritura de verificación", categoría
`escritura-interrumpida`/pruebas técnicas en el manifiesto). No es un hallazgo de la auditoría en
sí — es un efecto conocido del mecanismo de pruebas ya existente, no introducido por esta fase. Se
gestiona así:
- Inmediatamente después del paso 6, `git status --porcelain -- exploration/laboratorio/caso5/
  resultados/` confirma qué carpeta(s) nueva(s) aparecieron.
- Se registran en la sección de evidencia de `AUDITORIA_INTEGRAL_POST_CASO5C_V1.md` (nombre de
  carpeta, origen: "efecto colateral de `caso5/Program.cs`, Área D/E/F de auditoría integral").
- **No se incorporan al manifiesto** — quedan fuera del corpus oficial por defecto, sin necesidad
  de una decisión aparte, porque no son evidencia experimental deliberada (mismo criterio que ya
  excluyó las carpetas de prueba técnica y escritura interrumpida anteriores).
- Si el auditor prefiere evitar incluso esta escritura colateral, la alternativa es no ejecutar el
  paso 6 y dar por buenas las Áreas D/E/F únicamente por referencia a su última ejecución ya
  documentada en Caso 5C (25/25, sin cambios de código desde entonces) — a decidir por el auditor
  antes de ejecutar (ver §7).

---

## 6. Verificación previa de que nada cambió desde el último 25/25 documentado

Antes de decidir si el paso 6 se ejecuta o se da por bueno por referencia, se verifica sin
ejecutar nada:

```
git log --oneline -- exploration/laboratorio/caso5/TestsGestoresRiesgo.cs
                      exploration/laboratorio/caso5/TestsComparadorGestores.cs
                      exploration/laboratorio/caso5/TestsPersistidorComparaciones.cs
                      exploration/laboratorio/caso5/Program.cs
```

Si no hay commits posteriores al último 25/25 ya documentado (`AUDITORIA_CIERRE_CASO5C_V2.md`), el
auditor puede optar por no re-ejecutar y aceptar la evidencia ya existente como vigente para D/E/F
— evitando por completo la escritura colateral. Si hay commits posteriores, la re-ejecución del
paso 6 es necesaria para que la auditoría sea evidencia real y no una cita de memoria (D-057).

---

## 7. Fuera de alcance de esta especificación

No se ejecuta ninguna verificación. No se decide todavía si el paso 6 se ejecuta o se da por bueno
por referencia (queda para el momento de la ejecución, según el resultado de §6). No se diseña el
recomendador. No se modifica ningún código.

---

## Próximo paso

Autorización explícita del auditor para ejecutar los pasos 1-11 de §2, en el orden indicado,
aplicando la gestión de hallazgos de §5 ante cualquier defecto encontrado, y generando
`caso6/AUDITORIA_INTEGRAL_POST_CASO5C_V1.md` como resultado.
