using Test.Model;

namespace Test.Service;

public interface IDbService
{
    Task<VisitDTO> GetVisit(int id);
    Task<bool> PostVisit(InsertVisitDTO visit);
}