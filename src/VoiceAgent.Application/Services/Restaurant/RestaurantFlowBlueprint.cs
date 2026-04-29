namespace VoiceAgent.Application.Services.Restaurant;

public static class RestaurantFlowBlueprint
{
    public static readonly IReadOnlyCollection<string> OrderingSequence =
    [
        "Greeting",
        "Identify restaurant order intent",
        "Ask menu/deal/item question or collect items",
        "Search item/deal",
        "Add to cart",
        "Calculate total",
        "Ask anything else",
        "Collect fulfillment type",
        "If delivery, collect address and check coverage",
        "Collect phone/name",
        "Confirm order",
        "Save internally or dispatch externally",
        "Complete"
    ];
}
