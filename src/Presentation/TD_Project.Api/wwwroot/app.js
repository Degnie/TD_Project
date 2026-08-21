const sesion = {
  datasetHash: null,
  estrategiaDslJson: null,
  capitalInicial: null,
  ultimoResultado: null,
  ultimaRecomendacion: null,
};

document.addEventListener('DOMContentLoaded', cargarCatalogoDatasets);
document.getElementById('select-dataset').addEventListener('change', seleccionarDatasetDelCatalogo);
document.getElementById('btn-subir-dataset').addEventListener('click', subirDataset);
document.getElementById('btn-ejecutar-estrategia').addEventListener('click', ejecutarEstrategia);
document.getElementById('btn-comparar-gestores').addEventListener('click', compararGestores);

async function cargarCatalogoDatasets() {
  try {
    const respuesta = await fetch('/api/datasets');
    if (!respuesta.ok) return;
    const catalogo = await respuesta.json();
    const select = document.getElementById('select-dataset');
    for (const entrada of catalogo) {
      const opcion = document.createElement('option');
      opcion.value = entrada.hash;
      opcion.textContent = entrada.nombre;
      select.appendChild(opcion);
    }
  } catch (err) {
    // Catálogo no disponible al cargar la página: el usuario aún puede subir un dataset nuevo.
  }
}

function seleccionarDatasetDelCatalogo(evento) {
  const hash = evento.target.value;
  if (!hash) return;
  const nombre = evento.target.options[evento.target.selectedIndex].textContent;
  confirmarDatasetSeleccionado(hash, nombre);
}

async function subirDataset() {
  ocultarError();
  const archivo = document.getElementById('input-archivo-csv').files[0];
  const nombre = document.getElementById('input-nombre-dataset').value.trim();
  if (!archivo || !nombre) {
    mostrarError('Selecciona un archivo CSV y escribe un nombre para el dataset.');
    return;
  }

  try {
    const texto = await archivo.text();
    const velas = parsearCsvAVelas(texto);
    const respuesta = await fetch('/api/datasets', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ nombre: nombre, velas: velas }),
    });
    if (!respuesta.ok) {
      const mensaje = await respuesta.text();
      mostrarError('El dataset no pudo guardarse: alguna vela no cumple el formato esperado.', mensaje);
      return;
    }
    const { datasetHash } = await respuesta.json();
    confirmarDatasetSeleccionado(datasetHash, nombre);
  } catch (err) {
    mostrarError('No se pudo leer o enviar el archivo: ' + err.message);
  }
}

// Parsear no es validar: esta funcion solo da forma a CandleDto[] a partir de texto CSV.
// La aceptacion real del dataset (timestamps ordenados, precios consistentes, RN-15) depende
// exclusivamente de ValidadorDataset en el servidor.
function parsearCsvAVelas(textoCsv) {
  const lineas = textoCsv.trim().split('\n').filter(function (l) { return l.trim().length > 0; });
  const primeraEsEncabezado = isNaN(Number(lineas[0].split(',')[0]));
  const filas = primeraEsEncabezado ? lineas.slice(1) : lineas;
  return filas.map(function (linea) {
    const columnas = linea.split(',').map(function (c) { return c.trim(); });
    return {
      timestamp: Number(columnas[0]),
      open: Number(columnas[1]),
      high: Number(columnas[2]),
      low: Number(columnas[3]),
      close: Number(columnas[4]),
      volume: Number(columnas[5]),
    };
  });
}

function confirmarDatasetSeleccionado(datasetHash, nombre) {
  sesion.datasetHash = datasetHash;
  const p = document.getElementById('dataset-seleccionado');
  p.textContent = 'Dataset seleccionado: ' + nombre;
  p.hidden = false;
  document.getElementById('paso-estrategia').hidden = false;
}

