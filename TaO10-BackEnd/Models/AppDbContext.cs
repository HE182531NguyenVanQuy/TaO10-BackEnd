using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace TaO10_BackEnd.Models;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<BlogPost> BlogPosts { get; set; }

    public virtual DbSet<Document> Documents { get; set; }

    public virtual DbSet<Exam> Exams { get; set; }

    public virtual DbSet<ForumCategory> ForumCategories { get; set; }

    public virtual DbSet<ForumReply> ForumReplies { get; set; }

    public virtual DbSet<ForumThread> ForumThreads { get; set; }

    public virtual DbSet<GrammarLesson> GrammarLessons { get; set; }

    public virtual DbSet<GrammarTopic> GrammarTopics { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Package> Packages { get; set; }

    public virtual DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Question> Questions { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<Schedule> Schedules { get; set; }

    public virtual DbSet<Testimonial> Testimonials { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserAnswer> UserAnswers { get; set; }

    public virtual DbSet<UserExamAttempt> UserExamAttempts { get; set; }

    public virtual DbSet<UserPackage> UserPackages { get; set; }

    public virtual DbSet<UserProgress> UserProgresses { get; set; }

    public virtual DbSet<VocabularyTopic> VocabularyTopics { get; set; }

    public virtual DbSet<VocabularyWord> VocabularyWords { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var builder = new ConfigurationBuilder();
        builder.SetBasePath(Directory.GetCurrentDirectory());
        builder.AddJsonFile("appsettings.Development.json", true, true);
        var configuration = builder.Build();
        optionsBuilder.UseNpgsql(configuration.GetConnectionString("MyCnn"));

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("uuid-ossp");

        modelBuilder.Entity<BlogPost>(entity =>
        {
            entity.HasKey(e => e.BlogPostId).HasName("blog_posts_pkey");

            entity.ToTable("blog_posts");

            entity.Property(e => e.BlogPostId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("blog_post_id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.IsPublished)
                .HasDefaultValue(true)
                .HasColumnName("is_published");
            entity.Property(e => e.Meta).HasColumnName("meta");
            entity.Property(e => e.PublishedAt).HasColumnName("published_at");
            entity.Property(e => e.ReadTime)
                .HasMaxLength(50)
                .HasColumnName("read_time");
            entity.Property(e => e.ThumbClass)
                .HasMaxLength(100)
                .HasColumnName("thumb_class");
            entity.Property(e => e.Title)
                .HasMaxLength(500)
                .HasColumnName("title");
            entity.Property(e => e.ViewsCount)
                .HasDefaultValue(0)
                .HasColumnName("views_count");
        });

        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.DocumentId).HasName("documents_pkey");

            entity.ToTable("documents");

            entity.Property(e => e.DocumentId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("document_id");
            entity.Property(e => e.CoverGradient)
                .HasMaxLength(500)
                .HasColumnName("cover_gradient");
            entity.Property(e => e.CoverType)
                .HasMaxLength(100)
                .HasColumnName("cover_type");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.DocumentType)
                .HasMaxLength(100)
                .HasColumnName("document_type");
            entity.Property(e => e.DownloadsCount)
                .HasDefaultValue(0)
                .HasColumnName("downloads_count");
            entity.Property(e => e.FileType)
                .HasMaxLength(50)
                .HasColumnName("file_type");
            entity.Property(e => e.FileUrl)
                .HasMaxLength(500)
                .HasColumnName("file_url");
            entity.Property(e => e.Pages)
                .HasMaxLength(50)
                .HasColumnName("pages");
            entity.Property(e => e.Rating)
                .HasPrecision(3, 1)
                .HasColumnName("rating");
            entity.Property(e => e.Tag)
                .HasMaxLength(100)
                .HasColumnName("tag");
            entity.Property(e => e.Title)
                .HasMaxLength(500)
                .HasColumnName("title");
        });

        modelBuilder.Entity<Exam>(entity =>
        {
            entity.HasKey(e => e.ExamId).HasName("exams_pkey");

            entity.ToTable("exams");

            entity.Property(e => e.ExamId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("exam_id");
            entity.Property(e => e.AttemptsCount)
                .HasDefaultValue(0)
                .HasColumnName("attempts_count");
            entity.Property(e => e.CoverClass)
                .HasMaxLength(100)
                .HasColumnName("cover_class");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Difficulty)
                .HasMaxLength(50)
                .HasColumnName("difficulty");
            entity.Property(e => e.DurationMinutes).HasColumnName("duration_minutes");
            entity.Property(e => e.ExamType)
                .HasMaxLength(100)
                .HasColumnName("exam_type");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.PackageId).HasColumnName("package_id");
            entity.Property(e => e.QuestionsCount)
                .HasDefaultValue(0)
                .HasColumnName("questions_count");
            entity.Property(e => e.Tag)
                .HasMaxLength(100)
                .HasColumnName("tag");
            entity.Property(e => e.TagClass)
                .HasMaxLength(100)
                .HasColumnName("tag_class");
            entity.Property(e => e.Title)
                .HasMaxLength(500)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");
            entity.Property(e => e.ViewsCount)
                .HasDefaultValue(0)
                .HasColumnName("views_count");
            entity.Property(e => e.Year).HasColumnName("year");

            entity.HasOne(d => d.Package).WithMany(p => p.Exams)
                .HasForeignKey(d => d.PackageId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("exams_package_id_fkey");
        });

        modelBuilder.Entity<ForumCategory>(entity =>
        {
            entity.HasKey(e => e.ForumCategoryId).HasName("forum_categories_pkey");

            entity.ToTable("forum_categories");

            entity.Property(e => e.ForumCategoryId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("forum_category_id");
            entity.Property(e => e.Badge)
                .HasMaxLength(100)
                .HasColumnName("badge");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.RepliesCount)
                .HasDefaultValue(0)
                .HasColumnName("replies_count");
            entity.Property(e => e.ThreadsCount)
                .HasDefaultValue(0)
                .HasColumnName("threads_count");
        });

        modelBuilder.Entity<ForumReply>(entity =>
        {
            entity.HasKey(e => e.ForumReplyId).HasName("forum_replies_pkey");

            entity.ToTable("forum_replies");

            entity.Property(e => e.ForumReplyId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("forum_reply_id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.ForumThreadId).HasColumnName("forum_thread_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.ForumThread).WithMany(p => p.ForumReplies)
                .HasForeignKey(d => d.ForumThreadId)
                .HasConstraintName("forum_replies_forum_thread_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.ForumReplies)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("forum_replies_user_id_fkey");
        });

        modelBuilder.Entity<ForumThread>(entity =>
        {
            entity.HasKey(e => e.ForumThreadId).HasName("forum_threads_pkey");

            entity.ToTable("forum_threads");

            entity.Property(e => e.ForumThreadId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("forum_thread_id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.Excerpt).HasColumnName("excerpt");
            entity.Property(e => e.ForumCategoryId).HasColumnName("forum_category_id");
            entity.Property(e => e.IsHot)
                .HasDefaultValue(false)
                .HasColumnName("is_hot");
            entity.Property(e => e.IsPinned)
                .HasDefaultValue(false)
                .HasColumnName("is_pinned");
            entity.Property(e => e.RepliesCount)
                .HasDefaultValue(0)
                .HasColumnName("replies_count");
            entity.Property(e => e.Tags)
                .HasColumnType("jsonb")
                .HasColumnName("tags");
            entity.Property(e => e.Title)
                .HasMaxLength(500)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ViewsCount)
                .HasDefaultValue(0)
                .HasColumnName("views_count");

            entity.HasOne(d => d.ForumCategory).WithMany(p => p.ForumThreads)
                .HasForeignKey(d => d.ForumCategoryId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("forum_threads_forum_category_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.ForumThreads)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("forum_threads_user_id_fkey");
        });

        modelBuilder.Entity<GrammarLesson>(entity =>
        {
            entity.HasKey(e => e.GrammarLessonId).HasName("grammar_lessons_pkey");

            entity.ToTable("grammar_lessons");

            entity.Property(e => e.GrammarLessonId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("grammar_lesson_id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.GrammarTopicId).HasColumnName("grammar_topic_id");
            entity.Property(e => e.LessonType)
                .HasMaxLength(100)
                .HasColumnName("lesson_type");
            entity.Property(e => e.Level)
                .HasMaxLength(50)
                .HasColumnName("level");
            entity.Property(e => e.OrderIndex).HasColumnName("order_index");
            entity.Property(e => e.Title)
                .HasMaxLength(500)
                .HasColumnName("title");

            entity.HasOne(d => d.GrammarTopic).WithMany(p => p.GrammarLessons)
                .HasForeignKey(d => d.GrammarTopicId)
                .HasConstraintName("grammar_lessons_grammar_topic_id_fkey");
        });

        modelBuilder.Entity<GrammarTopic>(entity =>
        {
            entity.HasKey(e => e.GrammarTopicId).HasName("grammar_topics_pkey");

            entity.ToTable("grammar_topics");

            entity.Property(e => e.GrammarTopicId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("grammar_topic_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.LessonsCount)
                .HasDefaultValue(0)
                .HasColumnName("lessons_count");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("notifications_pkey");

            entity.ToTable("notifications");

            entity.Property(e => e.NotificationId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("notification_id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.IsRead)
                .HasDefaultValue(false)
                .HasColumnName("is_read");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("notifications_user_id_fkey");
        });

        modelBuilder.Entity<Package>(entity =>
        {
            entity.HasKey(e => e.PackageId).HasName("packages_pkey");

            entity.ToTable("packages");

            entity.Property(e => e.PackageId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("package_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.DurationDays).HasColumnName("duration_days");
            entity.Property(e => e.ExamLimit).HasColumnName("exam_limit");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Price).HasColumnName("price");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(e => e.ResetTokenId).HasName("password_reset_tokens_pkey");

            entity.ToTable("password_reset_tokens");

            entity.HasIndex(e => new { e.UserId, e.OtpCode }, "uk_user_otp").IsUnique();

            entity.Property(e => e.ResetTokenId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("reset_token_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiryTime).HasColumnName("expiry_time");
            entity.Property(e => e.IsUsed)
                .HasDefaultValue(false)
                .HasColumnName("is_used");
            entity.Property(e => e.OtpCode)
                .HasMaxLength(6)
                .HasColumnName("otp_code");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.PasswordResetTokens)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("password_reset_tokens_user_id_fkey");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("payments_pkey");

            entity.ToTable("payments");

            entity.Property(e => e.PaymentId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("payment_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpectedAmount).HasColumnName("expected_amount");
            entity.Property(e => e.ExpiredAt).HasColumnName("expired_at");
            entity.Property(e => e.PackageId).HasColumnName("package_id");
            entity.Property(e => e.PaidAt).HasColumnName("paid_at");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50)
                .HasColumnName("payment_method");
            entity.Property(e => e.ReceivedAmount).HasColumnName("received_amount");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'pending'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.TransactionCode)
                .HasMaxLength(100)
                .HasColumnName("transaction_code");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Package).WithMany(p => p.Payments)
                .HasForeignKey(d => d.PackageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("payments_package_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Payments)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("payments_user_id_fkey");
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasKey(e => e.QuestionId).HasName("questions_pkey");

            entity.ToTable("questions");

            entity.Property(e => e.QuestionId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("question_id");
            entity.Property(e => e.CorrectAnswer).HasColumnName("correct_answer");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.ExamId).HasColumnName("exam_id");
            entity.Property(e => e.Explanation).HasColumnName("explanation");
            entity.Property(e => e.OptionA).HasColumnName("option_a");
            entity.Property(e => e.OptionB).HasColumnName("option_b");
            entity.Property(e => e.OptionC).HasColumnName("option_c");
            entity.Property(e => e.OptionD).HasColumnName("option_d");
            entity.Property(e => e.Points)
                .HasPrecision(3, 1)
                .HasDefaultValueSql("0.25")
                .HasColumnName("points");
            entity.Property(e => e.QuestionNumber).HasColumnName("question_number");
            entity.Property(e => e.QuestionText).HasColumnName("question_text");
            entity.Property(e => e.Section)
                .HasMaxLength(100)
                .HasColumnName("section");

            entity.HasOne(d => d.Exam).WithMany(p => p.Questions)
                .HasForeignKey(d => d.ExamId)
                .HasConstraintName("questions_exam_id_fkey");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.RefreshTokenId).HasName("refresh_tokens_pkey");

            entity.ToTable("refresh_tokens");

            entity.Property(e => e.RefreshTokenId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("refresh_token_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.IsRevoked)
                .HasDefaultValue(false)
                .HasColumnName("is_revoked");
            entity.Property(e => e.RevokedAt).HasColumnName("revoked_at");
            entity.Property(e => e.Token)
                .HasMaxLength(500)
                .HasColumnName("token");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("refresh_tokens_user_id_fkey");
        });

        modelBuilder.Entity<Schedule>(entity =>
        {
            entity.HasKey(e => e.ScheduleId).HasName("schedules_pkey");

            entity.ToTable("schedules");

            entity.Property(e => e.ScheduleId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("schedule_id");
            entity.Property(e => e.BgClass)
                .HasMaxLength(100)
                .HasColumnName("bg_class");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.Day).HasColumnName("day");
            entity.Property(e => e.EventDate).HasColumnName("event_date");
            entity.Property(e => e.Month)
                .HasMaxLength(50)
                .HasColumnName("month");
            entity.Property(e => e.Time)
                .HasMaxLength(100)
                .HasColumnName("time");
            entity.Property(e => e.Title)
                .HasMaxLength(500)
                .HasColumnName("title");
        });

        modelBuilder.Entity<Testimonial>(entity =>
        {
            entity.HasKey(e => e.TestimonialId).HasName("testimonials_pkey");

            entity.ToTable("testimonials");

            entity.Property(e => e.TestimonialId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("testimonial_id");
            entity.Property(e => e.AvatarBg)
                .HasMaxLength(500)
                .HasColumnName("avatar_bg");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.Initial)
                .HasMaxLength(1)
                .HasColumnName("initial");
            entity.Property(e => e.IsFeatured)
                .HasDefaultValue(false)
                .HasColumnName("is_featured");
            entity.Property(e => e.Meta)
                .HasMaxLength(255)
                .HasColumnName("meta");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Rating).HasColumnName("rating");
            entity.Property(e => e.Text).HasColumnName("text");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("users_pkey");

            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "users_email_key").IsUnique();

            entity.HasIndex(e => e.Phone, "users_phone_key").IsUnique();

            entity.Property(e => e.UserId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("user_id");
            entity.Property(e => e.AvatarUrl)
                .HasMaxLength(500)
                .HasColumnName("avatar_url");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(255)
                .HasColumnName("full_name");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Location)
                .HasMaxLength(255)
                .HasColumnName("location");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.Phone)
                .HasMaxLength(15)
                .HasColumnName("phone");
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .HasDefaultValueSql("'customer'::character varying")
                .HasColumnName("role");
            entity.Property(e => e.TotalExams)
                .HasDefaultValue(0)
                .HasColumnName("total_exams");
            entity.Property(e => e.TotalScore)
                .HasDefaultValue(0)
                .HasColumnName("total_score");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<UserAnswer>(entity =>
        {
            entity.HasKey(e => e.UserAnswerId).HasName("user_answers_pkey");

            entity.ToTable("user_answers");

            entity.Property(e => e.UserAnswerId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("user_answer_id");
            entity.Property(e => e.AnsweredAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("answered_at");
            entity.Property(e => e.AttemptId).HasColumnName("attempt_id");
            entity.Property(e => e.IsCorrect).HasColumnName("is_correct");
            entity.Property(e => e.QuestionId).HasColumnName("question_id");
            entity.Property(e => e.UserAnswer1)
                .HasMaxLength(1)
                .HasColumnName("user_answer");

            entity.HasOne(d => d.Attempt).WithMany(p => p.UserAnswers)
                .HasForeignKey(d => d.AttemptId)
                .HasConstraintName("user_answers_attempt_id_fkey");

            entity.HasOne(d => d.Question).WithMany(p => p.UserAnswers)
                .HasForeignKey(d => d.QuestionId)
                .HasConstraintName("user_answers_question_id_fkey");
        });

        modelBuilder.Entity<UserExamAttempt>(entity =>
        {
            entity.HasKey(e => e.AttemptId).HasName("user_exam_attempts_pkey");

            entity.ToTable("user_exam_attempts");

            entity.HasIndex(e => new { e.UserId, e.ExamId, e.StartedAt }, "user_exam_attempts_user_id_exam_id_started_at_key").IsUnique();

            entity.Property(e => e.AttemptId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("attempt_id");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.CorrectAnswers).HasColumnName("correct_answers");
            entity.Property(e => e.ExamId).HasColumnName("exam_id");
            entity.Property(e => e.IsCompleted)
                .HasDefaultValue(false)
                .HasColumnName("is_completed");
            entity.Property(e => e.Score)
                .HasPrecision(5, 2)
                .HasColumnName("score");
            entity.Property(e => e.StartedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("started_at");
            entity.Property(e => e.TimeSpentMinutes).HasColumnName("time_spent_minutes");
            entity.Property(e => e.TotalQuestions).HasColumnName("total_questions");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Exam).WithMany(p => p.UserExamAttempts)
                .HasForeignKey(d => d.ExamId)
                .HasConstraintName("user_exam_attempts_exam_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.UserExamAttempts)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("user_exam_attempts_user_id_fkey");
        });

        modelBuilder.Entity<UserPackage>(entity =>
        {
            entity.HasKey(e => e.UserPackageId).HasName("user_packages_pkey");

            entity.ToTable("user_packages");

            entity.Property(e => e.UserPackageId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("user_package_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.PackageId).HasColumnName("package_id");
            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.RemainingExams).HasColumnName("remaining_exams");
            entity.Property(e => e.StartDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("start_date");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Package).WithMany(p => p.UserPackages)
                .HasForeignKey(d => d.PackageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("user_packages_package_id_fkey");

            entity.HasOne(d => d.Payment).WithMany(p => p.UserPackages)
                .HasForeignKey(d => d.PaymentId)
                .HasConstraintName("user_packages_payment_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.UserPackages)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("user_packages_user_id_fkey");
        });

        modelBuilder.Entity<UserProgress>(entity =>
        {
            entity.HasKey(e => e.ProgressId).HasName("user_progress_pkey");

            entity.ToTable("user_progress");

            entity.HasIndex(e => new { e.UserId, e.ItemType, e.ItemId }, "user_progress_user_id_item_type_item_id_key").IsUnique();

            entity.Property(e => e.ProgressId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("progress_id");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.ItemType)
                .HasMaxLength(50)
                .HasColumnName("item_type");
            entity.Property(e => e.LastAccessed)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("last_accessed");
            entity.Property(e => e.ProgressPercentage)
                .HasDefaultValue(0)
                .HasColumnName("progress_percentage");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.UserProgresses)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("user_progress_user_id_fkey");
        });

        modelBuilder.Entity<VocabularyTopic>(entity =>
        {
            entity.HasKey(e => e.VocabularyTopicId).HasName("vocabulary_topics_pkey");

            entity.ToTable("vocabulary_topics");

            entity.Property(e => e.VocabularyTopicId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("vocabulary_topic_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.WordsCount)
                .HasDefaultValue(0)
                .HasColumnName("words_count");
        });

        modelBuilder.Entity<VocabularyWord>(entity =>
        {
            entity.HasKey(e => e.VocabularyWordId).HasName("vocabulary_words_pkey");

            entity.ToTable("vocabulary_words");

            entity.Property(e => e.VocabularyWordId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("vocabulary_word_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.Difficulty)
                .HasMaxLength(50)
                .HasColumnName("difficulty");
            entity.Property(e => e.Example).HasColumnName("example");
            entity.Property(e => e.ExampleTranslation).HasColumnName("example_translation");
            entity.Property(e => e.Meaning).HasColumnName("meaning");
            entity.Property(e => e.Phonetic)
                .HasMaxLength(255)
                .HasColumnName("phonetic");
            entity.Property(e => e.VocabularyTopicId).HasColumnName("vocabulary_topic_id");
            entity.Property(e => e.Word)
                .HasMaxLength(255)
                .HasColumnName("word");
            entity.Property(e => e.WordType)
                .HasMaxLength(50)
                .HasColumnName("word_type");

            entity.HasOne(d => d.VocabularyTopic).WithMany(p => p.VocabularyWords)
                .HasForeignKey(d => d.VocabularyTopicId)
                .HasConstraintName("vocabulary_words_vocabulary_topic_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
