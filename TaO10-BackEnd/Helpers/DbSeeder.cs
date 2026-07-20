using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TaO10_BackEnd.Models;

namespace TaO10_BackEnd.Helpers
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            // Ensure Database is created
            await context.Database.EnsureCreatedAsync();

            // 1. SEED STATUSES
            var requiredStatuses = new List<Status>
            {
                new Status { StatusId = Guid.NewGuid(), EntityType = "User", Code = "ACTIVE", DisplayName = "Hoạt động", Description = "Tài khoản đang hoạt động bình thường" },
                new Status { StatusId = Guid.NewGuid(), EntityType = "User", Code = "BLOCKED", DisplayName = "Bị khóa", Description = "Tài khoản bị khóa do vi phạm" },
                
                new Status { StatusId = Guid.NewGuid(), EntityType = "Package", Code = "ACTIVE", DisplayName = "Đang bán", Description = "Gói học tập đang mở bán" },
                new Status { StatusId = Guid.NewGuid(), EntityType = "Package", Code = "INACTIVE", DisplayName = "Ngừng bán", Description = "Gói học tập tạm ngừng bán" },
                
                new Status { StatusId = Guid.NewGuid(), EntityType = "Payment", Code = "PENDING", DisplayName = "Chờ thanh toán", Description = "Giao dịch đang chờ khách thanh toán" },
                new Status { StatusId = Guid.NewGuid(), EntityType = "Payment", Code = "SUCCESS", DisplayName = "Thành công", Description = "Thanh toán thành công" },
                new Status { StatusId = Guid.NewGuid(), EntityType = "Payment", Code = "FAILED", DisplayName = "Thất bại", Description = "Thanh toán thất bại hoặc hủy" },
                
                new Status { StatusId = Guid.NewGuid(), EntityType = "UserPackage", Code = "ACTIVE", DisplayName = "Đang hoạt động", Description = "Gói dịch vụ người dùng đang hoạt động" },
                new Status { StatusId = Guid.NewGuid(), EntityType = "UserPackage", Code = "EXPIRED", DisplayName = "Hết hạn", Description = "Gói dịch vụ đã hết hạn sử dụng" },
                
                new Status { StatusId = Guid.NewGuid(), EntityType = "Exam", Code = "ACTIVE", DisplayName = "Đang mở", Description = "Đề thi đang hiển thị để làm" },
                new Status { StatusId = Guid.NewGuid(), EntityType = "Exam", Code = "INACTIVE", DisplayName = "Bản nháp", Description = "Đề thi nháp, chưa công bố" },
                
                new Status { StatusId = Guid.NewGuid(), EntityType = "Question", Code = "ACTIVE", DisplayName = "Hoạt động", Description = "Câu hỏi đang sử dụng" },
                
                new Status { StatusId = Guid.NewGuid(), EntityType = "ForumThread", Code = "ACTIVE", DisplayName = "Hoạt động", Description = "Chủ đề thảo luận hoạt động bình thường" },
                new Status { StatusId = Guid.NewGuid(), EntityType = "ForumThread", Code = "LOCKED", DisplayName = "Bị khóa", Description = "Chủ đề thảo luận bị khóa" },
                
                new Status { StatusId = Guid.NewGuid(), EntityType = "BlogPost", Code = "PUBLISHED", DisplayName = "Đã xuất bản", Description = "Bài viết blog hiển thị cho người đọc" },
                new Status { StatusId = Guid.NewGuid(), EntityType = "BlogPost", Code = "DRAFT", DisplayName = "Bản nháp", Description = "Bài viết blog dạng nháp" },
                new Status { StatusId = Guid.NewGuid(), EntityType = "UserExamAttempt", Code = "ACTIVE", DisplayName = "Đang làm", Description = "Đang làm bài thi" },
                new Status { StatusId = Guid.NewGuid(), EntityType = "UserExamAttempt", Code = "COMPLETED", DisplayName = "Hoàn thành", Description = "Đã hoàn thành bài thi" }
            };

            foreach (var status in requiredStatuses)
            {
                var exists = await context.Statuses.AnyAsync(s => s.EntityType == status.EntityType && s.Code == status.Code);
                if (!exists)
                {
                    context.Statuses.Add(status);
                }
            }
            await context.SaveChangesAsync();

            // Reference Status IDs
            var activeUserStatusId = (await context.Statuses.FirstAsync(s => s.EntityType == "User" && s.Code == "ACTIVE")).StatusId;
            var activePkgStatusId = (await context.Statuses.FirstAsync(s => s.EntityType == "Package" && s.Code == "ACTIVE")).StatusId;
            var activeExamStatusId = (await context.Statuses.FirstAsync(s => s.EntityType == "Exam" && s.Code == "ACTIVE")).StatusId;
            var activeQuestionStatusId = (await context.Statuses.FirstAsync(s => s.EntityType == "Question" && s.Code == "ACTIVE")).StatusId;
            var activeThreadStatusId = (await context.Statuses.FirstAsync(s => s.EntityType == "ForumThread" && s.Code == "ACTIVE")).StatusId;
            var publishedBlogStatusId = (await context.Statuses.FirstAsync(s => s.EntityType == "BlogPost" && s.Code == "PUBLISHED")).StatusId;

            // 3. SEED 5 PACKAGES: Dùng thử, Cấp tốc, Nâng cao, Chuyên sâu, Premium
            var packagesCount = await context.Packages.CountAsync();
            var hasChuyenSau = await context.Packages.AnyAsync(p => p.Name == "Gói Chuyên Sâu");
            var shouldReSeedPackages = packagesCount != 5 || !hasChuyenSau;

            if (shouldReSeedPackages)
            {
                // Clear old package exams and packages first
                var oldPackageExams = await context.PackageExams.ToListAsync();
                context.PackageExams.RemoveRange(oldPackageExams);

                var oldPackages = await context.Packages.ToListAsync();
                context.Packages.RemoveRange(oldPackages);
                await context.SaveChangesAsync();

                var packages = new List<Package>
                {
                    new Package
                    {
                        PackageId = Guid.NewGuid(),
                        Name = "Gói Dùng Thử",
                        Description = "Trải nghiệm miễn phí 3 ngày với 1 bộ đề thi đầy đủ tính năng.",
                        Price = 0,
                        DurationTime = 3,
                        StatusId = activePkgStatusId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Package
                    {
                        PackageId = Guid.NewGuid(),
                        Name = "Gói Cấp Tốc",
                        Description = "Khóa học 1 tháng với 30 bộ đề thi mới nhất theo đúng chuẩn đề thi thực tế.",
                        Price = 129000,
                        DurationTime = 30,
                        StatusId = activePkgStatusId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Package
                    {
                        PackageId = Guid.NewGuid(),
                        Name = "Gói Nâng Cao",
                        Description = "Khóa học 3 tháng với 65 bộ đề thi toàn diện, bảng điều khiển cá nhân và dự đoán kết quả.",
                        Price = 269000,
                        DurationTime = 90,
                        StatusId = activePkgStatusId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Package
                    {
                        PackageId = Guid.NewGuid(),
                        Name = "Gói Chuyên Sâu",
                        Description = "Khóa học 6 tháng với 150+ bộ đề thi chất lượng cao và phân tích kết quả chuyên sâu.",
                        Price = 469000,
                        DurationTime = 180,
                        StatusId = activePkgStatusId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Package
                    {
                        PackageId = Guid.NewGuid(),
                        Name = "Gói Premium",
                        Description = "Khóa học 12 tháng không giới hạn đề thi, đầy đủ tính năng cao cấp nhất.",
                        Price = 899000,
                        DurationTime = 365,
                        StatusId = activePkgStatusId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                };

                await context.Packages.AddRangeAsync(packages);
                await context.SaveChangesAsync();
            }


            // 5. SEED FORUM CATEGORIES
            if (!await context.ForumCategories.AnyAsync())
            {
                var cat1 = new ForumCategory { ForumCategoryId = Guid.NewGuid(), Name = "TOEIC", Description = "Khu vực trao đổi kiến thức, tài liệu và mẹo thi TOEIC đạt điểm cao.", ThreadsCount = 1, RepliesCount = 1, Badge = "TOEIC Master", CreatedAt = DateTime.UtcNow };
                var cat2 = new ForumCategory { ForumCategoryId = Guid.NewGuid(), Name = "IELTS", Description = "Cộng đồng luyện thi IELTS 4 kỹ năng Nghe - Nói - Đọc - Viết.", ThreadsCount = 0, RepliesCount = 0, Badge = "IELTS 8.0+", CreatedAt = DateTime.UtcNow };
                var cat3 = new ForumCategory { ForumCategoryId = Guid.NewGuid(), Name = "Kinh nghiệm học tập", Description = "Chia sẻ phương pháp học tiếng Anh hiệu quả, tài liệu quý báu.", ThreadsCount = 1, RepliesCount = 0, Badge = "Kinh Nghiệm", CreatedAt = DateTime.UtcNow };

                await context.ForumCategories.AddRangeAsync(cat1, cat2, cat3);
                await context.SaveChangesAsync();
            }

            // 6. SEED BLOG POSTS
            if (!await context.BlogPosts.AnyAsync())
            {
                var blogs = new List<BlogPost>
                {
                    new BlogPost
                    {
                        BlogPostId = Guid.NewGuid(),
                        Title = "Cấu trúc đề thi Tiếng Anh vào lớp 10 năm 2026 – Những thay đổi quan trọng cần biết",
                        Content = "Kỳ thi tuyển sinh lớp 10 THPT môn Tiếng Anh năm 2026 dự kiến sẽ có một số điều chỉnh về phân bổ câu hỏi. Cụ thể, các câu hỏi ngữ pháp sẽ giảm bớt và tập trung nhiều hơn vào đọc hiểu...",
                        Meta = "📅 15/03/2026 · ⏱ 5 phút đọc · 👁 8.2k lượt xem",
                        ViewsCount = 8200,
                        StatusId = publishedBlogStatusId,
                        CreatedAt = DateTime.UtcNow,
                        PublishedAt = DateTime.UtcNow
                    },
                    new BlogPost
                    {
                        BlogPostId = Guid.NewGuid(),
                        Title = "10 mẹo làm bài thi Tiếng Anh đạt điểm cao cho học sinh lớp 9",
                        Content = "Để đạt điểm cao trong kỳ thi Tiếng Anh, học sinh cần rèn luyện phản xạ đọc đề, phân bổ thời gian hợp lý, không bỏ sót các câu hỏi trắc nghiệm dễ và luôn kiểm tra kỹ đáp án...",
                        Meta = "📅 12/03/2026 · ⏱ 7 phút đọc · 👁 6.5k lượt xem",
                        ViewsCount = 6500,
                        StatusId = publishedBlogStatusId,
                        CreatedAt = DateTime.UtcNow,
                        PublishedAt = DateTime.UtcNow
                    }
                };

                await context.BlogPosts.AddRangeAsync(blogs);
                await context.SaveChangesAsync();
            }
        }
    }
}
