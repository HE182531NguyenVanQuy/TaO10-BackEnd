using System.Text.Json;
using TaO10_BackEnd.Common;

namespace TaO10_BackEnd.Services;

public static class AiRoadmapFallbackLibrary
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly object SyncRoot = new();
    private static Queue<int> _remainingIndexes = CreateShuffledIndexes();
    private static readonly IReadOnlyList<string> DefaultPracticeTypeOrder =
    [
        QuestionTypeConstants.MultipleChoice,
        QuestionTypeConstants.Reading,
        QuestionTypeConstants.Cloze,
        QuestionTypeConstants.ErrorCorrection,
        QuestionTypeConstants.Pronunciation,
        QuestionTypeConstants.Stress,
        QuestionTypeConstants.Communication,
        QuestionTypeConstants.Synonym,
        QuestionTypeConstants.Antonym,
        QuestionTypeConstants.Rewrite,
        QuestionTypeConstants.RewriteWithGivenWords,
        QuestionTypeConstants.SentenceInsertion,
        QuestionTypeConstants.SignNotice,
        QuestionTypeConstants.Vocabulary,
        QuestionTypeConstants.ArrangeCompleteParagraph
    ];

    private static readonly IReadOnlyList<string> RoadmapJsonTemplates =
    [
        """
        {"summary":"Bạn cần củng cố nền tảng ngữ pháp và tăng độ chính xác khi đọc đề. Lộ trình 3 tuần sẽ đi từ nhận diện lỗi, luyện dạng câu trọng tâm đến làm bài tốc độ.","strengths":["Có khả năng hoàn thành bài thi đúng cấu trúc","Đã có nền tảng từ vựng cơ bản"],"weaknesses":["Ngữ pháp","Đọc hiểu"],"weeks":[{"title":"Tuần 1: Củng cố chọn đáp án đúng","goal":"Giảm lỗi ngữ pháp và từ loại trong câu ngắn.","tasks":["Ôn thì động từ, mệnh đề quan hệ và câu điều kiện","Làm 10 câu chọn đáp án đúng mỗi ngày","Ghi lại 5 cấu trúc sai thường gặp"],"practiceType":"Chọn đáp án đúng"},{"title":"Tuần 2: Tăng tốc đọc hiểu","goal":"Nắm ý chính và tìm thông tin nhanh hơn.","tasks":["Đọc đoạn văn ngắn và gạch keyword","Luyện câu hỏi main idea, reference, synonym","Tóm tắt mỗi bài đọc bằng 2 câu"],"practiceType":"Đọc hiểu"},{"title":"Tuần 3: Tổng hợp lỗi yếu","goal":"Kết hợp ngữ pháp và đọc hiểu trong điều kiện thời gian.","tasks":["Làm xen kẽ chọn đáp án đúng và đọc hiểu","Chấm lỗi theo từng type","Làm lại các câu đã sai sau 24 giờ"],"practiceType":"Chọn đáp án đúng"}],"dailyTime":"45 phút/ngày","nextAction":"Bắt đầu với 10 câu Chọn đáp án đúng"}
        """,
        """
        {"summary":"Điểm yếu chính nằm ở phát âm và trọng âm. Kế hoạch này giúp bạn nhận diện âm khác biệt, vị trí nhấn âm và áp dụng vào bài thi nhanh hơn.","strengths":["Có ý thức phân loại dạng câu","Hoàn thành được phần trắc nghiệm cơ bản"],"weaknesses":["Phát âm","Trọng âm"],"weeks":[{"title":"Tuần 1: Nhận diện phát âm","goal":"Phân biệt các âm đuôi, nguyên âm và phụ âm dễ nhầm.","tasks":["Ôn nhóm âm /s/, /z/, /iz/ và ed endings","Làm 10 câu phát âm mỗi ngày","Đọc to đáp án đúng để nhớ âm"],"practiceType":"Phát âm"},{"title":"Tuần 2: Trọng âm 2-3 âm tiết","goal":"Nhận ra quy luật trọng âm thường gặp.","tasks":["Ôn trọng âm danh từ, động từ, tính từ","Luyện 10 câu trọng âm mỗi ngày","Tạo bảng từ sai và đọc lại cuối ngày"],"practiceType":"Trọng âm"},{"title":"Tuần 3: Trộn phát âm và trọng âm","goal":"Tăng tốc xử lý 4 câu đầu đề thi.","tasks":["Làm xen kẽ phát âm và trọng âm","Canh thời gian tối đa 3 phút cho 4 câu","Ôn lại tất cả từ đã sai"],"practiceType":"Phát âm"}],"dailyTime":"30 phút/ngày","nextAction":"Luyện 10 câu Phát âm trước"}
        """,
        """
        {"summary":"Bạn cần cải thiện phần giao tiếp và chọn đáp án theo ngữ cảnh. Lộ trình tập trung vào phản xạ chọn câu đáp phù hợp và loại bẫy lịch sự.","strengths":["Có vốn từ giao tiếp thông dụng","Biết loại trừ đáp án vô lý"],"weaknesses":["Giao tiếp","Chọn đáp án đúng"],"weeks":[{"title":"Tuần 1: Mẫu câu giao tiếp","goal":"Nắm các phản hồi đồng ý, từ chối, đề nghị, cảm ơn.","tasks":["Học 15 mẫu câu giao tiếp phổ biến","Làm 10 câu giao tiếp mỗi ngày","Ghi chú cặp câu hỏi - câu đáp"],"practiceType":"Giao tiếp"},{"title":"Tuần 2: Ngữ cảnh trong câu","goal":"Chọn đáp án phù hợp nghĩa và sắc thái.","tasks":["Ôn cụm động từ và liên từ","Luyện chọn đáp án đúng theo ngữ cảnh","Giải thích vì sao 3 đáp án còn lại sai"],"practiceType":"Chọn đáp án đúng"},{"title":"Tuần 3: Luyện phản xạ","goal":"Rút ngắn thời gian làm dạng giao tiếp và ngữ cảnh.","tasks":["Làm bài theo set 10 câu","Chấm ngay và đọc giải thích","Làm lại câu sai không nhìn đáp án"],"practiceType":"Giao tiếp"}],"dailyTime":"35 phút/ngày","nextAction":"Luyện 10 câu Giao tiếp"}
        """,
        """
        {"summary":"Bạn đang mất điểm ở từ đồng nghĩa/trái nghĩa. Cần mở rộng vốn từ theo ngữ cảnh thay vì học rời rạc.","strengths":["Nhận biết được keyword trong câu","Có nền tảng từ vựng phổ thông"],"weaknesses":["Từ đồng nghĩa","Từ trái nghĩa"],"weeks":[{"title":"Tuần 1: Từ đồng nghĩa theo ngữ cảnh","goal":"Chọn từ gần nghĩa dựa trên câu chứa từ.","tasks":["Học từ theo cặp nghĩa","Làm 10 câu từ đồng nghĩa mỗi ngày","Viết lại câu với từ thay thế"],"practiceType":"Từ đồng nghĩa"},{"title":"Tuần 2: Từ trái nghĩa và bẫy phủ định","goal":"Nhận diện từ trái nghĩa trong câu có sắc thái phủ định.","tasks":["Ôn tính từ/trạng từ trái nghĩa thường gặp","Gạch chân từ khóa trước khi chọn","Làm 10 câu từ trái nghĩa mỗi ngày"],"practiceType":"Từ trái nghĩa"},{"title":"Tuần 3: Tổng hợp synonym/antonym","goal":"Tăng tốc chọn đáp án dựa trên nghĩa toàn câu.","tasks":["Làm xen kẽ đồng nghĩa và trái nghĩa","Tạo sổ 30 từ đã sai","Ôn lại bằng flashcard"],"practiceType":"Từ đồng nghĩa"}],"dailyTime":"40 phút/ngày","nextAction":"Bắt đầu với Từ đồng nghĩa"}
        """,
        """
        {"summary":"Bạn cần luyện đọc hiểu - điền từ vì đây là dạng yêu cầu cả ngữ pháp, từ vựng và mạch văn. Lộ trình đi từ liên kết câu đến chọn từ phù hợp.","strengths":["Có khả năng đọc đoạn văn ngắn","Biết dựa vào từ xung quanh chỗ trống"],"weaknesses":["Đọc hiểu - điền từ","Chọn đáp án đúng"],"weeks":[{"title":"Tuần 1: Xác định loại từ","goal":"Nhận ra chỗ trống cần danh từ, động từ, tính từ hay trạng từ.","tasks":["Ôn dấu hiệu loại từ","Làm 10 câu đọc hiểu - điền từ","Ghi chú từ đứng trước/sau chỗ trống"],"practiceType":"Đọc hiểu - điền từ"},{"title":"Tuần 2: Liên kết ý trong đoạn","goal":"Chọn đáp án theo mạch câu và liên từ.","tasks":["Ôn however, therefore, although, because","Tóm tắt ý từng câu trong đoạn","Làm bài cloze theo thời gian"],"practiceType":"Đọc hiểu - điền từ"},{"title":"Tuần 3: Tổng hợp cloze test","goal":"Hoàn thành đoạn điền từ ổn định hơn.","tasks":["Làm 2 set cloze mỗi ngày","Chấm và phân loại lỗi","Ôn lại collocation sai"],"practiceType":"Đọc hiểu - điền từ"}],"dailyTime":"45 phút/ngày","nextAction":"Luyện Đọc hiểu - điền từ"}
        """,
        """
        {"summary":"Bạn cần tập trung vào viết lại câu. Lộ trình này ôn các cấu trúc chuyển đổi thường gặp và giúp chọn đáp án gần nghĩa chính xác hơn.","strengths":["Nắm được ý chính của câu gốc","Có nền tảng ngữ pháp cơ bản"],"weaknesses":["Viết lại câu (gần nghĩa)","Viết lại câu (từ cho sẵn)"],"weeks":[{"title":"Tuần 1: Câu gần nghĩa","goal":"Nhận diện cấu trúc tương đương giữa hai câu.","tasks":["Ôn reported speech, passive voice, comparison","Làm 10 câu viết lại gần nghĩa mỗi ngày","Gạch phần nghĩa không được đổi"],"practiceType":"Viết lại câu (gần nghĩa)"},{"title":"Tuần 2: Từ cho sẵn","goal":"Dùng đúng cụm từ gợi ý để hoàn thành câu.","tasks":["Ôn cấu trúc too/enough, so/such, wish","Làm 10 câu từ cho sẵn mỗi ngày","So sánh đáp án với câu gốc"],"practiceType":"Viết lại câu (từ cho sẵn)"},{"title":"Tuần 3: Tổng hợp viết lại câu","goal":"Giảm lỗi sai do đổi thì, đổi chủ ngữ và sai nghĩa.","tasks":["Làm xen kẽ hai dạng viết lại","Ghi chú cấu trúc sai nhiều nhất","Làm lại câu sai sau 1 ngày"],"practiceType":"Viết lại câu (gần nghĩa)"}],"dailyTime":"40 phút/ngày","nextAction":"Luyện Viết lại câu (gần nghĩa)"}
        """,
        """
        {"summary":"Bạn cần cải thiện tìm lỗi sai, đặc biệt lỗi về thì, dạng động từ và giới từ. Kế hoạch sẽ giúp bạn soi câu theo từng lớp lỗi.","strengths":["Đọc được cấu trúc câu cơ bản","Có khả năng phát hiện lỗi rõ ràng"],"weaknesses":["Tìm lỗi sai","Ngữ pháp"],"weeks":[{"title":"Tuần 1: Lỗi động từ","goal":"Tìm lỗi về thì, chia động từ và verb form.","tasks":["Ôn thì hiện tại, quá khứ, hoàn thành","Làm 10 câu tìm lỗi sai mỗi ngày","Gạch chủ ngữ và động từ chính"],"practiceType":"Tìm lỗi sai"},{"title":"Tuần 2: Lỗi từ loại và giới từ","goal":"Nhận diện sai adjective/adverb/preposition.","tasks":["Ôn vị trí từ loại trong câu","Lập bảng giới từ hay đi kèm","Giải thích lỗi trước khi xem đáp án"],"practiceType":"Tìm lỗi sai"},{"title":"Tuần 3: Tổng hợp error correction","goal":"Tăng tốc tìm lỗi trong câu dài.","tasks":["Làm set 10 câu có bấm giờ","Phân loại lỗi sau khi chấm","Ôn lại 3 nhóm lỗi yếu nhất"],"practiceType":"Tìm lỗi sai"}],"dailyTime":"35 phút/ngày","nextAction":"Luyện Tìm lỗi sai"}
        """,
        """
        {"summary":"Bạn cần luyện điền câu vào đoạn văn. Trọng tâm là nhận diện mạch ý, đại từ tham chiếu và câu nối đoạn.","strengths":["Hiểu được ý từng câu riêng lẻ","Biết tìm keyword lặp lại"],"weaknesses":["Điền câu vào đoạn văn","Đọc hiểu"],"weeks":[{"title":"Tuần 1: Mạch ý đoạn văn","goal":"Nhận ra câu trước - câu sau liên kết thế nào.","tasks":["Gạch keyword ở câu trước và sau chỗ trống","Tìm đại từ thay thế this, they, it","Làm 10 câu điền câu vào đoạn văn"],"practiceType":"Điền câu vào đoạn văn"},{"title":"Tuần 2: Câu chuyển ý","goal":"Chọn câu nối phù hợp với however, therefore, besides.","tasks":["Ôn liên từ và trạng từ nối","Tóm tắt vai trò từng câu trong đoạn","Luyện dạng đoạn văn dài hơn"],"practiceType":"Điền câu vào đoạn văn"},{"title":"Tuần 3: Đọc hiểu hỗ trợ","goal":"Kết hợp kỹ năng đọc hiểu để chọn câu chính xác.","tasks":["Làm 1 bài đọc hiểu và 1 set điền câu mỗi ngày","Chấm lỗi theo keyword bị bỏ sót","Làm lại câu sai"],"practiceType":"Đọc hiểu"}],"dailyTime":"45 phút/ngày","nextAction":"Luyện Điền câu vào đoạn văn"}
        """,
        """
        {"summary":"Bạn cần luyện dạng biển báo/thông báo để đọc nhanh thông tin ngắn và chọn ý đúng. Đây là dạng dễ lấy điểm nếu quen keyword.","strengths":["Đọc được thông tin ngắn","Biết loại trừ đáp án không liên quan"],"weaknesses":["Biển báo/Thông báo","Đọc hiểu"],"weeks":[{"title":"Tuần 1: Từ khóa trong thông báo","goal":"Nắm các từ chỉ cấm, yêu cầu, cảnh báo, hướng dẫn.","tasks":["Học nhóm từ must, should, allowed, prohibited","Làm 10 câu biển báo/thông báo","Gạch từ thể hiện mục đích thông báo"],"practiceType":"Biển báo/Thông báo"},{"title":"Tuần 2: Suy luận ý ngắn","goal":"Hiểu thông điệp ẩn sau câu ngắn.","tasks":["Đọc kỹ đối tượng nhận thông báo","So sánh từng đáp án với nội dung gốc","Ghi lại mẫu câu thông báo phổ biến"],"practiceType":"Biển báo/Thông báo"},{"title":"Tuần 3: Kết hợp đọc nhanh","goal":"Tăng tốc nhưng vẫn chính xác.","tasks":["Làm 10 câu trong 8 phút","Chấm ngay và đọc giải thích","Ôn lại từ khóa sai"],"practiceType":"Biển báo/Thông báo"}],"dailyTime":"25 phút/ngày","nextAction":"Luyện Biển báo/Thông báo"}
        """,
        """
        {"summary":"Bạn cần học cân bằng giữa ngữ pháp, đọc hiểu và viết lại câu. Lộ trình này chia đều các dạng để cải thiện điểm tổng.","strengths":["Không bỏ trống nhiều câu","Có thể theo lộ trình đều đặn"],"weaknesses":["Chọn đáp án đúng","Đọc hiểu","Viết lại câu (gần nghĩa)"],"weeks":[{"title":"Tuần 1: Ngữ pháp nền","goal":"Ổn định phần chọn đáp án đúng.","tasks":["Ôn 5 chủ điểm ngữ pháp hay gặp","Làm 10 câu chọn đáp án đúng mỗi ngày","Ghi lại cấu trúc sai"],"practiceType":"Chọn đáp án đúng"},{"title":"Tuần 2: Đọc hiểu","goal":"Tăng khả năng tìm thông tin và suy luận.","tasks":["Làm 1 bài đọc ngắn mỗi ngày","Gạch keyword trước khi chọn","Tóm tắt đáp án sai"],"practiceType":"Đọc hiểu"},{"title":"Tuần 3: Viết lại câu","goal":"Chốt điểm phần biến đổi câu.","tasks":["Ôn câu bị động, so sánh, reported speech","Làm 10 câu viết lại","Tạo bảng cấu trúc tương đương"],"practiceType":"Viết lại câu (gần nghĩa)"}],"dailyTime":"50 phút/ngày","nextAction":"Bắt đầu luyện Chọn đáp án đúng"}
        """,
        """
        {"summary":"Bạn làm chưa ổn định ở nhóm câu đầu đề thi. Hãy ưu tiên phát âm và trọng âm để lấy điểm nhanh, sau đó mới chuyển sang câu ngữ pháp.","strengths":["Có khả năng học theo nhóm từ","Biết nhận diện dạng bài"],"weaknesses":["Phát âm","Trọng âm","Chọn đáp án đúng"],"weeks":[{"title":"Tuần 1: Phát âm cơ bản","goal":"Không mất điểm ở âm đuôi và nguyên âm phổ biến.","tasks":["Ôn ed endings và s/es endings","Làm 10 câu phát âm mỗi ngày","Nghe và đọc lại từ sai"],"practiceType":"Phát âm"},{"title":"Tuần 2: Trọng âm","goal":"Nắm quy tắc stress thông dụng.","tasks":["Ôn trọng âm từ 2-4 âm tiết","Làm 10 câu trọng âm mỗi ngày","Đánh dấu âm tiết chính"],"practiceType":"Trọng âm"},{"title":"Tuần 3: Ngữ pháp nhanh","goal":"Chuyển sang phần chọn đáp án đúng để tăng điểm.","tasks":["Ôn thì và mệnh đề quan hệ","Làm 10 câu chọn đáp án đúng","Chấm lỗi theo chủ điểm"],"practiceType":"Chọn đáp án đúng"}],"dailyTime":"35 phút/ngày","nextAction":"Luyện Phát âm"}
        """,
        """
        {"summary":"Bạn cần củng cố từ vựng và khả năng hiểu ngữ cảnh. Kế hoạch này đi từ chọn từ trong câu đến đọc hiểu đoạn văn.","strengths":["Có vốn từ cơ bản","Có thể suy luận từ xung quanh"],"weaknesses":["Từ vựng","Đọc hiểu"],"weeks":[{"title":"Tuần 1: Từ vựng trong câu","goal":"Chọn đúng từ theo collocation và ngữ cảnh.","tasks":["Học 10 collocation mỗi ngày","Làm câu chọn đáp án đúng về từ vựng","Ghi lại cụm từ sai"],"practiceType":"Từ vựng"},{"title":"Tuần 2: Đọc hiểu từ vựng","goal":"Suy luận nghĩa từ trong đoạn văn.","tasks":["Gạch câu chứa từ cần hỏi","Đoán nghĩa trước khi nhìn đáp án","Làm 10 câu đọc hiểu"],"practiceType":"Đọc hiểu"},{"title":"Tuần 3: Đồng/trái nghĩa","goal":"Mở rộng vốn từ theo nhóm nghĩa.","tasks":["Học cặp synonym/antonym","Làm xen kẽ đồng nghĩa và trái nghĩa","Ôn lại từ sai bằng ví dụ"],"practiceType":"Từ đồng nghĩa"}],"dailyTime":"40 phút/ngày","nextAction":"Luyện Từ vựng"}
        """,
        """
        {"summary":"Bạn cần tăng độ chính xác ở phần đọc hiểu. Lộ trình ưu tiên đọc câu hỏi trước, tìm keyword và kiểm tra bằng chứng trong bài.","strengths":["Đọc được câu hỏi tiếng Anh","Không ngại đoạn văn dài"],"weaknesses":["Đọc hiểu","Đọc hiểu - điền từ"],"weeks":[{"title":"Tuần 1: Đọc câu hỏi trước","goal":"Biết tìm vị trí thông tin trong đoạn.","tasks":["Đọc câu hỏi và gạch keyword","Tìm câu chứa thông tin trong đoạn","Làm 10 câu đọc hiểu"],"practiceType":"Đọc hiểu"},{"title":"Tuần 2: Suy luận và reference","goal":"Trả lời câu hỏi ý chính, đại từ tham chiếu, gần nghĩa.","tasks":["Ôn dạng main idea, reference, synonym","Viết bằng chứng cho mỗi đáp án","Không chọn theo cảm giác"],"practiceType":"Đọc hiểu"},{"title":"Tuần 3: Cloze hỗ trợ đọc","goal":"Dùng mạch văn để điền từ chính xác.","tasks":["Làm cloze test mỗi ngày","Xác định loại từ trước khi chọn","Chấm lỗi theo ngữ pháp/từ vựng"],"practiceType":"Đọc hiểu - điền từ"}],"dailyTime":"45 phút/ngày","nextAction":"Luyện Đọc hiểu"}
        """,
        """
        {"summary":"Bạn cần luyện ngữ pháp ứng dụng trong các câu chọn đáp án. Tập trung vào thì, câu điều kiện và mệnh đề quan hệ sẽ giúp tăng điểm nhanh.","strengths":["Có nền tảng câu đơn","Biết đọc nghĩa câu trước khi chọn"],"weaknesses":["Ngữ pháp","Chọn đáp án đúng"],"weeks":[{"title":"Tuần 1: Thì và dạng động từ","goal":"Chọn đúng động từ theo thời gian và chủ ngữ.","tasks":["Ôn 6 thì cơ bản","Làm 10 câu chọn đáp án đúng","Gạch dấu hiệu thời gian"],"practiceType":"Chọn đáp án đúng"},{"title":"Tuần 2: Mệnh đề và liên từ","goal":"Chọn đúng who/which/that/although/because.","tasks":["Ôn mệnh đề quan hệ","Luyện liên từ chỉ nguyên nhân, nhượng bộ","Giải thích đáp án bằng cấu trúc"],"practiceType":"Chọn đáp án đúng"},{"title":"Tuần 3: Câu điều kiện và so sánh","goal":"Hoàn thiện nhóm cấu trúc hay ra thi.","tasks":["Ôn if clauses, wish, comparison","Làm bài tổng hợp 10 câu/ngày","Tạo bảng công thức ngắn"],"practiceType":"Chọn đáp án đúng"}],"dailyTime":"45 phút/ngày","nextAction":"Luyện Chọn đáp án đúng"}
        """,
        """
        {"summary":"Bạn cần luyện dạng tổng hợp theo tuần để không lệch kỹ năng. Mỗi tuần tập trung một nhóm lỗi nổi bật và giữ nhịp làm bài đều.","strengths":["Có khả năng tự học","Biết kiểm tra đáp án sau khi làm"],"weaknesses":["Đề tổng hợp","Chọn đáp án đúng"],"weeks":[{"title":"Tuần 1: Nền tảng câu hỏi ngắn","goal":"Giảm lỗi ở câu trắc nghiệm đơn.","tasks":["Làm 10 câu chọn đáp án đúng","Ghi lỗi theo chủ điểm","Ôn lại ngay sau khi chấm"],"practiceType":"Chọn đáp án đúng"},{"title":"Tuần 2: Đọc và từ vựng","goal":"Cải thiện câu có ngữ cảnh dài hơn.","tasks":["Làm đọc hiểu và từ đồng nghĩa","Tạo flashcard từ sai","Tóm tắt đoạn văn sau khi làm"],"practiceType":"Đọc hiểu"},{"title":"Tuần 3: Tổng hợp trước khi thi","goal":"Rèn nhịp làm bài và phản xạ chọn đáp án.","tasks":["Làm mixed set 10 câu","Canh thời gian nghiêm túc","Làm lại câu sai sau 24 giờ"],"practiceType":"Đề tổng hợp"}],"dailyTime":"50 phút/ngày","nextAction":"Luyện Chọn đáp án đúng"}
        """,
        """
        {"summary":"Bạn cần tập trung vào phần câu hỏi có đáp án dễ gây nhiễu. Lộ trình giúp luyện cách loại trừ và kiểm tra ngữ pháp trước khi chọn.","strengths":["Biết so sánh các đáp án","Có khả năng sửa lỗi sau khi xem giải thích"],"weaknesses":["Chọn đáp án đúng","Tìm lỗi sai"],"weeks":[{"title":"Tuần 1: Loại trừ đáp án nhiễu","goal":"Không chọn theo cảm giác khi các đáp án gần giống nhau.","tasks":["Đọc hết 4 đáp án trước khi chọn","Loại đáp án sai ngữ pháp trước","Làm 10 câu chọn đáp án đúng"],"practiceType":"Chọn đáp án đúng"},{"title":"Tuần 2: Soi lỗi trong câu","goal":"Nhìn ra lỗi sai về từ loại và cấu trúc.","tasks":["Gạch chủ ngữ, động từ, bổ ngữ","Làm 10 câu tìm lỗi sai","Viết lý do lỗi sai"],"practiceType":"Tìm lỗi sai"},{"title":"Tuần 3: Tổng hợp loại trừ","goal":"Áp dụng quy trình loại trừ vào nhiều dạng.","tasks":["Làm mixed set 10 câu","Ghi lại bẫy thường gặp","Ôn lại câu sai cuối tuần"],"practiceType":"Chọn đáp án đúng"}],"dailyTime":"40 phút/ngày","nextAction":"Luyện Chọn đáp án đúng"}
        """,
        """
        {"summary":"Bạn cần củng cố kỹ năng viết và chọn cấu trúc tương đương. Lộ trình này ưu tiên các mẫu viết lại câu thường gặp trong đề vào 10.","strengths":["Hiểu nghĩa câu gốc","Có nền tảng cấu trúc câu"],"weaknesses":["Viết","Viết lại câu (gần nghĩa)"],"weeks":[{"title":"Tuần 1: Cấu trúc tương đương","goal":"Nhận diện cặp cấu trúc cùng nghĩa.","tasks":["Ôn because/because of, although/despite","Làm 10 câu viết lại gần nghĩa","Gạch phần nghĩa cần giữ"],"practiceType":"Viết lại câu (gần nghĩa)"},{"title":"Tuần 2: Từ gợi ý","goal":"Dùng đúng từ cho sẵn để hoàn thành câu.","tasks":["Ôn enough/too, so/such, wish","Làm 10 câu từ cho sẵn","Không đổi từ gợi ý nếu đề yêu cầu"],"practiceType":"Viết lại câu (từ cho sẵn)"},{"title":"Tuần 3: Tổng hợp viết","goal":"Tăng độ chính xác khi gặp câu dài.","tasks":["Làm xen kẽ hai dạng viết","Chấm lỗi theo cấu trúc","Ôn lại công thức sai"],"practiceType":"Viết"}],"dailyTime":"40 phút/ngày","nextAction":"Luyện Viết lại câu (gần nghĩa)"}
        """,
        """
        {"summary":"Bạn cần cải thiện khả năng hoàn thành đoạn văn. Hãy tập trung vào câu nối, đại từ thay thế và tính logic giữa các câu.","strengths":["Đọc được ý chính đoạn văn","Có khả năng nhận diện keyword"],"weaknesses":["Sắp xếp/hoàn thành đoạn văn","Điền câu vào đoạn văn"],"weeks":[{"title":"Tuần 1: Trật tự ý trong đoạn","goal":"Nhận biết mở đoạn, triển khai và kết đoạn.","tasks":["Tìm câu chủ đề trong đoạn","Đánh dấu từ nối thời gian/logic","Luyện sắp xếp/hoàn thành đoạn văn"],"practiceType":"Sắp xếp/hoàn thành đoạn văn"},{"title":"Tuần 2: Điền câu vào đoạn","goal":"Chọn câu phù hợp với trước và sau chỗ trống.","tasks":["Gạch đại từ và từ lặp","Xác định câu cần bổ sung ví dụ hay kết luận","Làm 10 câu điền câu"],"practiceType":"Điền câu vào đoạn văn"},{"title":"Tuần 3: Tổng hợp đoạn văn","goal":"Tăng độ chắc khi xử lý đoạn dài.","tasks":["Làm 2 set đoạn văn mỗi ngày","Viết lý do chọn đáp án","Ôn lại lỗi logic"],"practiceType":"Sắp xếp/hoàn thành đoạn văn"}],"dailyTime":"45 phút/ngày","nextAction":"Luyện Sắp xếp/hoàn thành đoạn văn"}
        """,
        """
        {"summary":"Bạn cần tăng điểm ổn định bằng cách ưu tiên các dạng dễ cải thiện nhanh: phát âm, giao tiếp và chọn đáp án đúng.","strengths":["Có thể học đều mỗi ngày","Không bị mất nền tảng hoàn toàn"],"weaknesses":["Phát âm","Giao tiếp","Chọn đáp án đúng"],"weeks":[{"title":"Tuần 1: Lấy điểm câu đầu","goal":"Giảm lỗi phát âm cơ bản.","tasks":["Ôn âm đuôi thường gặp","Làm 10 câu phát âm","Đọc lại từ sai"],"practiceType":"Phát âm"},{"title":"Tuần 2: Giao tiếp ngắn","goal":"Chọn phản hồi tự nhiên trong hội thoại.","tasks":["Học mẫu câu cảm ơn, xin lỗi, đề nghị","Làm 10 câu giao tiếp","Ghi cặp hỏi đáp hay gặp"],"practiceType":"Giao tiếp"},{"title":"Tuần 3: Ngữ pháp nền","goal":"Tăng điểm phần chọn đáp án đúng.","tasks":["Ôn thì, giới từ, từ loại","Làm 10 câu mỗi ngày","Chấm và ghi lỗi"],"practiceType":"Chọn đáp án đúng"}],"dailyTime":"35 phút/ngày","nextAction":"Luyện Phát âm"}
        """,
        """
        {"summary":"Bạn cần luyện sâu phần đọc hiểu vì lỗi đọc thường kéo theo sai từ vựng và điền từ. Kế hoạch này tập trung đọc có bằng chứng.","strengths":["Có thể đọc đoạn văn vừa phải","Biết tìm keyword"],"weaknesses":["Đọc hiểu","Từ đồng nghĩa"],"weeks":[{"title":"Tuần 1: Tìm bằng chứng","goal":"Mỗi đáp án phải có câu chứng minh trong bài.","tasks":["Đọc câu hỏi trước","Gạch keyword trong đoạn","Làm 10 câu đọc hiểu"],"practiceType":"Đọc hiểu"},{"title":"Tuần 2: Nghĩa từ trong đoạn","goal":"Suy luận từ đồng nghĩa theo ngữ cảnh.","tasks":["Không dịch từng từ rời rạc","Dựa vào câu trước/sau","Làm câu từ đồng nghĩa"],"practiceType":"Từ đồng nghĩa"},{"title":"Tuần 3: Đọc hiểu tổng hợp","goal":"Hoàn thành bài đọc trong thời gian ngắn hơn.","tasks":["Làm bài đọc có bấm giờ","Ghi dạng câu sai","Ôn từ mới sau mỗi bài"],"practiceType":"Đọc hiểu"}],"dailyTime":"45 phút/ngày","nextAction":"Luyện Đọc hiểu"}
        """,
        """
        {"summary":"Bạn cần sửa lỗi bỏ qua câu và câu không chắc đáp án. Lộ trình ưu tiên rèn chiến thuật làm bài cùng dạng câu dễ lấy điểm.","strengths":["Có khả năng hoàn thành bài nếu quản lý thời gian tốt","Biết đọc giải thích sau khi sai"],"weaknesses":["Đề tổng hợp","Chọn đáp án đúng"],"weeks":[{"title":"Tuần 1: Câu chắc điểm","goal":"Làm chắc phần phát âm, trọng âm, giao tiếp.","tasks":["Làm 10 câu ngắn mỗi ngày","Đánh dấu câu chưa chắc","Ôn lại đáp án sai"],"practiceType":"Phát âm"},{"title":"Tuần 2: Ngữ pháp trọng tâm","goal":"Tăng số câu đúng ở phần chọn đáp án.","tasks":["Ôn thì, từ loại, mệnh đề","Làm 10 câu chọn đáp án đúng","Giải thích lỗi bằng công thức"],"practiceType":"Chọn đáp án đúng"},{"title":"Tuần 3: Set tổng hợp","goal":"Rèn chiến thuật không bỏ câu.","tasks":["Làm set 10 câu random","Canh thời gian","Làm lại câu đã đánh dấu"],"practiceType":"Đề tổng hợp"}],"dailyTime":"45 phút/ngày","nextAction":"Luyện Phát âm"}
        """,
        """
        {"summary":"Bạn cần cải thiện nhóm câu từ vựng và ngữ cảnh. Hãy học từ theo cụm, theo câu và luyện chọn đáp án bằng nghĩa toàn câu.","strengths":["Có thể đoán nghĩa cơ bản","Không ngại học từ mới"],"weaknesses":["Từ vựng","Chọn đáp án đúng"],"weeks":[{"title":"Tuần 1: Collocation cơ bản","goal":"Nhớ cụm từ đi cùng nhau trong câu thi.","tasks":["Học 10 collocation/ngày","Đặt câu với từ mới","Làm 10 câu từ vựng"],"practiceType":"Từ vựng"},{"title":"Tuần 2: Ngữ cảnh câu","goal":"Chọn từ phù hợp với nghĩa câu.","tasks":["Đọc cả câu trước khi chọn","Loại đáp án sai sắc thái","Làm 10 câu chọn đáp án đúng"],"practiceType":"Chọn đáp án đúng"},{"title":"Tuần 3: Đồng/trái nghĩa","goal":"Mở rộng vốn từ theo nhóm nghĩa.","tasks":["Làm synonym/antonym mỗi ngày","Tạo flashcard","Ôn lại từ sai"],"practiceType":"Từ trái nghĩa"}],"dailyTime":"40 phút/ngày","nextAction":"Luyện Từ vựng"}
        """,
        """
        {"summary":"Bạn cần luyện mạch văn và câu nối. Đây là nhóm câu đòi hỏi đọc trước sau cẩn thận hơn là chỉ biết ngữ pháp riêng lẻ.","strengths":["Có khả năng đọc câu dài","Biết dùng keyword"],"weaknesses":["Điền câu vào đoạn văn","Đọc hiểu - điền từ"],"weeks":[{"title":"Tuần 1: Câu nối đoạn","goal":"Chọn câu phù hợp với ý trước và sau.","tasks":["Tìm từ nối và đại từ tham chiếu","Làm 10 câu điền câu vào đoạn","Giải thích vai trò câu được chọn"],"practiceType":"Điền câu vào đoạn văn"},{"title":"Tuần 2: Cloze theo mạch văn","goal":"Dùng ngữ cảnh để điền từ chính xác.","tasks":["Xác định loại từ chỗ trống","Đọc cả đoạn trước khi chọn","Làm 10 câu cloze"],"practiceType":"Đọc hiểu - điền từ"},{"title":"Tuần 3: Đoạn văn tổng hợp","goal":"Tăng độ chính xác với đoạn dài.","tasks":["Làm xen kẽ hai dạng đoạn văn","Ghi lỗi do bỏ qua câu trước/sau","Làm lại câu sai"],"practiceType":"Điền câu vào đoạn văn"}],"dailyTime":"45 phút/ngày","nextAction":"Luyện Điền câu vào đoạn văn"}
        """,
        """
        {"summary":"Bạn cần ôn chắc các dạng viết lại câu cơ bản để tránh mất điểm ở phần cuối đề. Lộ trình đi từ công thức đến áp dụng.","strengths":["Hiểu câu gốc","Có thể nhận ra cấu trúc quen thuộc"],"weaknesses":["Viết lại câu (từ cho sẵn)","Viết lại câu (gần nghĩa)"],"weeks":[{"title":"Tuần 1: Công thức từ cho sẵn","goal":"Không sai cấu trúc khi dùng từ gợi ý.","tasks":["Ôn so/such, too/enough, although/despite","Làm 10 câu từ cho sẵn","Kiểm tra thì và chủ ngữ"],"practiceType":"Viết lại câu (từ cho sẵn)"},{"title":"Tuần 2: Gần nghĩa","goal":"Chọn đáp án không làm đổi nghĩa câu.","tasks":["Ôn passive, reported speech, comparison","Làm 10 câu gần nghĩa","Gạch chi tiết nghĩa quan trọng"],"practiceType":"Viết lại câu (gần nghĩa)"},{"title":"Tuần 3: Chốt lỗi viết","goal":"Rút ngắn thời gian làm phần viết.","tasks":["Làm mixed set viết lại câu","Chấm theo nhóm cấu trúc","Ôn lại công thức sai"],"practiceType":"Viết lại câu (từ cho sẵn)"}],"dailyTime":"40 phút/ngày","nextAction":"Luyện Viết lại câu (từ cho sẵn)"}
        """,
        """
        {"summary":"Bạn cần luyện lỗi sai vì đây là dạng dễ mất điểm do đọc lướt. Hãy học cách kiểm tra câu theo thứ tự chủ ngữ, động từ, từ loại, giới từ.","strengths":["Có nền tảng ngữ pháp cơ bản","Biết xem lại câu sau khi làm"],"weaknesses":["Tìm lỗi sai","Chọn đáp án đúng"],"weeks":[{"title":"Tuần 1: Quy trình soi lỗi","goal":"Không bỏ qua lỗi động từ và hòa hợp chủ vị.","tasks":["Gạch chủ ngữ và động từ","Làm 10 câu tìm lỗi sai","Ghi loại lỗi sau khi chấm"],"practiceType":"Tìm lỗi sai"},{"title":"Tuần 2: Từ loại và cụm từ","goal":"Nhận ra sai adjective/adverb/noun/verb.","tasks":["Ôn vị trí từ loại","Làm câu chọn đáp án đúng về từ loại","Tạo bảng lỗi sai"],"practiceType":"Chọn đáp án đúng"},{"title":"Tuần 3: Tổng hợp error","goal":"Tăng tốc tìm lỗi trong câu dài.","tasks":["Làm set 10 câu có bấm giờ","Không đổi đáp án nếu không có lý do","Ôn lại lỗi sai nhiều nhất"],"practiceType":"Tìm lỗi sai"}],"dailyTime":"35 phút/ngày","nextAction":"Luyện Tìm lỗi sai"}
        """,
        """
        {"summary":"Bạn cần luyện giao tiếp và phản xạ ngữ cảnh. Dạng này thường ngắn nhưng dễ chọn đáp án thiếu lịch sự hoặc sai tình huống.","strengths":["Biết một số mẫu câu giao tiếp","Làm tốt câu ngắn hơn câu đọc dài"],"weaknesses":["Giao tiếp","Biển báo/Thông báo"],"weeks":[{"title":"Tuần 1: Hội thoại thường gặp","goal":"Nắm câu đáp cho lời mời, cảm ơn, xin lỗi, đề nghị.","tasks":["Học mẫu câu theo tình huống","Làm 10 câu giao tiếp","Đọc lại hội thoại thành tiếng"],"practiceType":"Giao tiếp"},{"title":"Tuần 2: Thông báo ngắn","goal":"Hiểu mục đích của biển báo và thông báo.","tasks":["Gạch từ chỉ cấm/cho phép/yêu cầu","Làm 10 câu biển báo/thông báo","Tóm tắt thông điệp bằng tiếng Việt"],"practiceType":"Biển báo/Thông báo"},{"title":"Tuần 3: Ngữ cảnh nhanh","goal":"Chọn đáp án tự nhiên và đúng vai giao tiếp.","tasks":["Làm xen kẽ giao tiếp và thông báo","Chấm lỗi do sai sắc thái","Ôn lại mẫu câu sai"],"practiceType":"Giao tiếp"}],"dailyTime":"30 phút/ngày","nextAction":"Luyện Giao tiếp"}
        """,
        """
        {"summary":"Bạn cần cân bằng kỹ năng đọc và ngữ pháp. Lộ trình này giúp xử lý câu hỏi dài bằng cách chia nhỏ thông tin.","strengths":["Có khả năng đọc hiểu câu đơn","Biết tìm dấu hiệu ngữ pháp"],"weaknesses":["Đọc hiểu","Ngữ pháp"],"weeks":[{"title":"Tuần 1: Câu hỏi ngữ pháp","goal":"Chọn đúng cấu trúc trong câu đơn và câu ghép.","tasks":["Ôn thì, mệnh đề, liên từ","Làm 10 câu chọn đáp án đúng","Giải thích đáp án bằng dấu hiệu"],"practiceType":"Chọn đáp án đúng"},{"title":"Tuần 2: Đọc hiểu cơ bản","goal":"Tìm thông tin đúng trong đoạn văn.","tasks":["Đọc câu hỏi trước","Gạch keyword","Làm 10 câu đọc hiểu"],"practiceType":"Đọc hiểu"},{"title":"Tuần 3: Đọc có ngữ pháp","goal":"Hiểu câu dài có mệnh đề phụ.","tasks":["Tách câu dài thành cụm nhỏ","Làm bài đọc và câu ngữ pháp xen kẽ","Ghi câu khó đã gặp"],"practiceType":"Đọc hiểu"}],"dailyTime":"45 phút/ngày","nextAction":"Luyện Chọn đáp án đúng"}
        """,
        """
        {"summary":"Bạn cần luyện dạng cloze và từ vựng để chọn đáp án theo cả loại từ lẫn nghĩa. Kế hoạch này giúp giảm chọn sai do chỉ nhìn một từ gần chỗ trống.","strengths":["Có vốn từ phổ thông","Biết đọc câu xung quanh"],"weaknesses":["Đọc hiểu - điền từ","Từ vựng"],"weeks":[{"title":"Tuần 1: Loại từ trong cloze","goal":"Xác định chức năng ngữ pháp của chỗ trống.","tasks":["Nhìn từ trước và sau chỗ trống","Ôn danh từ, động từ, tính từ, trạng từ","Làm 10 câu cloze"],"practiceType":"Đọc hiểu - điền từ"},{"title":"Tuần 2: Nghĩa và collocation","goal":"Chọn từ đúng nghĩa trong đoạn.","tasks":["Học cụm từ theo chủ đề","Làm câu từ vựng","Viết lại câu chứa từ sai"],"practiceType":"Từ vựng"},{"title":"Tuần 3: Cloze tổng hợp","goal":"Hoàn thành đoạn điền từ chính xác hơn.","tasks":["Làm cloze có bấm giờ","Chấm lỗi theo loại từ/nghĩa","Ôn lại cụm sai"],"practiceType":"Đọc hiểu - điền từ"}],"dailyTime":"40 phút/ngày","nextAction":"Luyện Đọc hiểu - điền từ"}
        """,
        """
        {"summary":"Bạn cần luyện phần đọc thông tin ngắn và giao tiếp vì đây là nhóm câu có thể tăng điểm nhanh nếu quen mẫu.","strengths":["Xử lý được câu ngắn","Có thể học mẫu theo tình huống"],"weaknesses":["Biển báo/Thông báo","Giao tiếp"],"weeks":[{"title":"Tuần 1: Biển báo/thông báo","goal":"Hiểu đúng mục đích thông báo.","tasks":["Học từ khóa warning, notice, allowed, must","Làm 10 câu biển báo/thông báo","Tóm tắt ý chính mỗi câu"],"practiceType":"Biển báo/Thông báo"},{"title":"Tuần 2: Giao tiếp","goal":"Chọn phản hồi phù hợp và lịch sự.","tasks":["Ôn mẫu câu lời mời, lời khuyên, lời cảm ơn","Làm 10 câu giao tiếp","Đọc hội thoại thành tiếng"],"practiceType":"Giao tiếp"},{"title":"Tuần 3: Phản xạ tình huống","goal":"Làm nhanh các câu ngữ cảnh ngắn.","tasks":["Làm xen kẽ hai dạng","Ghi đáp án dễ nhầm","Làm lại câu sai"],"practiceType":"Giao tiếp"}],"dailyTime":"30 phút/ngày","nextAction":"Luyện Biển báo/Thông báo"}
        """,
        """
        {"summary":"Bạn cần cải thiện phần phát âm, trọng âm và từ vựng để tránh mất điểm ở các câu ngắn đầu bài.","strengths":["Có khả năng học từ theo nhóm","Biết luyện lại câu sai"],"weaknesses":["Phát âm","Trọng âm","Từ vựng"],"weeks":[{"title":"Tuần 1: Phát âm","goal":"Nắm âm khác biệt thường gặp.","tasks":["Ôn âm đuôi và nguyên âm","Làm 10 câu phát âm","Đọc lại từ sai"],"practiceType":"Phát âm"},{"title":"Tuần 2: Trọng âm","goal":"Nhận biết vị trí nhấn âm.","tasks":["Ôn quy tắc stress","Làm 10 câu trọng âm","Đánh dấu âm tiết chính"],"practiceType":"Trọng âm"},{"title":"Tuần 3: Từ vựng","goal":"Mở rộng từ theo ngữ cảnh đề thi.","tasks":["Học 10 từ/cụm từ mỗi ngày","Làm câu từ vựng","Đặt câu với từ sai"],"practiceType":"Từ vựng"}],"dailyTime":"35 phút/ngày","nextAction":"Luyện Phát âm"}
        """,
        """
        {"summary":"Bạn cần luyện phần từ đồng nghĩa/trái nghĩa vì dễ nhầm nếu không xét cả câu. Kế hoạch giúp học từ theo nhóm nghĩa và ví dụ.","strengths":["Có vốn từ nền tảng","Biết dựa vào ngữ cảnh"],"weaknesses":["Từ đồng nghĩa","Từ trái nghĩa"],"weeks":[{"title":"Tuần 1: Đồng nghĩa","goal":"Chọn từ gần nghĩa dựa trên ngữ cảnh.","tasks":["Học 10 cặp synonym/ngày","Làm 10 câu từ đồng nghĩa","Viết ví dụ với từ mới"],"practiceType":"Từ đồng nghĩa"},{"title":"Tuần 2: Trái nghĩa","goal":"Không nhầm câu hỏi opposite và closest.","tasks":["Khoanh CLOSEST/OPPOSITE trước khi làm","Học cặp antonym phổ biến","Làm 10 câu từ trái nghĩa"],"practiceType":"Từ trái nghĩa"},{"title":"Tuần 3: Tổng hợp từ vựng","goal":"Tăng phản xạ chọn nghĩa đúng.","tasks":["Làm xen kẽ synonym/antonym","Tạo flashcard lỗi sai","Ôn lại sau 24 giờ"],"practiceType":"Từ đồng nghĩa"}],"dailyTime":"35 phút/ngày","nextAction":"Luyện Từ đồng nghĩa"}
        """,
        """
        {"summary":"Bạn cần luyện chọn đáp án đúng và viết lại câu vì hai phần này dùng chung nền tảng cấu trúc. Học theo cặp cấu trúc sẽ hiệu quả hơn.","strengths":["Hiểu được ý chính câu","Có nền tảng ngữ pháp căn bản"],"weaknesses":["Chọn đáp án đúng","Viết lại câu (gần nghĩa)"],"weeks":[{"title":"Tuần 1: Cấu trúc câu","goal":"Chọn đúng dạng động từ và mệnh đề.","tasks":["Ôn thì, bị động, câu điều kiện","Làm 10 câu chọn đáp án đúng","Ghi công thức sai"],"practiceType":"Chọn đáp án đúng"},{"title":"Tuần 2: Viết lại gần nghĩa","goal":"Nhận diện cấu trúc tương đương.","tasks":["Ôn reported speech, comparison, wish","Làm 10 câu viết lại gần nghĩa","Gạch ý không được đổi"],"practiceType":"Viết lại câu (gần nghĩa)"},{"title":"Tuần 3: Tổng hợp cấu trúc","goal":"Áp dụng công thức vào nhiều dạng câu.","tasks":["Làm mixed set 10 câu","Chấm theo cấu trúc","Ôn lại câu sai"],"practiceType":"Chọn đáp án đúng"}],"dailyTime":"45 phút/ngày","nextAction":"Luyện Chọn đáp án đúng"}
        """,
        """
        {"summary":"Bạn cần luyện đọc hiểu và câu hỏi reference/synonym trong đoạn. Hãy tập tìm bằng chứng thay vì chọn theo cảm giác.","strengths":["Đọc được đoạn văn vừa phải","Có khả năng tìm keyword"],"weaknesses":["Đọc hiểu","Từ đồng nghĩa"],"weeks":[{"title":"Tuần 1: Keyword và vị trí thông tin","goal":"Tìm câu chứa đáp án trong bài đọc.","tasks":["Gạch keyword trong câu hỏi","Quét đoạn văn theo keyword","Làm 10 câu đọc hiểu"],"practiceType":"Đọc hiểu"},{"title":"Tuần 2: Reference và synonym","goal":"Xác định they/it/this và từ gần nghĩa trong đoạn.","tasks":["Tìm danh từ đứng trước đại từ","Đoán nghĩa từ theo câu","Làm câu từ đồng nghĩa"],"practiceType":"Từ đồng nghĩa"},{"title":"Tuần 3: Đọc hiểu có bấm giờ","goal":"Tăng tốc làm bài đọc.","tasks":["Làm bài đọc trong giới hạn thời gian","Viết bằng chứng cho đáp án","Ôn từ mới"],"practiceType":"Đọc hiểu"}],"dailyTime":"45 phút/ngày","nextAction":"Luyện Đọc hiểu"}
        """,
        """
        {"summary":"Bạn cần luyện kỹ năng nghe hiểu nếu có phần audio hoặc bài tập nghe trong hệ thống. Kế hoạch tập trung nghe ý chính, từ khóa và chọn đáp án.","strengths":["Có vốn từ cơ bản","Có thể luyện đều mỗi ngày"],"weaknesses":["Nghe hiểu","Từ vựng"],"weeks":[{"title":"Tuần 1: Nghe keyword","goal":"Nhận ra từ khóa chính trong câu ngắn.","tasks":["Nghe 2 lần và ghi keyword","Ôn từ vựng chủ đề trường học/gia đình","Làm bài nghe hiểu nếu có"],"practiceType":"Nghe hiểu"},{"title":"Tuần 2: Nghe ý chính","goal":"Không cần hiểu từng từ vẫn chọn được ý đúng.","tasks":["Tóm tắt nội dung sau khi nghe","Ghi từ nối và số liệu","Luyện nghe đoạn ngắn"],"practiceType":"Nghe hiểu"},{"title":"Tuần 3: Từ vựng hỗ trợ nghe","goal":"Tăng nhận diện từ quen trong audio.","tasks":["Học phát âm từ mới","Đọc to trước khi nghe","Ôn lại từ nghe sai"],"practiceType":"Từ vựng"}],"dailyTime":"30 phút/ngày","nextAction":"Luyện Nghe hiểu"}
        """,
        """
        {"summary":"Bạn cần luyện viết và hoàn thành đoạn văn. Mục tiêu là hiểu mạch văn, giữ đúng ngữ pháp và chọn câu không làm lệch nghĩa.","strengths":["Có khả năng hiểu câu riêng lẻ","Biết nhận diện cấu trúc quen thuộc"],"weaknesses":["Viết","Sắp xếp/hoàn thành đoạn văn"],"weeks":[{"title":"Tuần 1: Viết lại câu","goal":"Ôn cấu trúc câu thường gặp.","tasks":["Ôn passive, reported speech, comparison","Làm 10 câu viết lại","Ghi công thức sai"],"practiceType":"Viết"},{"title":"Tuần 2: Hoàn thành đoạn văn","goal":"Chọn câu phù hợp mạch văn.","tasks":["Xác định câu chủ đề","Tìm từ nối và đại từ","Luyện sắp xếp/hoàn thành đoạn văn"],"practiceType":"Sắp xếp/hoàn thành đoạn văn"},{"title":"Tuần 3: Tổng hợp viết và đoạn","goal":"Chuyển giữa câu đơn và đoạn văn tốt hơn.","tasks":["Làm xen kẽ hai dạng","Chấm lỗi theo nghĩa và cấu trúc","Làm lại câu sai"],"practiceType":"Viết"}],"dailyTime":"45 phút/ngày","nextAction":"Luyện Viết"}
        """,
        """
        {"summary":"Bạn cần cải thiện phần câu ngắn có bẫy ngữ pháp và từ loại. Lộ trình này giúp làm chắc phần chọn đáp án đúng trước.","strengths":["Biết đọc toàn câu","Có khả năng ghi nhớ cấu trúc"],"weaknesses":["Chọn đáp án đúng","Tìm lỗi sai"],"weeks":[{"title":"Tuần 1: Từ loại","goal":"Chọn đúng noun/verb/adjective/adverb.","tasks":["Ôn vị trí từ loại","Làm 10 câu chọn đáp án đúng","Ghi cụm từ đi kèm"],"practiceType":"Chọn đáp án đúng"},{"title":"Tuần 2: Lỗi sai từ loại","goal":"Nhận ra phần sai trong câu.","tasks":["Gạch chức năng từng cụm từ","Làm 10 câu tìm lỗi sai","Viết lý do sai"],"practiceType":"Tìm lỗi sai"},{"title":"Tuần 3: Tổng hợp câu ngắn","goal":"Làm nhanh và chắc nhóm câu ngữ pháp.","tasks":["Làm mixed set 10 câu","Canh thời gian","Ôn lại lỗi sai"],"practiceType":"Chọn đáp án đúng"}],"dailyTime":"40 phút/ngày","nextAction":"Luyện Chọn đáp án đúng"}
        """,
        """
        {"summary":"Bạn cần luyện dạng khác/đề tổng hợp khi lỗi phân tán nhiều phần. Kế hoạch này dùng cách xoay vòng để không bỏ sót kỹ năng.","strengths":["Có thể luyện đều theo lịch","Biết đọc đáp án giải thích"],"weaknesses":["Khác","Đề tổng hợp"],"weeks":[{"title":"Tuần 1: Lọc lỗi phổ biến","goal":"Tìm nhóm câu sai nhiều nhất.","tasks":["Làm set tổng hợp 10 câu","Ghi type của câu sai","Ôn lại giải thích"],"practiceType":"Đề tổng hợp"},{"title":"Tuần 2: Tập trung type yếu","goal":"Dành nhiều thời gian cho dạng sai lặp lại.","tasks":["Chọn một type sai nhiều nhất","Làm 10 câu cùng type mỗi ngày","So sánh lỗi trước/sau"],"practiceType":"Chọn đáp án đúng"},{"title":"Tuần 3: Tổng kiểm tra","goal":"Kiểm tra lại mức tiến bộ toàn bài.","tasks":["Làm mixed set mỗi ngày","Canh thời gian","Làm lại câu sai"],"practiceType":"Đề tổng hợp"}],"dailyTime":"50 phút/ngày","nextAction":"Luyện Đề tổng hợp"}
        """,
        """
        {"summary":"Bạn cần luyện theo hướng tăng tốc. Lộ trình này chia nhỏ mỗi ngày để vừa ôn kiến thức vừa rèn thời gian làm bài.","strengths":["Có thể làm bài đủ số câu","Có động lực luyện tập"],"weaknesses":["Chọn đáp án đúng","Đọc hiểu","Tìm lỗi sai"],"weeks":[{"title":"Tuần 1: Tốc độ câu ngắn","goal":"Làm chắc câu chọn đáp án và lỗi sai.","tasks":["Làm 10 câu chọn đáp án đúng","Làm 5 câu tìm lỗi sai bổ sung","Ghi lỗi vào sổ"],"practiceType":"Chọn đáp án đúng"},{"title":"Tuần 2: Tốc độ đọc hiểu","goal":"Đọc nhanh nhưng vẫn có bằng chứng.","tasks":["Làm 1 bài đọc ngắn mỗi ngày","Gạch keyword","Tóm tắt lỗi sai"],"practiceType":"Đọc hiểu"},{"title":"Tuần 3: Tốc độ tổng hợp","goal":"Duy trì độ chính xác trong thời gian ngắn.","tasks":["Làm set 10 câu random","Đặt giới hạn thời gian","Ôn câu sai cuối ngày"],"practiceType":"Tìm lỗi sai"}],"dailyTime":"45 phút/ngày","nextAction":"Luyện Chọn đáp án đúng"}
        """,
        """
        {"summary":"Bạn cần luyện dạng câu theo chủ điểm ưu tiên để không bị lan man. Kế hoạch này bắt đầu từ lỗi dễ sửa nhất rồi sang đọc hiểu.","strengths":["Có khả năng học theo chủ điểm","Biết tự chấm bài"],"weaknesses":["Trọng âm","Chọn đáp án đúng","Đọc hiểu"],"weeks":[{"title":"Tuần 1: Trọng âm lấy điểm nhanh","goal":"Không mất điểm ở câu stress cơ bản.","tasks":["Ôn quy tắc trọng âm","Làm 10 câu trọng âm","Đọc lại từ sai"],"practiceType":"Trọng âm"},{"title":"Tuần 2: Chọn đáp án đúng","goal":"Củng cố ngữ pháp và từ vựng trong câu.","tasks":["Ôn thì, giới từ, từ loại","Làm 10 câu chọn đáp án đúng","Ghi cấu trúc sai"],"practiceType":"Chọn đáp án đúng"},{"title":"Tuần 3: Đọc hiểu","goal":"Tăng điểm câu đoạn văn.","tasks":["Đọc câu hỏi trước","Gạch keyword","Làm 10 câu đọc hiểu"],"practiceType":"Đọc hiểu"}],"dailyTime":"40 phút/ngày","nextAction":"Luyện Trọng âm"}
        """,
        """
        {"summary":"Bạn cần luyện phần câu hỏi có tính suy luận. Đọc hiểu, điền câu và hoàn thành đoạn văn sẽ được ưu tiên trong 3 tuần.","strengths":["Có thể đọc câu dài","Biết tìm ý chính"],"weaknesses":["Đọc hiểu","Điền câu vào đoạn văn","Sắp xếp/hoàn thành đoạn văn"],"weeks":[{"title":"Tuần 1: Đọc hiểu cơ bản","goal":"Tìm thông tin trực tiếp trong bài.","tasks":["Gạch keyword câu hỏi","Tìm bằng chứng trong đoạn","Làm 10 câu đọc hiểu"],"practiceType":"Đọc hiểu"},{"title":"Tuần 2: Điền câu vào đoạn","goal":"Chọn câu theo mạch trước sau.","tasks":["Tìm đại từ và từ nối","Xác định vai trò chỗ trống","Làm 10 câu điền câu"],"practiceType":"Điền câu vào đoạn văn"},{"title":"Tuần 3: Hoàn thành đoạn văn","goal":"Sắp xếp ý logic hơn.","tasks":["Nhận diện câu chủ đề","Xác định trình tự ý","Luyện hoàn thành đoạn văn"],"practiceType":"Sắp xếp/hoàn thành đoạn văn"}],"dailyTime":"50 phút/ngày","nextAction":"Luyện Đọc hiểu"}
        """
    ];

    public static GeneratedRoadmap GetRandomRoadmap()
    {
        var template = RoadmapJsonTemplates[NextIndex()];
        var roadmap = JsonSerializer.Deserialize<GeneratedRoadmap>(template, JsonOptions);

        if (roadmap == null)
            throw new InvalidOperationException("Fallback roadmap JSON không hợp lệ.");

        NormalizePracticeTypes(roadmap);
        return roadmap;
    }

    private static int NextIndex()
    {
        lock (SyncRoot)
        {
            if (_remainingIndexes.Count == 0)
                _remainingIndexes = CreateShuffledIndexes();

            return _remainingIndexes.Dequeue();
        }
    }

    private static Queue<int> CreateShuffledIndexes()
    {
        var indexes = Enumerable.Range(0, RoadmapJsonTemplates.Count).ToList();

        for (var i = indexes.Count - 1; i > 0; i--)
        {
            var j = Random.Shared.Next(i + 1);
            (indexes[i], indexes[j]) = (indexes[j], indexes[i]);
        }

        return new Queue<int>(indexes);
    }

    private static void NormalizePracticeTypes(GeneratedRoadmap roadmap)
    {
        var usedPracticeTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        roadmap.Strengths = roadmap.Strengths.Where(item => !string.IsNullOrWhiteSpace(item)).ToList();
        roadmap.Weaknesses = roadmap.Weaknesses.Where(item => !string.IsNullOrWhiteSpace(item)).ToList();
        roadmap.Weeks = roadmap.Weeks
            .Where(week =>
                !string.IsNullOrWhiteSpace(week.Title) &&
                !string.IsNullOrWhiteSpace(week.Goal) &&
                week.Tasks.Count > 0)
            .Select(week =>
            {
                var practiceType = EnsureUniquePracticeType(QuestionTypeConstants.Normalize(week.PracticeType), usedPracticeTypes);

                return new StudyRoadmapWeekResult
                {
                    Title = week.Title,
                    Goal = week.Goal,
                    Tasks = week.Tasks.Where(item => !string.IsNullOrWhiteSpace(item)).ToList(),
                    PracticeType = practiceType
                };
            })
            .ToList();
    }

    private static string EnsureUniquePracticeType(string preferredType, HashSet<string> usedPracticeTypes)
    {
        if (IsUsablePracticeType(preferredType) && usedPracticeTypes.Add(preferredType))
            return preferredType;

        foreach (var fallbackType in DefaultPracticeTypeOrder)
        {
            if (usedPracticeTypes.Add(fallbackType))
                return fallbackType;
        }

        return QuestionTypeConstants.MultipleChoice;
    }

    private static bool IsUsablePracticeType(string? type)
    {
        return !string.IsNullOrWhiteSpace(type) &&
            !string.Equals(type, QuestionTypeConstants.Other, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(type, QuestionTypeConstants.Mixed, StringComparison.OrdinalIgnoreCase);
    }
}