async function ejecutarEstrategia() {
  ocultarError();
  sesion.estrategiaDslJson = document.getElementById('textarea-estrategia-dsl').value.trim();
  sesion.capitalInicial = Number(document.getElementById('input-capital-inicial').value);

  if (!sesion.datasetHash) {
    mostrarError('Selecciona o sube un dataset antes de ejecutar.');
    return;
  }
  if (!sesion.estrategiaDslJson) {
    mostrarError('Ingresa una estrategia en formato DSL JSON.');
    return;
  }

  mostrarCargando('estado-carga-ejecucion', true);
  try {
    const respuesta = await fetch('/api/strategies/dsl/run', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        datasetHash: sesion.datasetHash,
        capitalInicial: sesion.capitalInicial,
        estrategiaDslJson: sesion.estrategiaDslJson,
      }),
    });
    if (!respuesta.ok) {
      const mensaje = await respuesta.text();
      mostrarError('La estrategia no pudo ejecutarse: revisa el formato del DSL o el dataset seleccionado.', mensaje);
      return;
    }
    const dto = await respuesta.json();
    sesion.ultimoResultado = dto;
    renderizarResultado(dto);
  } catch (err) {
    mostrarError('No se pudo ejecutar la estrategia: ' + err.message);
  } finally {
    mostrarCargando('estado-carga-ejecucion', false);
  }
}

async function compararGestores() {
  ocultarError();
  mostrarCargando('estado-carga-gestores', true);
  try {
    const respuesta = await fetch('/api/capital-managers/recommend', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        datasetHash: sesion.datasetHash,
        capitalInicial: sesion.capitalInicial,
        estrategiaDslJson: sesion.estrategiaDslJson,
      }),
    });
    if (!respuesta.ok) {
      const mensaje = await respuesta.text();
      mostrarError('No se pudo comparar los gestores de capital.', mensaje);
      return;
    }
    const dto = await respuesta.json();
    sesion.ultimaRecomendacion = dto;
    renderizarComparacionGestores(dto);
  } catch (err) {
    mostrarError('No se pudo comparar los gestores: ' + err.message);
  } finally {
    mostrarCargando('estado-carga-gestores', false);
  }
}

function renderizarResultado(dto) {
  document.getElementById('paso-resultado').hidden = false;

  const badge = document.getElementById('estado-badge');
  badge.textContent = dto.estado;
  badge.dataset.estado = dto.estado;

  renderizarNivel1(dto);
  renderizarFasesRegimen(dto.reporteRegimen);
  renderizarExposicionFinal(dto.exposicion);
  renderizarIncapacidades(dto.incapacidades);
  document.getElementById('resultado-gestores').hidden = true;

  renderizarCurvaEquity(dto.equityCurve);
  renderizarTrades(dto.trades);
  renderizarBranchResolutions(dto.branchResolutions);
  renderizarFillLog(dto.fillLog);
  renderizarPortfolioSnapshots(dto.portfolioSnapshots);
}

function renderizarNivel1(dto) {
  const explicacion = dto.explicacion;
  document.getElementById('resumen-explicacion').textContent = explicacion ? explicacion.resumen : '';
  document.getElementById('resumen-equity-final').textContent = dto.metrics.equityFinal;
  document.getElementById('resumen-pnl-total').textContent = dto.metrics.pnLTotal;
  document.getElementById('resumen-total-trades').textContent = dto.metrics.totalTrades;
  document.getElementById('aviso-simulacion-historica').textContent = explicacion ? explicacion.avisoSimulacionHistorica : '';

  mostrarAdvertenciaSiExiste('advertencia-posiciones-abiertas', explicacion && explicacion.advertenciaPosicionesAbiertas);
  mostrarAdvertenciaSiExiste('advertencia-incapacidad-capital', explicacion && explicacion.advertenciaIncapacidadCapital);
}

function mostrarAdvertenciaSiExiste(idElemento, texto) {
  const p = document.getElementById(idElemento);
  if (texto) {
    p.textContent = texto;
    p.hidden = false;
  } else {
    p.hidden = true;
  }
}

