using HealthIntelligence.Dtos.blogDto;
using HealthIntelligence.Common.Models;
using HealthIntelligence.Dtos.CommonDto;

namespace HealthIntelligence.IServices
{
    public interface IBlogService
    {
        Task<PaginationResponse<BlogResponseDto>> GetBlogs(PaginationRequest request);
        Task<ResultResponseDto<BlogResponseDto>> GetBlogByIdAsync(int id);
        Task<ResultResponseDto<bool>> AddUpdateBlog(AddUpdateBlogDto blog);
        Task<ResultResponseDto<bool>> DeleteBlog(int blogID);
        Task<PaginationResponse<BlogResponseDto>> GetPublicUsersBlogs(PaginationRequest request);
    }
    
}
