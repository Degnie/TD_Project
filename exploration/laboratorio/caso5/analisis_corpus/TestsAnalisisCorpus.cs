using System.Reflection;

namespace TD_Project.Caso5.AnalisisCorpus;

// spec: ESPECIFICACION_IMPLEMENTACION_CASO5C_CAPA2_V1.md §6 (D-123) — P1-P9. Mismo patron "Caso"
// ya usado en TestsCampanaCorpus.cs/TestsComparadorGestores.cs.
public static class TestsAnalisisCorpus
{
    public static (int Total, int Pasaron, IReadOnlyList<string> Detalles) EjecutarTodas(string dirResultadosReal, string rutaManifiestoReal)
    {
        var detalles = new List<string>();
        var pasaron = 0;
        var total = 0;

        void Caso(string nombre, Action verificacion)
        {
            total++;
            try
            {
                verificacion();
                pasaron++;
                detalles.Add($"[PASA] {nombre}");
            }
            catch (Exception ex)
            {
                detalles.Add($"[FALLA] {nombre}: {ex.Message}");
            }
        }

        Caso("P1 — LectorCorpus no pierde ni inventa filas", () => VerificarP1(CrearFixtureBasico()));
        Caso("P2 — Parseo de metricas coincide con el formato congelado del renderizador", VerificarP2);
        Caso("P3 — Metricas ausentes se leen como null, no como error ni como 0", VerificarP3);
        Caso("P4 — Cobertura no omite carpetas validas ni oculta las invalidas", VerificarP4);
        Caso("P4b — Evidencia incompleta cuenta como evidencia, no como ausencia", VerificarP4b);
        Caso("P5 — Ausencia estructural de ranking en los tipos de salida (reflexion)", VerificarP5);
        Caso("P6 — Ausencia estructural de ordenamiento por valor (orden de insercion)", VerificarP6);
        Caso("P7 — DetectarCasosAtipicos reproduce los hallazgos ya documentados sobre el corpus real", () => VerificarP7(dirResultadosReal, rutaManifiestoReal));
        Caso("P8 — ResumenCorpus.Limitaciones nunca vacio ni ausente (incluye corpus vacio)", VerificarP8);
        Caso("P8b — Un dataset sin ninguna fila con metrica no cuenta como periodo temporal", VerificarP8b);
        Caso("P9 — Ausencia estructural de llamadas a componentes de ejecucion (textual)", VerificarP9);

        return (total, pasaron, detalles);
    }

    // --- Fixtures ---

    // spec: LectorCorpus.Leer requiere MANIFIESTO_CORPUS_CASO5C_V1.json (no escanea el directorio
    // directamente) — cada fixture debe declarar sus carpetas ahi. Formato minimo compatible con
    // LeerNombresDelManifiesto (solo necesita el array "comparaciones"[].carpeta).
    private static void EscribirManifiestoFixture(string dirResultados, IEnumerable<string> nombresCarpetas)
    {
        var entradas = string.Join(",\n    ", nombresCarpetas.Select(n => $$"""{ "carpeta": "{{n}}" }"""));
        File.WriteAllText(Path.Combine(dirResultados, "MANIFIESTO_CORPUS_CASO5C_V1.json"), $$"""
            {
              "corpus": "fixture-test",
              "comparaciones": [
                {{entradas}}
              ],
              "excluidos": { "categorias": [] }
            }
            """);
    }

