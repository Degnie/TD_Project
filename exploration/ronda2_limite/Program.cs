using TD_Project.Domain.Portfolio;
using TD_Project.Domain.Shared;

// Ronda 2 — Configuración al límite: llama AplicadorFill.Aplicar directamente en secuencia,
// sin pasar por BacktestRunner/Strategy, para forzar caminos que el fixture normal no dispara
// (multiples lotes cortos, cross-zero real, FIFO multi-lote en una sola reduccion).
const decimal TasaMargen = 0.1m;
var fallos = 0;

fallos += Escenario1_CortosMultiples();
fallos += Escenario2_CrossZeroReal();
fallos += Escenario3_FifoMultiLote_Long();
fallos += Escenario3b_FifoMultiLote_Short();
Escenario4_AcumulacionResiduoDecimal(); // informativo, no cuenta como fallo binario pass/fail

Console.WriteLine();
Console.WriteLine(fallos == 0 ? "=== TODOS LOS ESCENARIOS OK ===" : $"=== {fallos} ASSERT(S) FALLIDOS ===");
return fallos == 0 ? 0 : 1;

// ---------------------------------------------------------------------------------------------
// Escenario 1: muchas posiciones CORTAS abiertas y cerradas seguidas (secuencia: Sell, Buy,
// Sell, Buy...), cada ciclo abre corto y lo cierra por completo. Verifica signo de RealizedPnL
// y MarginLiberado en cada cierre corto, ahora que el bug de signo de Ronda 1 fue corregido en
// CalculadoraLotes (Margin = Math.Abs(cantidad) * precio * tasa, sin importar signo).
static int Escenario1_CortosMultiples()
{
    Console.WriteLine("--- Escenario 1: Cortos multiples consecutivos (Sell/Buy alternado) ---");
    var f = 0;
    var pf = new PortfolioState { Cash = 10_000m };
    long seq = 1;

    // Ciclo 1: Short 10 @ 100, cierra @ 90 -> precio bajo, corto gana. PnL esperado = 10*(100-90)=+100
    f += AbrirYCerrarCorto(pf, ref seq, cantidad: 10m, precioApertura: 100m, precioCierre: 90m, pnlEsperado: 100m);

    // Ciclo 2: Short 5 @ 90, cierra @ 95 -> precio sube, corto pierde. PnL esperado = 5*(90-95)=-25
    f += AbrirYCerrarCorto(pf, ref seq, cantidad: 5m, precioApertura: 90m, precioCierre: 95m, pnlEsperado: -25m);

    // Ciclo 3: Short 20 @ 95, cierra @ 95 (flat) -> PnL = 0
    f += AbrirYCerrarCorto(pf, ref seq, cantidad: 20m, precioApertura: 95m, precioCierre: 95m, pnlEsperado: 0m);

    // Ciclo 4: Short 7 @ 200, cierra @ 50 -> gran ganancia. PnL = 7*(200-50)=1050
    f += AbrirYCerrarCorto(pf, ref seq, cantidad: 7m, precioApertura: 200m, precioCierre: 50m, pnlEsperado: 1050m);

    // Al final, sin posiciones vivas, Margin debe ser exactamente 0 y Cash == 10000 + suma PnL.
    var pnlTotalEsperado = 100m - 25m + 0m + 1050m;
    Assert(pf.Margin, 0m, "Margin residual tras cerrar todos los cortos", ref f);
    Assert(pf.Cash, 10_000m + pnlTotalEsperado, "Cash final tras 4 ciclos cortos", ref f);
    Assert(pf.LotesVivos.Count, 0m, "LotesVivos vacios al final", ref f);

    Console.WriteLine(f == 0 ? "  OK" : $"  {f} FALLO(S)");
    return f;
}

