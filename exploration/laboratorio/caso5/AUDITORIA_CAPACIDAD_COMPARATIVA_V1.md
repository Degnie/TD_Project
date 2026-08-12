# Auditoría — Capacidad Comparativa de Gestores de Riesgo

Estado: **documento de verificación de estado actual, no de evolución**. Responde una pregunta
factual sobre el sistema tal como quedó congelado en `caso5a-v1-experimental` — no propone diseño,
no abre decisiones D-N, no define Caso 5B. Mantiene la misma disciplina de todo el proyecto:
evidencia del estado actual → propuesta de evolución → decisiones → implementación. Este documento
cubre únicamente el primer paso.

Origen de la pregunta: antes de escribir `PROPUESTA_CASO5B_V1.md` (sistema recomendador de
gestores de riesgo), es necesario confirmar contra código si la infraestructura que un recomendador
necesitaría — comparación estructurada entre gestores — ya existe o no, en vez de asumir que
"reutiliza directamente la infraestructura recién creada" de Caso 5A.

---

## 1. Pregunta de auditoría

**¿Existe actualmente una capacidad nativa para comparar múltiples gestores de riesgo bajo una
misma estrategia, dataset y configuración experimental?**

**Resultado**: No existe actualmente.

---

## 2. Evidencia encontrada

### Ejecución individual únicamente

El sistema ejecuta una combinación a la vez:

```
Estrategia + Dataset + Gestor  →  ResultadoProtocolo
```

No existe ningún componente que reciba múltiples gestores y produzca un resultado conjunto:

```
Estrategia + Dataset + [Gestor A, Gestor B, Gestor C]  →  Resultado comparativo
```

Verificado: `EjecutorProtocolo.Ejecutar` (`exploration/laboratorio/protocolo/EjecutorProtocolo.cs`)
recibe una única `EntradaProtocolo` — que a su vez contiene un único `ConfiguracionSizing?`, por lo
tanto un único gestor activo — y devuelve un único `ResultadoProtocolo`. No existe sobrecarga ni
variante que acepte una colección de configuraciones.

### Reportes: una corrida por llamada

`ReporteFinancieroGenerador.Generar` (`exploration/laboratorio/modelo_financiero/
ReporteFinancieroGenerador.cs:14`) tiene la firma `Generar(ResultadoProtocolo resultado,
EntradaProtocolo entrada)` — un solo resultado, una sola configuración, por invocación. No existe:

- ninguna colección de resultados como entrada de ningún generador de reporte;
- ninguna tabla comparativa;
- ningún ranking;
- ninguna evaluación conjunta de N corridas.

Búsqueda exhaustiva en todo el repositorio de cualquier tipo `*Comparad*`, o de cualquier firma que
acepte `List<IGestorRiesgo>`, `List<MetricasFinancieras>`, `IEnumerable<ResultadoBacktest>` o
equivalente: **0 resultados**.

### Tests: P7 no es un comparador

`caso5/TestsGestoresRiesgo.cs`, método `VerificarComparacionDeControl` (P7): itera 3 `IGestorRiesgo`
en un array local, ejecuta `EjecutorProtocolo.Ejecutar` una vez por gestor, y verifica una única
invariante booleana — que la cantidad de señales emitidas por la estrategia no cambia entre
gestores (`senalesPorGestor.Distinct().Count() == 1`). No lee, no acumula, ni compara ninguna
`MetricasFinancieras` entre las 3 corridas. Es un método `private static void` dentro de una clase
de test — no es invocable ni reutilizable fuera de ese archivo. Confirma la ausencia de capacidad
comparativa, no la sustituye.

---

## 3. Capacidades existentes reutilizables

La ausencia de un comparador no es una carencia general del sistema — la base sobre la que
construirlo ya existe y está verificada:

- **Ejecución reproducible**: `EjecutorProtocolo.Ejecutar` produce resultados bit-a-bit idénticos
  entre corridas idénticas (verificado en Caso 5A P1, y en la Validación Integral V1).
