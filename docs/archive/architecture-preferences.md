> **Status: archived.** See [archive README](README.md) for current guidance.

My preferred architecture is a modern simplified Clean Architecture with Domain, Application and Infrastructure layers but without Ports and Adapters.
There will be abstractions in Domain and Application layers and implementations in Infrastructure layer.
I also like modeling a rich domain with DDD.
Because this is a trading assistant, it will support Capital.com in the future so I need to model the domain independent of any specific broker.
Help me draw up a plan to refactor the project to the new architecture.