using CallCenter.Shared.Entities;

namespace CallCenter.Api.Factories;

internal static class SalonBranchScope
{
    public static IQueryable<SlnClient> ApplyToClients(IQueryable<SlnClient> query, int? branchId)
    {
        if (!branchId.HasValue)
            return query;

        var id = branchId.Value;
        return query.Where(c =>
            c.BranchId == id
            || c.Appointments.Any(a => a.BranchId == id)
            || c.Invoices.Any(i => i.BranchId == id));
    }

    public static IQueryable<SlnCampaign> ApplyToCampaigns(IQueryable<SlnCampaign> query, int? branchId)
        => branchId.HasValue ? query.Where(c => c.BranchId == null || c.BranchId == branchId.Value) : query;

    public static IQueryable<SlnAutoReminder> ApplyToReminders(IQueryable<SlnAutoReminder> query, int? branchId)
        => branchId.HasValue ? query.Where(r => r.BranchId == null || r.BranchId == branchId.Value) : query;

    public static IQueryable<SlnEmailCampaign> ApplyToEmailCampaigns(IQueryable<SlnEmailCampaign> query, int? branchId)
        => branchId.HasValue ? query.Where(c => c.BranchId == null || c.BranchId == branchId.Value) : query;

    public static IQueryable<SlnWinbackRule> ApplyToWinbackRules(IQueryable<SlnWinbackRule> query, int? branchId)
        => branchId.HasValue ? query.Where(r => r.BranchId == null || r.BranchId == branchId.Value) : query;

    public static IQueryable<SlnMembershipPlan> ApplyToMembershipPlans(IQueryable<SlnMembershipPlan> query, int? branchId)
        => branchId.HasValue ? query.Where(p => p.BranchId == null || p.BranchId == branchId.Value) : query;

    public static IQueryable<SlnBeforeAfterPhoto> ApplyToBeforeAfterPhotos(IQueryable<SlnBeforeAfterPhoto> query, int? branchId)
    {
        if (!branchId.HasValue)
            return query;

        var id = branchId.Value;
        return query.Where(p =>
            p.BranchId == id
            || (p.SlnClient != null
                && (p.SlnClient.BranchId == id
                    || p.SlnClient.Appointments.Any(a => a.BranchId == id)
                    || p.SlnClient.Invoices.Any(i => i.BranchId == id))));
    }

    public static IQueryable<SlnReview> ApplyToReviews(IQueryable<SlnReview> query, int? branchId)
    {
        if (!branchId.HasValue)
            return query;

        var id = branchId.Value;
        return query.Where(r =>
            r.BranchId == id
            || (r.SlnClient != null
                && (r.SlnClient.BranchId == id
                    || r.SlnClient.Appointments.Any(a => a.BranchId == id)
                    || r.SlnClient.Invoices.Any(i => i.BranchId == id))));
    }

    public static IQueryable<SlnClientMembership> ApplyToMemberships(IQueryable<SlnClientMembership> query, int? branchId)
    {
        if (!branchId.HasValue)
            return query;

        var id = branchId.Value;
        return query.Where(m =>
            m.SlnClient != null
            && (m.SlnClient.BranchId == id
                || m.SlnClient.Appointments.Any(a => a.BranchId == id)
                || m.SlnClient.Invoices.Any(i => i.BranchId == id)));
    }

    public static IQueryable<SlnClientLoyalty> ApplyToLoyalties(IQueryable<SlnClientLoyalty> query, int? branchId)
    {
        if (!branchId.HasValue)
            return query;

        var id = branchId.Value;
        return query.Where(l =>
            l.SlnClient != null
            && (l.SlnClient.BranchId == id
                || l.SlnClient.Appointments.Any(a => a.BranchId == id)
                || l.SlnClient.Invoices.Any(i => i.BranchId == id)));
    }

    public static IQueryable<SlnClientPackage> ApplyToClientPackages(IQueryable<SlnClientPackage> query, int? branchId)
    {
        if (!branchId.HasValue)
            return query;

        var id = branchId.Value;
        return query.Where(p =>
            p.BranchId == id
            || (p.SlnClient != null
                && (p.SlnClient.BranchId == id
                    || p.SlnClient.Appointments.Any(a => a.BranchId == id)
                    || p.SlnClient.Invoices.Any(i => i.BranchId == id))));
    }

    public static IQueryable<SlnWhatsAppMessage> ApplyToWhatsAppMessages(IQueryable<SlnWhatsAppMessage> query, int? branchId)
    {
        if (!branchId.HasValue)
            return query;

        var id = branchId.Value;
        return query.Where(m =>
            m.BranchId == id
            || (m.SlnClient != null
                && (m.SlnClient.BranchId == id
                    || m.SlnClient.Appointments.Any(a => a.BranchId == id)
                    || m.SlnClient.Invoices.Any(i => i.BranchId == id))));
    }
}
