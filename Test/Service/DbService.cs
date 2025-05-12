using System.Data;
using Microsoft.Data.SqlClient;
using Test.Model;

namespace Test.Service;

public class DbService : IDbService
{
    private readonly IConfiguration _configuration;
    private readonly string _connectionString;
    
    public DbService(IConfiguration configuration)
    {
        _configuration = configuration;
        _connectionString = _configuration.GetConnectionString("Default")!;
    }

    private async Task<List<ServiceDTO>> getServices(int id)
    {
        var command =
            """
            SELECT VS.service_fee as Fee, S.name as SName FROM Visit 
            JOIN dbo.Visit_Service VS on Visit.visit_id = VS.visit_id 
            JOIN dbo.Service S on S.service_id = VS.service_id
            WHERE Visit.visit_id = @Id
            """;
        
        await using var connection = new SqlConnection(_connectionString);
        await using var cmd = new SqlCommand(command, connection);

        await connection.OpenAsync();

        cmd.Parameters.AddWithValue("@Id", id);
        List<ServiceDTO> services = [];
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            services.Add(new ServiceDTO()
            {
                Name = reader.GetString(1),
                ServiceFee = reader.GetDecimal(0)
            });
        }

        return services;
        
    }
    public async Task<VisitDTO> getVisit(int id)
    {
        var command = """
                            SELECT Visit.date as VDate, C.first_name AS CFName, C.last_name As CLName, C.date_of_birth As CDB,
                                   M.mechanic_id as MId, M.licence_number AS MLNum
                                FROM Visit 
                                JOIN dbo.Client C on Visit.client_id = C.client_id 
                                JOIN dbo.Mechanic M on M.mechanic_id = Visit.mechanic_id
                                WHERE Visit.visit_id = @Id
                      """;
        
        
        await using var connection = new SqlConnection(_connectionString);
        await using var cmd = new SqlCommand(command, connection);

        await connection.OpenAsync();

        cmd.Parameters.AddWithValue("@Id", id);

        await using var result = await cmd.ExecuteReaderAsync();
        try
        {
            VisitDTO visit = null;
            while (await result.ReadAsync())
            {
                visit = new VisitDTO()
                {
                    client = new ClientDTO()
                    {
                        DateOfBirth = result.GetDateTime(3), FirstName = result.GetString(1),
                        LastName = result.GetString(2)
                    },
                    date = result.GetDateTime(0),
                    mechanic = new MechanicDTO() { Id = result.GetInt32(4), LicenseNumber = result.GetString(5) }
                };
                break;
            }

            if (visit == null) throw new Exception("Visit is not found");
            var services = await getServices(id);

            visit.services = services;
            return visit;
        }
        catch (Exception e)
        {
            throw;
        }

    }

    public async Task<int> getMechanicId(string license)
    { 
        await using var connection = new SqlConnection(_connectionString);
        await using var cmd = new SqlCommand("SELECT mechanic_id from Mechanic WHERE licence_number = @license", connection);

        await connection.OpenAsync();

        cmd.Parameters.AddWithValue("@license", license);

        var result= await cmd.ExecuteScalarAsync();
        if (result == null) return -1;
        return (int)result;
    }
    
    public async Task<bool> DoesVisitorExists(int id)
    { 
        await using var connection = new SqlConnection(_connectionString);
        await using var cmd = new SqlCommand("SELECT 1 from Client WHERE client_id = @id", connection);

        await connection.OpenAsync();

        cmd.Parameters.AddWithValue("@id", id);

        var result= await cmd.ExecuteScalarAsync();
        return result != null;
    }

    public async Task<int> GetService(string name)
    {
        await using var connection = new SqlConnection(_connectionString);
        await using var cmd = new SqlCommand("SELECT service_id from Service WHERE name = @name", connection);

        await connection.OpenAsync();

        cmd.Parameters.AddWithValue("@name", name);

        var result= await cmd.ExecuteScalarAsync();
        if (result == null) return -1;
        return (int)result;
    }
    
    

    public async Task<bool> postVisit(InsertVisitDTO visit)
    {
        var mechanicId = await getMechanicId(visit.MechanicLicenseNumber);
        if (mechanicId == -1) throw new Exception("Mechanic is not found");
        var client = await DoesVisitorExists(visit.ClientId);
        if (!client) throw new Exception("Client does not exist");
        List<int> ids = [];
        foreach (ServiceDTO s in visit.Services)
        {
            var sid = await GetService(s.Name);
            if (sid == -1) throw new Exception("Service is not found");
            ids.Add(sid);
        }
        try
        {
            var v = await getVisit(visit.VisitId);
            throw new Exception("Visit does exist");
        }
        catch (Exception e)
        {
            
            await using (var connection = new SqlConnection(_connectionString))
            await using (var cmd = new SqlCommand("INSERT INTO Visit VALUES (@visitId, @clientId, @mechanic, @date)", connection))
            {
                await connection.OpenAsync();
                cmd.Parameters.AddWithValue("@visitId", visit.VisitId);
                cmd.Parameters.AddWithValue("@clientId", visit.ClientId);
                cmd.Parameters.AddWithValue("@mechanic", mechanicId);
                cmd.Parameters.AddWithValue("@date", DateTime.Now);
                await cmd.ExecuteNonQueryAsync();
            }
            
            await using (var connection = new SqlConnection(_connectionString))
            {
                for (int i = 0; i < ids.Count; i++)
                {
                    await using (var cmd = new SqlCommand("INSERT INTO Visit_Service VALUES (@visitId, @serviceId, @fee)", connection))
                    {
                        cmd.Parameters.AddWithValue("@visitId", visit.VisitId);
                        cmd.Parameters.AddWithValue("@serviceId", ids[i]);
                        cmd.Parameters.AddWithValue("@fee", visit.Services[i].ServiceFee);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            return true;

        }


    }

}