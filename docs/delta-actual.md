<delta_aprobado>
  <resumen> Reemplazo del dashboard estático (`wwwroot/{index.html,app.js,styles.css}`) por un flujo de usuario final de 4 pasos (dataset → estrategia → resultado → profundización), consumiendo los endpoints y campos ya expuestos por SPEC 7.0/caso14/caso15 (`POST /api/datasets`, `POST /api/strategies/dsl/run`, `POST /api/capital-managers/recommend`, `Explicacion`/`Incapacidades`/`Exposicion`/`ReporteRegimen`), priorizados en 3 niveles de información (resultado inmediato, análisis a demanda, detalle técnico). El endpoint demo `POST /api/backtest/run` y su código de soporte (`Demo/`) permanecen intactos en el backend, solo se retira su botón de la UI. Sesión de flujo mantenida en memoria del navegador, sin persistencia. Parseo de CSV en el cliente; validación de dataset (RN-15) permanece exclusivamente en el servidor. </resumen>
  <clasificacion> estructural </clasificacion>
  <ids_nuevos> ninguno — RNF-16 ya exige explicabilidad de reportes para no expertos; este delta construye la interfaz que hace eso visible, sin definir ninguna regla de negocio ni comportamiento de backend nuevo. </ids_nuevos>
  <ids_modificados> RNF-16 (clarificación: la explicabilidad ya exigida se hace efectiva en una interfaz real de usuario, no solo en el JSON de respuesta). </ids_modificados>
  <ids_retirados> ninguno </ids_retirados>
  <decision_adr> ninguno — no se introduce ningún framework, dependencia ni componente arquitectónico nuevo; se mantiene HTML/CSS/JavaScript puro (evaluado y descartado React/TypeScript por ausencia de decisión previa que lo autorice, ver caso16/ESPECIFICACION_IMPLEMENTACION_DASHBOARD_ANALISIS_HISTORICO_V1.md §0). </decision_adr>
  <spec_version> 7.0 </spec_version>
</delta_aprobado>