function renderizarFasesRegimen(reporteRegimen) {
  const contenedor = document.getElementById('fases-regimen');
  if (!reporteRegimen) {
    contenedor.hidden = true;
    return;
  }
  contenedor.hidden = false;

  const regimenOptimo = document.getElementById('regimen-optimo');
  regimenOptimo.textContent = reporteRegimen.regimenOptimo
    ? '— mejor desempeño en fase ' + reporteRegimen.regimenOptimo
    : '';

  const cuerpo = document.getElementById('fases-regimen-body');
  cuerpo.innerHTML = '';
  for (const fase of reporteRegimen.fases) {
    const fila = document.createElement('tr');
    fila.innerHTML =
      '<td>' + fase.regimen + '</td>' +
      '<td>' + fase.totalTrades + '</td>' +
      '<td>' + fase.pnLTotal + '</td>' +
      '<td>' + fase.winRate + '</td>';
    cuerpo.appendChild(fila);
  }
}

function renderizarExposicionFinal(exposicion) {
  const contenedor = document.getElementById('exposicion-final');
  if (!exposicion) {
    contenedor.hidden = true;
    return;
  }
  contenedor.hidden = false;

  document.getElementById('exposicion-pnl-realizado').textContent = exposicion.pnLRealizado;
  document.getElementById('exposicion-resultado-abierto').textContent = exposicion.resultadoConPosicionesAbiertas;
  document.getElementById('exposicion-cantidad-neta').textContent = exposicion.cantidadNetaViva;
}

function renderizarIncapacidades(incapacidades) {
  const contenedor = document.getElementById('incapacidades');
  if (!incapacidades || incapacidades.length === 0) {
    contenedor.hidden = true;
    return;
  }
  contenedor.hidden = false;

  const cuerpo = document.getElementById('incapacidades-body');
  cuerpo.innerHTML = '';
  for (const i of incapacidades) {
    const fila = document.createElement('tr');
    fila.innerHTML =
      '<td>' + i.timestamp + '</td>' +
      '<td>' + i.side + '</td>' +
      '<td>' + i.cantidad + '</td>' +
      '<td>' + (i.bloqueada ? 'Sí' : 'No') + '</td>';
    cuerpo.appendChild(fila);
  }
}

function renderizarComparacionGestores(dto) {
  document.getElementById('resultado-gestores').hidden = false;
  document.getElementById('gestor-recomendado').textContent = dto.gestorRecomendado
    ? 'Gestor de capital recomendado: ' + dto.gestorRecomendado
    : 'Ningún gestor de capital evaluado evitó la liquidación de la cuenta para esta estrategia.';

  const cuerpo = document.getElementById('gestores-body');
  cuerpo.innerHTML = '';
  for (const r of dto.resultados) {
    const fila = document.createElement('tr');
    fila.innerHTML =
      '<td>' + r.identidadGestor + '</td>' +
      '<td>' + r.pnLTotal + '</td>' +
      '<td>' + r.maxDrawdown + '</td>' +
      '<td>' + r.cr + '</td>' +
      '<td>' + (r.cuentaLiquidada ? 'Sí' : 'No') + '</td>';
    cuerpo.appendChild(fila);
  }
}

function renderizarTrades(trades) {
  const cuerpo = document.getElementById('trades-body');
  cuerpo.innerHTML = '';
  for (const t of trades) {
    const fila = document.createElement('tr');
    fila.innerHTML =
      '<td>' + t.cantidadInicial + '</td>' +
      '<td>' + t.precioApertura + '</td>' +
      '<td>' + (t.precioCierre ?? '') + '</td>' +
      '<td>' + t.realizedPnL + '</td>';
    cuerpo.appendChild(fila);
  }
}

