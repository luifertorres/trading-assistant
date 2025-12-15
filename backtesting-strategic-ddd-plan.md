# Backtesting Strategic DDD Plan

## 1. Vision General
El objetivo es incorporar un módulo de Backtesting robusto que permita validar las estrategias (como RSI(5) DCA) antes de desplegarlas en Live Trading. Se busca una arquitectura orientada a eventos (EDA) y DDD, alineada con la refactorización en curso.

## 2. Análisis de Bounded Contexts (Contextos Delimitados)

Identificamos preliminarmente los siguientes contextos:

### A. Core Domain (El Corazón del Negocio)
Aquí reside la ventaja competitiva.
- **Strategy Context**: Donde se definen las reglas de entrada, salida y gestión de posición (ej. `Rsi5ExtremeStrategy`).
- **Market Analysis Context**: Cálculo de indicadores, patrones de velas (ej. `IndicatorTracker`).

### B. Supporting Subdomains (Soporte)
- **Backtesting Context**: Motor de simulación que "reproduce" el mercado y valida las estrategias.
- **Reporting/Analytics Context**: Análisis de resultados del backtesting (Drawdown, Profit factor, Sharpe ratio).

### C. Generic Subdomains (Genéricos)
- **Exchange/Execution Context**: Conexión con Binance/Capital.com. En Backtesting, este contexto se sustituye por un simulador.
- **Market Data Context**: Obtención y persistencia de velas históricas.

## 3. Decisión Estratégica: Modelo de Dominio Puro
**Problema:** Depender de librerías externas (`Binance.Net`) en el Dominio causa acoplamiento y roturas frecuentes por cambios de terceros. Además, impide modelar otros brokers (Capital.com) o simular ejecuciones en Backtesting sin "hacks".

**Decisión:** Definir **Nuestros Propios Modelos de Dominio** (Entities, Value Objects, Enums) y usar **Adaptadores** en la capa de Infraestructura.
- Eliminar `Binance.Net` de `TradingAssistant.Domain`.
- Crear enums propios: `TimeFrame` (en vez de `KlineInterval`), `OrderSide`, etc.
- **Beneficio:** Estabilidad, testabilidad pura, soporte multi-broker y backtesting limpio.

## 4. Ubiquitous Language (Lenguaje Ubicuo) - Candidatos
- *Candle / Vela*
- *Signal / Señal* (Entry, Exit, Rebuy)
- *Position / Posición*
- *Fill / Ejecución*
- *Slippage / Deslizamiento*
- *Equity Curve / Curva de Capital*
- *Drawdown*
- *TimeFrame* (vs KlineInterval)

## 5. Relación entre Contextos (Context Mapping)
- **Shared Kernel (Núcleo Compartido)**: Es probable que `TradingAssistant.Domain` actúe como Shared Kernel conteniendo primitivas como `Candle`, `Rsi`, `Side`, etc., que usan tanto Live como Backtesting.
- **Strategy** debe ser agnóstica del entorno (Live vs Backtest).
- **Anti-Corruption Layer (ACL)**: Necesaria en `Infrastructure` para traducir de `Binance.Net` objects -> `Domain` objects.

## 6. Preguntas para Definición Estratégica
*(A ser respondidas durante la sesión)*
1. ¿La "Cuenta" (Balance, Margen) en Backtesting debe comportarse exactamente igual que en Binance (reglas complejas de margen) o es una simplificación?
2. ¿Las estrategias necesitan estado persistente entre reinicios durante un backtest?
3. ¿Qué tan granular debe ser la simulación? (ej. ¿simulamos el Order Book o solo precios OHLC?)
