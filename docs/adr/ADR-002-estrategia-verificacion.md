# ADR-002 · Estrategia de verificación

**Estado:** Aceptado
**Fecha:** 2026-08-08

## Contexto

Con el stack y la arquitectura de ADR-001 decididos, se define qué
metodologías de verificación activa el SPEC y el contrato del comando único
`verify`, que todo cambio debe satisfacer.

## Decisión

### Metodologías condicionales

| Metodología | Activación | ID que lo justifica |
|---|---|---|
| Regla de arquitectura automatizada | Activa | RNF-12, vía la frontera declarada en ADR-001 (`Domain` no depende de `Infrastructure`/`Application`) |
| Pruebas por propiedades | Activa | RN-04, RN-11, CU-05, RNF-06 |
| Pruebas metamórficas | No activa como metodología principal | Las relaciones exigidas por el SPEC ya quedan cubiertas por determinismo (RNF-06) y pruebas por propiedades (CU-05). No se descarta por principio: queda abierta si aparecen relaciones metamórficas útiles no cubiertas por lo anterior. |
| Tipos que impiden estados inválidos | Activa, acotada | RN-01 (transiciones terminales de Order), RN-06 (máquina de estados Stop-Limit) |
| Precisión financiera decimal | Activa, como requisito separado | RNF-05 — `System.Decimal` es la elección de tipo numérico, no una instancia de "tipos que impiden estados inválidos" |
| Contratos en tiempo de ejecución | No activa como metodología general | Los tipos cubren estados inválidos cuando corresponde (RN-01, RN-06) y las pruebas por propiedades cubren invariantes sobre secuencias de ejecución (RN-04, RN-11, RNF-06). Los contratos runtime no se adoptan como metodología transversal; validaciones puntuales pueden añadirse en la implementación donde corresponda, sin constituir una metodología declarada aquí. |
| Salida congelada (golden master) | Activa, acotada a regresión funcional | RNF-08 (Fill Log + Estado Canónico Inicial permiten reconstrucción trazable). No incluye RNF-01/02/03, que se verifican como benchmark de rendimiento separado. |
| Migraciones como código | No activa | Ningún ID habla de esquema evolutivo de persistencia; RNF-13 exige solo simetría serialización/deserialización |
| Modelado de amenazas ligero | No activa | El dominio no incluye datos personales |

### Contrato de `verify`

El contrato declara objetivos verificables; los mecanismos concretos son
decisión de implementación y pueden evolucionar sin reabrir esta fase.

1. **Compilación sin errores** — el proyecto compila en modo estricto.
2. **Suite de tests en verde** — toda la suite pasa.
3. **Trazabilidad spec↔test** — todo test cita un ID existente de `SPEC.md`;
   toda RN, CU, EC y RNF verificable activa (RNF-05, RNF-06, RNF-08, RNF-09,
   RNF-10, RNF-13) tiene al menos un test que la cita. RNF-01/02/03/04 no
   tienen obligación de test con cita mientras sus objetivos cuantitativos
   permanezcan pendientes de decisión; en cuanto se definan sus umbrales,
   pasan a exigir el benchmark asociado descrito en `TESTING_STRATEGY.md`
   como su forma de verificación — no una cita de test convencional.
4. **Frontera de arquitectura respetada** — la regla de ADR-001 se verifica
   de forma automatizada en cada corrida.
5. **Umbral de mutación sobre módulos modificados** — los módulos de
   `src/Domain/**` que cambien deben superar el umbral definido en
   `TESTING_STRATEGY.md`.
6. **Alcance: archivos modificados coinciden con los declarados** — el diff
   real se compara contra los archivos que el implementador declaró tocar.

**Mecanismos de referencia (no vinculantes, ejemplo para C#):**
- (1) `dotnet build -warnaserror`
- (2) `dotnet test`
- (3) convención `[Trait("spec","ID")]` sobre cada test, comparación de IDs:
  ```bash
  grep -oE '\*\*(RN|CU|EC|RNF)-[0-9]{2}' SPEC.md | grep -oE '(RN|CU|EC|RNF)-[0-9]{2}' | sort -u > /tmp/spec_ids
  grep -rhoE 'Trait\("spec", *"(RN|CU|EC|RNF)-[0-9]{2}"\)' tests/ | grep -oE '(RN|CU|EC|RNF)-[0-9]{2}' | sort -u > /tmp/test_ids
  comm -13 /tmp/spec_ids /tmp/test_ids   # test que cita un ID inexistente
  comm -23 /tmp/spec_ids /tmp/test_ids   # regla sin ningún test
  ```
  Tests sin etiqueta `spec:` adyacente:
  ```bash
  grep -B2 -rE '\[(Fact|Theory)\]' tests/ | grep -v 'Trait("spec"' | grep -E '\[(Fact|Theory)\]'
  ```
- (4) NetArchTest sobre los ensamblados de `Domain`
- (5) Stryker.NET sobre los proyectos modificados
- (6) `git diff --name-only <base>...HEAD` contra la declaración de alcance

## Alternativas descartadas

- **Contratos en tiempo de ejecución como metodología general:** descartado
  porque los tipos ya cubren los estados inválidos donde corresponde y las
  pruebas por propiedades ya cubren las invariantes sobre secuencias de
  ejecución; no hay un ID que exija además verificación en runtime como
  capa transversal adicional.
- **Pruebas metamórficas como metodología activa:** descartado por ahora,
  no de forma permanente — ver tabla de activación.
- **Golden master incluyendo RNF-01/02/03:** descartado porque mezclaría
  regresión funcional (comparación exacta de salida) con benchmark de
  rendimiento (comparación contra umbral numérico) bajo una misma técnica,
  cuando son verificaciones de naturaleza distinta.

## Consecuencias

- Todo test queda trazado a un ID real del SPEC, incluyendo RNF verificables.
- El umbral de mutación (propuesto 70%, sin respaldo en un ID — decisión
  técnica nuestra) queda documentado en `TESTING_STRATEGY.md` y abierto a
  ajuste.
- RNF-01/02/03/04 no exigen test citado mientras estén pendientes de umbral;
  en cuanto se definan, exigen su benchmark asociado como forma de
  verificación.
