<hallazgos_auditoria>
  <contexto> Implementación inicial completada con arquitectura robusta en 3 capas. La suite de pruebas cubre ampliamente los contratos especificados (50/50 tests). El hallazgo sobre el cálculo de Equity queda validado como BUG obligatorio. La propuesta sobre Tasa de Margen se reclasifica como deuda técnica. </contexto>
  <bugs> 
    - [BUG] RN-08 (Fórmula Equity): `ResolutorVela.CalcularEquity` no está evaluando `UnrealizedPnL`, utilizando únicamente `Cash + Margin`. Se debe implementar la fórmula completa `Equity = Cash + Margin + UnrealizedPnL` incluso si los tests actuales no arrastran una posición viva al cierre de la vela, y añadir test específico para verificarlo sin romper los casos existentes.
  </bugs>
  <reglas_propuestas> 
  </reglas_propuestas>
  <descartados> 
    - [DESCARTADO] Regla Nueva RNF-08 estricta para Tasa de Margen: Descartada como regla de SPEC. Reclasificada como [MEJORA DE DISEÑO] (deuda técnica) para mejorar la auditabilidad inyectándola desde la configuración.
  </descartados>
</hallazgos_auditoria>