    private static string CrearFixtureBasico()
    {
        var dir = Path.Combine(Path.GetTempPath(), "AnalisisCorpusFixture_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);

        var carpeta = EscribirComparacionFixture(dir, "EstrategiaX", "1h", "BTCUSDT_2024-01-02_2025-01-02",
            new[]
            {
                ("gestorA:v1", "Success", (decimal?)10m, (decimal?)0.2m, (decimal?)1.5m, (decimal?)5m, (decimal?)110m, (decimal?)110m),
                ("gestorB:v1", "Success", (decimal?)-5m, (decimal?)0.4m, (decimal?)0.8m, (decimal?)8m, (decimal?)95m, (decimal?)95m),
            });
        EscribirManifiestoFixture(dir, new[] { carpeta });

        return dir;
    }

    private static string EscribirComparacionFixture(
        string dirResultados, string estrategia, string timeframe, string nombreDataset,
        (string Identidad, string Estado, decimal? PnL, decimal? Drawdown, decimal? ProfitFactor, decimal? Exposicion, decimal? CashFinal, decimal? EquityFinal)[] gestores)
    {
        var nombreCarpeta = $"{estrategia}_{timeframe}_{Guid.NewGuid():N}";
        var carpeta = Path.Combine(dirResultados, nombreCarpeta);
        Directory.CreateDirectory(carpeta);

        var gestoresJson = string.Join(",\n    ", gestores.Select(g => $$"""{ "identidad": "{{g.Identidad}}", "estado": "{{g.Estado}}" }"""));
        File.WriteAllText(Path.Combine(carpeta, "IDENTIDAD_COMPARACION.json"), $$"""
            {
              "estrategia": "{{estrategia}}",
              "timeframe": "{{timeframe}}",
              "nombreDataset": "{{nombreDataset}}",
              "gestores": [
                {{gestoresJson}}
              ],
              "fechaGeneracionUtc": "2026-01-01T00:00:00Z"
            }
            """);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"=== Comparación de gestores de riesgo — {estrategia} / {timeframe} / {nombreDataset} ===");
        sb.AppendLine();
        foreach (var g in gestores)
        {
            sb.AppendLine($"Gestor: {g.Identidad}");
            sb.AppendLine($"  Estado: {g.Estado}");
            if (g.PnL.HasValue)
            {
                sb.AppendLine($"  PnLTotal: {g.PnL}");
                sb.AppendLine($"  DrawdownMaximoPct: {(g.Drawdown is { } d ? d.ToString() : "null")}");
                sb.AppendLine($"  ProfitFactor: {(g.ProfitFactor is { } pf ? pf.ToString() : "null")}");
                sb.AppendLine($"  ExposicionMaxima: {g.Exposicion}");
                sb.AppendLine($"  CashFinal: {g.CashFinal}");
                sb.AppendLine($"  EquityFinal: {g.EquityFinal}");
            }
            else
            {
                sb.AppendLine("  Métricas: (no disponibles — corrida no exitosa)");
            }
            sb.AppendLine();
        }
        File.WriteAllText(Path.Combine(carpeta, "COMPARACION_GESTORES_V1.md"), sb.ToString());

        return nombreCarpeta;
    }

    // --- Verificaciones ---

    private static void VerificarP1(string dirFixture)
    {
        try
        {
            var (filas, ignoradas) = LectorCorpus.Leer(dirFixture, Path.Combine(dirFixture, "MANIFIESTO_CORPUS_CASO5C_V1.json"));
            if (filas.Count != 2)
                throw new Exception($"Se esperaban 2 filas (1 comparacion x 2 gestores), se obtuvieron {filas.Count}.");
            if (ignoradas.Count != 0)
                throw new Exception($"No deberia haber carpetas ignoradas en el fixture basico, hubo {ignoradas.Count}.");
            var a = filas.Single(f => f.IdentidadGestor == "gestorA:v1");
            if (a.PnLTotal != 10m || a.DrawdownMaximoPct != 0.2m || a.Estrategia != "EstrategiaX")
                throw new Exception("Valores de la fila gestorA no coinciden con el fixture.");
        }
        finally
        {
            Directory.Delete(dirFixture, recursive: true);
        }
    }