static int AbrirYCerrarCorto(PortfolioState pf, ref long seq, decimal cantidad, decimal precioApertura, decimal precioCierre, decimal pnlEsperado)
{
    var fails = 0;
    var marginEsperadoApertura = cantidad * precioApertura * TasaMargen;
    var cashAntes = pf.Cash;

    var rApertura = AplicadorFill.Aplicar(pf, new Fill(seq++, Side.Sell, cantidad, precioApertura, 0m, seq, OrderType.Market), TasaMargen);
    Assert(pf.Margin, marginEsperadoApertura, $"Margin tras abrir Short {cantidad}@{precioApertura}", ref fails);
    Assert(pf.Cash, cashAntes - marginEsperadoApertura, $"Cash tras abrir Short {cantidad}@{precioApertura}", ref fails);
    Assert(pf.LotesVivos[^1].Cantidad, -cantidad, $"Lote.Cantidad negativa en Short {cantidad}@{precioApertura}", ref fails);
    Assert(pf.LotesVivos[^1].Margin, marginEsperadoApertura, $"Lote.Margin no-negativo en Short {cantidad}@{precioApertura}", ref fails);

    var cashAntesCierre = pf.Cash;
    var rCierre = AplicadorFill.Aplicar(pf, new Fill(seq++, Side.Buy, cantidad, precioCierre, 0m, seq, OrderType.Market), TasaMargen);
    Assert(rCierre.RealizedPnLReconocido, pnlEsperado, $"RealizedPnL cierre Short {cantidad}@{precioApertura}->{precioCierre}", ref fails);
    Assert(rCierre.MarginLiberado, marginEsperadoApertura, $"MarginLiberado cierre Short {cantidad}@{precioApertura}->{precioCierre}", ref fails);
    Assert(pf.Cash, cashAntesCierre + marginEsperadoApertura + pnlEsperado, $"Cash tras cerrar Short {cantidad}@{precioApertura}->{precioCierre}", ref fails);
    Assert(rCierre.TradeCerrado?.RealizedPnL ?? decimal.MinValue, pnlEsperado, $"Trade.RealizedPnL Short {cantidad}@{precioApertura}->{precioCierre}", ref fails);

    return fails;
}

// ---------------------------------------------------------------------------------------------
// Escenario 2: Cross-Zero real forzado. Secuencia de Fills en un solo PortfolioState, diseñada
// para que cada reversión sea un UNICO Fill que invierte el signo de la posición (no dos Fills
// separados de "cerrar todo" + "abrir nuevo"). Ruta: Buy 5 (abre Long 5) -> Sell 8 (Long 5 ->
// Short 3, cross-zero) -> Buy 8 (Short 3 -> Long 5, cross-zero) -> Sell 8 (Long 5 -> Short 3,
// cross-zero de nuevo). Verifica RN-10: el Trade cerrado usa la cantidad/precio de la posicion
// VIEJA completa, y el lote nuevo retiene margin nuevo basado en el excedente.
static int Escenario2_CrossZeroReal()
{
    Console.WriteLine("--- Escenario 2: Cross-Zero real (Fill unico invierte signo) ---");
    var f = 0;
    var pf = new PortfolioState { Cash = 10_000m };
    long seq = 1;

    // Paso 1: Buy 5 @ 100 -> abre Long 5. (mismo signo, abre lote, no cross-zero)
    AplicadorFill.Aplicar(pf, new Fill(seq++, Side.Buy, 5m, 100m, 0m, seq, OrderType.Market), TasaMargen);
    Assert(PosicionActual.De(pf), 5m, "Posicion tras Buy 5@100", ref f);

    // Paso 2: Sell 8 @ 110. |Fill|=8 > |Posicion|=5 -> Cross-Zero.
    // RN-10 esperado: Trade cerrado sobre Long 5 (apertura 100, cierre 110), PnL = 5*(110-100)=+50.
    // Posicion nueva = 8 - 5 = 3 (Short 3), Margin nuevo = 3*110*0.1 = 33.
    var cashAntesPaso2 = pf.Cash;
    var marginAntesPaso2 = pf.Margin; // 5*100*0.1 = 50
    var r2 = AplicadorFill.Aplicar(pf, new Fill(seq++, Side.Sell, 8m, 110m, 0m, seq, OrderType.Market), TasaMargen);
    Assert(marginAntesPaso2, 50m, "Margin previo Paso2 (Long 5@100)", ref f);
    Assert(r2.RealizedPnLReconocido, 50m, "RealizedPnL Cross-Zero Paso2 (Long5@100 cerrado @110)", ref f);
    Assert(r2.TradeCerrado?.CantidadInicial ?? -1m, 5m, "Trade.CantidadInicial Paso2", ref f);
    Assert(r2.TradeCerrado?.PrecioApertura ?? -1m, 100m, "Trade.PrecioApertura Paso2", ref f);
    Assert(r2.TradeCerrado?.PrecioCierre ?? -1m, 110m, "Trade.PrecioCierre Paso2", ref f);
    Assert(PosicionActual.De(pf), -3m, "Posicion tras Paso2 (Short 3)", ref f);
    Assert(pf.Margin, 33m, "Margin tras Paso2 (3*110*0.1=33, posicion nueva Short 3)", ref f);
    Assert(pf.Cash, cashAntesPaso2 + 50m /*margin liberado viejo*/ + 50m /*pnl*/ - 33m /*margin nuevo*/, "Cash tras Paso2", ref f);

    // Paso 3: Buy 8 @ 90. |Fill|=8 > |Posicion Short|=3 -> Cross-Zero de nuevo.
    // Posicion vieja: Short 3 @ 110 (precio entrada). Precio fill inversion = 90.
    // posicionEraLarga=false -> signo=-1. RealizedPnL = -1 * (-3) * (90-110) = -1*(-3)*(-20) = -60.
    //   (short pierde: compró a 90 para cubrir algo que vendió a 110... espera, short gana si compra
    //   mas barato que vendio. Vendio a 110, compra a 90 -> deberia ganar 3*(110-90)=60, no perder.)
    // Verificamos contra la formula real del codigo, no contra intuicion, y marcamos si diverge.
    var cashAntesPaso3 = pf.Cash;
    var marginAntesPaso3 = pf.Margin; // 33
    var r3 = AplicadorFill.Aplicar(pf, new Fill(seq++, Side.Buy, 8m, 90m, 0m, seq, OrderType.Market), TasaMargen);
    Assert(marginAntesPaso3, 33m, "Margin previo Paso3 (Short 3@110)", ref f);
    // Formula RN-08/RN-10: para Short, ganancia = cantidad * (PrecioEntrada - PrecioFill) = 3*(110-90) = 60.
    Assert(r3.RealizedPnLReconocido, 60m, "RealizedPnL Cross-Zero Paso3 (Short3@110 cubierto @90, deberia ganar 60)", ref f);
    Assert(r3.TradeCerrado?.CantidadInicial ?? -1m, 3m, "Trade.CantidadInicial Paso3", ref f);
    Assert(r3.TradeCerrado?.PrecioApertura ?? -1m, 110m, "Trade.PrecioApertura Paso3", ref f);
    Assert(r3.TradeCerrado?.PrecioCierre ?? -1m, 90m, "Trade.PrecioCierre Paso3", ref f);
    Assert(PosicionActual.De(pf), 5m, "Posicion tras Paso3 (Long 5, 8-3)", ref f);
    Assert(pf.Margin, 45m, "Margin tras Paso3 (5*90*0.1=45)", ref f);

    // Paso 4: Sell 8 @ 120. |Fill|=8 > |Posicion Long|=5 -> Cross-Zero otra vez.
    // Posicion vieja: Long 5 @ 90. PnL = 5*(120-90) = 150.
    var r4 = AplicadorFill.Aplicar(pf, new Fill(seq++, Side.Sell, 8m, 120m, 0m, seq, OrderType.Market), TasaMargen);
    Assert(r4.RealizedPnLReconocido, 150m, "RealizedPnL Cross-Zero Paso4 (Long5@90 cerrado @120)", ref f);
    Assert(PosicionActual.De(pf), -3m, "Posicion tras Paso4 (Short 3)", ref f);
    Assert(pf.Margin, 36m, "Margin tras Paso4 (3*120*0.1=36)", ref f);

    Console.WriteLine(f == 0 ? "  OK" : $"  {f} FALLO(S)");
    return f;
}

