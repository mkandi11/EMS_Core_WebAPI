using System.ComponentModel.DataAnnotations;

namespace EMS_Core_WebAPI.Models
{
    public class Employee
    {
        public int EmployeeID { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Company { get; set; }
        public string? Department { get; set; }
        public int ExperienceInMonths { get; set; }
        public bool? IsActive { get; set; }
    }

    public class PersonalDetails
    {
        public int EmployeeID { get; set; }
        public int Age { get; set; }
        public string? Gender { get; set; }
        public Address? Address { get; set; }
    }

    public class Department
    {
        public int DepartmentID { get; set; }
        public string? DepartmentName { get; set; }
    }

    public class Address
    {
        public string? City { get; set; }
        public string? Country { get; set; }
    }

    public class Salary
    {
        public decimal? Basic { get; set; }
        public decimal? HRA { get; set; }
        public decimal? Misc { get; set; }
    }

    public class EmployeeSkills
    {
        public int EmployeeID { get; set; }
        public string[]? Skills { get; set; }
    }

    public class EmployeeCompleteDetails
    {
        public int EmployeeID { get; set; }

        [Required, MaxLength(100)]
        public string FirstName { get; set; } = null!;

        [MaxLength(100)]
        public string? LastName { get; set; }

        [MaxLength(150)]
        public string? Email { get; set; }

        [MaxLength(100)]
        public string? Company { get; set; }

        [Required]
        public int DepartmentID { get; set; }

        public string? DepartmentName { get; set; }

        public int? ExperienceInMonths { get; set; }

        public Salary? Salary { get; set; }

        // Skills
        public string[]? Skills { get; set; }

        // Personal Details
        public int? Age { get; set; }

        [MaxLength(20)]
        public string? Gender { get; set; }
        public Address? Address { get; set; }

    }
}
