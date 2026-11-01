using System.ComponentModel.DataAnnotations;
using ProcurementSystem.Core.Enums;

namespace ProcurementSystem.Core.DTOs.ProcuringEntity
{
    public class SendInvitationRequest
    {
        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [MaxLength(150, ErrorMessage = "Email tối đa 150 ký tự.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn vai trò thành viên.")]
        public ProcuringEntityMemberRole RoleInEntity { get; set; } = ProcuringEntityMemberRole.Officer;
    }
}
