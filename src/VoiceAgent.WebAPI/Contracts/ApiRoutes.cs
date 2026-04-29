namespace VoiceAgent.WebAPI.Contracts;

public static class ApiRoutes
{
    public static class Demo
    {
        public const string Start = "POST /api/demo/start";
        public const string Message = "POST /api/demo/message";
        public const string End = "POST /api/demo/end";
        public const string GetByCallSession = "GET /api/demo/{callSessionId}";
        public const string Campaigns = "GET /api/demo/campaigns";
    }

    public static class Voice
    {
        public const string StartSession = "POST /api/voice/session/start";
        public const string StreamSession = "WS /api/voice/session/{callSessionId}/stream";
        public const string EndSession = "POST /api/voice/session/{callSessionId}/end";
    }

    public static class Calls
    {
        public const string List = "GET /api/calls";
        public const string Get = "GET /api/calls/{id}";
        public const string Turns = "GET /api/calls/{id}/turns";
        public const string Events = "GET /api/calls/{id}/events";
        public const string ToolLogs = "GET /api/calls/{id}/tool-logs";
        public const string Recording = "GET /api/calls/{id}/recording";
    }

    public static class TenantClientCampaign
    {
        public const string CreateTenant = "POST /api/tenants";
        public const string ListTenants = "GET /api/tenants";
        public const string GetTenant = "GET /api/tenants/{id}";
        public const string UpdateTenant = "PUT /api/tenants/{id}";
        public const string CreateClient = "POST /api/clients";
        public const string ListClientsByTenant = "GET /api/clients/by-tenant/{tenantId}";
        public const string GetClient = "GET /api/clients/{id}";
        public const string UpdateClient = "PUT /api/clients/{id}";
        public const string CreateBranch = "POST /api/branches";
        public const string ListBranchesByClient = "GET /api/branches/by-client/{clientId}";
        public const string UpdateBranch = "PUT /api/branches/{id}";
        public const string CreateCampaign = "POST /api/campaigns";
        public const string ListCampaignsByClient = "GET /api/campaigns/by-client/{clientId}";
        public const string ListDemoCampaigns = "GET /api/campaigns/demo";
        public const string UpdateCampaign = "PUT /api/campaigns/{id}";
        public const string CreateCampaignConfiguration = "POST /api/campaign-configurations";
        public const string ListConfigurationsByCampaign = "GET /api/campaign-configurations/by-campaign/{campaignId}";
        public const string UpdateCampaignConfiguration = "PUT /api/campaign-configurations/{id}";
    }

    public static class Restaurant
    {
        public const string CreateMenu = "POST /api/menus";
        public const string ListMenusByClient = "GET /api/menus/by-client/{clientId}";
        public const string CreateCategory = "POST /api/menu-categories";
        public const string ListCategoriesByMenu = "GET /api/menu-categories/by-menu/{menuId}";
        public const string CreateMenuItem = "POST /api/menu-items";
        public const string ListMenuItemsByMenu = "GET /api/menu-items/by-menu/{menuId}";
        public const string UpdateMenuItem = "PUT /api/menu-items/{id}";
        public const string CreateVariant = "POST /api/menu-item-variants";
        public const string CreateAddon = "POST /api/menu-item-addons";
        public const string CreateDeal = "POST /api/restaurant-deals";
        public const string ListDealsByClient = "GET /api/restaurant-deals/by-client/{clientId}";
        public const string UpdateDeal = "PUT /api/restaurant-deals/{id}";
        public const string TestOrderPrice = "POST /api/restaurant/orders/test-price";
        public const string ListOrdersByCall = "GET /api/restaurant/orders/by-call/{callSessionId}";
    }

    public static class Courier
    {
        public const string CreatePricingProfile = "POST /api/courier-pricing-profiles";
        public const string ListPricingProfilesByClient = "GET /api/courier-pricing-profiles/by-client/{clientId}";
        public const string UpdatePricingProfile = "PUT /api/courier-pricing-profiles/{id}";
        public const string TestQuote = "POST /api/courier/quote/test";
        public const string ListQuotesByCall = "GET /api/courier/quotes/by-call/{callSessionId}";
    }

    public static class Rag
    {
        public const string CreateKnowledgeBase = "POST /api/knowledge-bases";
        public const string ListKnowledgeBasesByCampaign = "GET /api/knowledge-bases/by-campaign/{campaignId}";
        public const string UploadDocument = "POST /api/knowledge-documents/upload";
        public const string CreateDocumentText = "POST /api/knowledge-documents/text";
        public const string ListDocumentsByKb = "GET /api/knowledge-documents/by-kb/{knowledgeBaseId}";
        public const string DeactivateDocument = "PUT /api/knowledge-documents/{id}/deactivate";
        public const string ReindexDocument = "POST /api/knowledge-documents/{id}/reindex";
        public const string TestSearch = "POST /api/rag/search/test";
    }

    public static class ExternalApiConfiguration
    {
        public const string Create = "POST /api/external-api-configurations";
        public const string ListByCampaign = "GET /api/external-api-configurations/by-campaign/{campaignId}";
        public const string Update = "PUT /api/external-api-configurations/{id}";
        public const string Test = "POST /api/external-api-configurations/{id}/test";
    }

    public static class Billing
    {
        public const string TenantUsage = "GET /api/billing/tenant-usage/{tenantId}";
        public const string CampaignUsage = "GET /api/billing/campaign-usage/{campaignId}";
        public const string BlockTenant = "POST /api/billing/tenant/{tenantId}/block";
        public const string UnblockTenant = "POST /api/billing/tenant/{tenantId}/unblock";
    }
}