- **Gestores intercambiables**: `IGestorRiesgo` (D-108) permite activar cualquiera de los 3
  gestores congelados sin tocar la estrategia ni el motor — el punto de variación ya está aislado.
- **Identidad experimental**: `IdentidadExperimentoCompleta`/`IIdentidadGestorRiesgo` (D-109)
  identifican de forma determinista y reproducible qué gestor, con qué parámetros, produjo cada
  resultado — precondición necesaria para poder atribuir una diferencia de métricas a un gestor
  específico.
- **Métricas financieras**: `MetricasFinancieras` (D-111) ya expone los campos necesarios para una
  comparación significativa (`ProfitFactor`, `CapitalLibreMinimo`, `DrawdownMaximoPct`,
  `ExposicionMaxima`, etc.), calculados desde una única fuente oficial (D-072/D-077).
- **Resultados serializables**: `ResultadoProtocolo`/`ResultadoBacktest` son estructuras de datos
  planas, sin estado mutable oculto — agregables en una colección sin efectos secundarios.
- **Configuración económica identificable**: `HashConfiguracionEconomica` distingue de forma
  determinista dos corridas que solo difieren en el gestor activo.

**Conclusión de esta sección**: la base existe: falta exclusivamente la capa de comparación —
ningún componente adicional de bajo nivel (ejecución, identidad, métricas) necesita construirse de
nuevo.

---

## 4. Brecha necesaria para Caso 5B (sin decidir diseño)

Registro de qué tendría que existir, no de cómo debe construirse — el diseño concreto es materia de
una propuesta y decisiones posteriores, no de esta auditoría.

**Acumulador de ejecuciones**: una estructura capaz de retener el resultado de N corridas
(estrategia + dataset + timeframe + gestor fijos salvo el eje que varía), algo conceptualmente
equivalente a:

```
ResultadoComparativo
{
    Estrategia,
    Dataset,
    Timeframe,
    Gestor,
    Metricas,
    IdentidadConfiguracion
}
```

**Comparador**: un componente capaz de responder "Gestor A vs. Gestor B" manteniendo constantes
estrategia, dataset, timeframe, capital inicial y costes — mismo criterio de control experimental
ya fijado en `DECISIONES_CASO5_V1.md` ("Criterio adicional de Caso 5A — control experimental"),
extendido de una comparación manual (2 corridas leídas por un humano) a una comparación asistida
por el propio sistema.

**Salida estructurada**: no basta con texto suelto por corrida (que es lo único que existe hoy vía
`ReporteFinancieroGenerador`). Se necesitaría al menos:
- una tabla que alinee las métricas de N gestores lado a lado;
- algún mecanismo de ranking o diferencia explícita entre gestores;
- criterios de comparación declarados (qué métrica se compara y por qué), no solo una lista de
  números.

---

## 5. Fuera de alcance de esta auditoría

Esta auditoría no decide:
- cómo se ordenan o rankean los gestores;
- cómo se genera una recomendación;
- qué métricas pesan más que otras en una comparación;
- si existirá algún componente de reglas o aprendizaje automático;
- ninguna forma concreta de Caso 5B — nombre, alcance, decisiones D-N, ni diseño de código.

Nada de lo anterior se resuelve aquí. Esta auditoría solo confirma un hecho verificable sobre el
estado actual del sistema.

---

## Estado final

```
Caso 5A               — gestores intercambiables               ✅ construido
Capacidad comparativa — comparación estructurada entre gestores ⚠️ inexistente
Caso 5B               — requiere primero construir la capa comparativa, no solo la recomendación
```

---

## Próximo documento

`PROPUESTA_CASO5B_V1.md`, con la pregunta reformulada a partir de esta evidencia: **¿cómo construir
una capa experimental de comparación de gestores de riesgo, reproducible, que permita
posteriormente recomendar configuraciones?** — no "¿cómo hacer un recomendador?", porque el
recomendador depende de una capa comparativa que hoy no existe.
