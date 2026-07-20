using Microsoft.EntityFrameworkCore;
using TaO10_BackEnd.Common;
using TaO10_BackEnd.Models;

namespace TaO10_BackEnd.Repositories;

/// <summary>
/// Implementation of Exam repository
/// </summary>
public class ExamRepository : Repository<Exam>, IExamRepository
{
    /// <summary>
    /// Initializes a new instance of the ExamRepository class
    /// </summary>
    public ExamRepository(AppDbContext context) : base(context) { }

    /// <summary>
    /// Gets an exam by ID with all related questions
    /// </summary>
    public async Task<Exam?> GetByIdWithQuestionsAsync(Guid examId)
    {
        return await _dbSet
            .Include(e => e.Status)
            .Include(e => e.Questions.Where(q =>
                q.Status.EntityType == AppStatusCodes.EntityTypes.Question &&
                q.Status.Code == AppStatusCodes.Questions.Active))
            .FirstOrDefaultAsync(e => e.ExamId == examId);
    }

    /// <summary>
    /// Gets all active exams with pagination
    /// </summary>
    public async Task<(List<Exam> Exams, int TotalCount)> GetActiveExamsAsync(int pageNumber = 1, int pageSize = 10)
    {
        var query = _dbSet
            .Include(e => e.Status)
            .Where(e =>
                e.Status.EntityType == AppStatusCodes.EntityTypes.Exam &&
                e.Status.Code == AppStatusCodes.Exams.Active)
            .OrderByDescending(e => e.CreatedAt)
            .ThenBy(e => e.ExamId);

        int totalCount = await query.CountAsync();
        var exams = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (exams, totalCount);
    }

    /// <summary>
    /// Checks if an exam exists
    /// </summary>
    public async Task<bool> ExamExistsAsync(Guid examId)
    {
        return await _dbSet.AnyAsync(e => e.ExamId == examId);
    }

    /// <summary>
    /// Gets an exam with status information
    /// </summary>
    public async Task<Exam?> GetByIdWithStatusAsync(Guid examId)
    {
        return await _dbSet
            .Include(e => e.Status)
            .FirstOrDefaultAsync(e => e.ExamId == examId);
    }

    /// <summary>
    /// Increments the views count for an exam
    /// </summary>
    public async Task IncrementViewsCountAsync(Guid examId)
    {
        var exam = await GetByIdAsync(examId);
        if (exam != null)
        {
            exam.ViewsCount = (exam.ViewsCount ?? 0) + 1;
            await UpdateAsync(exam);
            await SaveChangesAsync();
        }
    }

    /// <summary>
    /// Increments the attempts count for an exam
    /// </summary>
    public async Task IncrementAttemptsCountAsync(Guid examId)
    {
        var exam = await GetByIdAsync(examId);
        if (exam != null)
        {
            exam.AttemptsCount = (exam.AttemptsCount ?? 0) + 1;
            await UpdateAsync(exam);
            await SaveChangesAsync();
        }
    }
}
