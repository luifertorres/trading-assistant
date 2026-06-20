# Trading Assistant

Bot de trading automatizado para **Binance Futures (USDT Perpetual)** que monitorea el mercado en tiempo real, detecta señales técnicas y ejecuta operaciones automáticamente con gestión de riesgo integrada.

El repositorio contiene tres soluciones bajo `src/` (ver [src/README.md](src/README.md)):

| Bucket | Solución | Uso |
|--------|----------|-----|
| `platform/` | TradingPlatform | Monolito modular DDD (desarrollo principal) |
| `legacy/` | TradingAssistant | Bot en vivo + CandlestickData (mantenimiento) |
| `mvp/` | Backtesting | Herramienta aislada de backtesting |

## Características

- Monitoreo de velas (candlesticks) vía WebSocket en tiempo real
- Cálculo de indicadores técnicos (RSI, SMAs)
- Múltiples estrategias de trading configurables
- Ejecución automática de órdenes (Market/Limit)
- Gestión de riesgo: Stop-Loss, Take-Profit, Break-Even, Trailing Stop
- Notificaciones vía Telegram
- Cache de alta performance con Microsoft FASTER

## Stack Tecnológico

| Componente | Tecnología |
|------------|------------|
| Lenguaje | C# / .NET 10.0 |
| Tipo de aplicación | Worker Service |
| Exchange API | Binance.Net 12.2.0 (REST + WebSockets) |
| Mediator/CQRS | MediatR 14.0 |
| Indicadores técnicos | Skender.Stock.Indicators 2.7.1 |
| Cache en memoria | Microsoft FASTER (FasterKV) |
| Base de datos | SQLite (EF Core 10.0.2) |
| Notificaciones | Telegram |
| Container | Docker (Linux) |

## Arquitectura

El proyecto sigue una arquitectura Clean Architecture simplificada con DDD:

```
TradingAssistant/
├── TradingAssistant.Domain/          # Entidades y lógica de negocio
├── TradingAssistant.Application/     # Casos de uso e interfaces
├── TradingAssistant.Infrastructure/  # Implementaciones (Binance, EF Core, FASTER)
└── TradingAssistant/                 # Host/Worker Service
```

## Requisitos

- .NET 10.0 SDK
- Cuenta de Binance con API Key habilitada para Futures
- (Opcional) Bot de Telegram para notificaciones

## Configuración

### 1. Configurar API Keys

Usando User Secrets (recomendado para desarrollo):

```bash
cd src/legacy/TradingAssistant/TradingAssistant
dotnet user-secrets set "Binance:Futures:ApiKey" "TU_API_KEY"
dotnet user-secrets set "Binance:Futures:ApiSecret" "TU_API_SECRET"
```

O editando `appsettings.Development.json`:

```json
{
  "Binance": {
    "Futures": {
      "ApiKey": "TU_API_KEY",
      "ApiSecret": "TU_API_SECRET"
    }
  }
}
```

### 2. Configurar Telegram (opcional)

```json
{
  "Logging": {
    "Telegram": {
      "AccessToken": "TU_BOT_TOKEN",
      "ChatId": "TU_CHAT_ID"
    }
  }
}
```

### 3. Configurar Estrategia

En `appsettings.json`:

```json
{
  "Binance": {
    "Service": {
      "TimeFrameSeconds": 3600,      // 1H = 3600, 5m = 300, 1m = 60
      "CandlestickSize": 2200        // Cantidad de velas históricas
    },
    "Strategy": {
      "LengthA": 5,
      "LengthB": 8,
      "LengthC": 20,
      "LengthD": 200
    },
    "RiskManagement": {
      "AccountMarginPercentage": 0.5,
      "StopLossRoi": 100,
      "MinRoiBeforeBreakEven": 100,
      "TakeProfitRoi": 100
    }
  }
}
```

## Ejecución

### Desarrollo local

```bash
cd src/legacy/TradingAssistant/TradingAssistant
dotnet run
```

### Docker

```bash
# Build
docker build -t trading-assistant \
  -f src/legacy/TradingAssistant/TradingAssistant/Dockerfile \
  src/legacy/TradingAssistant

# Run
docker run -d \
  -e Binance__Futures__ApiKey=TU_API_KEY \
  -e Binance__Futures__ApiSecret=TU_API_SECRET \
  trading-assistant
```

## Estrategias Disponibles

| Estrategia | Descripción |
|------------|-------------|
| `MeanReversion1mOr15mStrategy` | Mean reversion en timeframes cortos |
| `MeanReversion5mStrategy` | Mean reversion en 5 minutos |
| `TrendFollowing1mOr15mStrategy` | Seguimiento de tendencia |
| `Rsi5Below10On1mStrategy` | RSI extremo (<10) en 1 minuto |
| `Rsi5Below10On1dStrategy` | RSI extremo (<10) en diario |
| `Rsi5ExtremeStrategy` | RSI en niveles extremos |

## Gestión de Riesgo

El bot incluye múltiples mecanismos de protección:

- **Stop-Loss**: Cierre automático por pérdida máxima
- **Take-Profit**: Cierre automático al alcanzar objetivo
- **Break-Even**: Mover stop-loss a precio de entrada cuando hay ganancia
- **Trailing Stop**: Stop-loss dinámico que sigue al precio
- **Stepped Trailing Stop**: Trailing stop con escalones de ROI

## Estructura del Proyecto

```
src/
├── platform/TradingPlatform/   # Greenfield DDD modular monolith
├── legacy/TradingAssistant/    # Live bot + CandlestickData
└── mvp/Backtesting/            # Isolated backtest MVP
```

Detalle del bot legacy:

```
src/legacy/TradingAssistant/
├── TradingAssistant/
│   ├── Program.cs                    # Entry point
│   ├── *Strategy.cs                  # Estrategias de trading
│   ├── *Worker.cs                    # Background services
│   ├── *Manager.cs                   # Gestores de órdenes
│   └── appsettings.json
│
├── TradingAssistant.Domain/
│   ├── Candle.cs                     # Entidad vela
│   ├── Rsi.cs                        # Value object RSI
│   ├── StopLossPrice.cs              # Cálculo de stop-loss
│   ├── TakeProfitPrice.cs            # Cálculo de take-profit
│   └── OpenPosition.cs               # Posición abierta
│
├── TradingAssistant.Application/
│   ├── IExchangeService.cs           # Abstracción del exchange
│   ├── ICandleRepository.cs          # Repositorio de velas
│   ├── CandleClosedNotification.cs   # Evento MediatR
│   └── TradingSignalNotification.cs  # Señal de trading
│
└── TradingAssistant.Infrastructure/
    ├── Binance/
    │   └── BinanceService.cs         # Implementación Binance
    ├── Faster/
    │   └── FasterCandleRepository.cs # Cache FASTER
    └── TradingContext.cs             # EF Core DbContext
```

## Advertencia

Este software es para uso educativo y experimental. El trading de criptomonedas con apalancamiento conlleva un alto riesgo de pérdida. Úsalo bajo tu propia responsabilidad.

## Licencia

Ver archivo [LICENSE](LICENSE).