// ---------------------------------------------------------------------------------------------
// Escenario 3 (Long): abre 3 lotes seguidos del mismo signo (Buy 2@50, Buy 3@60, Buy 5@70), luego
// UNA reduccion parcial que cruza mas de un lote: Sell 4, que debe consumir Lote1(2 completo) +
// 2 del Lote2(3). Verifica orden FIFO y PnL agregado, y que el Lote2 quede con 1 unidad restante.
static int Escenario3_FifoMultiLote_Long()
{
    Console.WriteLine("--- Escenario 3: FIFO multi-lote, reduccion cruza 2 lotes (Long) ---");
    var f = 0;
    var pf = new PortfolioState { Cash = 10_000m };
    long seq = 1;

    AplicadorFill.Aplicar(pf, new Fill(seq++, Side.Buy, 2m, 50m, 0m, seq, OrderType.Market), TasaMargen); // Lote1: 2@50, margin=10
    AplicadorFill.Aplicar(pf, new Fill(seq++, Side.Buy, 3m, 60m, 0m, seq, OrderType.Market), TasaMargen); // Lote2: 3@60, margin=18
    AplicadorFill.Aplicar(pf, new Fill(seq++, Side.Buy, 5m, 70m, 0m, seq, OrderType.Market), TasaMargen); // Lote3: 5@70, margin=35

    Assert(pf.LotesVivos.Count, 3m, "3 lotes vivos tras 3 aperturas Long", ref f);
    Assert(pf.Margin, 10m + 18m + 35m, "Margin acumulado tras 3 aperturas Long", ref f);
    Assert(PosicionActual.De(pf), 10m, "Posicion total tras 3 aperturas Long (2+3+5)", ref f);

    // Sell 4 @ 80. Consume Lote1 completo (2@50) + 2 de Lote2 (2@60).
    // PnL = 2*(80-50) + 2*(80-60) = 60 + 40 = 100.
    // Margin liberado = 10 (lote1 completo) + 18*(2/3) = 10+12 = 22.
    var cashAntes = pf.Cash;
    var marginAntes = pf.Margin;
    var r = AplicadorFill.Aplicar(pf, new Fill(seq++, Side.Sell, 4m, 80m, 0m, seq, OrderType.Market), TasaMargen);

    Assert(r.RealizedPnLReconocido, 100m, "PnL Sell4@80 cruzando Lote1(2@50)+parcial Lote2(2/3@60)", ref f);
    Assert(r.MarginLiberado, 22m, "MarginLiberado Sell4@80 (10 + 18*2/3=12)", ref f);
    Assert(pf.LotesVivos.Count, 2m, "Quedan 2 lotes vivos (resto Lote2 + Lote3 intacto)", ref f);

    // Verifica orden FIFO: el lote consumido primero debe ser Lote1, y el remanente de Lote2 debe
    // ser 1 unidad @60 con margin proporcional 18*(1/3)=6.
    var loteRemanente = pf.LotesVivos[0];
    Assert(loteRemanente.Cantidad, 1m, "Lote2 remanente = 1 unidad tras consumir 2/3 (orden FIFO respetado)", ref f);
    Assert(loteRemanente.PrecioEntrada, 60m, "Lote2 remanente conserva PrecioEntrada original", ref f);
    Assert(loteRemanente.Margin, 6m, "Lote2 remanente Margin proporcional = 18*(1/3)=6", ref f);

    var lote3 = pf.LotesVivos[1];
    Assert(lote3.Cantidad, 5m, "Lote3 intacto (no tocado por Sell4)", ref f);
    Assert(lote3.Margin, 35m, "Lote3 Margin intacto", ref f);

    Assert(pf.Margin, marginAntes - 22m, "Margin total tras reduccion parcial", ref f);
    Assert(pf.Cash, cashAntes + 22m + 100m, "Cash tras reduccion parcial (margin liberado + pnl)", ref f);
    AssertNull(r.TradeCerrado, "No hay TradeCerrado (posicion no llega a 0, sigue habiendo 6 vivas)", ref f);

    Console.WriteLine(f == 0 ? "  OK" : $"  {f} FALLO(S)");
    return f;
}

