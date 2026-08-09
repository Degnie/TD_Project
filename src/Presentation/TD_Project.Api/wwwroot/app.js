document.getElementById('btn-ejecutar').addEventListener('click', ejecutarBacktest);

async function ejecutarBacktest() {
  ocultarError();
  try {
    const respuesta = await fetch('/api/backtest/run', { method: 'POST' });
    if (!respuesta.ok) {
      throw new Error('La API respondio con estado ' + respuesta.status);
    }
    const resultDto = await respuesta.json();
    renderizar(resultDto);
  } catch (err) {
    mostrarError('No se pudo ejecutar el backtest: ' + err.message);
  }
}

function renderizar(dto) {
  document.getElementById('resultado').hidden = false;
  document.getElementById('estado-badge').textContent = dto.estado;

  renderizarResumen(dto);
  renderizarCurvaEquity(dto.equityCurve);
  renderizarTrades(dto.trades);
  renderizarBranchResolutions(dto.branchResolutions);
}

function renderizarResumen(dto) {
  document.getElementById('resumen-estado').textContent = dto.estado;
  document.getElementById('resumen-equity-final').textContent = dto.metrics.equityFinal;
  document.getElementById('resumen-pnl-total').textContent = dto.metrics.pnLTotal;
  document.getElementById('resumen-total-trades').textContent = dto.metrics.totalTrades;
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
    const fila = document.createElement('tr');
    fila.innerHTML =
      '<td>' + b.timestamp + '</td>' +
      '<td>' + b.trayectoriaOficial + '</td>' +
      '<td>' + b.equityA + '</td>' +
      '<td>' + b.equityB + '</td>' +
      '<td>' + b.fillsA.length + '</td>' +
      '<td>' + b.fillsB.length + '</td>';
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
  polyline.setAttribute('stroke', '#2563eb');
  polyline.setAttribute('stroke-width', '2');
  svg.appendChild(polyline);
}

function mostrarError(texto) {
  const p = document.getElementById('mensaje-error');
  p.textContent = texto;
  p.hidden = false;
}

function ocultarError() {
  document.getElementById('mensaje-error').hidden = true;
}
