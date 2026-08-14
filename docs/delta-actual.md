<delta_aprobado>
  <resumen> Evolución del motor de backtesting para permitir la ingestión y catálogo de paquetes de velas (1m), la ejecución de estrategias cargadas mediante DSL JSON declarativo, la evaluación comparativa automatizada entre gestores de capital pre-cargados para recomendar el de mejor balance retorno/riesgo (sin recomendar estrategias ni asesoría financiera), el desglose determinista del rendimiento según 3 regímenes de mercado (Alza, Baja, Horizontal) y la emisión de reportes explicativos claros para no expertos basados exclusivamente en simulación histórica. </resumen>
  <clasificacion> estructural </clasificacion>
  <ids_nuevos> RN-15 (Ingestión y Persistencia de Datasets), RN-16 (Estrategias DSL JSON declarativo), RN-17 (Modelos de Gestores de Capital), RN-18 (Recomendación Automatizada de Gestor de Capital por Balance Retorno/Riesgo), RN-19 (Clasificación Determinista de Regímenes de Mercado), CU-21 (Ingestión de Datasets), CU-22 (Ejecución DSL JSON), CU-23 (Recomendación de Gestores), CU-24 (Segmentación por Regímenes), RNF-16 (Explicabilidad e Interpretación Histórica de Reportes). </ids_nuevos>
  <ids_modificados> CU-02 (Precisión de redacción para validación de datasets en ingestión). </ids_modificados>
  <ids_retirados> ninguno </ids_retirados>
  <decision_adr> Opción A (Actualizar ADR-001 para incorporar el adaptador de repositorio local de datasets IDatasetRepository en Infrastructure y el orquestador multi-experimento CapitalManagerRecommender en Application). </decision_adr>
  <spec_version> 7.0 </spec_version>
</delta_aprobado>
