# <a id="Flowthru_Core_Services"></a> Namespace Flowthru.Core.Services

### Namespaces

 [Flowthru.Core.Services.Models](Flowthru.Core.Services.Models.md)

### Classes

 [FlowthruInspectionExtensions](Flowthru.Core.Services.FlowthruInspectionExtensions.md)

<xref href="Microsoft.Extensions.DependencyInjection.IServiceCollection" data-throw-if-not-resolved="false"></xref> extensions for registering Flowthru preflight
inspectors against external services. Sidecar registration is the single path —
services themselves never implement Flowthru types.

 [FlowthruServiceBuilder](Flowthru.Core.Services.FlowthruServiceBuilder.md)

Fluent builder for configuring Flowthru service registration.

### Interfaces

 [IFlowthruBuilder](Flowthru.Core.Services.IFlowthruBuilder.md)

Builder interface for configuring Flowthru service registration.

 [IFlowthruService](Flowthru.Core.Services.IFlowthruService.md)

Core service for executing Flowthru flows programmatically.