function renderizarBranchResolutions(branchResolutions) {
  const cuerpo = document.getElementById('branch-body');
  cuerpo.innerHTML = '';
  for (const b of branchResolutions) {
    const esOficialA = b.trayectoriaOficial === 'A';
    const fila = document.createElement('tr');
    fila.innerHTML =
      '<td>' + b.timestamp + '</td>' +
      '<td data-trayectoria="oficial">' + b.trayectoriaOficial + '</td>' +
      '<td' + (esOficialA ? '' : ' data-descartada="true"') + '>' + b.equityA + '</td>' +
      '<td' + (esOficialA ? ' data-descartada="true"' : '') + '>' + b.equityB + '</td>' +
      '<td' + (esOficialA ? '' : ' data-descartada="true"') + '>' + b.fillsA.length + '</td>' +
      '<td' + (esOficialA ? ' data-descartada="true"' : '') + '>' + b.fillsB.length + '</td>';
    cuerpo.appendChild(fila);
  }
}

function renderizarFillLog(fillLog) {
  const cuerpo = document.getElementById('fill-log-body');
  cuerpo.innerHTML = '';
  for (const f of fillLog) {
    const fila = document.createElement('tr');
    fila.innerHTML =
      '<td>' + f.secuenciaCausal + '</td>' +
      '<td>' + f.side + '</td>' +
      '<td>' + f.cantidad + '</td>' +
      '<td>' + f.precioFill + '</td>' +
      '<td>' + f.costoFriccionReal + '</td>' +
      '<td>' + f.velaTimestamp + '</td>' +
      '<td>' + f.tipoOrdenOriginal + '</td>';
    cuerpo.appendChild(fila);
  }
}

function renderizarPortfolioSnapshots(portfolioSnapshots) {
  const cuerpo = document.getElementById('portfolio-snapshots-body');
  cuerpo.innerHTML = '';
  for (const s of portfolioSnapshots) {
    const lotes = s.lotesVivos.length === 0
      ? '—'
      : s.lotesVivos.map(function (l) { return l.cantidad + ' @ ' + l.precioEntrada; }).join(', ');
    const fila = document.createElement('tr');
    fila.innerHTML =
      '<td>' + s.timestamp + '</td>' +
      '<td>' + s.cash + '</td>' +
      '<td>' + s.margin + '</td>' +
      '<td>' + lotes + '</td>';
    cuerpo.appendChild(fila);
  }
}

function renderizarCurvaEquity(equityCurve) {
  const svg = document.getElementById('svg-equity');
  const ns = 'http://www.w3.org/2000/svg';
  while (svg.firstChild) svg.removeChild(svg.firstChild);

  if (equityCurve.length === 0) return;

  const ancho = 600, alto = 200, margen = 20;
  const equities = equityCurve.map(function (p) { return p.equity; });
  const minEquity = Math.min.apply(null, equities);
  const maxEquity = Math.max.apply(null, equities);
  const rango = maxEquity - minEquity || 1;

  const puntos = equityCurve.map(function (p, i) {
    const x = margen + (i / Math.max(equityCurve.length - 1, 1)) * (ancho - 2 * margen);
    const y = alto - margen - ((p.equity - minEquity) / rango) * (alto - 2 * margen);
    return x + ',' + y;
  }).join(' ');

  const polyline = document.createElementNS(ns, 'polyline');
  polyline.setAttribute('points', puntos);
  polyline.setAttribute('fill', 'none');
  polyline.setAttribute('stroke', '#E8A33D');
  polyline.setAttribute('stroke-width', '2');
  svg.appendChild(polyline);
}

function mostrarCargando(idElemento, visible) {
  document.getElementById(idElemento).hidden = !visible;
}

function mostrarError(texto, detalleTecnico) {
  const p = document.getElementById('mensaje-error');
  p.textContent = texto;
  p.hidden = false;

  const detalle = document.getElementById('detalle-error-tecnico');
  if (detalleTecnico) {
    document.getElementById('mensaje-error-tecnico').textContent = detalleTecnico;
    detalle.hidden = false;
  } else {
    detalle.hidden = true;
  }
}

function ocultarError() {
  document.getElementById('mensaje-error').hidden = true;
  document.getElementById('detalle-error-tecnico').hidden = true;
}
