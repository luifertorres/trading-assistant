> **Status: archived.** See [archive README](README.md) for current guidance.

# Event-Driven Trading Architecture Plan

## Goals
- Desacoplar estrategias, manejo de riesgo y ejecución para poder habilitar DCA sobre RSI(5) sin interferencias del `TradeHandler` actual.
- Permitir recompras en el mismo símbolo mientras se mantiene una única exposición global para evitar sobreapalancamiento.
- Modelar el flujo completo como eventos y suscripciones, facilitando auditoría, replays y nuevas estrategias.

## Capas y Responsabilidades
| Capa | Responsabilidad |
| --- | --- |
| Domain | Eventos, entidades (posición, margen reservado, candle snapshot), invariantes (una posición activa, límite de balance). |
| Application | Handlers/event processors, políticas de riesgo, orquestadores independientes del broker. |
| Infrastructure | Adaptadores Binance, EventBus (in-process + opcional persistencia), repositorios/persistencia de proyecciones, workers. |

## Bus de Eventos
- **Abstracción**: `IEventBus` (publicar/suscribir), transport in-process (MediatR o Channel) inicialmente.
- **Contratos**: Definir en `TradingAssistant.Domain.Events.*` para evitar dependencias circulares.
- **Persistencia opcional**: registrar eventos para replay/debug (ej. tabla SQL `EventLog`).

## Taxonomía de Eventos
1. **Mercado / Indicadores**
   - `CandleClosed`
   - `IndicatorCalculated`
   - `IndicatorThresholdCrossed`
2. **Intención de Estrategia**
   - `EntryRequested`
   - `RebuyRequested`
   - `ExitRequested`
3. **Cuenta / Capital**
   - `AccountSnapshotUpdated`
   - `MarginReserved`
   - `MarginReleased`
   - `MarginDepleted`
4. **Sizing & Ejecución**
   - `OrderSizingRequested`
   - `OrderSizingApproved`
   - `OrderPlacementRequested`
   - `OrderFilled`
   - `OrderRejected`
5. **Ciclo de Posición**
   - `PositionOpened`
   - `PositionAugmented`
   - `PositionClosed`
   - `PositionFailed`

## Servicios/Event Handlers
### 1. `IndicatorTracker`
- Calcula RSI(5) en tiempo real y publica `IndicatorCalculated`.
### 2. `Rsi5ExtremeStrategy`
- Suscribe `IndicatorThresholdCrossed`.
- Emite `EntryRequested` cuando RSI(5) \< 10 con vela bearish.
- Emite `RebuyRequested` si ya existe posición y se repiten condiciones.
- Opcional: emite `ExitRequested` con RSI(5) \> 90 (sin SL/TP/TSL).
### 3. `ExposureGuard`
- Mantiene símbolo activo (`ActiveSymbol`).
- Rechaza `EntryRequested` de símbolos distintos mientras haya exposición abierta.
### 4. `RebuyPolicy`
- Valida RSI, vela bearish y que el símbolo activo coincida.
- Publica `OrderSizingRequested` para bloques de recompra.
### 5. `CapitalAllocator`
- Usa `AccountSnapshotUpdated` + información de filtros de símbolo.
- Regla: bloque inicial = 5 % del balance sin apalancamiento.
- Recompras = 5 % o mínimo permitido.
- Emite `OrderSizingApproved` con leverage=1 y registra `MarginReserved`.
### 6. `MarginSupervisor`
- Lleva tracking de margen comprometido vs balance.
- Emite `MarginDepleted` para bloquear nuevas recompras cuando no hay saldo.
### 7. `ExecutionOrchestrator`
- Traduce `OrderPlacementRequested` a llamadas `IExchangeService`.
- Publica `OrderFilled` o `OrderRejected`.
### 8. `PositionProjector`
- Escucha `OrderFilled`, actualiza promedio y número de bloques.
- Emite `PositionOpened` (primera orden) o `PositionAugmented` (recompras).
- Emite `PositionClosed` cuando se cierra manualmente o por evento externo.

## Flujo RSI(5) DCA
1. `CandleClosed` → `IndicatorTracker` calcula RSI(5).
2. RSI(5) \< 10 y candle bearish → `Rsi5ExtremeStrategy` publica `EntryRequested`.
3. `ExposureGuard` asegura sólo un símbolo activo:
   - Si no hay posición, deja pasar `EntryRequested`.
   - Si el símbolo coincide con activo, lo convierte en `RebuyRequested`.
4. `RebuyPolicy` verifica reglas y envía `OrderSizingRequested`.
5. `CapitalAllocator` aprueba tamaño (5 % balance o mínimo), leverage=1, registra `MarginReserved`, emite `OrderPlacementRequested`.
6. `ExecutionOrchestrator` envía orden a Binance; al llenarse publica `OrderFilled`.
7. `PositionProjector` actualiza estado y emite `PositionOpened/PositionAugmented`.
8. `MarginSupervisor` escucha `OrderFilled` para ajustar margen libre y liberar cuando se cierre.
9. Cuando RSI(5) \> 90 o usuario decide cerrar:
   - `ExitRequested` → `ExecutionOrchestrator` cierra posición → `PositionClosed` libera símbolo y margen.

## Salvaguardas
- **Una posición por símbolo global**: enforced por `ExposureGuard`.
- **Sin apalancamiento**: `OrderSizingApproved` siempre fija leverage=1 y `IExchangeService` usa modo 1x.
- **Balance máximo**: `CapitalAllocator` niega solicitudes cuando la suma de montos aprobados ≥ balance.
- **Sin SL/TP/TSL**: ningún handler para RSI subscribe a esos eventos; módulos de SL/TP permanecen desacoplados.

## Roadmap de Implementación
1. **Infraestructura base**
   - Crear `TradingAssistant.Domain.Events` y `IEventBus`.
   - Implementar EventBus in-process (MediatR wrapper) + registro básico.
2. **Refactor de señal**
   - `Rsi5ExtremeStrategy` deja de enviar `TradingSignalNotification`; publica `EntryRequested`.
   - `TradingSignalWorker` se reemplaza por `EventBusWorker` que sólo bombea eventos externos.
3. **Dividir TradeHandler**
   - Extraer `CapitalAllocator`, `ExecutionOrchestrator`, `ExposureGuard`, `MarginSupervisor`, `PositionProjector`.
   - Adaptar `IExchangeService` para órdenes sin SL/TP/TSL.
4. **Persistencia de proyecciones**
   - Tabla `Positions` (símbolo, bloques, promedio, margen reservado).
   - Tabla `EventLog` opcional para debugging.
5. **Validaciones de DCA**
   - Implementar `RebuyPolicy` con chequeos RSI + vela bearish + balance ≥ bloque.
   - Asegurar bloque = max(5 % balance, min símbolo).
6. **Tests**
   - Unit tests por handler (mock event bus).
   - Integration tests de flujo RSI usando bus in-memory.
7. **Migración de otras estrategias**
   - Portar gradualmente para compartir guardas y capital.

## Notas Finales
- Mantener contratos inmutables y versionados para permitir replays.
- Monitorear métricas por evento (tiempo en cola, rechazos).
- Documentar políticas en [architecture-preferences.md](architecture-preferences.md) una vez estabilice el pipeline (superseded by Platform ADRs and engineering-principles).

