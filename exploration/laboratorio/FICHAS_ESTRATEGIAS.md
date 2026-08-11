# Fichas de estrategia — Fase 1.5 (Laboratorio Sintético)

Conocimiento acumulado sobre comportamiento observado en datasets sintéticos de `datasets/market/`.
No son conclusiones de ventaja estadística ni recomendaciones de trading — los datasets fueron
diseñados para estudiar comportamiento del motor y de la estrategia, no para demostrar
rentabilidad. Ver `Program.cs` (bloque Fase 1.5) para la corrida que generó estos datos.

## Tres Mosqueteros

**Fortalezas observadas**
- Escenarios con estructura direccional definida y patrones repetitivos: DobleTecho (+0.89%),
  VolatilidadTrasCalma (+1.96%).

**Debilidades observadas**
- Ruido sin sesgo (RuidoAleatorio: -0.31%): la señal de color de una sola vela no tiene ventaja
  cuando no hay estructura direccional que capturar.
- Volatilidad decreciente (-0.92%): al reducirse el rango intra-vela hacia el final, la señal de
  color pierde separación (velas casi doji), degradando la confiabilidad de la referencia.
- TendenciaBajista (-0.12%): el ruido local alrededor de la pendiente sigue generando velas en
  contra del sesgo global.

**Dependencia de martingala**: media (17.6%–46.9% de operaciones resueltas en M1/M2 según
escenario; mínimo en DobleTecho, máximo en VolatilidadTrasCalma y MercadoLateral).

## MHI Mayoría

**Fortalezas observadas**
- Expansión y cambios de volatilidad: VolatilidadExtrema (+2.35%), VolatilidadTrasCalma (+3.81%),
  VolatilidadDecreciente (+1.08%). La mayoría de 3 velas parece capturar mejor los tramos donde
  el rango cambia de régimen que la señal de una sola vela.

**Debilidades observadas**
- Lateralidad pura (MercadoLateral: -0.18%): sin tendencia neta, la mayoría de 3 velas tampoco
  anticipa reversión ni continuidad.

**Dependencia de martingala**: media (19.4%–48.5%; mínimo en DobleTecho, máximo en
MercadoLateral).

## Ambas estrategias

**SinMovimiento**: 0 operaciones en ambas. No es fallo — es la estrategia reconociendo
correctamente que está fuera de su dominio de señal (todas las velas son dojis). Comportamiento
deseable: no fuerza operaciones ni genera ruido artificial.

**Nota sobre winrate**: ambas estrategias ganan 85%-95% de sus operaciones por diseño de la
martingala (muchas ganancias chicas + pocas pérdidas grandes). Winrate por sí solo es engañoso
para comparar estrategias o escenarios — las métricas primarias para comparación futura deberían
ser: retorno neto, máxima pérdida por operación completa, capital máximo comprometido, y
frecuencia de agotamiento de martingala (ya expuestas en el reporte de Fase 1.5 vía
`PerfilEstrategia`).