// ---------------------------------------------------------------------------------------------
// Escenario 3b (Short): mismo patron pero con posiciones CORTAS -- el angulo especifico de
// Ronda 2 dado que el bug de Ronda 1 afectaba signos en reducciones sobre Short.
// Abre Sell 2@50, Sell 3@60, Sell 5@70 (Short 10 total). Luego Buy 4@40 (precio baja, corto gana),
// que consume Lote1(2 completo)+2 de Lote2.
static int Escenario3b_FifoMultiLote_Short()
{
    Console.WriteLine("--- Escenario 3b: FIFO multi-lote, reduccion cruza 2 lotes (Short) ---");
    var f = 0;
    var pf = new PortfolioState { Cash = 10_000m };
    long seq = 1;

    AplicadorFill.Aplicar(pf, new Fill(seq++, Side.Sell, 2m, 50m, 0m, seq, OrderType.Market), TasaMargen); // Lote1: -2@50, margin=10
    AplicadorFill.Aplicar(pf, new Fill(seq++, Side.Sell, 3m, 60m, 0m, seq, OrderType.Market), TasaMargen); // Lote2: -3@60, margin=18
    AplicadorFill.Aplicar(pf, new Fill(seq++, Side.Sell, 5m, 70m, 0m, seq, OrderType.Market), TasaMargen); // Lote3: -5@70, margin=35

    Assert(PosicionActual.De(pf), -10m, "Posicion total tras 3 aperturas Short (-2-3-5)", ref f);
    Assert(pf.Margin, 63m, "Margin acumulado tras 3 aperturas Short (todo Math.Abs, positivo)", ref f);

    // Buy 4 @ 40 (cubre). Consume Lote1 completo (2@50) + 2 de Lote2 (2@60).
    // Short gana si compra mas barato de lo que vendio: PnL = 2*(50-40) + 2*(60-40) = 20+40 = 60.
    var cashAntes = pf.Cash;
    var marginAntes = pf.Margin;
    var r = AplicadorFill.Aplicar(pf, new Fill(seq++, Side.Buy, 4m, 40m, 0m, seq, OrderType.Market), TasaMargen);

    Assert(r.RealizedPnLReconocido, 60m, "PnL Buy4@40 cubriendo Short Lote1(2@50)+parcial Lote2(2/3@60)", ref f);
    Assert(r.MarginLiberado, 22m, "MarginLiberado Buy4@40 (10 + 18*2/3=12)", ref f);
    Assert(pf.LotesVivos.Count, 2m, "Quedan 2 lotes Short vivos", ref f);

    var loteRemanente = pf.LotesVivos[0];
    Assert(loteRemanente.Cantidad, -1m, "Lote2 Short remanente = -1 unidad (signo Short conservado)", ref f);
    Assert(loteRemanente.Margin, 6m, "Lote2 Short remanente Margin proporcional positivo = 6", ref f);

    Assert(pf.Margin, marginAntes - 22m, "Margin total tras reduccion parcial Short", ref f);
    Assert(pf.Cash, cashAntes + 22m + 60m, "Cash tras reduccion parcial Short (margin liberado + pnl positivo)", ref f);

    Console.WriteLine(f == 0 ? "  OK" : $"  {f} FALLO(S)");
    return f;
}

