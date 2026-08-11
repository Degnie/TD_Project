document.getElementById('btn-ejecutar').addEventListener('click', ejecutarBacktest);

async function ejecutarBacktest() {
  ocultarError();
  try {
    const respuesta = await fetch('/api/backtest/run', { method: 'POST' });
    if (!respuesta.ok) {
      throw new Error('La API respondió con estado ' + respuesta.status);
    }
    const resultDto = await respuesta.json();
    renderizar(resultDto);
  } catch (err) {
    mostrarError('No se pudo ejecutar el backtest: ' + err.message);
  }
}

function renderizar(dto) {
  document.getElementById('resultado').hidden = false;
  const badge = document.getElementById('estado-badge');
  badge.textContent = dto.estado;
  badge.dataset.estado = dto.estado;

  renderizarResumen(dto);
  renderizarCurvaEquity(dto.equityCurve);
  renderizarTrades(dto.trades);
  renderizarBranchResolutions(dto.branchResolutions);
  renderizarFillLog(dto.fillLog);
  renderizarPortfolioSnapshots(dto.portfolioSnapshots);
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

function mostrarError(texto) {
  const p = document.getElementById('mensaje-error');
  p.textContent = texto;
  p.hidden = false;
}

function ocultarError() {
  document.getElementById('mensaje-error').hidden = true;
}