    // spec: §6 P2 — el formato reproducido linea por linea es exactamente el que
    // RenderizadorComparacionGestores.Generar produce (ComparadorGestores.cs:76-104, congelado,
    // Caso 5B D-115). No se referencia el tipo real (Domain/Application/EjecutorProtocolo) para
    // mantener analisis_corpus/ sin dependencia de ejecucion (D-123, verificado ademas por P9).
    private static void VerificarP2()
    {
        var dir = Path.Combine(Path.GetTempPath(), "AnalisisCorpusFixtureP2_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var carpeta = Path.Combine(dir, "carpeta1");
            Directory.CreateDirectory(carpeta);
            var md =
                "=== Comparación de gestores de riesgo — EstrategiaReal / 15m / DatasetReal ===\n\n" +
                "Gestor: gestorReal:v1\n" +
                "  Estado: Success\n" +
                "  PnLTotal: 12.5\n" +
                "  DrawdownMaximoPct: 0.33\n" +
                "  ProfitFactor: 1.2\n" +
                "  ExposicionMaxima: 40\n" +
                "  CashFinal: 1012.5\n" +
                "  EquityFinal: 1012.5\n\n";
            File.WriteAllText(Path.Combine(carpeta, "COMPARACION_GESTORES_V1.md"), md);
            File.WriteAllText(Path.Combine(carpeta, "IDENTIDAD_COMPARACION.json"),
                """
                { "estrategia": "EstrategiaReal", "timeframe": "15m", "nombreDataset": "DatasetReal", "gestores": [ { "identidad": "gestorReal:v1", "estado": "Success" } ], "fechaGeneracionUtc": "2026-01-01T00:00:00Z" }
                """);
            EscribirManifiestoFixture(dir, new[] { "carpeta1" });

            var (filas, _) = LectorCorpus.Leer(dir, Path.Combine(dir, "MANIFIESTO_CORPUS_CASO5C_V1.json"));
            if (filas.Count != 1)
                throw new Exception($"Se esperaba 1 fila, se obtuvieron {filas.Count}.");
            var f = filas[0];
            if (f.PnLTotal != 12.5m || f.DrawdownMaximoPct != 0.33m || f.ProfitFactor != 1.2m || f.ExposicionMaxima != 40m || f.CashFinal != 1012.5m || f.EquityFinal != 1012.5m)
                throw new Exception($"Metricas parseadas no coinciden con el formato congelado del renderizador: PnLTotal={f.PnLTotal}, Drawdown={f.DrawdownMaximoPct}, PF={f.ProfitFactor}, Exp={f.ExposicionMaxima}, Cash={f.CashFinal}, Equity={f.EquityFinal}.");
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    private static void VerificarP3()
    {
        var dir = Path.Combine(Path.GetTempPath(), "AnalisisCorpusFixtureP3_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var carpeta = EscribirComparacionFixture(dir, "EstrategiaFallida", "1D", "DatasetX",
                new[] { ("gestorC:v1", "Incomplete", (decimal?)null, (decimal?)null, (decimal?)null, (decimal?)null, (decimal?)null, (decimal?)null) });
            EscribirManifiestoFixture(dir, new[] { carpeta });

            var (filas, _) = LectorCorpus.Leer(dir, Path.Combine(dir, "MANIFIESTO_CORPUS_CASO5C_V1.json"));
            if (filas.Count != 1)
                throw new Exception($"Se esperaba 1 fila, se obtuvieron {filas.Count}.");
            var f = filas[0];
            if (f.PnLTotal is not null || f.DrawdownMaximoPct is not null || f.CashFinal is not null)
                throw new Exception("Las metricas de una corrida no exitosa deberian ser todas null.");
            if (f.Estado != "Incomplete")
                throw new Exception($"Estado deberia ser 'Incomplete', fue '{f.Estado}'.");
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    // spec: LectorCorpus solo lee lo declarado en el manifiesto — "carpeta invalida" ahora
    // significa "declarada en el manifiesto pero sin estructura de Capa 1 en disco" (ej. congelada
    // en el manifiesto historico pero borrada, o nunca escrita correctamente), no una carpeta
    // suelta ajena al manifiesto (esa ni siquiera se visita).
    private static void VerificarP4()
    {
        var dir = Path.Combine(Path.GetTempPath(), "AnalisisCorpusFixtureP4_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var carpetaValida = EscribirComparacionFixture(dir, "EstrategiaValida", "1h", "DatasetX",
                new[] { ("gestorA:v1", "Success", (decimal?)1m, (decimal?)0.1m, (decimal?)1m, (decimal?)1m, (decimal?)1m, (decimal?)1m) });
            EscribirManifiestoFixture(dir, new[] { carpetaValida, "carpetaDeclaradaPeroInexistente" });

            var (filas, ignoradas) = LectorCorpus.Leer(dir, Path.Combine(dir, "MANIFIESTO_CORPUS_CASO5C_V1.json"));
            if (filas.Count != 1)
                throw new Exception($"Se esperaba 1 fila valida, se obtuvieron {filas.Count}.");
            if (ignoradas.Count != 1 || !ignoradas[0].EndsWith("carpetaDeclaradaPeroInexistente"))
                throw new Exception("La carpeta declarada en el manifiesto pero ausente en disco deberia aparecer en CarpetasIgnoradas.");
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    private static void VerificarP4b()
    {
        var dir = Path.Combine(Path.GetTempPath(), "AnalisisCorpusFixtureP4b_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var carpeta = EscribirComparacionFixture(dir, "EstrategiaIncompleta", "1D", "DatasetX",
                new[] { ("gestorZ:v1", "Incomplete", (decimal?)null, (decimal?)null, (decimal?)null, (decimal?)null, (decimal?)null, (decimal?)null) });
            EscribirManifiestoFixture(dir, new[] { carpeta });

            var (filas, ignoradas) = LectorCorpus.Leer(dir, Path.Combine(dir, "MANIFIESTO_CORPUS_CASO5C_V1.json"));
            var cobertura = AnalisisDescriptivo.CalcularCobertura(filas, ignoradas);
            if (cobertura.TotalFilas != 1)
                throw new Exception("Una fila Incomplete con JSON estructural valido debe contar en TotalFilas.");
            if (!cobertura.ComparacionesPorEstrategia.ContainsKey("EstrategiaIncompleta"))
                throw new Exception("La estrategia con evidencia incompleta debe aparecer en ComparacionesPorEstrategia.");
            if (ignoradas.Count != 0)
                throw new Exception("Una fila Incomplete con estructura valida no debe aparecer en CarpetasIgnoradas.");
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    private static void VerificarP5()
    {
        var prohibidos = new[] { "mejor", "ganador", "ranking", "score", "recomend", "winner", "best", "leader", "top" };
        var tipos = new[]
        {
            typeof(CoberturaAnalizada), typeof(EstadisticaDescriptiva), typeof(DistribucionMetrica),
            typeof(ComparacionPeriodos), typeof(CasoAtipico), typeof(ResumenCorpus), typeof(FilaCorpus),
        };

        foreach (var tipo in tipos)
        {
            foreach (var prop in tipo.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var nombreLower = prop.Name.ToLowerInvariant();
                foreach (var p in prohibidos)
                {
                    if (nombreLower.Contains(p))
                        throw new Exception($"{tipo.Name}.{prop.Name} contiene el termino prohibido '{p}'.");
                }
            }
        }

        foreach (var metodo in typeof(AnalisisDescriptivo).GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            var nombreLower = metodo.Name.ToLowerInvariant();
            foreach (var p in prohibidos)
            {
                if (nombreLower.Contains(p) && p != "top")
                    throw new Exception($"AnalisisDescriptivo.{metodo.Name} contiene el termino prohibido '{p}'.");
            }
        }
    }

    private static void VerificarP6()
    {
        var filas = new List<FilaCorpus>
        {
            new("Est", "1h", "DatasetX", "gestorZ:v1", "Success", 999m, 0.9m, 1m, 1m, 1m, 1m, "c1"),
            new("Est", "1h", "DatasetX", "gestorA:v1", "Success", 1m, 0.1m, 1m, 1m, 1m, 1m, "c2"),
            new("Est", "1h", "DatasetX", "gestorM:v1", "Success", 500m, 0.5m, 1m, 1m, 1m, 1m, "c3"),
        };

        var distribucion = AnalisisDescriptivo.CalcularDistribucion(filas, "PnLTotal", "Gestor");
        var clavesEnOrden = distribucion.PorGrupo.Keys.ToList();
        var ordenEsperado = new[] { "gestorZ:v1", "gestorA:v1", "gestorM:v1" };
        if (!clavesEnOrden.SequenceEqual(ordenEsperado))
            throw new Exception($"El orden de las claves deberia ser el orden de insercion de los datos ({string.Join(",", ordenEsperado)}), fue ({string.Join(",", clavesEnOrden)}).");
    }

    private static void VerificarP7(string dirResultadosReal, string rutaManifiestoReal)
    {
        var (filas, _) = LectorCorpus.Leer(dirResultadosReal, rutaManifiestoReal);
        if (filas.Count == 0)
            throw new Exception($"No se encontraron filas en el corpus real ('{dirResultadosReal}') — no se puede verificar P7 sin evidencia persistida.");

        var casos = AnalisisDescriptivo.DetectarCasosAtipicos(filas);
        if (!casos.Any(c => c.Descripcion.Contains("ZScore Reversion", StringComparison.OrdinalIgnoreCase) || c.Descripcion.Contains("ZScoreReversion", StringComparison.OrdinalIgnoreCase)))
            throw new Exception("No se detecto el caso de 'sin actividad' ya documentado para ZScore Reversion en el corpus real.");
        if (!casos.Any(c => c.Descripcion.Contains("DrawdownMaximoPct=")))
            throw new Exception("No se detecto ningun caso de drawdown extremo (>=99%) ya documentado en el corpus real.");
    }

    private static void VerificarP8()
    {
        var vacio = AnalisisDescriptivo.Resumir(Array.Empty<FilaCorpus>(), Array.Empty<string>());
        if (string.IsNullOrWhiteSpace(vacio.Limitaciones))
            throw new Exception("Limitaciones no debe estar vacio incluso con corpus vacio.");
        if (!vacio.Limitaciones.Contains("0 filas"))
            throw new Exception($"Limitaciones deberia reflejar '0 filas' para corpus vacio, fue: {vacio.Limitaciones}");

        var filas = new List<FilaCorpus> { new("Est", "1h", "DatasetX", "gestorA:v1", "Success", 1m, 0.1m, 1m, 1m, 1m, 1m, "c1") };
        var conDatos = AnalisisDescriptivo.Resumir(filas, Array.Empty<string>());
        if (string.IsNullOrWhiteSpace(conDatos.Limitaciones) || !conDatos.Limitaciones.Contains("1 filas"))
            throw new Exception($"Limitaciones deberia reflejar '1 filas', fue: {conDatos.Limitaciones}");
    }

    // spec: hallazgo durante la redaccion de RESULTADO_ANALISIS_CORPUS_CASO5C_CAPA2_V1.md — un
    // dataset que solo aparece en filas sin metrica (evidencia parcial deliberada, ej. sub-campana
    // C con DatasetInexistente_ParaCorpusDeFallo) no debe contarse como "periodo temporal" en
    // Limitaciones — solo cuenta un dataset con al menos una fila con metrica numerica real.
    private static void VerificarP8b()
    {
        var filas = new List<FilaCorpus>
        {
            new("Est", "1D", "BTCUSDT_2024-01-02_2025-01-02", "gestorA:v1", "Success", 1m, 0.1m, 1m, 1m, 1m, 1m, "c1"),
            new("Est", "1D", "DatasetInexistente_ParaCorpusDeFallo", "gestorA:v1", "Incomplete", null, null, null, null, null, null, "c2"),
            new("Est", "1D", "DatasetInexistente_ParaCorpusDeFallo", "gestorB:v1", "Incomplete", null, null, null, null, null, null, "c2"),
        };
        var resumen = AnalisisDescriptivo.Resumir(filas, Array.Empty<string>());
        if (!resumen.Limitaciones.Contains("1 periodo(s)"))
            throw new Exception($"Limitaciones deberia reflejar '1 periodo(s)' (el dataset inexistente no cuenta), fue: {resumen.Limitaciones}");
        if (resumen.Cobertura.ComparacionesPorDataset.Count != 2)
            throw new Exception("La cobertura cruda (ComparacionesPorDataset) SI debe seguir mostrando ambos datasets — solo Limitaciones filtra por periodo real.");
    }

    private static void VerificarP9()
    {
        var rutaCarpeta = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var archivos = new[] { "LectorCorpus.cs", "AnalisisDescriptivo.cs" }
            .Select(nombre => Directory.GetFiles(rutaCarpeta, nombre, SearchOption.AllDirectories).FirstOrDefault())
            .Where(p => p is not null)
            .ToList();

        if (archivos.Count != 2)
            throw new Exception($"No se pudieron localizar LectorCorpus.cs/AnalisisDescriptivo.cs bajo '{rutaCarpeta}' para verificar P9.");

        var prohibidos = new[] { "ComparadorGestores.Comparar", "EjecutorProtocolo.Ejecutar", "CrearEstrategia", "IStrategy" };
        foreach (var archivo in archivos)
        {
            var codigo = File.ReadAllText(archivo!);
            foreach (var patron in prohibidos)
            {
                if (codigo.Contains(patron, StringComparison.Ordinal))
                    throw new Exception($"'{Path.GetFileName(archivo)}' contiene una referencia prohibida a '{patron}' — Capa 2 no debe ejecutar backtests.");
            }
        }
    }
}
