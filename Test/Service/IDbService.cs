using Test.Model;

namespace Test.Service;

public interface IDbService
{
    Task<VisitDTO> getVisit(int id);
    Task<bool> postVisit(InsertVisitDTO visit);
}