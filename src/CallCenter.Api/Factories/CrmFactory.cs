using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class CrmFactory : ICrmFactory
{
    private readonly IContactEntityService _contactEs;
    private readonly ICrmTicketEntityService _ticketEs;
    private readonly ICrmTicketCategoryEntityService _ticketCategoryEs;
    private readonly ICrmTicketCommentEntityService _ticketCommentEs;
    private readonly ICrmDealEntityService _dealEs;
    private readonly ICrmActivityEntityService _activityEs;
    private readonly ICrmTaskEntityService _taskEs;
    private readonly ICrmSurveyEntityService _surveyEs;
    private readonly ICrmSurveyResponseEntityService _surveyResponseEs;
    private readonly ICrmContactTagEntityService _contactTagEs;
    private readonly IUnitOfWork _uow;

    public CrmFactory(
        IContactEntityService contactEs,
        ICrmTicketEntityService ticketEs,
        ICrmTicketCategoryEntityService ticketCategoryEs,
        ICrmTicketCommentEntityService ticketCommentEs,
        ICrmDealEntityService dealEs,
        ICrmActivityEntityService activityEs,
        ICrmTaskEntityService taskEs,
        ICrmSurveyEntityService surveyEs,
        ICrmSurveyResponseEntityService surveyResponseEs,
        ICrmContactTagEntityService contactTagEs,
        IUnitOfWork uow)
    {
        _contactEs = contactEs;
        _ticketEs = ticketEs;
        _ticketCategoryEs = ticketCategoryEs;
        _ticketCommentEs = ticketCommentEs;
        _dealEs = dealEs;
        _activityEs = activityEs;
        _taskEs = taskEs;
        _surveyEs = surveyEs;
        _surveyResponseEs = surveyResponseEs;
        _contactTagEs = contactTagEs;
        _uow = uow;
    }

    // ═══════════════════════════════════════
    // DASHBOARD
    // ═══════════════════════════════════════

    public async Task<CrmDashboardDto> GetDashboardAsync(int customerId)
    {
        var today = DateTime.UtcNow.Date;

        var totalContacts = await _contactEs.GetAllQueryable()
            .CountAsync(c => c.CustomerId == customerId);

        var openTickets = await _ticketEs.GetAllQueryable()
            .CountAsync(t => t.CustomerId == customerId
                && t.StatusId != TicketStatuses.Ids.Closed
                && t.StatusId != TicketStatuses.Ids.Resolved);

        var activeDeals = await _dealEs.GetAllQueryable()
            .CountAsync(d => d.CustomerId == customerId
                && d.StageId != DealStages.Ids.Won
                && d.StageId != DealStages.Ids.Lost);

        var todayActivities = await _activityEs.GetAllQueryable()
            .CountAsync(a => a.CustomerId == customerId && a.CreatedAt >= today);

        var pipelineValue = await _dealEs.GetAllQueryable()
            .Where(d => d.CustomerId == customerId
                && d.StageId != DealStages.Ids.Won
                && d.StageId != DealStages.Ids.Lost)
            .SumAsync(d => d.Value);

        var recentActivities = await _activityEs.GetAllQueryable()
            .Where(a => a.CustomerId == customerId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(10)
            .Include(a => a.Contact)
            .Include(a => a.Personnel).ThenInclude(p => p.User)
            .ToListAsync();

        var upcomingTasks = await _taskEs.GetAllQueryable()
            .Where(t => t.CustomerId == customerId
                && t.StatusId != CrmTaskStatuses.Ids.Done
                && t.StatusId != CrmTaskStatuses.Ids.Cancelled)
            .OrderBy(t => t.DueDate)
            .Take(10)
            .Include(t => t.Contact)
            .Include(t => t.AssignedToPersonnel).ThenInclude(p => p.User)
            .ToListAsync();

        return new CrmDashboardDto
        {
            TotalContacts = totalContacts,
            OpenTickets = openTickets,
            ActiveDeals = activeDeals,
            TodayActivities = todayActivities,
            PipelineValue = pipelineValue,
            RecentActivities = recentActivities.Select(MapActivityDto).ToList(),
            UpcomingTasks = upcomingTasks.Select(MapTaskDto).ToList()
        };
    }

    // ═══════════════════════════════════════
    // CONTACTS
    // ═══════════════════════════════════════

    public async Task<CrmCallerIdDto?> LookupByPhoneAsync(int customerId, string phoneNumber)
    {
        // Numara normalizasyonu: sadece rakamlari al, son 10 haneyle karsilastir
        var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());
        var suffix = digits.Length > 10 ? digits[^10..] : digits;

        var contacts = await _contactEs.GetAllQueryable()
            .Where(c => c.CustomerId == customerId)
            .ToListAsync();

        // Client-side numara eslestirme (normalize edilmis son haneler)
        var match = contacts.FirstOrDefault(c =>
        {
            var p1 = new string(c.PhoneNumber.Where(char.IsDigit).ToArray());
            if (p1.Length > 10) p1 = p1[^10..];
            if (p1 == suffix) return true;

            if (!string.IsNullOrEmpty(c.PhoneNumber2))
            {
                var p2 = new string(c.PhoneNumber2.Where(char.IsDigit).ToArray());
                if (p2.Length > 10) p2 = p2[^10..];
                if (p2 == suffix) return true;
            }
            return false;
        });

        if (match == null) return null;

        var openTickets = await _ticketEs.GetAllQueryable()
            .CountAsync(t => t.ContactId == match.Id
                && t.StatusId != TicketStatuses.Ids.Closed
                && t.StatusId != TicketStatuses.Ids.Resolved);

        var activeDeals = await _dealEs.GetAllQueryable()
            .CountAsync(d => d.ContactId == match.Id
                && d.StageId != DealStages.Ids.Won
                && d.StageId != DealStages.Ids.Lost);

        return new CrmCallerIdDto
        {
            ContactId = match.Id,
            FullName = match.FullName,
            Company = match.Company,
            Email = match.Email,
            PhoneNumber = match.PhoneNumber,
            OpenTickets = openTickets,
            ActiveDeals = activeDeals
        };
    }

    public async Task<List<CrmContactDto>> GetContactsAsync(int customerId, string? search)
    {
        var query = _contactEs.GetAllQueryable()
            .Where(c => c.CustomerId == customerId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(c =>
                c.FullName.ToLower().Contains(s)
                || (c.PhoneNumber != null && c.PhoneNumber.Contains(s))
                || (c.Email != null && c.Email.ToLower().Contains(s))
                || (c.Company != null && c.Company.ToLower().Contains(s)));
        }

        var contacts = await query.OrderBy(c => c.FullName).ToListAsync();
        var contactIds = contacts.Select(c => c.Id).ToList();

        var ticketCounts = await _ticketEs.GetAllQueryable()
            .Where(t => t.ContactId != null && contactIds.Contains(t.ContactId.Value))
            .GroupBy(t => t.ContactId)
            .Select(g => new { ContactId = g.Key, Count = g.Count() })
            .ToListAsync();

        var dealCounts = await _dealEs.GetAllQueryable()
            .Where(d => d.ContactId != null && contactIds.Contains(d.ContactId.Value))
            .GroupBy(d => d.ContactId)
            .Select(g => new { ContactId = g.Key, Count = g.Count() })
            .ToListAsync();

        var activityCounts = await _activityEs.GetAllQueryable()
            .Where(a => a.ContactId != null && contactIds.Contains(a.ContactId.Value))
            .GroupBy(a => a.ContactId)
            .Select(g => new { ContactId = g.Key, Count = g.Count() })
            .ToListAsync();

        return contacts.Select(c => new CrmContactDto
        {
            Id = c.Id,
            Uid = c.Uid,
            FullName = c.FullName,
            PhoneNumber = c.PhoneNumber,
            PhoneNumber2 = c.PhoneNumber2,
            Email = c.Email,
            Company = c.Company,
            Department = c.Department,
            Title = c.Title,
            Notes = c.Notes,
            IsFavorite = c.IsFavorite,
            CreatedAt = c.CreatedAt,
            TicketCount = ticketCounts.FirstOrDefault(x => x.ContactId == c.Id)?.Count ?? 0,
            DealCount = dealCounts.FirstOrDefault(x => x.ContactId == c.Id)?.Count ?? 0,
            ActivityCount = activityCounts.FirstOrDefault(x => x.ContactId == c.Id)?.Count ?? 0,
        }).ToList();
    }

    public async Task<CrmContactDto?> GetContactDetailAsync(int contactId, int customerId)
    {
        var contact = await _contactEs.GetAllQueryable()
            .FirstOrDefaultAsync(c => c.Id == contactId && c.CustomerId == customerId);
        if (contact == null) return null;

        var ticketCount = await _ticketEs.GetAllQueryable().CountAsync(t => t.ContactId == contactId);
        var dealCount = await _dealEs.GetAllQueryable().CountAsync(d => d.ContactId == contactId);
        var activityCount = await _activityEs.GetAllQueryable().CountAsync(a => a.ContactId == contactId);
        var taskCount = await _taskEs.GetAllQueryable().CountAsync(t => t.ContactId == contactId);

        return new CrmContactDto
        {
            Id = contact.Id,
            Uid = contact.Uid,
            FullName = contact.FullName,
            PhoneNumber = contact.PhoneNumber,
            PhoneNumber2 = contact.PhoneNumber2,
            Email = contact.Email,
            Company = contact.Company,
            Department = contact.Department,
            Title = contact.Title,
            Notes = contact.Notes,
            IsFavorite = contact.IsFavorite,
            CreatedAt = contact.CreatedAt,
            TicketCount = ticketCount,
            DealCount = dealCount,
            ActivityCount = activityCount,
            TaskCount = taskCount
        };
    }

    public async Task<(bool success, int id, string? error)> CreateContactAsync(
        CrmContactCreateDto dto, int customerId, int personnelId)
    {
        var contact = new Contact
        {
            FullName = dto.FullName,
            PhoneNumber = dto.PhoneNumber,
            PhoneNumber2 = dto.PhoneNumber2,
            Email = dto.Email,
            Company = dto.Company,
            Department = dto.Department,
            Title = dto.Title,
            Notes = dto.Notes,
            CustomerId = customerId
        };

        _contactEs.Add(contact);
        await _uow.SaveChangesAsync();
        return (true, contact.Id, null);
    }

    public async Task<(bool success, string? error)> UpdateContactAsync(
        int contactId, CrmContactUpdateDto dto, int customerId)
    {
        var contact = await _contactEs.GetAllQueryable()
            .FirstOrDefaultAsync(c => c.Id == contactId && c.CustomerId == customerId);
        if (contact == null) return (false, "Kisi bulunamadi");

        contact.FullName = dto.FullName;
        contact.PhoneNumber = dto.PhoneNumber;
        contact.PhoneNumber2 = dto.PhoneNumber2;
        contact.Email = dto.Email;
        contact.Company = dto.Company;
        contact.Department = dto.Department;
        contact.Title = dto.Title;
        contact.Notes = dto.Notes;
        contact.IsFavorite = dto.IsFavorite;
        contact.UpdatedAt = DateTime.UtcNow;

        _contactEs.Update(contact);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool success, string? error)> DeleteContactAsync(int contactId, int customerId)
    {
        var contact = await _contactEs.GetAllQueryable()
            .FirstOrDefaultAsync(c => c.Id == contactId && c.CustomerId == customerId);
        if (contact == null) return (false, "Kisi bulunamadi");

        _contactEs.Remove(contact);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    // ═══════════════════════════════════════
    // TICKETS
    // ═══════════════════════════════════════

    public async Task<List<CrmTicketDto>> GetTicketsAsync(int customerId, int? statusId)
    {
        var query = _ticketEs.GetAllQueryable()
            .Where(t => t.CustomerId == customerId);

        if (statusId.HasValue)
            query = query.Where(t => t.StatusId == statusId.Value);

        var tickets = await query
            .OrderByDescending(t => t.CreatedAt)
            .Include(t => t.Contact)
            .Include(t => t.AssignedToPersonnel).ThenInclude(p => p!.User)
            .Include(t => t.CreatedByPersonnel).ThenInclude(p => p.User)
            .ToListAsync();

        return tickets.Select(MapTicketDto).ToList();
    }

    public async Task<CrmTicketDto?> GetTicketDetailAsync(int ticketId, int customerId)
    {
        var ticket = await _ticketEs.GetAllQueryable()
            .Where(t => t.Id == ticketId && t.CustomerId == customerId)
            .Include(t => t.Contact)
            .Include(t => t.AssignedToPersonnel).ThenInclude(p => p!.User)
            .Include(t => t.CreatedByPersonnel).ThenInclude(p => p.User)
            .FirstOrDefaultAsync();

        if (ticket == null) return null;

        var dto = MapTicketDto(ticket);
        dto.ActivityCount = await _activityEs.GetAllQueryable().CountAsync(a => a.TicketId == ticketId);
        return dto;
    }

    public async Task<(bool success, int id, string? error)> CreateTicketAsync(
        CrmTicketCreateDto dto, int customerId, int personnelId)
    {
        var ticket = new CrmTicket
        {
            Subject = dto.Subject,
            Description = dto.Description,
            PriorityId = dto.PriorityId,
            StatusId = TicketStatuses.Ids.Open,
            ContactId = dto.ContactId,
            AssignedToPersonnelId = dto.AssignedToPersonnelId,
            CreatedByPersonnelId = personnelId,
            CustomerId = customerId
        };

        _ticketEs.Add(ticket);
        await _uow.SaveChangesAsync();
        return (true, ticket.Id, null);
    }

    public async Task<(bool success, string? error)> UpdateTicketAsync(
        int ticketId, CrmTicketUpdateDto dto, int customerId)
    {
        var ticket = await _ticketEs.GetAllQueryable()
            .FirstOrDefaultAsync(t => t.Id == ticketId && t.CustomerId == customerId);
        if (ticket == null) return (false, "Talep bulunamadi");

        ticket.Subject = dto.Subject;
        ticket.Description = dto.Description;
        ticket.PriorityId = dto.PriorityId;
        ticket.StatusId = dto.StatusId;
        ticket.ContactId = dto.ContactId;
        ticket.AssignedToPersonnelId = dto.AssignedToPersonnelId;
        ticket.UpdatedAt = DateTime.UtcNow;

        if (dto.StatusId == TicketStatuses.Ids.Closed || dto.StatusId == TicketStatuses.Ids.Resolved)
            ticket.ClosedAt ??= DateTime.UtcNow;

        _ticketEs.Update(ticket);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    // ═══════════════════════════════════════
    // DEALS
    // ═══════════════════════════════════════

    public async Task<List<CrmDealDto>> GetDealsAsync(int customerId, int? stageId)
    {
        var query = _dealEs.GetAllQueryable()
            .Where(d => d.CustomerId == customerId);

        if (stageId.HasValue)
            query = query.Where(d => d.StageId == stageId.Value);

        var deals = await query
            .OrderByDescending(d => d.CreatedAt)
            .Include(d => d.Contact)
            .Include(d => d.OwnerPersonnel).ThenInclude(p => p!.User)
            .Include(d => d.CreatedByPersonnel).ThenInclude(p => p.User)
            .ToListAsync();

        return deals.Select(MapDealDto).ToList();
    }

    public async Task<CrmDealDto?> GetDealDetailAsync(int dealId, int customerId)
    {
        var deal = await _dealEs.GetAllQueryable()
            .Where(d => d.Id == dealId && d.CustomerId == customerId)
            .Include(d => d.Contact)
            .Include(d => d.OwnerPersonnel).ThenInclude(p => p!.User)
            .Include(d => d.CreatedByPersonnel).ThenInclude(p => p.User)
            .FirstOrDefaultAsync();

        if (deal == null) return null;

        var dto = MapDealDto(deal);
        dto.ActivityCount = await _activityEs.GetAllQueryable().CountAsync(a => a.DealId == dealId);
        return dto;
    }

    public async Task<(bool success, int id, string? error)> CreateDealAsync(
        CrmDealCreateDto dto, int customerId, int personnelId)
    {
        var deal = new CrmDeal
        {
            Title = dto.Title,
            Value = dto.Value,
            StageId = dto.StageId,
            Probability = dto.Probability,
            ExpectedCloseDate = dto.ExpectedCloseDate,
            Notes = dto.Notes,
            ContactId = dto.ContactId,
            OwnerPersonnelId = dto.OwnerPersonnelId ?? personnelId,
            CreatedByPersonnelId = personnelId,
            CustomerId = customerId
        };

        _dealEs.Add(deal);
        await _uow.SaveChangesAsync();
        return (true, deal.Id, null);
    }

    public async Task<(bool success, string? error)> UpdateDealAsync(
        int dealId, CrmDealUpdateDto dto, int customerId)
    {
        var deal = await _dealEs.GetAllQueryable()
            .FirstOrDefaultAsync(d => d.Id == dealId && d.CustomerId == customerId);
        if (deal == null) return (false, "Firsat bulunamadi");

        deal.Title = dto.Title;
        deal.Value = dto.Value;
        deal.StageId = dto.StageId;
        deal.Probability = dto.Probability;
        deal.ExpectedCloseDate = dto.ExpectedCloseDate;
        deal.Notes = dto.Notes;
        deal.ContactId = dto.ContactId;
        deal.OwnerPersonnelId = dto.OwnerPersonnelId;
        deal.UpdatedAt = DateTime.UtcNow;

        if (dto.StageId == DealStages.Ids.Won || dto.StageId == DealStages.Ids.Lost)
            deal.ActualCloseDate ??= DateTime.UtcNow;

        _dealEs.Update(deal);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool success, string? error)> DeleteDealAsync(int dealId, int customerId)
    {
        var deal = await _dealEs.GetAllQueryable()
            .FirstOrDefaultAsync(d => d.Id == dealId && d.CustomerId == customerId);
        if (deal == null) return (false, "Firsat bulunamadi");

        _dealEs.Remove(deal);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    // ═══════════════════════════════════════
    // ACTIVITIES
    // ═══════════════════════════════════════

    public async Task<List<CrmActivityDto>> GetActivitiesAsync(
        int customerId, int? contactId, int? ticketId, int? dealId)
    {
        var query = _activityEs.GetAllQueryable()
            .Where(a => a.CustomerId == customerId);

        if (contactId.HasValue)
            query = query.Where(a => a.ContactId == contactId.Value);
        if (ticketId.HasValue)
            query = query.Where(a => a.TicketId == ticketId.Value);
        if (dealId.HasValue)
            query = query.Where(a => a.DealId == dealId.Value);

        var activities = await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(100)
            .Include(a => a.Contact)
            .Include(a => a.Personnel).ThenInclude(p => p.User)
            .ToListAsync();

        return activities.Select(MapActivityDto).ToList();
    }

    public async Task<(bool success, int id, string? error)> CreateActivityAsync(
        CrmActivityCreateDto dto, int customerId, int personnelId)
    {
        var activity = new CrmActivity
        {
            TypeId = dto.TypeId,
            Summary = dto.Summary,
            Detail = dto.Detail,
            ContactId = dto.ContactId,
            TicketId = dto.TicketId,
            DealId = dto.DealId,
            PersonnelId = personnelId,
            CustomerId = customerId
        };

        _activityEs.Add(activity);
        await _uow.SaveChangesAsync();
        return (true, activity.Id, null);
    }

    // ═══════════════════════════════════════
    // TASKS
    // ═══════════════════════════════════════

    public async Task<List<CrmTaskDto>> GetTasksAsync(int customerId, int? assignedTo, int? statusId)
    {
        var query = _taskEs.GetAllQueryable()
            .Where(t => t.CustomerId == customerId);

        if (assignedTo.HasValue)
            query = query.Where(t => t.AssignedToPersonnelId == assignedTo.Value);
        if (statusId.HasValue)
            query = query.Where(t => t.StatusId == statusId.Value);

        var tasks = await query
            .OrderBy(t => t.DueDate)
            .Include(t => t.Contact)
            .Include(t => t.AssignedToPersonnel).ThenInclude(p => p.User)
            .Include(t => t.CreatedByPersonnel).ThenInclude(p => p.User)
            .ToListAsync();

        return tasks.Select(MapTaskDto).ToList();
    }

    public async Task<(bool success, int id, string? error)> CreateTaskAsync(
        CrmTaskCreateDto dto, int customerId, int personnelId)
    {
        var task = new CrmTask
        {
            Title = dto.Title,
            Description = dto.Description,
            StatusId = CrmTaskStatuses.Ids.Todo,
            DueDate = dto.DueDate,
            ContactId = dto.ContactId,
            TicketId = dto.TicketId,
            DealId = dto.DealId,
            AssignedToPersonnelId = dto.AssignedToPersonnelId,
            CreatedByPersonnelId = personnelId,
            CustomerId = customerId
        };

        _taskEs.Add(task);
        await _uow.SaveChangesAsync();
        return (true, task.Id, null);
    }

    public async Task<(bool success, string? error)> UpdateTaskAsync(
        int taskId, CrmTaskUpdateDto dto, int customerId)
    {
        var task = await _taskEs.GetAllQueryable()
            .FirstOrDefaultAsync(t => t.Id == taskId && t.CustomerId == customerId);
        if (task == null) return (false, "Gorev bulunamadi");

        task.Title = dto.Title;
        task.Description = dto.Description;
        task.StatusId = dto.StatusId;
        task.DueDate = dto.DueDate;
        task.ContactId = dto.ContactId;
        task.TicketId = dto.TicketId;
        task.DealId = dto.DealId;
        task.AssignedToPersonnelId = dto.AssignedToPersonnelId;

        if (dto.StatusId == CrmTaskStatuses.Ids.Done)
            task.CompletedAt ??= DateTime.UtcNow;

        _taskEs.Update(task);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    // ═══════════════════════════════════════
    // SURVEYS (Musteri Memnuniyet Anketi)
    // ═══════════════════════════════════════

    public async Task<List<CrmSurveyDto>> GetSurveysAsync(int customerId)
    {
        var surveys = await _surveyEs.GetAllQueryable()
            .Where(s => s.CustomerId == customerId)
            .OrderByDescending(s => s.CreatedAt)
            .Include(s => s.CreatedByPersonnel).ThenInclude(p => p.User)
            .ToListAsync();

        var surveyIds = surveys.Select(s => s.Id).ToList();

        var questionCounts = await _surveyEs.GetAllQueryable()
            .Where(s => surveyIds.Contains(s.Id))
            .Select(s => new { s.Id, Count = s.Questions.Count })
            .ToListAsync();

        var responseCounts = await _surveyResponseEs.GetAllQueryable()
            .Where(r => surveyIds.Contains(r.SurveyId))
            .GroupBy(r => r.SurveyId)
            .Select(g => new { SurveyId = g.Key, Count = g.Count(), Avg = g.Average(r => r.OverallScore) })
            .ToListAsync();

        return surveys.Select(s =>
        {
            var rc = responseCounts.FirstOrDefault(r => r.SurveyId == s.Id);
            var type = SurveyTypes.GetById(s.TypeId);
            return new CrmSurveyDto
            {
                Id = s.Id,
                Uid = s.Uid,
                Title = s.Title,
                Description = s.Description,
                TypeId = s.TypeId,
                TypeName = type?.Description,
                IsActive = s.IsActive,
                QuestionCount = questionCounts.FirstOrDefault(q => q.Id == s.Id)?.Count ?? 0,
                ResponseCount = rc?.Count ?? 0,
                AverageScore = rc?.Avg,
                CreatedByName = s.CreatedByPersonnel?.User?.FullName,
                CreatedAt = s.CreatedAt
            };
        }).ToList();
    }

    public async Task<CrmSurveyDetailDto?> GetSurveyDetailAsync(int surveyId, int customerId)
    {
        var survey = await _surveyEs.GetAllQueryable()
            .Where(s => s.Id == surveyId && s.CustomerId == customerId)
            .Include(s => s.Questions.OrderBy(q => q.SortOrder))
            .Include(s => s.CreatedByPersonnel).ThenInclude(p => p.User)
            .FirstOrDefaultAsync();
        if (survey == null) return null;

        var responseCount = await _surveyResponseEs.GetAllQueryable().CountAsync(r => r.SurveyId == surveyId);
        var avgScore = await _surveyResponseEs.GetAllQueryable()
            .Where(r => r.SurveyId == surveyId && r.OverallScore != null)
            .AverageAsync(r => (decimal?)r.OverallScore);

        var type = SurveyTypes.GetById(survey.TypeId);
        return new CrmSurveyDetailDto
        {
            Id = survey.Id,
            Uid = survey.Uid,
            Title = survey.Title,
            Description = survey.Description,
            TypeId = survey.TypeId,
            TypeName = type?.Description,
            IsActive = survey.IsActive,
            QuestionCount = survey.Questions.Count,
            ResponseCount = responseCount,
            AverageScore = avgScore,
            CreatedByName = survey.CreatedByPersonnel?.User?.FullName,
            CreatedAt = survey.CreatedAt,
            Questions = survey.Questions.Select(q =>
            {
                var qType = SurveyQuestionTypes.GetById(q.QuestionTypeId);
                return new CrmSurveyQuestionDto
                {
                    Id = q.Id,
                    Text = q.Text,
                    QuestionTypeId = q.QuestionTypeId,
                    QuestionTypeName = qType?.Description,
                    Options = q.Options,
                    MinValue = q.MinValue,
                    MaxValue = q.MaxValue,
                    SortOrder = q.SortOrder,
                    IsRequired = q.IsRequired
                };
            }).ToList()
        };
    }

    public async Task<(bool success, int id, string? error)> CreateSurveyAsync(
        CrmSurveyCreateDto dto, int customerId, int personnelId)
    {
        var survey = new CrmSurvey
        {
            Title = dto.Title,
            Description = dto.Description,
            TypeId = dto.TypeId,
            CustomerId = customerId,
            CreatedByPersonnelId = personnelId
        };

        foreach (var q in dto.Questions)
        {
            survey.Questions.Add(new CrmSurveyQuestion
            {
                Text = q.Text,
                QuestionTypeId = q.QuestionTypeId,
                Options = q.Options,
                MinValue = q.MinValue,
                MaxValue = q.MaxValue,
                SortOrder = q.SortOrder,
                IsRequired = q.IsRequired
            });
        }

        _surveyEs.Add(survey);
        await _uow.SaveChangesAsync();
        return (true, survey.Id, null);
    }

    public async Task<(bool success, string? error)> ToggleSurveyAsync(int surveyId, int customerId)
    {
        var survey = await _surveyEs.GetAllQueryable()
            .FirstOrDefaultAsync(s => s.Id == surveyId && s.CustomerId == customerId);
        if (survey == null) return (false, "Anket bulunamadi");

        survey.IsActive = !survey.IsActive;
        survey.UpdatedAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool success, int id, string? error)> SubmitSurveyResponseAsync(
        CrmSurveyResponseCreateDto dto, int customerId, int personnelId)
    {
        var survey = await _surveyEs.GetAllQueryable()
            .Where(s => s.Id == dto.SurveyId && s.CustomerId == customerId && s.IsActive)
            .Include(s => s.Questions)
            .FirstOrDefaultAsync();
        if (survey == null) return (false, 0, "Anket bulunamadi veya aktif degil");

        var response = new CrmSurveyResponse
        {
            SurveyId = dto.SurveyId,
            ContactId = dto.ContactId,
            CallRecordId = dto.CallRecordId,
            RespondentPhone = dto.RespondentPhone,
            RespondentName = dto.RespondentName,
            CreatedByPersonnelId = personnelId,
            CustomerId = customerId,
            CompletedAt = DateTime.UtcNow
        };

        decimal totalScore = 0;
        int scoreCount = 0;

        foreach (var ans in dto.Answers)
        {
            response.Answers.Add(new CrmSurveyAnswer
            {
                QuestionId = ans.QuestionId,
                AnswerText = ans.AnswerText,
                AnswerScore = ans.AnswerScore
            });

            if (ans.AnswerScore.HasValue)
            {
                totalScore += ans.AnswerScore.Value;
                scoreCount++;
            }
        }

        if (scoreCount > 0)
            response.OverallScore = totalScore / scoreCount;

        _surveyResponseEs.Add(response);
        await _uow.SaveChangesAsync();
        return (true, response.Id, null);
    }

    public async Task<List<CrmSurveyResponseDto>> GetSurveyResponsesAsync(int surveyId, int customerId)
    {
        var responses = await _surveyResponseEs.GetAllQueryable()
            .Where(r => r.SurveyId == surveyId && r.CustomerId == customerId)
            .OrderByDescending(r => r.CreatedAt)
            .Include(r => r.Contact)
            .Include(r => r.CreatedByPersonnel).ThenInclude(p => p!.User)
            .Include(r => r.Answers)
            .ToListAsync();

        return responses.Select(r => new CrmSurveyResponseDto
        {
            Id = r.Id,
            Uid = r.Uid,
            ContactName = r.Contact?.FullName,
            RespondentPhone = r.RespondentPhone,
            RespondentName = r.RespondentName ?? r.Contact?.FullName,
            OverallScore = r.OverallScore,
            CompletedAt = r.CompletedAt,
            CreatedAt = r.CreatedAt,
            CreatedByName = r.CreatedByPersonnel?.User?.FullName,
            Answers = r.Answers.Select(a => new CrmSurveyAnswerDto
            {
                QuestionId = a.QuestionId,
                AnswerText = a.AnswerText,
                AnswerScore = a.AnswerScore
            }).ToList()
        }).ToList();
    }

    // ═══════════════════════════════════════
    // TICKET CATEGORIES & COMMENTS
    // ═══════════════════════════════════════

    public async Task<List<CrmTicketCategoryDto>> GetTicketCategoriesAsync(int customerId)
    {
        return await _ticketCategoryEs.GetAllQueryable()
            .Where(c => c.CustomerId == customerId && c.IsActive)
            .OrderBy(c => c.SortOrder)
            .Select(c => new CrmTicketCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                SortOrder = c.SortOrder,
                IsActive = c.IsActive
            })
            .ToListAsync();
    }

    public async Task<(bool success, int id, string? error)> CreateTicketCategoryAsync(
        string name, int customerId)
    {
        var maxOrder = await _ticketCategoryEs.GetAllQueryable()
            .Where(c => c.CustomerId == customerId)
            .MaxAsync(c => (int?)c.SortOrder) ?? 0;

        var cat = new CrmTicketCategory
        {
            Name = name,
            SortOrder = maxOrder + 1,
            CustomerId = customerId
        };
        _ticketCategoryEs.Add(cat);
        await _uow.SaveChangesAsync();
        return (true, cat.Id, null);
    }

    public async Task<List<CrmTicketCommentDto>> GetTicketCommentsAsync(int ticketId, int customerId)
    {
        // Firma erisim kontrolu
        var ticketExists = await _ticketEs.GetAllQueryable()
            .AnyAsync(t => t.Id == ticketId && t.CustomerId == customerId);
        if (!ticketExists) return new();

        return await _ticketCommentEs.GetAllQueryable()
            .Where(c => c.TicketId == ticketId)
            .OrderBy(c => c.CreatedAt)
            .Include(c => c.CreatedByPersonnel).ThenInclude(p => p.User)
            .Select(c => new CrmTicketCommentDto
            {
                Id = c.Id,
                Content = c.Content,
                IsInternal = c.IsInternal,
                CreatedByName = c.CreatedByPersonnel.User!.FullName,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<(bool success, int id, string? error)> CreateTicketCommentAsync(
        int ticketId, CrmTicketCommentCreateDto dto, int customerId, int personnelId)
    {
        var ticket = await _ticketEs.GetAllQueryable()
            .FirstOrDefaultAsync(t => t.Id == ticketId && t.CustomerId == customerId);
        if (ticket == null) return (false, 0, "Talep bulunamadi");

        var comment = new CrmTicketComment
        {
            TicketId = ticketId,
            Content = dto.Content,
            IsInternal = dto.IsInternal,
            CreatedByPersonnelId = personnelId
        };

        _ticketCommentEs.Add(comment);
        ticket.UpdatedAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync();
        return (true, comment.Id, null);
    }

    // ═══════════════════════════════════════
    // CONTACT TAGS
    // ═══════════════════════════════════════

    public async Task<List<CrmContactTagDto>> GetContactTagsAsync(int customerId)
    {
        return await _contactTagEs.GetAllQueryable()
            .Where(t => t.CustomerId == customerId)
            .OrderBy(t => t.Name)
            .Select(t => new CrmContactTagDto
            {
                Id = t.Id,
                Name = t.Name,
                Color = t.Color
            })
            .ToListAsync();
    }

    public async Task<(bool success, int id, string? error)> CreateContactTagAsync(
        string name, string? color, int customerId)
    {
        var tag = new CrmContactTag { Name = name, Color = color, CustomerId = customerId };
        _contactTagEs.Add(tag);
        await _uow.SaveChangesAsync();
        return (true, tag.Id, null);
    }

    // ═══════════════════════════════════════
    // MAPPING HELPERS
    // ═══════════════════════════════════════

    private static CrmTicketDto MapTicketDto(CrmTicket t)
    {
        var status = TicketStatuses.GetById(t.StatusId);
        var priority = TicketPriorities.GetById(t.PriorityId);
        return new CrmTicketDto
        {
            Id = t.Id,
            Uid = t.Uid,
            Subject = t.Subject,
            Description = t.Description,
            StatusId = t.StatusId,
            StatusName = status?.Description,
            StatusCss = status?.CssClass,
            PriorityId = t.PriorityId,
            PriorityName = priority?.Description,
            PriorityCss = priority?.CssClass,
            ContactId = t.ContactId,
            ContactName = t.Contact?.FullName,
            AssignedToPersonnelId = t.AssignedToPersonnelId,
            AssignedToName = t.AssignedToPersonnel?.User?.FullName,
            CreatedByName = t.CreatedByPersonnel?.User?.FullName,
            CreatedAt = t.CreatedAt,
            ClosedAt = t.ClosedAt
        };
    }

    private static CrmDealDto MapDealDto(CrmDeal d)
    {
        var stage = DealStages.GetById(d.StageId);
        return new CrmDealDto
        {
            Id = d.Id,
            Uid = d.Uid,
            Title = d.Title,
            Value = d.Value,
            StageId = d.StageId,
            StageName = stage?.Description,
            StageCss = stage?.CssClass,
            Probability = d.Probability,
            ExpectedCloseDate = d.ExpectedCloseDate,
            ActualCloseDate = d.ActualCloseDate,
            Notes = d.Notes,
            ContactId = d.ContactId,
            ContactName = d.Contact?.FullName,
            OwnerPersonnelId = d.OwnerPersonnelId,
            OwnerName = d.OwnerPersonnel?.User?.FullName,
            CreatedByName = d.CreatedByPersonnel?.User?.FullName,
            CreatedAt = d.CreatedAt
        };
    }

    private static CrmActivityDto MapActivityDto(CrmActivity a)
    {
        var type = ActivityTypes.GetById(a.TypeId);
        return new CrmActivityDto
        {
            Id = a.Id,
            TypeId = a.TypeId,
            TypeName = type?.Description,
            TypeIcon = type?.Icon,
            TypeCss = type?.CssClass,
            Summary = a.Summary,
            Detail = a.Detail,
            ContactId = a.ContactId,
            ContactName = a.Contact?.FullName,
            TicketId = a.TicketId,
            DealId = a.DealId,
            CallRecordId = a.CallRecordId,
            PersonnelName = a.Personnel?.User?.FullName,
            CreatedAt = a.CreatedAt
        };
    }

    private static CrmTaskDto MapTaskDto(CrmTask t)
    {
        var status = CrmTaskStatuses.GetById(t.StatusId);
        return new CrmTaskDto
        {
            Id = t.Id,
            Title = t.Title,
            Description = t.Description,
            StatusId = t.StatusId,
            StatusName = status?.Description,
            StatusCss = status?.CssClass,
            DueDate = t.DueDate,
            ContactId = t.ContactId,
            ContactName = t.Contact?.FullName,
            TicketId = t.TicketId,
            DealId = t.DealId,
            AssignedToPersonnelId = t.AssignedToPersonnelId,
            AssignedToName = t.AssignedToPersonnel?.User?.FullName,
            CreatedByName = t.CreatedByPersonnel?.User?.FullName,
            CreatedAt = t.CreatedAt,
            CompletedAt = t.CompletedAt
        };
    }
}