// ---------------------------------------------------------------------------------------------
// Escenario 4 (informativo): fuerza divisiones no exactas (1/3, 1/7) repetidas muchas veces en
// reducciones FIFO parciales encadenadas, para ver si el residuo de precision (evidenciado en
// Escenario 3/3b) se acumula lo suficiente como para escapar del redondeo final a 2 decimales
// (RNF-05). Abre 1 lote grande y lo reduce en 21 mordiscos de 1/3 y 1/7 de lo restante.
static void Escenario4_AcumulacionResiduoDecimal()
{
    Console.WriteLine("--- Escenario 4 (informativo): acumulacion de residuo decimal en Margin/Cash ---");
    var pf = new PortfolioState { Cash = 1_000_000m };
    long seq = 1;

    // Abre UN lote grande que se ira reduciendo con fracciones no exactas.
    AplicadorFill.Aplicar(pf, new Fill(seq++, Side.Buy, 21_000m, 123.456789m, 0m, seq, OrderType.Market), TasaMargen);
    // Reabre en paralelo mas lotes pequeños para que cada reduccion parcial cruce fracciones.
    for (var i = 0; i < 30; i++)
        AplicadorFill.Aplicar(pf, new Fill(seq++, Side.Buy, 7m, 100m + i, 0m, seq, OrderType.Market), TasaMargen);

    // 40 reducciones parciales sucesivas de cantidades que fuerzan division no exacta contra el
    // remanente de cada lote (7 dividido de a 3 en 3 dificilmente cae exacto).
    for (var i = 0; i < 40; i++)
    {
        var pos = PosicionActual.De(pf);
        if (pos < 3m) break;
        AplicadorFill.Aplicar(pf, new Fill(seq++, Side.Sell, 3m, 90m + i, 0m, seq, OrderType.Market), TasaMargen);
    }

    var marginInterno = pf.Margin;
    var marginRedondeado = RedondeoReporte.ADosDecimales(marginInterno);
    var residuo = marginInterno - marginRedondeado;
    Console.WriteLine($"  Margin interno (crudo)  = {marginInterno}");
    Console.WriteLine($"  Margin redondeado (rep) = {marginRedondeado}");
    Console.WriteLine($"  Residuo (interno-redondeado) = {residuo}");
    Console.WriteLine($"  Cash interno = {pf.Cash}");
    Console.WriteLine($"  Lotes vivos restantes = {pf.LotesVivos.Count}, Posicion = {PosicionActual.De(pf)}");
    foreach (var l in pf.LotesVivos.Take(5))
        Console.WriteLine($"    Lote: Cantidad={l.Cantidad} PrecioEntrada={l.PrecioEntrada} Margin={l.Margin}");
}

// ---------------------------------------------------------------------------------------------
static void Assert(decimal actual, decimal esperado, string etiqueta, ref int fails)
{
    if (actual != esperado)
    {
        Console.WriteLine($"  [FALLO] {etiqueta}: esperado={esperado} actual={actual}");
        fails++;
    }
}

static void AssertNull(Trade? actual, string etiqueta, ref int fails)
{
    if (actual is not null)
    {
        Console.WriteLine($"  [FALLO] {etiqueta}: esperado=null actual={actual}");
        fails++;
    }
}
