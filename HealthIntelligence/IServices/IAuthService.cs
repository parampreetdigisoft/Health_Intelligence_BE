using HealthIntelligence.Common.Models;
using HealthIntelligence.Dtos.CountryDto;
using HealthIntelligence.Dtos.UserDtos;
using HealthIntelligence.Models;

namespace HealthIntelligence.IServices
{
    public interface IAuthService
    {
        User Register(string fullName, string email, string phn, string password, UserRole role);
        User? GetByEmail(string email);
        Task<User?> GetByEmailAysync(string email);
        bool VerifyPassword(string password, string hash);
        Task<ResultResponseDto<UserResponseDto>> Login(string email, string password);
        Task<ResultResponseDto<object>> ForgotPassword(string email);
        Task<ResultResponseDto<object>> ChangePassword(string passwordToken, string password);
        Task<ResultResponseDto<object>> InviteUser(InviteUserDto inviteUser);
        Task<ResultResponseDto<object>> InviteBulkUser(InviteBulkUserDto inviteUser);
        Task<ResultResponseDto<object>> UpdateInviteUser(UpdateInviteUserDto inviteUser);
        Task<ResultResponseDto<object>> DeleteUser(int userId);
        Task<ResultResponseDto<UserResponseDto>> RefreshToken(int userId);
        Task<ResultResponseDto<string>> SendMailForEditAssessment(SendRequestMailToUpdateCountry request);
        Task<ResultResponseDto<UserResponseDto>> CountryUserSignUp(CountryUserSignUpDto request);
        Task<ResultResponseDto<object>> ConfirmMail(string passwordToken);
        Task<ResultResponseDto<object>> ContactUs(ContactUsRequestDto passwordToken);
        Task<ResultResponseDto<UserResponseDto>> TwofaVerification(string email, int otp);
        Task<ResultResponseDto<string>> ReSendLoginOtp(string email);
        Task<ResultResponseDto<UpdateUserResponseDto>> UpdateUser(UpdateUserDto requestDto);
    }
}
