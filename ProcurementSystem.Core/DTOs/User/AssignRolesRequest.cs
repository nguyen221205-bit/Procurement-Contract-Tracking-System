using System.ComponentModel.DataAnnotations;

namespace ProcurementSystem.Core.DTOs.User
{
    public class AssignRolesRequest
    {
        [Required(ErrorMessage = "Danh sách vai trò không được để trống.")]
        [MinLength(1, ErrorMessage = "Người dùng phải có ít nhất một vai trò.")]
        public List<string> Roles { get; set; } = new();
    }
}
