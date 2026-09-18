using MediatR;

namespace Application.Tools;

/// <summary>
///     Pricing page data: every tool (all free basics — advanced is what costs),
///     purchasable packages, and the current user's access state.
/// </summary>
public sealed class GetPricingCatalogQuery : IRequest<PricingCatalogResponse>;
