using System.ComponentModel.DataAnnotations;
using ProcurementSystem.Core.Enums;

namespace ProcurementSystem.Core.DTOs.ProcuringEntity
{
    public class UpdateMemberRoleRequest
    {
        [Required(ErrorMessage = "Vui lòng chọn vai trò thành viên.")]
        public ProcuringEntityMemberRole RoleInEntity { get; set; }

        public MemberStatus? Status { get; set; }
    }
}
