namespace Test.Model;

public class InsertVisitDTO
{
    public int VisitId { get; set; }
    public int ClientId { get; set; }
    public string MechanicLicenseNumber { get; set; }
    public List<ServiceDTO> Services { get; set; }
}